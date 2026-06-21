// Direct browser <-> Azure Voice Live realtime session for one accent tutor.
//
// The backend mints a short-lived token (GET /api/speaking/accent-tutors/{id}/voice-live/token);
// the browser opens the realtime WebSocket straight to Azure (no relay), streams microphone PCM,
// and plays the tutor's streamed audio back. Azure server VAD detects end-of-speech, so there is
// no manual turn/commit machinery. Barge-in (interrupting the tutor) is handled by stopping
// playback the moment the learner starts speaking again.

import { apiUrl } from "./config";
import { getAuthToken, isNativePlatform } from "./nativeAuth";

export type VoiceLiveStatus =
  | "connecting"
  | "listening" // mic open, waiting for the learner to speak
  | "user_speaking" // Azure VAD detected the learner talking
  | "thinking" // learner stopped, agent is generating
  | "tutor_speaking" // tutor audio is playing
  | "error";

export interface VoiceLiveCallbacks {
  onStatus?(status: VoiceLiveStatus): void;
  onUserTranscript?(text: string, final: boolean): void;
  /** Final tutor turn text, for the conversation feed (called once per turn). */
  onTutorTranscript?(text: string, final: boolean): void;
  /** Live tutor caption synced to audio playback: the full words + how many are spoken so far. */
  onTutorCaption?(words: string[], spokenCount: number): void;
  onError?(message: string, fatal: boolean): void;
  /** Fired when the session auto-closes after prolonged silence (cost guard). */
  onIdleTimeout?(): void;
}

/** Thrown when the backend refuses to mint a token, carrying the machine-readable reason. */
export class VoiceLiveTokenError extends Error {
  constructor(
    readonly code: string,
    readonly status: number,
  ) {
    super(`Voice Live token request failed: ${status} (${code})`);
    this.name = "VoiceLiveTokenError";
  }
}

interface VoiceLiveTokenResponse {
  webSocketUrl: string;
  authorizationQueryParameter: string;
  authorizationValue: string;
  expiresAt: string;
  /** Correlates the completion report below with the server-side reservation. */
  sessionId: string;
  /** Ceiling the server will bill regardless of what this client reports. */
  maxSessionSeconds: number;
  session: {
    voiceName: string;
    inputAudioFormat: string;
    outputAudioFormat: string;
    inputSamplingRate: number;
    silenceDurationMs: number;
    turnDetectionType: string;
  };
}

const SAMPLE_RATE = 24_000;
// Cost guard: Azure bills audio input (~10 tokens/s) for as long as we stream, even during
// silence. Auto-close the session after this much time with no detected speech so a forgotten
// open tab never streams (and bills) indefinitely.
const IDLE_TIMEOUT_MS = 45_000;

export class VoiceLiveSession {
  private ws: WebSocket | null = null;
  private stream: MediaStream | null = null;
  private audioContext: AudioContext | null = null;
  private micSource: MediaStreamAudioSourceNode | null = null;
  private processor: ScriptProcessorNode | null = null;
  private playbackTime = 0; // next scheduled playback start (audioContext time)
  private playing = new Set<AudioBufferSourceNode>();
  private muted = false;
  private stopped = false;
  private tutorTranscript = "";
  private tutorTextDone = false;
  private speakingStartTime: number | null = null; // audioContext time the tutor turn started playing
  private totalAudioDuration = 0; // seconds of tutor audio buffered this turn
  private captionTimer: ReturnType<typeof setInterval> | null = null;
  private idleTimer: ReturnType<typeof setTimeout> | null = null;
  private sessionId: string | null = null;
  private startedAtMs = 0;
  private completionSent = false;
  private pageHideHandler: (() => void) | null = null;

  constructor(
    private readonly tutorId: string,
    private readonly callbacks: VoiceLiveCallbacks,
  ) {}

