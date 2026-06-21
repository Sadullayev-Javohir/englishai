import { useCallback, useEffect, useRef, useState } from "react";
import { api, apiErrorDetails } from "@/api/client";
import { exitFullscreenIfActive, fullscreenElement, fullscreenSupported, onFullscreenChange, requestFullscreenOn } from "./fullscreen";
import { applyKioskLock, releaseKioskLock } from "./kioskLock";

/**
 * Server-side allowlist (ReportPlacementIntegrityViolationCommandValidator) - do not extend
 * without changing the API first. Kiosk-mode signals map onto `visibility_hidden`.
 */
export type AssessmentViolationReason = "visibility_hidden" | "window_blur" | "fullscreen_exit";

/**
 * How the test surface is secured. `fullscreen` is the real Fullscreen API; `kiosk` is the
 * fallback for browsers that have none (iOS Safari/WKWebView, locked-down WebViews), where the
 * document is pinned to the viewport instead - see kioskLock.ts.
 */
export type SecureAssessmentMode = "fullscreen" | "kiosk";

/** Whether the real Fullscreen API can be used to secure the test. */
export function supportsFullscreenAssessment(): boolean {
  return fullscreenSupported();
}

/**
 * A test can now be secured on every device: fullscreen where available, kiosk everywhere else.
 * Kept as an export because callers and tests read it as "the secure flow is available".
 */
export function supportsSecureAssessment(): boolean {
  return typeof document !== "undefined";
}

/**
 * Touch-primary devices raise `window blur` for the on-screen keyboard, the notification shade
 * and browser toolbars, none of which are cheating - blur is only trusted on pointer devices.
 */
export function isTouchPrimaryDevice(): boolean {
  if (typeof window === "undefined") return false;
  if (window.matchMedia?.("(pointer: coarse)").matches) return true;
  return (navigator.maxTouchPoints ?? 0) > 0;
}

/**
 * Enters the strongest secure mode the browser supports. Never fails: a browser without the
 * Fullscreen API falls back to the kiosk lock rather than blocking the learner from testing.
 */
export async function enterSecureAssessment(): Promise<SecureAssessmentMode> {
  if (typeof document === "undefined") return "kiosk";
  if (await requestFullscreenOn(document.documentElement)) {
    releaseKioskLock();
    return "fullscreen";
  }
  applyKioskLock();
  return "kiosk";
}

export async function exitSecureAssessment(): Promise<void> {
  releaseKioskLock();
  await exitFullscreenIfActive();
}

export function isIntegrityApiError(error: unknown): boolean {
  return apiErrorDetails(error)?.code === "placement_integrity_violation";
}

type SecureAssessmentState = {
  warningOpen: boolean;
  invalidated: boolean;
  reporting: boolean;
  returnToTest: () => Promise<void>;
  leaveSecureAssessment: () => Promise<void>;
  markInvalidated: () => void;
  /**
   * Runs an action that the browser itself answers with a fullscreen exit - today the
   * microphone permission prompt on the Speaking task, which Chrome shows only after
   * dropping the page out of fullscreen. Backgrounding stays monitored throughout.
   */
  withoutFullscreenWatch: <T>(action: () => Promise<T>) => Promise<T>;
};

