import { MicVAD } from "@ricky0123/vad-web";
import ortWasmModuleUrl from "@/vendor/ort-wasm-simd-threaded.mjs?url";
import ortWasmBinaryUrl from "@/vendor/ort-wasm-simd-threaded.wasm?url";
import { requestSpeakingMicrophone, stopMediaStream } from "./microphoneCapture";

export interface LiveVoiceRecorderOptions {
  maximumTurnMs?: number;
  positiveSpeechThreshold?: number;
  negativeSpeechThreshold?: number;
  redemptionMs?: number;
  preSpeechPadMs?: number;
  minimumSpeechMs?: number;
  onSpeechStarted?: () => void | Promise<void>;
  onSpeechCandidate?: () => void | Promise<void>;
  onSpeechMisfire?: () => void | Promise<void>;
  onAudioChunk?: (audio: Float32Array) => void | Promise<void>;
  onSpeechPaused?: (audio: Float32Array) => void | Promise<void>;
  /** Backward-compatible whole-utterance fallback callback. */
  onSpeechEnded?: (audio: Float32Array) => void | Promise<void>;
  onError?: (error: unknown) => void;
}

export class LiveVoiceRecorder {
  private readonly options: Required<Omit<LiveVoiceRecorderOptions, "onSpeechStarted" | "onSpeechCandidate" | "onSpeechMisfire" | "onAudioChunk" | "onSpeechPaused" | "onSpeechEnded" | "onError">> &
    Pick<LiveVoiceRecorderOptions, "onSpeechStarted" | "onSpeechCandidate" | "onSpeechMisfire" | "onAudioChunk" | "onSpeechPaused" | "onSpeechEnded" | "onError">;
  private stream: MediaStream | null = null;
  private vad: MicVAD | null = null;
  private active = false;
  private paused = false;
  private speechActive = false;
  private candidateActive = false;
  private maximumTurnTimer = 0;
  private turnFrames: Float32Array[] = [];
  private preRollFrames: Float32Array[] = [];
  private basicFallback: BasicVoiceRecorder | null = null;

  constructor(options: LiveVoiceRecorderOptions) {
    if (!options.onSpeechPaused && !options.onSpeechEnded) {
      throw new Error("LiveVoiceRecorder requires a pause or speech-ended callback.");
    }
    this.options = {
      maximumTurnMs: options.maximumTurnMs ?? 60_000,
      positiveSpeechThreshold: options.positiveSpeechThreshold ?? 0.65,
      negativeSpeechThreshold: options.negativeSpeechThreshold ?? 0.45,
      // Both windows are billed: everything captured here is sent to speech-to-text, which charges
      // per audio second. The pad and the redemption window together used to add ~2s of silence to
      // every turn - roughly a tenth of a conversation's speech-to-text bill. Trimmed to the
      // smallest values that still keep a word onset and let a learner pause mid-sentence; going
      // lower starts clipping the first phoneme, which matters more for L2 speakers than the cost.
      redemptionMs: options.redemptionMs ?? 900,
      preSpeechPadMs: options.preSpeechPadMs ?? 400,
      minimumSpeechMs: options.minimumSpeechMs ?? 320,
      onSpeechStarted: options.onSpeechStarted,
      onSpeechCandidate: options.onSpeechCandidate,
      onSpeechMisfire: options.onSpeechMisfire,
      onAudioChunk: options.onAudioChunk,
      onSpeechPaused: options.onSpeechPaused,
      onSpeechEnded: options.onSpeechEnded,
      onError: options.onError,
    };
  }

