import { createContext, useCallback, useContext, useMemo, useState } from "react";

/**
 * Hearts (limited attempts) provider (Jungle Academy spec §2.7).
 * Canonical game-layer copy (migrated from the deprecated duo/* layer).
 * Hearts are the learner's "lives" - they decrement on lesson failure/retry and
 * regenerate over time. Server is source of truth on lesson start; this client
 * provider keeps a smooth HUD count and lets lesson screens spend a heart.
 */

export const HEARTS_MAX = 5;
const REGEN_MS = 30 * 60 * 1000; // one heart per 30 min

interface HeartsContextValue {
  hearts: number;
  spendHeart: () => void;
  boostHearts: (n?: number) => void;
}

const HeartsContext = createContext<HeartsContextValue | null>(null);

const STORAGE_KEY = "englishai-hearts";
const EXPIRY_KEY = "englishai-hearts-expiry";

function loadInitial(): number {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const expiry = Number(localStorage.getItem(EXPIRY_KEY) ?? "0");
    if (raw == null) return HEARTS_MAX;
    let h = Number(raw);
    if (h < HEARTS_MAX && expiry > 0) {
      const elapsed = Date.now() - expiry;
      const regen = Math.floor(elapsed / REGEN_MS);
      h = Math.min(HEARTS_MAX, h + regen);
    }
    return h;
  } catch {
    return HEARTS_MAX;
  }
}

function persist(hearts: number) {
  try {
    localStorage.setItem(STORAGE_KEY, String(hearts));
    if (hearts < HEARTS_MAX) {
      if (!localStorage.getItem(EXPIRY_KEY)) {
        localStorage.setItem(EXPIRY_KEY, String(Date.now()));
      }
    } else {
      localStorage.removeItem(EXPIRY_KEY);
    }
  } catch {
    /* ignore */
  }
}

export function HeartsProvider({ children }: { children: React.ReactNode }) {
  const [hearts, setHearts] = useState<number>(loadInitial);

  const spendHeart = useCallback(() => {
    setHearts((h) => {
      const next = Math.max(0, h - 1);
      persist(next);
      return next;
    });
  }, []);

  const boostHearts = useCallback((n = 1) => {
    setHearts((h) => {
      const next = Math.min(HEARTS_MAX, h + n);
      persist(next);
      return next;
    });
  }, []);

  const value = useMemo(
    () => ({ hearts, spendHeart, boostHearts }),
    [hearts, spendHeart, boostHearts],
  );

  return <HeartsContext.Provider value={value}>{children}</HeartsContext.Provider>;
}

export function useHeartsState(): HeartsContextValue {
  const ctx = useContext(HeartsContext);
  if (!ctx) {
    return { hearts: HEARTS_MAX, spendHeart: () => {}, boostHearts: () => {} };
  }
  return ctx;
}
