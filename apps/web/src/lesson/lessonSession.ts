/**
 * Per-session lesson state: hearts (lives) + XP + hearts animation.
 *
 * Lives for the whole tab session (survives the wizard re-mounting as the learner moves
 * between steps/words/exercises), and is mirrored to sessionStorage so a route re-mount or
 * a mid-lesson refresh keeps the same hearts/XP - the lesson feels continuous (acceptance:
 * "Progress/heart state preserved across the session").
 *
 * Implemented as a tiny external store consumed via useSyncExternalStore, so every 3D tile
 * and the global HUD re-render from one source of truth without prop drilling.
 */
import { useSyncExternalStore } from "react";

const STORAGE_KEY = "englishai.lesson.session.v1";

export const MAX_HEARTS = 5;
const START_XP = 0;

export interface LessonSessionState {
  hearts: number;
  xp: number;
}

function load(): LessonSessionState {
  if (typeof window === "undefined") {
    return { hearts: MAX_HEARTS, xp: START_XP };
  }
  try {
    const raw = window.sessionStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Partial<LessonSessionState>;
      return {
        hearts: clampHearts(parsed.hearts ?? MAX_HEARTS),
        xp: Math.max(0, parsed.xp ?? START_XP),
      };
    }
  } catch {
    /* corrupt storage - fall through to fresh state */
  }
  return { hearts: MAX_HEARTS, xp: START_XP };
}

function clampHearts(n: number): number {
  return Math.max(0, Math.min(MAX_HEARTS, Math.floor(n)));
}

let state: LessonSessionState = load();
const listeners = new Set<() => void>();

function persist() {
  if (typeof window !== "undefined") {
    try {
      window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch {
      /* storage may be unavailable (private mode) - state still works in-memory */
    }
  }
}

function emit() {
  persist();
  listeners.forEach((l) => l());
}

/** Subscribe a React component to lesson-session changes. */
function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function getSnapshot(): LessonSessionState {
  return state;
}

export function useLessonSession(): LessonSessionState {
  return useSyncExternalStore(subscribe, getSnapshot, getSnapshot);
}

/** Remove one heart (no-op at zero). Returns the new heart count. */
export function loseHeart(): number {
  if (state.hearts <= 0) return state.hearts;
  state = { ...state, hearts: state.hearts - 1 };
  emit();
  return state.hearts;
}

/** Award XP (positive) for a correct answer. */
export function awardXp(amount: number): void {
  if (amount <= 0) return;
  state = { ...state, xp: state.xp + amount };
  emit();
}

/** Refill hearts to full (used when starting a fresh topic / on retry). */
export function refillHearts(): void {
  state = { ...state, hearts: MAX_HEARTS };
  emit();
}

/** Reset the whole session (fresh topic open). */
export function resetLessonSession(): void {
  state = { hearts: MAX_HEARTS, xp: START_XP };
  emit();
}