  async start(): Promise<void> {
    if (this.active) return;
    this.stream = await requestSpeakingMicrophone();
    const audioTrack = this.stream.getAudioTracks()[0];
    if (!audioTrack || audioTrack.readyState === "ended") {
      stopMediaStream(this.stream);
      this.stream = null;
      throw new DOMException("Microphone is unavailable", "NotFoundError");
    }

    audioTrack.addEventListener("ended", () => {
      const error = new DOMException("Microphone track ended", "NotReadableError");
      this.options.onError?.(error);
      void this.stop();
    }, { once: true });

    try {
      const wasmBasePath = ortWasmModuleUrl.slice(0, ortWasmModuleUrl.lastIndexOf("/") + 1);
      this.vad = await MicVAD.new({
        model: "v5",
        startOnLoad: false,
        baseAssetPath: "/vendor/vad/",
        onnxWASMBasePath: wasmBasePath,
        ortConfig: (ort) => {
          ort.env.logLevel = "error";
          ort.env.wasm.wasmPaths = { mjs: ortWasmModuleUrl, wasm: ortWasmBinaryUrl };
        },
        getStream: async () => this.stream ?? Promise.reject(new DOMException("Microphone is unavailable", "NotReadableError")),
        pauseStream: async () => undefined,
        resumeStream: async () => this.stream ?? requestSpeakingMicrophone(),
        positiveSpeechThreshold: this.options.positiveSpeechThreshold,
        negativeSpeechThreshold: this.options.negativeSpeechThreshold,
        redemptionMs: this.options.redemptionMs,
        preSpeechPadMs: this.options.preSpeechPadMs,
        minSpeechMs: this.options.minimumSpeechMs,
        submitUserSpeechOnPause: false,
        onSpeechStart: () => {
          this.candidateActive = true;
          void Promise.resolve(this.options.onSpeechCandidate?.()).catch(this.options.onError);
        },
        onSpeechRealStart: () => this.handleSpeechStart(),
        onSpeechEnd: (audio) => this.handleSpeechPause(audio),
        onVADMisfire: () => this.handleMisfire(),
        onFrameProcessed: (_probabilities, frame) => this.handleFrame(frame),
      });
      this.active = true;
      this.paused = false;
      await this.vad.start();
    } catch {
      // The microphone can be available while the optional ONNX/VAD runtime is not
      // supported by a browser/device. Keep live speaking usable with a lightweight
      // Web Audio silence detector instead of reporting a microphone hardware failure.
      try {
        this.basicFallback = new BasicVoiceRecorder(
          this.stream,
          this.options.maximumTurnMs,
          this.options.minimumSpeechMs,
          this.options.redemptionMs,
          this.options.preSpeechPadMs,
          () => this.options.onSpeechCandidate?.(),
          () => this.options.onSpeechStarted?.(),
          () => this.options.onSpeechMisfire?.(),
          (audio) => this.options.onAudioChunk?.(audio),
          (audio) => this.options.onSpeechPaused?.(audio) ?? this.options.onSpeechEnded?.(audio),
          this.options.onError,
        );
        await this.basicFallback.start();
        this.active = true;
        this.paused = false;
      } catch (fallbackError) {
        stopMediaStream(this.stream);
        this.stream = null;
        this.options.onError?.(fallbackError);
        throw fallbackError;
      }
    }
  }

  pause() {
    this.paused = true;
    this.speechActive = false;
    this.clearSpeechTimer();
    void this.vad?.pause();
    void this.basicFallback?.pause();
  }

  resume() {
    if (!this.active) return;
    this.paused = false;
    void this.vad?.start().catch(this.options.onError);
    void this.basicFallback?.resume().catch(this.options.onError);
  }

  listenForInterruption() { this.resume(); }

  resetTurn() {
    this.turnFrames = [];
    this.preRollFrames = [];
    this.speechActive = false;
    this.candidateActive = false;
    this.clearSpeechTimer();
  }

  async stop(): Promise<void> {
    this.active = false;
    this.paused = false;
    this.resetTurn();
    const vad = this.vad;
    this.vad = null;
    if (vad) await vad.destroy();
    const basicFallback = this.basicFallback;
    this.basicFallback = null;
    if (basicFallback) await basicFallback.stop();
    stopMediaStream(this.stream);
    this.stream = null;
  }

