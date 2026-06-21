import { afterEach, describe, expect, it, vi } from "vitest";

const vadInstances: FakeVad[] = [];

class FakeVad {
  listening = false;
  start = vi.fn(async () => { this.listening = true; });
  pause = vi.fn(async () => { this.listening = false; });
  destroy = vi.fn(async () => { this.listening = false; });
  constructor(public options: Record<string, unknown>) { vadInstances.push(this); }
}

vi.mock("@ricky0123/vad-web", () => ({
  MicVAD: { new: vi.fn(async (options: Record<string, unknown>) => new FakeVad(options)) },
}));

import { LiveVoiceRecorder } from "./liveVoiceRecorder";

describe("LiveVoiceRecorder", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vadInstances.length = 0;
  });

  function installMicrophone() {
    const track = { readyState: "live", stop: vi.fn(), addEventListener: vi.fn() };
    const stream = { getAudioTracks: () => [track], getTracks: () => [track] } as unknown as MediaStream;
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: { getUserMedia: vi.fn().mockResolvedValue(stream) },
    });
    return { track, stream };
  }

  it("starts Silero V5 with local assets and tuned speech thresholds", async () => {
    installMicrophone();
    const recorder = new LiveVoiceRecorder({ onSpeechEnded: vi.fn() });

    await recorder.start();

    const vad = vadInstances[0];
    expect(vad.options.model).toBe("v5");
    expect(vad.options.baseAssetPath).toBe("/vendor/vad/");
    expect(String(vad.options.onnxWASMBasePath)).toContain("/src/vendor/");
    expect(vad.options.positiveSpeechThreshold).toBe(0.65);
    expect(vad.options.negativeSpeechThreshold).toBe(0.45);
    expect(vad.options.minSpeechMs).toBe(320);
    // Pad and redemption are billed audio - every captured millisecond is charged by speech-to-text.
    expect(vad.options.preSpeechPadMs).toBe(400);
    expect(vad.options.redemptionMs).toBe(900);
    expect(vad.start).toHaveBeenCalledOnce();
  });

  it("delivers 16 kHz speech samples without a WebM decode roundtrip", async () => {
    installMicrophone();
    const speechEnded = vi.fn().mockResolvedValue(undefined);
    const recorder = new LiveVoiceRecorder({ onSpeechEnded: speechEnded });
    await recorder.start();
    const vad = vadInstances[0];
    const audio = new Float32Array([0.1, -0.1, 0.2]);

    (vad.options.onSpeechStart as () => void)();
    (vad.options.onSpeechRealStart as () => void)();
    await (vad.options.onSpeechEnd as (samples: Float32Array) => Promise<void>)(audio);

    expect(speechEnded).toHaveBeenCalledWith(audio);
    expect(vad.pause).not.toHaveBeenCalled();
  });

  it("publishes the complete Silero utterance once and keeps listening after a pause", async () => {
    installMicrophone();
    const chunk = vi.fn();
    const paused = vi.fn();
    const recorder = new LiveVoiceRecorder({ onAudioChunk: chunk, onSpeechPaused: paused });
    await recorder.start();
    const vad = vadInstances[0];
    const frame = new Float32Array([0.2, -0.2]);

    (vad.options.onSpeechStart as () => void)();
    (vad.options.onSpeechRealStart as () => void)();
    await (vad.options.onFrameProcessed as (probabilities: unknown, samples: Float32Array) => Promise<void>)(
      { isSpeech: 0.9, notSpeech: 0.1 }, frame,
    );
    await (vad.options.onSpeechEnd as (samples: Float32Array) => Promise<void>)(frame);

    expect(chunk).toHaveBeenCalledOnce();
    expect(chunk).toHaveBeenCalledWith(frame);
    expect(paused).toHaveBeenCalled();
    expect(vad.pause).not.toHaveBeenCalled();

    (vad.options.onSpeechStart as () => void)();
    (vad.options.onSpeechRealStart as () => void)();
    expect(vad.pause).not.toHaveBeenCalled();
  });

  it("announces speech before publishing the completed utterance", async () => {
    installMicrophone();
    const order: string[] = [];
    const recorder = new LiveVoiceRecorder({
      onSpeechStarted: () => { order.push("started"); },
      onAudioChunk: () => { order.push("chunk"); },
      onSpeechPaused: vi.fn(),
    });
    await recorder.start();
    const vad = vadInstances[0];
    const frame = new Float32Array([0.1, 0.2]);
    await (vad.options.onFrameProcessed as (probabilities: unknown, samples: Float32Array) => Promise<void>)({}, frame);

    (vad.options.onSpeechStart as () => void)();
    (vad.options.onSpeechRealStart as () => void)();
    await (vad.options.onSpeechEnd as (samples: Float32Array) => Promise<void>)(frame);
    await Promise.resolve();

    expect(order).toEqual(["started", "chunk"]);
  });

  it("does not stream a tentative noise candidate until Silero confirms real speech", async () => {
    installMicrophone();
    const candidate = vi.fn();
    const started = vi.fn();
    const chunk = vi.fn();
    const recorder = new LiveVoiceRecorder({
      onSpeechCandidate: candidate,
      onSpeechStarted: started,
      onAudioChunk: chunk,
      onSpeechPaused: vi.fn(),
    });
    await recorder.start();
    const vad = vadInstances[0];
    const frame = new Float32Array([0.1, 0.2]);

    await (vad.options.onFrameProcessed as (probabilities: unknown, samples: Float32Array) => Promise<void>)({}, frame);
    (vad.options.onSpeechStart as () => void)();
    await Promise.resolve();

    expect(candidate).toHaveBeenCalledOnce();
    expect(started).not.toHaveBeenCalled();
    expect(chunk).not.toHaveBeenCalled();

    (vad.options.onSpeechRealStart as () => void)();
    await (vad.options.onSpeechEnd as (samples: Float32Array) => Promise<void>)(frame);
    await Promise.resolve();

    expect(started).toHaveBeenCalledOnce();
    expect(chunk).toHaveBeenCalledWith(frame);
  });

  it("clears tentative audio when Silero reports a VAD misfire", async () => {
    installMicrophone();
    const misfire = vi.fn();
    const chunk = vi.fn();
    const recorder = new LiveVoiceRecorder({
      onSpeechMisfire: misfire,
      onAudioChunk: chunk,
      onSpeechPaused: vi.fn(),
    });
    await recorder.start();
    const vad = vadInstances[0];

    await (vad.options.onFrameProcessed as (probabilities: unknown, samples: Float32Array) => Promise<void>)(
      {},
      new Float32Array([0.1]),
    );
    (vad.options.onSpeechStart as () => void)();
    (vad.options.onVADMisfire as () => void)();
    await Promise.resolve();

    expect(misfire).toHaveBeenCalledOnce();
    expect(chunk).not.toHaveBeenCalled();
  });

  it("pauses, resumes, and keeps listening for tutor interruption", async () => {
    installMicrophone();
    const recorder = new LiveVoiceRecorder({ onSpeechEnded: vi.fn() });
    await recorder.start();
    const vad = vadInstances[0];

    recorder.pause();
    recorder.resume();
    recorder.listenForInterruption();

    expect(vad.pause).toHaveBeenCalledOnce();
    expect(vad.start).toHaveBeenCalledTimes(3);
  });

  it("stops the microphone track and destroys the VAD", async () => {
    const { track } = installMicrophone();
    const recorder = new LiveVoiceRecorder({ onSpeechEnded: vi.fn() });
    await recorder.start();
    const vad = vadInstances[0];

    await recorder.stop();

    expect(vad.destroy).toHaveBeenCalledOnce();
    expect(track.stop).toHaveBeenCalledOnce();
  });
});
