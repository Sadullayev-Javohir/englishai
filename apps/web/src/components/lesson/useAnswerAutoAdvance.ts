import { useCallback, useEffect, useRef, useState } from "react";

const DEFAULT_AUTO_ADVANCE_DELAY_MS = 3000;
const COUNTDOWN_TICK_MS = 250;

export interface UseAnswerAutoAdvanceOptions {
  enabled: boolean;
  onAdvance: () => void;
  resetKey?: unknown;
  delayMs?: number;
}

export interface AnswerAutoAdvanceState {
  active: boolean;
  remainingSeconds: number;
  advance: () => void;
  cancel: () => void;
}

export function useAnswerAutoAdvance({
  enabled,
  onAdvance,
  resetKey,
  delayMs = DEFAULT_AUTO_ADVANCE_DELAY_MS,
}: UseAnswerAutoAdvanceOptions): AnswerAutoAdvanceState {
  const callbackRef = useRef(onAdvance);
  const completedRef = useRef(false);
  const enabledRef = useRef(enabled);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const [active, setActive] = useState(false);
  const [remainingSeconds, setRemainingSeconds] = useState(0);

  callbackRef.current = onAdvance;
  enabledRef.current = enabled;

  const clearTimers = useCallback(() => {
    if (timeoutRef.current !== null) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
    if (intervalRef.current !== null) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  }, []);

  const cancel = useCallback(() => {
    completedRef.current = true;
    clearTimers();
    setActive(false);
    setRemainingSeconds(0);
  }, [clearTimers]);

  const complete = useCallback(() => {
    if (!enabledRef.current || completedRef.current) return;
    completedRef.current = true;
    clearTimers();
    setActive(false);
    setRemainingSeconds(0);
    callbackRef.current();
  }, [clearTimers]);

  useEffect(() => {
    clearTimers();
    completedRef.current = false;

    if (!enabled) {
      setActive(false);
      setRemainingSeconds(0);
      return clearTimers;
    }

    const safeDelayMs = Math.max(0, delayMs);
    const deadline = Date.now() + safeDelayMs;
    const updateCountdown = () => {
      setRemainingSeconds(Math.max(1, Math.ceil((deadline - Date.now()) / 1000)));
    };

    setActive(true);
    updateCountdown();
    intervalRef.current = setInterval(updateCountdown, COUNTDOWN_TICK_MS);
    timeoutRef.current = setTimeout(complete, safeDelayMs);

    return clearTimers;
  }, [clearTimers, complete, delayMs, enabled, resetKey]);

  return { active, remainingSeconds, advance: complete, cancel };
}