  private handleSpeechStart() {
    if (!this.active || this.paused) return;
    this.speechActive = true;
    this.candidateActive = false;
    // Publish the state transition before flushing pre-roll. Consumers use this to
    // mark barge-in before the utterance is published by onSpeechEnd.
    void Promise.resolve(this.options.onSpeechStarted?.()).catch(this.options.onError);
    this.preRollFrames = [];
    this.clearSpeechTimer();
    this.maximumTurnTimer = window.setTimeout(() => this.handleMaximumTurn(), this.options.maximumTurnMs);
  }

  private handleFrame(frame: Float32Array) {
    if (!this.active || this.paused || frame.length === 0) return;
    const copy = frame.slice();
    if (!this.speechActive) {
      this.preRollFrames.push(copy);
      const maximumFrames = Math.max(1, Math.ceil(this.options.preSpeechPadMs / 32));
      if (this.preRollFrames.length > maximumFrames) this.preRollFrames.shift();
      return;
    }
    // vad-web's onFrameProcessed frame is the same frame already included in
    // onSpeechEnd's utterance buffer. Keep it only for pre-roll. Streaming both
    // callbacks duplicates PCM and can corrupt timing at the speech provider.
  }

  private handleSpeechPause(audio: Float32Array) {
    this.speechActive = false;
    this.candidateActive = false;
    this.clearSpeechTimer();
    if (!this.active || this.paused || audio.length === 0) return;
    const wholeTurn = audio.slice();
    if (this.options.onAudioChunk) {
      this.turnFrames = [wholeTurn];
      void Promise.resolve(this.options.onAudioChunk(wholeTurn))
        .then(() => this.options.onSpeechPaused?.(wholeTurn) ?? this.options.onSpeechEnded?.(wholeTurn))
        .catch(this.options.onError);
    } else {
      this.turnFrames = [wholeTurn];
      void Promise.resolve(this.options.onSpeechPaused?.(wholeTurn) ?? this.options.onSpeechEnded?.(wholeTurn))
        .catch(this.options.onError);
    }
  }

  private handleMisfire() {
    this.speechActive = false;
    this.turnFrames = [];
    this.clearSpeechTimer();
    if (this.candidateActive) {
      this.candidateActive = false;
      void Promise.resolve(this.options.onSpeechMisfire?.()).catch(this.options.onError);
    }
  }

  private handleMaximumTurn() {
    this.clearSpeechTimer();
    this.speechActive = false;
    const wholeTurn = this.concatFrames();
    if (wholeTurn.length > 0) {
      void Promise.resolve(this.options.onSpeechPaused?.(wholeTurn) ?? this.options.onSpeechEnded?.(wholeTurn))
        .catch(this.options.onError);
    }
  }

  private concatFrames(): Float32Array {
    const size = this.turnFrames.reduce((sum, frame) => sum + frame.length, 0);
    const result = new Float32Array(size);
    let offset = 0;
    for (const frame of this.turnFrames) { result.set(frame, offset); offset += frame.length; }
    return result;
  }

  private clearSpeechTimer() {
    if (this.maximumTurnTimer) window.clearTimeout(this.maximumTurnTimer);
    this.maximumTurnTimer = 0;
  }
}

class BasicVoiceRecorder {
  private context: AudioContext | null = null;
  private source: MediaStreamAudioSourceNode | null = null;
  private processor: ScriptProcessorNode | null = null;
  private silentSince = 0;
  private speechStartedAt = 0;
  private speechActive = false;
  private paused = false;
  private maximumTurnTimer = 0;
  private frames: Float32Array[] = [];
  private preRoll: Float32Array[] = [];
  private candidateStartedAt = 0;
  private noiseFloor = 0.003;

  constructor(
    private readonly stream: MediaStream,
    private readonly maximumTurnMs: number,
    private readonly minimumSpeechMs: number,
    private readonly redemptionMs: number,
    private readonly preSpeechPadMs: number,
    private readonly onSpeechCandidate: () => void | Promise<void>,
    private readonly onSpeechStarted: () => void | Promise<void>,
    private readonly onSpeechMisfire: () => void | Promise<void>,
    private readonly onAudioChunk: (audio: Float32Array) => void | Promise<void>,
    private readonly onSpeechPaused: (audio: Float32Array) => void | Promise<void>,
    private readonly onError?: (error: unknown) => void,
  ) {}