export function useSecureAssessment(
  sessionId: string | null,
  active: boolean,
  onInvalidated?: () => void,
  mode: SecureAssessmentMode = "fullscreen",
): SecureAssessmentState {
  const [warningOpen, setWarningOpen] = useState(false);
  const [invalidated, setInvalidated] = useState(false);
  const [reporting, setReporting] = useState(false);
  const activeRef = useRef(active);
  const suppressionRef = useRef(false);
  const fullscreenWatchSuspendedRef = useRef(false);
  const incidentOpenRef = useRef(false);
  const reportSucceededRef = useRef(false);
  const invalidatedCallbackRef = useRef(onInvalidated);

  useEffect(() => { activeRef.current = active; }, [active]);
  useEffect(() => { invalidatedCallbackRef.current = onInvalidated; }, [onInvalidated]);
  // Safety net: a browser back press mid-test unmounts the page without running any of the
  // navigation handlers below, and a stranded kiosk lock would leave the whole app pinned.
  useEffect(() => () => releaseKioskLock(), []);

  const markInvalidated = useCallback(() => {
    activeRef.current = false;
    setInvalidated(true);
    setWarningOpen(false);
    invalidatedCallbackRef.current?.();
  }, []);

  const reportViolation = useCallback(async (reason: AssessmentViolationReason) => {
    if (!activeRef.current || !sessionId || suppressionRef.current || incidentOpenRef.current || invalidated) return;
    incidentOpenRef.current = true;
    setWarningOpen(true);
    setReporting(true);
    try {
      const result = await api.placement.reportIntegrityViolation(sessionId, crypto.randomUUID(), reason);
      reportSucceededRef.current = true;
      if (result.invalidated) {
        markInvalidated();
      }
    } catch (error) {
      if (isIntegrityApiError(error)) {
        markInvalidated();
      }
    } finally {
      setReporting(false);
    }
  }, [invalidated, markInvalidated, sessionId]);

  useEffect(() => {
    if (!active || !sessionId) return;
    const cleanups: Array<() => void> = [];

    // Backgrounding the app/tab is the signal that works on every platform, so it is the one
    // signal every mode relies on. `pagehide` covers the iOS cases where visibilitychange is
    // skipped; the incident dedup collapses the two into one report.
    const onVisibility = () => {
      if (document.visibilityState === "hidden") void reportViolation("visibility_hidden");
    };
    const onPageHide = () => void reportViolation("visibility_hidden");
    document.addEventListener("visibilitychange", onVisibility);
    window.addEventListener("pagehide", onPageHide);
    cleanups.push(() => {
      document.removeEventListener("visibilitychange", onVisibility);
      window.removeEventListener("pagehide", onPageHide);
    });

    if (!isTouchPrimaryDevice()) {
      const onBlur = () => void reportViolation("window_blur");
      window.addEventListener("blur", onBlur);
      cleanups.push(() => window.removeEventListener("blur", onBlur));
    }

    // Leaving fullscreen only means cheating on a pointer device. Android Chrome drops
    // fullscreen by itself for the soft keyboard (Writing) and for permission prompts
    // (Speaking), so on phones this signal punished the last two questions of the test
    // rather than any actual tab-switch. Backgrounding still covers those devices.
    if (mode === "fullscreen" && !isTouchPrimaryDevice()) {
      cleanups.push(onFullscreenChange(() => {
        if (fullscreenWatchSuspendedRef.current) return;
        if (!fullscreenElement()) void reportViolation("fullscreen_exit");
      }));
    }

    return () => { for (const cleanup of cleanups) cleanup(); };
  }, [active, mode, reportViolation, sessionId]);

  const returnToTest = useCallback(async () => {
    if (reporting || !reportSucceededRef.current) return;
    suppressionRef.current = true;
    const resumedMode = await enterSecureAssessment();
    suppressionRef.current = false;
    fullscreenWatchSuspendedRef.current = resumedMode !== "fullscreen";
    incidentOpenRef.current = false;
    reportSucceededRef.current = false;
    setWarningOpen(false);
  }, [reporting]);

  const leaveSecureAssessment = useCallback(async () => {
    activeRef.current = false;
    suppressionRef.current = true;
    await exitSecureAssessment();
  }, []);

  const withoutFullscreenWatch = useCallback(async <T,>(action: () => Promise<T>): Promise<T> => {
    fullscreenWatchSuspendedRef.current = true;
    try {
      return await action();
    } finally {
      // The prompt usually leaves the page outside fullscreen and the user gesture that
      // could re-request it is spent. Fall back to the kiosk lock and keep the watch off
      // rather than firing a violation the learner did not cause.
      if (fullscreenElement()) fullscreenWatchSuspendedRef.current = false;
      else applyKioskLock();
    }
  }, []);

  return {
    warningOpen,
    invalidated,
    reporting,
    returnToTest,
    leaveSecureAssessment,
    markInvalidated,
    withoutFullscreenWatch,
  };
}
