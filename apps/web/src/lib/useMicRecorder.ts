import { useCallback, useEffect, useRef, useState } from "react";
import { createAudioRecorder, requestSpeakingMicrophone, stopMediaStream } from "./microphoneCapture";

/**
 * Shared microphone-recording lifecycle (permission → `MediaRecorder` → stop → blob), extracted
 * from the duplicated pattern in `PronunciationDetailPage.tsx`'s `PronunciationAttempt` and
 * `SpeakingPage.tsx`. Used by the word "sinab ko'ring" recorder and the video shadowing mode - both
 * new call sites, so this doesn't touch (or risk regressing) the two existing pages, which keep
 * their own inline copies for now.
 *
 * The hook only owns the mic/recorder lifecycle; it does not know about WAV conversion or the
 * scoring API - callers pass an `onRecorded` callback that receives the raw recorded `Blob` once
 * stopped, and do their own async work (encode + upload) around it.
 */
export function useMicRecorder(onRecorded: (blob: Blob) => void) {
  const [status, setStatus] = useState<"idle" | "recording">("idle");
  const [micError, setMicError] = useState(false);
  // Live only while recording - exposed so a caller (e.g. a frequency-bar visualizer) can attach
  // its own AnalyserNode without owning the recorder itself.
  const [stream, setStream] = useState<MediaStream | null>(null);

  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const onRecordedRef = useRef(onRecorded);
  onRecordedRef.current = onRecorded;

  const start = useCallback(async (): Promise<boolean> => {
    if (recorderRef.current) return false;
    setMicError(false);
    try {
      const mediaStream = await requestSpeakingMicrophone();
      const recorder = createAudioRecorder(mediaStream);
      recorderRef.current = recorder;
      chunksRef.current = [];
      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) chunksRef.current.push(e.data);
      };
      recorder.onstop = () => {
        stopMediaStream(mediaStream);
        const blob = new Blob(chunksRef.current, { type: recorder.mimeType });
        recorderRef.current = null;
        setStream(null);
        onRecordedRef.current(blob);
      };
      recorder.onerror = () => {
        stopMediaStream(mediaStream);
        recorderRef.current = null;
        setStream(null);
        setStatus("idle");
        setMicError(true);
      };
      recorder.start(250);
      setStream(mediaStream);
      setStatus("recording");
      return true;
    } catch {
      setMicError(true);
      return false;
    }
  }, []);

  const stop = useCallback(() => {
    if (recorderRef.current?.state === "recording") {
      recorderRef.current.requestData?.();
      recorderRef.current.stop();
    }
    setStatus("idle");
  }, []);

  // Stop cleanly (release the mic) if the component unmounts mid-recording.
  useEffect(() => () => recorderRef.current?.stop(), []);

  return { status, micError, stream, start, stop };
}
