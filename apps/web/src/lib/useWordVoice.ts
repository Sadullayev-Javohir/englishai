import { useCallback, useEffect, useRef } from "react";
import { api } from "@/api/client";
import { playWordVoice } from "@/lib/audio";

// Process-wide cache of successful Azure reference clips per word. Failed or unavailable clips are
// not cached, so a transient provider outage does not make pronunciation silent for the whole session.
const audioCache = new Map<string, string>();

/**
 * Returns a `speak(word)` callback that plays a word's pronunciation through the shared "Tinglash"
 * path: it fetches the Azure reference audio from the Speaking word-detail endpoint (cached), and
 * plays it via {@link playWordVoice}, falling back to the browser voice. This is for the simple
 * list/practice listen buttons that don't otherwise load the full word detail; the rich
 * tooltip/sheet play their already-loaded `audioBase64` directly.
 */
export function useWordVoice() {
  const audioRef = useRef<HTMLAudioElement | null>(null);

  useEffect(() => {
    audioRef.current = new Audio();
    return () => {
      audioRef.current?.pause();
      audioRef.current = null;
    };
  }, []);

  return useCallback((rawWord: string) => {
    const word = rawWord.trim();
    if (!word) return;

    const key = word.toLowerCase();
    const cached = audioCache.get(key);
    if (cached !== undefined) {
      playWordVoice(cached, word, audioRef.current);
      return;
    }

    // First time for this word: fetch the Azure clip, then play it (a brief delay, but it produces
    // real sound where the browser voice is silent). Any failure falls back to the browser voice.
    void api.speaking
      .wordDetail(word)
      .then((d) => {
        const clip = d.audioBase64 ?? null;
        if (clip) audioCache.set(key, clip);
        playWordVoice(clip, word, audioRef.current);
      })
      .catch(() => {
        playWordVoice(null, word, audioRef.current);
      });
  }, []);
}
