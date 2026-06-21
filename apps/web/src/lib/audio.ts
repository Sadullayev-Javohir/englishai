/**
 * Encodes raw recorded audio bytes as a base64 string for the JSON API.
 *
 * The backend commands carry the clip as a `byte[]` (e.g. SubmitSpeakingCommand,
 * SubmitUtteranceCommand). System.Text.Json maps a JSON `byte[]` to/from a base64
 * string - sending a plain number array instead fails model binding (HTTP 400).
 * Conversion is chunked so a long clip never overflows the call stack via spread.
 */
export function bytesToBase64(bytes: Uint8Array): string {
  const chunkSize = 0x8000; // 32 KB per chunk keeps String.fromCharCode within limits
  let binary = "";
  for (let i = 0; i < bytes.length; i += chunkSize) {
    binary += String.fromCharCode(...bytes.subarray(i, i + chunkSize));
  }
  return btoa(binary);
}

/** Sample rate the backend (Azure push-stream default) expects: 16 kHz mono 16-bit PCM. */
const TARGET_SAMPLE_RATE = 16000;

/**
 * Decodes a recorded blob into an AudioBuffer. Split out from {@link blobToWav16kMono} so the
 * shadowing mode can decode one continuous recording ONCE and then slice it into many per-segment
 * clips (see {@link sliceToWav16k}) instead of re-decoding the whole clip per segment.
 */
