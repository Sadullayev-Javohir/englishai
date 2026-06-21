import { useCallback, useEffect, useRef } from "react";
import {
  playLessonSound,
  unlockLessonAudio,
  type LessonSound,
  type StopLessonSound,
} from "@/lib/audio";

export type LessonSounds = {
  playAnswer: (correct: boolean) => void;
  playComplete: () => void;
};

/**
 * Provides the shared lesson sound cues while respecting browser autoplay policies.
 *
 * The first pointer or keyboard interaction unlocks Web Audio. Slide sounds only play for later
 * `slideKey` changes (never for the initial slide or keys visited before unlock). Scheduled sounds
 * are stopped on unmount, and refs prevent React StrictMode's repeated effects from double-playing.
 */
export function useLessonSounds(slideKey: string | number): LessonSounds {
  const unlockedRef = useRef(false);
  const mountedRef = useRef(true);
  const previousSlideKeyRef = useRef(slideKey);
  const pendingSlideKeyRef = useRef<string | number | null>(null);
  const lastPlayedSlideKeyRef = useRef<string | number | null>(null);
  const activeSoundsRef = useRef(new Set<StopLessonSound>());

  const play = useCallback((sound: LessonSound) => {
    const stop = playLessonSound(sound);
    if (!stop) return;

    const activeSounds = activeSoundsRef.current;
    activeSounds.add(stop);

    // Every cue is shorter than one second. Forget completed stop handles without retaining a
    // growing set for long lessons; calling a completed handle remains safe if cleanup wins.
    window.setTimeout(() => activeSounds.delete(stop), 1000);
  }, []);

  useEffect(() => {
    mountedRef.current = true;
    const activeSounds = activeSoundsRef.current;

    const unlock = () => {
      void unlockLessonAudio().then((unlocked) => {
        if (!mountedRef.current || !unlocked) return;
        unlockedRef.current = true;
        const pendingSlideKey = pendingSlideKeyRef.current;
        if (
          pendingSlideKey !== null &&
          !Object.is(lastPlayedSlideKeyRef.current, pendingSlideKey)
        ) {
          pendingSlideKeyRef.current = null;
          lastPlayedSlideKeyRef.current = pendingSlideKey;
          play("slide");
        }
      });
    };

    document.addEventListener("pointerdown", unlock, { capture: true, passive: true });
    document.addEventListener("keydown", unlock, { capture: true });

    return () => {
      mountedRef.current = false;
      document.removeEventListener("pointerdown", unlock, true);
      document.removeEventListener("keydown", unlock, true);
      for (const stop of activeSounds) stop();
      activeSounds.clear();
    };
  }, [play]);

  useEffect(() => {
    const changed = !Object.is(previousSlideKeyRef.current, slideKey);
    previousSlideKeyRef.current = slideKey;

    if (!changed || Object.is(lastPlayedSlideKeyRef.current, slideKey)) return;
    if (!unlockedRef.current) {
      pendingSlideKeyRef.current = slideKey;
      return;
    }

    pendingSlideKeyRef.current = null;
    lastPlayedSlideKeyRef.current = slideKey;
    play("slide");
  }, [play, slideKey]);

  const playAfterUnlock = useCallback((sound: LessonSound) => {
    void unlockLessonAudio().then((unlocked) => {
      if (!mountedRef.current || !unlocked) return;
      unlockedRef.current = true;
      play(sound);
    });
  }, [play]);

  const playAnswer = useCallback((correct: boolean) => {
    playAfterUnlock(correct ? "correct" : "incorrect");
  }, [playAfterUnlock]);

  const playComplete = useCallback(() => {
    playAfterUnlock("complete");
  }, [playAfterUnlock]);

  return { playAnswer, playComplete };
}
