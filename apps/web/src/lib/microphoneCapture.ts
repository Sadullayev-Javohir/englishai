export const SPEAKING_AUDIO_CONSTRAINTS: MediaTrackConstraints = {
  echoCancellation: true,
  noiseSuppression: true,
  autoGainControl: true,
  channelCount: 1,
  sampleRate: 48_000,
  sampleSize: 16,
};

const PREFERRED_AUDIO_TYPES = [
  "audio/webm;codecs=opus",
  "audio/webm",
  "audio/mp4",
];

export function createAudioRecorder(stream: MediaStream): MediaRecorder {
  const mimeType = PREFERRED_AUDIO_TYPES.find((type) => MediaRecorder.isTypeSupported?.(type));
  return new MediaRecorder(stream, mimeType ? { mimeType } : undefined);
}

export function requestSpeakingMicrophone(): Promise<MediaStream> {
  if (!navigator.mediaDevices?.getUserMedia) {
    return Promise.reject(new DOMException("Audio recording is not supported", "NotSupportedError"));
  }
  return navigator.mediaDevices
    .getUserMedia({ audio: SPEAKING_AUDIO_CONSTRAINTS })
    .catch((error: unknown) => {
      // Some Linux/browser audio stacks expose a working default microphone but reject
      // optional processing/sample-rate constraints with OverconstrainedError or
      // NotReadableError. Retry once with the browser's native defaults.
      if (error instanceof DOMException &&
          (error.name === "OverconstrainedError" || error.name === "NotReadableError")) {
        return navigator.mediaDevices.getUserMedia({ audio: true });
      }
      throw error;
    });
}

export function isMicrophonePermissionDenied(error: unknown): boolean {
  return error instanceof DOMException && (error.name === "NotAllowedError" || error.name === "SecurityError");
}

export function stopMediaStream(stream: MediaStream | null | undefined) {
  stream?.getTracks().forEach((track) => track.stop());
}
