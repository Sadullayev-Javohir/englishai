import { useCallback, useEffect, useRef, useState } from "react";

export type VocabularyVoiceState = "idle" | "loading" | "playing" | "error";

/** Playback state follows media/utterance events, not an estimated word-duration timer. */
export function useVocabularyVoice(word: string) {
  const [state, setState] = useState<VocabularyVoiceState>("idle");
  const cleanupRef = useRef<() => void>(() => undefined);
  const stop = useCallback(() => {
    cleanupRef.current();
    cleanupRef.current = () => undefined;
    setState("idle");
  }, []);

  const play = useCallback((base64?: string | null, rate = 1) => {
    cleanupRef.current();
    let cancelled = false;
    let audio: HTMLAudioElement | undefined;
    let utterance: SpeechSynthesisUtterance | undefined;
    let voiceTimer: number | undefined;
    let voicesReady: (() => void) | undefined;
    const synth = window.speechSynthesis;
    const update = (next: VocabularyVoiceState) => { if (!cancelled) setState(next); };
    const finish = (next: VocabularyVoiceState) => {
      window.clearTimeout(timeout);
      update(next);
    };
    const speak = () => {
      if (cancelled) return;
      if (!synth || !word.trim()) { finish("error"); return; }
      let started = false;
      voicesReady = () => {
        if (cancelled || started) return;
        started = true;
        window.clearTimeout(voiceTimer);
        synth.removeEventListener("voiceschanged", voicesReady!);
        utterance = new SpeechSynthesisUtterance(word);
        utterance.lang = "en-GB";
        utterance.rate = rate;
        utterance.voice = synth.getVoices().find(voice => voice.lang === "en-GB")
          ?? synth.getVoices().find(voice => voice.lang.startsWith("en")) ?? null;
        utterance.onstart = () => { window.clearTimeout(timeout); update("playing"); };
        utterance.onend = () => finish("idle");
        utterance.onerror = () => finish("error");
        try { synth.speak(utterance); synth.resume(); } catch { finish("error"); }
      };
      if (synth.getVoices().length) voiceTimer = window.setTimeout(voicesReady, 0);
      else {
        synth.addEventListener("voiceschanged", voicesReady);
        voiceTimer = window.setTimeout(voicesReady, 250);
      }
    };
    cleanupRef.current = () => {
      cancelled = true;
      window.clearTimeout(voiceTimer);
      window.clearTimeout(timeout);
      if (voicesReady) synth?.removeEventListener("voiceschanged", voicesReady);
      if (audio) {
        audio.onplaying = audio.onended = audio.onpause = audio.onerror = null;
        audio.pause();
      }
      if (utterance) {
        utterance.onstart = utterance.onend = utterance.onerror = null;
        synth?.cancel();
      }
    };
    update("loading");
    // A broken device/voice must not leave a permanent loading indicator.
    const timeout = window.setTimeout(() => { cleanupRef.current(); setState("error"); }, 12_000);
    if (!base64) { speak(); return; }
    try {
      audio = new Audio();
      audio.src = base64.startsWith("data:") ? base64 : `data:audio/wav;base64,${base64}`;
      audio.playbackRate = Math.max(.5, rate);
      audio.onplaying = () => { window.clearTimeout(timeout); update("playing"); };
      audio.onended = () => finish("idle");
      audio.onpause = () => update("idle");
      let fellBack = false;
      const fallback = () => {
        if (cancelled || fellBack) return;
        fellBack = true;
        if (audio) { audio.onplaying = audio.onended = audio.onpause = audio.onerror = null; audio.pause(); }
        speak();
      };
      audio.onerror = fallback;
      void audio.play().catch(fallback);
    } catch { speak(); }
  }, [word]);

  useEffect(() => {
    setState("idle");
    return () => cleanupRef.current();
  }, [word]);
  return { state, play, stop };
}