  async start(): Promise<void> {
    this.stopped = false;
    this.status("connecting");

    const token = await this.fetchToken();
    if (this.stopped) return;

    // From here on the server is holding a reservation for us, so every exit path has to report
    // back. teardown() covers the in-app paths; pagehide covers tab close / app backgrounding,
    // which is the only exit React never sees.
    this.sessionId = token.sessionId;
    this.startedAtMs = Date.now();
    this.completionSent = false;
    this.pageHideHandler = () => this.reportCompletion();
    window.addEventListener("pagehide", this.pageHideHandler);

    // One 24 kHz context for both capture and playback keeps everything in the same clock.
    this.audioContext = new AudioContext({ sampleRate: SAMPLE_RATE });
    await this.audioContext.resume();

    this.stream = await navigator.mediaDevices.getUserMedia({
      audio: { channelCount: 1, echoCancellation: true, noiseSuppression: true, autoGainControl: true },
    });
    if (this.stopped) {
      this.teardown();
      return;
    }

    const url = `${token.webSocketUrl}&${token.authorizationQueryParameter}=${encodeURIComponent(token.authorizationValue)}`;
    this.ws = new WebSocket(url);
    this.ws.onopen = () => this.startCapture();
    this.ws.onmessage = (event) => this.onServerEvent(event.data as string, token);
    this.ws.onerror = () => this.callbacks.onError?.("Voice connection error.", false);
    this.ws.onclose = () => {
      if (!this.stopped) this.callbacks.onError?.("Voice connection closed.", true);
    };
  }

  setMuted(muted: boolean): void {
    this.muted = muted;
    if (this.stream) for (const track of this.stream.getAudioTracks()) track.enabled = !muted;
  }

  stop(): void {
    this.stopped = true;
    this.teardown();
  }

  // --- token ---

  private async fetchToken(): Promise<VoiceLiveTokenResponse> {
    const response = await fetch(
      apiUrl(`/api/speaking/accent-tutors/${encodeURIComponent(this.tutorId)}/voice-live/token`),
      { credentials: "include", headers: authHeaders() },
    );
    if (!response.ok) {
      // The server refuses with a machine-readable code (budget_exhausted,
      // voice_live_daily_limit); the page turns that into the right learner-facing message.
      let code = "voice_live_unavailable";
      try {
        const body = (await response.json()) as { code?: string };
        if (body?.code) code = body.code;
      } catch {
        /* non-JSON error body: keep the generic code */
      }
      throw new VoiceLiveTokenError(code, response.status);
    }
    return (await response.json()) as VoiceLiveTokenResponse;
  }

  // Tells the server the session is over so it can replace its up-front reservation with the real
  // duration. Best-effort by design: if this never lands the learner keeps the reservation, which
  // is the safe direction. `keepalive` (not sendBeacon) because the native build is cross-origin
  // and needs both credentials and an Authorization header, neither of which beacon can send.
  private reportCompletion(): void {
    if (this.completionSent || !this.sessionId) return;
    this.completionSent = true;

    const body = JSON.stringify({
      sessionId: this.sessionId,
      durationSeconds: Math.max(0, (Date.now() - this.startedAtMs) / 1000),
    });

    void fetch(
      apiUrl(`/api/speaking/accent-tutors/${encodeURIComponent(this.tutorId)}/voice-live/complete`),
      {
        method: "POST",
        credentials: "include",
        keepalive: true,
        headers: { "Content-Type": "application/json", ...authHeaders() },
        body,
      },
    ).catch(() => undefined);
  }

  // --- capture (mic -> Azure) ---

  private startCapture(): void {
    if (!this.audioContext || !this.stream) return;
    this.micSource = this.audioContext.createMediaStreamSource(this.stream);
    // ScriptProcessor is deprecated but universally available and adequate for 16-bit voice
    // capture; the work per frame (Float32 -> Int16) is trivial.
    this.processor = this.audioContext.createScriptProcessor(4096, 1, 1);
    this.processor.onaudioprocess = (event) => {
      // Skip sending (and being billed for audio input) while muted or when the tab is hidden.
      if (this.stopped || this.muted || document.hidden || this.ws?.readyState !== WebSocket.OPEN) return;
      const input = event.inputBuffer.getChannelData(0);
      this.ws.send(JSON.stringify({ type: "input_audio_buffer.append", audio: floatToPcm16Base64(input) }));
    };
    this.micSource.connect(this.processor);
    this.processor.connect(this.audioContext.destination); // required for the node to run
    this.status("listening");
    this.resetIdleTimer();
  }