export async function decodeToBuffer(blob: Blob): Promise<AudioBuffer> {
  const arrayBuffer = await blob.arrayBuffer();

  // A fresh AudioContext just to decode; closed right after to free the device.
  const AudioCtx: typeof AudioContext =
    window.AudioContext ?? (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
  const decodeCtx = new AudioCtx();
  try {
    return await decodeCtx.decodeAudioData(arrayBuffer);
  } finally {
    void decodeCtx.close();
  }
}

/** Resamples + downmixes an already-decoded buffer to mono 16 kHz PCM WAV bytes. */
async function renderToWav16k(decoded: AudioBuffer): Promise<Uint8Array> {
  const frameCount = Math.ceil((decoded.duration || 0) * TARGET_SAMPLE_RATE);
  const offline = new OfflineAudioContext(1, Math.max(1, frameCount), TARGET_SAMPLE_RATE);
  const source = offline.createBufferSource();
  source.buffer = decoded;
  source.connect(offline.destination);
  source.start();
  const rendered = await offline.startRendering();
  return encodeWav16(rendered.getChannelData(0));
}

/**
 * Converts a recorded audio blob into a 16 kHz / 16-bit / mono PCM WAV.
 *
 * `MediaRecorder` produces a compressed container (WebM/Opus in Chrome, OGG/MP4
 * elsewhere) - NOT the WAV the backend's `WavAudio.ExtractPcm` and the Azure
 * push-stream expect. Sending the raw recording makes Azure return NoMatch, which
 * the server reports as an unrecognized turn. We decode the clip with the Web Audio
 * API, downmix to mono, resample to 16 kHz, and emit a RIFF/WAVE PCM container so
 * the samples line up with what the recognizer reads.
 */
export async function blobToWav16kMono(blob: Blob): Promise<Uint8Array> {
  return renderToWav16k(await decodeToBuffer(blob));
}

/**
 * Cuts an already-decoded buffer down to [startSec, endSec) and renders just that range to 16 kHz
 * mono WAV bytes - used by the shadowing mode to slice one continuous recording into per-transcript-
 * segment clips using the segment's own timestamps, without re-decoding the source blob each time.
 */
export async function sliceToWav16k(
  buffer: AudioBuffer,
  startSec: number,
  endSec: number,
): Promise<Uint8Array> {
  const sampleRate = buffer.sampleRate;
  const startFrame = Math.max(0, Math.floor(startSec * sampleRate));
  const endFrame = Math.min(buffer.length, Math.ceil(endSec * sampleRate));
  const frameCount = Math.max(1, endFrame - startFrame);

  const slice = new AudioBuffer({
    length: frameCount,
    numberOfChannels: buffer.numberOfChannels,
    sampleRate,
  });
  for (let channel = 0; channel < buffer.numberOfChannels; channel++) {
    const channelData = buffer.getChannelData(channel).subarray(startFrame, startFrame + frameCount);
    slice.copyToChannel(channelData, channel);
  }

  return renderToWav16k(slice);
}

/**
 * Speaks a single English word using the browser's built-in speech synthesis.
 *
 * Used for the "native pronunciation" button on the pronunciation detail screen.
 * The Web Speech API works fully offline, needs no Azure key, and costs nothing
 * (docs/development-guide.md rule 10), so it's the right fit for a one-word reference playback.
 * Best-effort: silently no-ops where the API is unavailable. Returns true when
 * playback was started so callers can sync a viseme animation to it.
 *
 * `rate` controls how fast the word is spoken (default 0.75 = slow and clear). Pass a
 * lower value (e.g. 0.45) for the "speak slowly" mode so learners hear each sound; the
 * caller should slow the viseme track by the same factor to keep mouth and audio aligned.
 */
export function speakEnglishWord(word: string, options?: { rate?: number }): boolean {
  const synth = typeof window !== "undefined" ? window.speechSynthesis : undefined;
  const text = word.trim();
  if (!synth || !text) return false;

  // Stop anything in flight so repeated taps restart cleanly.
  synth.cancel();

  const start = () => {
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = "en-US";
    utterance.rate = options?.rate ?? 0.75; // default: slow and clear
    const enVoice = synth.getVoices().find((v) => v.lang.toLowerCase().startsWith("en"));
    if (enVoice) utterance.voice = enVoice;
    synth.speak(utterance);
    // Chrome can leave the engine paused after a cancel/idle; resume() un-sticks it so
    // the utterance actually plays (the "no sound on Asl talaffuz" bug).
    synth.resume();
  };

  // Voices load asynchronously: on the first call after a page load getVoices() is often
  // empty, and an utterance queued then is silently dropped. Wait for the voices to arrive
  // (with a short fallback for browsers that report them late without the event), and defer
  // a tick so cancel() has settled before we speak - calling speak() in the same tick as
  // cancel() is another path that drops the audio.
  if (synth.getVoices().length === 0) {
    let started = false;
    const go = () => {
      if (started) return;
      started = true;
      synth.removeEventListener("voiceschanged", go);
      start();
    };
    synth.addEventListener("voiceschanged", go);
    window.setTimeout(go, 250);
  } else {
    window.setTimeout(start, 0);
  }
  return true;
}

/**
 * Plays a base64-encoded WAV reference clip (the Azure word voice from the backend).
 *
 * Preferred over {@link speakEnglishWord} because it does not depend on the browser having
 * a TTS engine installed - many Linux/Chromium clients have no speech-synthesis voices, so
 * `speakEnglishWord` stays silent there (the "no sound on Asl talaffuz / Sekin" bug). The
 * caller passes the same audio element across taps (via a ref) so repeated taps restart
 * cleanly. `rate` slows playback for the "Sekin" mode; it is clamped to a floor because
 * Chromium mutes audio below ~0.5x. Returns true once playback was started.
 */
export function playWordAudioBase64(
  base64: string,
  audio: HTMLAudioElement,
  rate = 1,
): boolean {
  if (!base64) return false;
  try {
    audio.pause();
    audio.src = `data:audio/wav;base64,${base64}`;
    audio.playbackRate = Math.max(0.5, rate); // Chromium mutes audio below ~0.5x
    audio.currentTime = 0;
    void audio.play().catch(() => undefined);
    return true;
  } catch {
    return false; // best-effort: let the caller fall back to the browser voice
  }
}

/**
 * Plays the best available pronunciation for a word: the Azure reference WAV when the backend
 * supplied one, otherwise the browser's built-in speech synthesis.
 *
 * This is the single "Tinglash" entry point used across the app. The Azure clip is preferred
 * because many Linux/Chromium clients ship no TTS voices, so {@link speakEnglishWord} stays
 * silent there (the "Tinglash chiqmayapti" bug). `audio` is a caller-owned <audio> element
 * (via a ref) reused across taps so playback restarts cleanly. Returns true once something
 * was started.
 */
export function playWordVoice(
  audioBase64: string | null | undefined,
  word: string,
  audio: HTMLAudioElement | null,
  rate = 1,
): boolean {
  if (audioBase64 && audio) {
    try {
      const normalizedBase64 = audioBase64.includes(",") ? audioBase64.split(",").pop()! : audioBase64;
      audio.pause();
      audio.src = `data:audio/wav;base64,${normalizedBase64}`;
      audio.playbackRate = Math.max(0.5, rate); // Chromium mutes audio below ~0.5x
      audio.currentTime = 0;
      // If the clip can't actually be decoded/played (corrupt or non-WAV bytes, or a
      // transient device error), fall back to the browser voice instead of staying silent -
      // play() returning a started promise is NOT proof the audio was audible. Guard with a
      // flag so a single tap never double-speaks.
      let fellBack = false;
      const fallback = () => {
        if (fellBack) return;
        fellBack = true;
        speakEnglishWord(word, { rate });
      };
      audio.onerror = fallback;
      void audio.play().catch(fallback);
      return true;
    } catch {
      // fall through to the browser voice below
    }
  }
  return speakEnglishWord(word, { rate });
}

let feedbackAudioContext: AudioContext | null = null;

type WebkitAudioWindow = Window & {
  webkitAudioContext?: typeof AudioContext;
};

function getFeedbackAudioContext(): AudioContext | null {
  if (typeof window === "undefined") return null;

  const AudioCtx = window.AudioContext ?? (window as WebkitAudioWindow).webkitAudioContext;
  if (!AudioCtx) return null;

  // A device change or browser lifecycle event can close a context. Recreate it on the next
  // interaction rather than leaving every subsequent lesson sound permanently silent.
  if (feedbackAudioContext?.state === "closed") feedbackAudioContext = null;
  if (!feedbackAudioContext) {
    try {
      feedbackAudioContext = new AudioCtx();
    } catch {
      return null;
    }
  }
  return feedbackAudioContext;
}

async function resumeFeedbackAudio(): Promise<AudioContext | null> {
  const context = getFeedbackAudioContext();
  if (!context) return null;
  if (context.state === "suspended") {
    try {
      await context.resume();
    } catch {
      return null;
    }
  }
  return context.state === "running" ? context : null;
}

export function unlockFeedbackAudio(): void {
  void resumeFeedbackAudio();
}

/**
 * Unlocks the shared Web Audio context from a pointer/keyboard interaction.
 * Returns false instead of throwing when Web Audio is unavailable or blocked.
 */
export async function unlockLessonAudio(): Promise<boolean> {
  return (await resumeFeedbackAudio()) !== null;
}

export type LessonSound = "slide" | "correct" | "incorrect" | "complete";
export type StopLessonSound = () => void;

type LessonNote = {
  frequency: number;
  offset: number;
  duration: number;
  volume?: number;
};

const LESSON_NOTES: Record<LessonSound, readonly LessonNote[]> = {
  slide: [
    { frequency: 659.25, offset: 0, duration: 0.09, volume: 0.08 },
    { frequency: 880, offset: 0.07, duration: 0.12, volume: 0.1 },
  ],
  correct: [
    { frequency: 523.25, offset: 0, duration: 0.16 },
    { frequency: 659.25, offset: 0.1, duration: 0.18 },
    { frequency: 783.99, offset: 0.2, duration: 0.22 },
  ],
  incorrect: [
    { frequency: 220, offset: 0, duration: 0.18, volume: 0.13 },
    { frequency: 174.61, offset: 0.14, duration: 0.24, volume: 0.13 },
  ],
  complete: [
    { frequency: 523.25, offset: 0, duration: 0.2, volume: 0.14 },
    { frequency: 659.25, offset: 0.11, duration: 0.22, volume: 0.15 },
    { frequency: 783.99, offset: 0.22, duration: 0.24, volume: 0.16 },
    { frequency: 1046.5, offset: 0.36, duration: 0.34, volume: 0.17 },
  ],
};

const LESSON_WAVE: Record<LessonSound, OscillatorType> = {
  slide: "sine",
  correct: "sine",
  incorrect: "triangle",
  complete: "sine",
};

/**
 * Synthesizes a short lesson UI sound. The returned function stops and disconnects every node,
 * allowing hooks to clean up scheduled notes when their lesson unmounts. Best-effort and silent
 * when Web Audio is unsupported, still locked, or fails during device setup.
 */
export function playLessonSound(sound: LessonSound): StopLessonSound | null {
  const context = getFeedbackAudioContext();
  if (!context || context.state !== "running") return null;

  const oscillators: OscillatorNode[] = [];
  const gains: GainNode[] = [];
  const startAt = context.currentTime + 0.015;
  let stopped = false;

  try {
    for (const note of LESSON_NOTES[sound]) {
      const oscillator = context.createOscillator();
      const gain = context.createGain();
      const noteStart = startAt + note.offset;
      const noteEnd = noteStart + note.duration;
      oscillators.push(oscillator);
      gains.push(gain);

      oscillator.type = LESSON_WAVE[sound];
      oscillator.frequency.setValueAtTime(note.frequency, noteStart);
      gain.gain.setValueAtTime(0.0001, noteStart);
      gain.gain.exponentialRampToValueAtTime(note.volume ?? 0.16, noteStart + 0.012);
      gain.gain.exponentialRampToValueAtTime(0.0001, noteEnd);
      oscillator.connect(gain).connect(context.destination);
      oscillator.addEventListener("ended", () => {
        oscillator.disconnect();
        gain.disconnect();
      }, { once: true });
      oscillator.start(noteStart);
      oscillator.stop(noteEnd + 0.01);
    }
  } catch {
    for (const oscillator of oscillators) {
      try { oscillator.stop(); } catch { /* The node may already be stopped. */ }
      oscillator.disconnect();
    }
    for (const gain of gains) gain.disconnect();
    return null;
  }

  return () => {
    if (stopped) return;
    stopped = true;
    for (const oscillator of oscillators) {
      try { oscillator.stop(); } catch { /* The node may already be stopped. */ }
      oscillator.disconnect();
    }
    for (const gain of gains) gain.disconnect();
  };
}

export function playPronunciationFeedback(success: boolean): void {
  const context = getFeedbackAudioContext();
  if (!context) return;
  if (context.state === "suspended") void context.resume().catch(() => undefined);

  const startAt = context.currentTime + 0.02;
  const notes = success
    ? [{ frequency: 523.25, offset: 0 }, { frequency: 659.25, offset: 0.12 }, { frequency: 783.99, offset: 0.24 }]
    : [{ frequency: 220, offset: 0 }, { frequency: 174.61, offset: 0.16 }];

  notes.forEach(({ frequency, offset }) => {
    const oscillator = context.createOscillator();
    const gain = context.createGain();
    oscillator.type = success ? "sine" : "triangle";
    oscillator.frequency.value = frequency;
    gain.gain.setValueAtTime(0.0001, startAt + offset);
    gain.gain.exponentialRampToValueAtTime(0.18, startAt + offset + 0.015);
    gain.gain.exponentialRampToValueAtTime(0.0001, startAt + offset + 0.18);
    oscillator.connect(gain).connect(context.destination);
    oscillator.start(startAt + offset);
    oscillator.stop(startAt + offset + 0.2);
  });
}

/**
 * Plays back a locally recorded {@link Blob} (the learner's own pronunciation/shadowing attempt)
 * through a caller-owned `<audio>` element, so the learner can hear themselves - unlike the other
 * playback helpers above, which play reference clips from the backend, not the learner's own voice.
 * Revokes the previous object URL (if any) to avoid leaking one per re-record/replay; returns the
 * new URL so the caller can revoke it too (on unmount, or the next call).
 */
export function playRecordedBlob(
  blob: Blob,
  audio: HTMLAudioElement,
  previousUrl?: string | null,
): string {
  const url = URL.createObjectURL(blob);
  audio.pause();
  audio.removeAttribute("src");
  audio.load();
  if (previousUrl) URL.revokeObjectURL(previousUrl);
  audio.src = url;
  audio.load();
  audio.currentTime = 0;
  void audio.play().catch(() => undefined);
  return url;
}

/** Encodes mono Float32 samples (already at 16 kHz) as a 16-bit PCM WAV byte array. */
export function encodeWav16(samples: Float32Array): Uint8Array {
  const bytesPerSample = 2;
  const blockAlign = bytesPerSample; // mono
  const byteRate = TARGET_SAMPLE_RATE * blockAlign;
  const dataSize = samples.length * bytesPerSample;
  const buffer = new ArrayBuffer(44 + dataSize);
  const view = new DataView(buffer);

  const writeString = (offset: number, text: string) => {
    for (let i = 0; i < text.length; i++) view.setUint8(offset + i, text.charCodeAt(i));
  };

  writeString(0, "RIFF");
  view.setUint32(4, 36 + dataSize, true);
  writeString(8, "WAVE");
  writeString(12, "fmt ");
  view.setUint32(16, 16, true); // PCM fmt chunk size
  view.setUint16(20, 1, true); // audio format = PCM
  view.setUint16(22, 1, true); // channels = mono
  view.setUint32(24, TARGET_SAMPLE_RATE, true);
  view.setUint32(28, byteRate, true);
  view.setUint16(32, blockAlign, true);
  view.setUint16(34, 16, true); // bits per sample
  writeString(36, "data");
  view.setUint32(40, dataSize, true);

  let offset = 44;
  for (let i = 0; i < samples.length; i++) {
    const clamped = Math.max(-1, Math.min(1, samples[i]));
    view.setInt16(offset, clamped < 0 ? clamped * 0x8000 : clamped * 0x7fff, true);
    offset += bytesPerSample;
  }

  return new Uint8Array(buffer);
}