  async start() {
    const AudioContextConstructor = window.AudioContext ??
      (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioContextConstructor) throw new DOMException("Web Audio is not supported", "NotSupportedError");
    this.context = new AudioContextConstructor({ sampleRate: 16_000 });
    await this.context.resume();
    this.source = this.context.createMediaStreamSource(this.stream);
    this.processor = this.context.createScriptProcessor(2048, 1, 1);
    this.processor.onaudioprocess = (event) => this.process(event.inputBuffer.getChannelData(0));
    this.source.connect(this.processor);
    this.processor.connect(this.context.destination);
  }

  pause() {
    this.paused = true;
    this.reset();
    return this.context?.suspend() ?? Promise.resolve();
  }

  async resume() {
    this.paused = false;
    await this.context?.resume();
  }

  async stop() {
    this.reset();
    this.processor?.disconnect();
    this.source?.disconnect();
    this.processor = null;
    this.source = null;
    const context = this.context;
    this.context = null;
    if (context && context.state !== "closed") await context.close();
  }

  private process(input: Float32Array) {
    if (this.paused || input.length === 0) return;
    const frame = input.slice();
    const rms = Math.sqrt(frame.reduce((sum, sample) => sum + sample * sample, 0) / frame.length);
    const now = performance.now();
    const startThreshold = Math.max(0.015, this.noiseFloor * 3);
    const releaseThreshold = Math.max(0.009, this.noiseFloor * 1.7);

    if (!this.speechActive) {
      this.preRoll.push(frame);
      const frameMs = frame.length / (this.context?.sampleRate ?? 16_000) * 1000;
      while (this.preRoll.length * frameMs > this.preSpeechPadMs) this.preRoll.shift();
      if (rms < startThreshold) {
        this.noiseFloor = this.noiseFloor * 0.95 + rms * 0.05;
        if (this.candidateStartedAt) {
          this.candidateStartedAt = 0;
          void Promise.resolve(this.onSpeechMisfire()).catch(this.onError);
        }
        return;
      }
      if (!this.candidateStartedAt) {
        this.candidateStartedAt = now;
        void Promise.resolve(this.onSpeechCandidate()).catch(this.onError);
        return;
      }
      if (now - this.candidateStartedAt < Math.max(280, this.minimumSpeechMs)) return;
      this.speechActive = true;
      this.speechStartedAt = now;
      this.candidateStartedAt = 0;
      this.silentSince = 0;
      void Promise.resolve(this.onSpeechStarted()).catch(this.onError);
      for (const preRollFrame of this.preRoll.splice(0)) this.publish(preRollFrame);
      this.maximumTurnTimer = window.setTimeout(() => this.commit(), this.maximumTurnMs);
    }

    this.publish(frame);
    if (rms >= releaseThreshold) {
      this.silentSince = 0;
    } else {
      this.silentSince ||= now;
      if (now - this.silentSince >= this.redemptionMs &&
          now - this.speechStartedAt >= this.minimumSpeechMs) {
        this.commit();
      }
    }
  }

  private publish(frame: Float32Array) {
    this.frames.push(frame);
    void Promise.resolve(this.onAudioChunk(frame)).catch(this.onError);
  }

  private commit() {
    if (!this.speechActive || this.frames.length === 0) return;
    const size = this.frames.reduce((sum, frame) => sum + frame.length, 0);
    const audio = new Float32Array(size);
    let offset = 0;
    for (const frame of this.frames) {
      audio.set(frame, offset);
      offset += frame.length;
    }
    this.reset();
    void Promise.resolve(this.onSpeechPaused(audio)).catch(this.onError);
  }

  private reset() {
    if (this.maximumTurnTimer) window.clearTimeout(this.maximumTurnTimer);
    this.maximumTurnTimer = 0;
    this.speechActive = false;
    this.silentSince = 0;
    this.speechStartedAt = 0;
    this.candidateStartedAt = 0;
    this.frames = [];
    this.preRoll = [];
  }
}