  // Cost guard: close the session if no speech is detected for IDLE_TIMEOUT_MS. Reset on any
  // real activity (learner speaking, tutor speaking) so an active conversation is never cut off.
  private resetIdleTimer(): void {
    if (this.idleTimer) clearTimeout(this.idleTimer);
    this.idleTimer = setTimeout(() => {
      if (this.stopped) return;
      this.stop();
      this.callbacks.onIdleTimeout?.();
    }, IDLE_TIMEOUT_MS);
  }

  // --- Azure server events ---

  private onServerEvent(data: string, token: VoiceLiveTokenResponse): void {
    let event: { type: string; [k: string]: unknown };
    try {
      event = JSON.parse(data);
    } catch {
      return;
    }
    switch (event.type) {
      case "session.created":
        this.sendSessionUpdate(token);
        break;
      case "input_audio_buffer.speech_started":
        // Learner (re)started talking: barge-in — cut tutor playback and flush its caption.
        this.resetIdleTimer();
        this.stopPlayback();
        this.finalizeTutorTurn();
        this.status("user_speaking");
        break;
      case "input_audio_buffer.speech_stopped":
        this.status("thinking");
        break;
      case "conversation.item.input_audio_transcription.delta":
        this.callbacks.onUserTranscript?.(String(event.delta ?? ""), false);
        break;
      case "conversation.item.input_audio_transcription.completed":
        this.callbacks.onUserTranscript?.(String(event.transcript ?? ""), true);
        break;
      case "response.audio_transcript.delta":
        // Accumulate; the caption reveals words in sync with audio playback (not on arrival).
        this.tutorTranscript += String(event.delta ?? "");
        break;
      case "response.audio_transcript.done":
        this.tutorTranscript = String(event.transcript ?? this.tutorTranscript);
        this.tutorTextDone = true;
        this.maybeFinalizeTutorTurn(); // finalize now if audio already finished (or was text-only)
        break;
      case "response.audio.delta":
        this.enqueueAudio(String(event.delta ?? ""));
        break;
      case "error":
        this.callbacks.onError?.(String((event.error as { message?: string })?.message ?? "Voice error."), false);
        break;
      default:
        break;
    }
  }

  private sendSessionUpdate(token: VoiceLiveTokenResponse): void {
    const s = token.session;
    this.send({
      type: "session.update",
      session: {
        modalities: ["text", "audio"],
        input_audio_format: s.inputAudioFormat,
        output_audio_format: s.outputAudioFormat,
        input_audio_sampling_rate: s.inputSamplingRate,
        voice: { name: s.voiceName, type: "azure-standard" },
        turn_detection: { type: s.turnDetectionType, silence_duration_ms: s.silenceDurationMs },
      },
    });
  }

  // --- playback (Azure -> speakers) ---

  private enqueueAudio(base64: string): void {
    if (!this.audioContext || this.stopped) return;
    this.resetIdleTimer(); // tutor speaking is activity too
    const pcm = base64ToInt16(base64);
    if (pcm.length === 0) return;
    const buffer = this.audioContext.createBuffer(1, pcm.length, SAMPLE_RATE);
    const channel = buffer.getChannelData(0);
    for (let i = 0; i < pcm.length; i += 1) channel[i] = pcm[i] / 0x8000;

    const source = this.audioContext.createBufferSource();
    source.buffer = buffer;
    source.connect(this.audioContext.destination);
    const now = this.audioContext.currentTime;
    const startAt = Math.max(now, this.playbackTime);
    source.start(startAt);
    this.playbackTime = startAt + buffer.duration;
    if (this.speakingStartTime === null) {
      this.speakingStartTime = startAt;
      this.startCaptionTimer();
    }
    this.totalAudioDuration += buffer.duration;
    this.playing.add(source);
    this.status("tutor_speaking");
    source.onended = () => {
      this.playing.delete(source);
      if (this.playing.size === 0) this.maybeFinalizeTutorTurn();
    };
  }

  // Reveal the tutor caption word-by-word in step with audio playback (karaoke). The page
  // renders words up to `spokenCount` as spoken and the rest as upcoming.
  private startCaptionTimer(): void {
    if (this.captionTimer) return;
    this.captionTimer = setInterval(() => {
      if (this.stopped || !this.audioContext || this.speakingStartTime === null) return;
      const words = this.tutorTranscript.trim().split(/\s+/).filter(Boolean);
      if (words.length === 0) return;
      const fraction = this.totalAudioDuration > 0
        ? Math.min(1, Math.max(0, (this.audioContext.currentTime - this.speakingStartTime) / this.totalAudioDuration))
        : 0;
      const spoken = Math.min(words.length, Math.round(fraction * words.length));
      this.callbacks.onTutorCaption?.(words, spoken);
    }, 80);
  }

  // Finalize once the transcript is complete AND all audio has finished playing.
  private maybeFinalizeTutorTurn(): void {
    if (!this.tutorTextDone || this.playing.size > 0) return;
    this.finalizeTutorTurn();
    if (!this.stopped) this.status("listening");
  }

  // Move the completed tutor text to the feed and clear the live caption. Also used to flush a
  // turn that was cut off by barge-in.
  private finalizeTutorTurn(): void {
    if (this.captionTimer) {
      clearInterval(this.captionTimer);
      this.captionTimer = null;
    }
    const text = this.tutorTranscript.trim();
    if (text) this.callbacks.onTutorTranscript?.(text, true);
    this.callbacks.onTutorCaption?.([], 0);
    this.tutorTranscript = "";
    this.tutorTextDone = false;
    this.speakingStartTime = null;
    this.totalAudioDuration = 0;
  }

  private stopPlayback(): void {
    for (const source of this.playing) {
      try {
        source.stop();
      } catch {
        /* already stopped */
      }
    }
    this.playing.clear();
    this.playbackTime = this.audioContext?.currentTime ?? 0;
  }

  // --- helpers ---

  private send(payload: unknown): void {
    if (this.ws?.readyState === WebSocket.OPEN) this.ws.send(JSON.stringify(payload));
  }

  private status(status: VoiceLiveStatus): void {
    if (!this.stopped) this.callbacks.onStatus?.(status);
  }

  private teardown(): void {
    // First, while sessionId and startedAtMs are still valid: settle the server-side reservation.
    // Every in-app exit (idle timeout, finish button, route change) funnels through here.
    this.reportCompletion();
    if (this.pageHideHandler) {
      window.removeEventListener("pagehide", this.pageHideHandler);
      this.pageHideHandler = null;
    }
    if (this.idleTimer) {
      clearTimeout(this.idleTimer);
      this.idleTimer = null;
    }
    if (this.captionTimer) {
      clearInterval(this.captionTimer);
      this.captionTimer = null;
    }
    this.stopPlayback();
    if (this.processor) {
      this.processor.onaudioprocess = null;
      this.processor.disconnect();
      this.processor = null;
    }
    this.micSource?.disconnect();
    this.micSource = null;
    for (const track of this.stream?.getTracks() ?? []) track.stop();
    this.stream = null;
    if (this.ws) {
      this.ws.onopen = this.ws.onmessage = this.ws.onerror = this.ws.onclose = null;
      try {
        this.ws.close();
      } catch {
        /* ignore */
      }
      this.ws = null;
    }
    void this.audioContext?.close().catch(() => undefined);
    this.audioContext = null;
  }
}

// Native shell authenticates with a Bearer token (the cross-origin cookie is unreliable); the web
// build rides the HttpOnly cookie via credentials:"include" and adds nothing here.
function authHeaders(): Record<string, string> {
  if (!isNativePlatform()) return {};
  const token = getAuthToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

function floatToPcm16Base64(input: Float32Array): string {
  const pcm = new Int16Array(input.length);
  for (let i = 0; i < input.length; i += 1) {
    const s = Math.max(-1, Math.min(1, input[i]));
    pcm[i] = s < 0 ? s * 0x8000 : s * 0x7fff;
  }
  return bytesToBase64(new Uint8Array(pcm.buffer));
}

function base64ToInt16(base64: string): Int16Array {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) bytes[i] = binary.charCodeAt(i);
  return new Int16Array(bytes.buffer, 0, bytes.length >> 1);
}

function bytesToBase64(bytes: Uint8Array): string {
  let binary = "";
  const chunk = 0x8000;
  for (let i = 0; i < bytes.length; i += chunk) {
    binary += String.fromCharCode(...bytes.subarray(i, i + chunk));
  }
  return btoa(binary);
}
