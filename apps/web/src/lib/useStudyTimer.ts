import { useEffect, useRef } from "react";
import { useLocation } from "react-router-dom";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { SkillType } from "@/api/types";

// How often accumulated study time is flushed to the backend while the learner stays on a page.
const FLUSH_INTERVAL_SECONDS = 30;

// Maps the active route to the skill its study time should be credited to. Returns null on pages
// that are not active practice (Home, Progress, Profile, …) so idle time there is never counted.
function routeSkill(pathname: string): SkillType | null {
  if (pathname.startsWith("/app/speaking")) return SkillType.Speaking;
  if (pathname.startsWith("/listening")) return SkillType.Listening;
  if (pathname.startsWith("/reading")) return SkillType.Reading;
  if (pathname.startsWith("/writing")) return SkillType.Writing;
  if (pathname.startsWith("/app/grammar")) return SkillType.Grammar;
  if (pathname.startsWith("/app/vocabulary")) return SkillType.Vocabulary;
  // Video and Listening are distinct activities: watching a video lesson must NOT be credited to
  // the Listening skill, or it would inflate the learner's Listening stats. Video has no dedicated
  // skill bucket, so its time is tracked separately (i.e. not folded into any core skill here).
  // Books, however, are genuine reading practice.
  if (pathname.startsWith("/video")) return null;
  if (pathname.startsWith("/books")) return SkillType.Reading;
  return null;
}

/** The learner's local calendar day as YYYY-MM-DD (so day boundaries match their own clock). */
function localDate(): string {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

/**
 * Tracks how long the learner actively spends on each practice page and reports it to the backend
 * (PROJECT-SPEC Faza 2 analitika). Counts a second only while the tab is visible and on a practice
 * route; flushes every 30s and on hide/close (via sendBeacon, which survives unload). Mounted once
 * in the app shell so it spans every screen.
 */
export function useStudyTimer(): void {
  const location = useLocation();
  const learnerId = getLearnerId();

  // Seconds accumulated but not yet sent, keyed by skill. A ref so the ticker mutates it without
  // re-rendering. Keyed by skill so switching pages mid-flush still attributes time correctly.
  const pendingRef = useRef<Map<SkillType, number>>(new Map());
  const skillRef = useRef<SkillType | null>(routeSkill(location.pathname));

  // Keep the active skill current without restarting the ticker on every navigation.
  skillRef.current = routeSkill(location.pathname);

  useEffect(() => {
    const pending = pendingRef.current;

    const flush = (useBeacon: boolean) => {
      for (const [skill, seconds] of pending) {
        if (seconds <= 0) continue;
        if (useBeacon) api.learning.studyTimeBeacon(learnerId, skill, seconds, localDate());
        else void api.learning.recordStudyTime(learnerId, skill, seconds, localDate()).catch(() => {});
      }
      pending.clear();
    };

    let sinceFlush = 0;
    const ticker = window.setInterval(() => {
      const skill = skillRef.current;
      if (document.visibilityState !== "visible" || skill === null) return;

      pending.set(skill, (pending.get(skill) ?? 0) + 1);
      sinceFlush += 1;
      if (sinceFlush >= FLUSH_INTERVAL_SECONDS) {
        flush(false);
        sinceFlush = 0;
      }
    }, 1000);

    // When the tab is hidden or the page is being unloaded, send what we have via sendBeacon so
    // the time isn't lost. visibilitychange covers tab switches and most mobile backgrounding.
    const onHide = () => {
      if (document.visibilityState === "hidden") {
        flush(true);
        sinceFlush = 0;
      }
    };
    const onPageHide = () => flush(true);

    document.addEventListener("visibilitychange", onHide);
    window.addEventListener("pagehide", onPageHide);

    return () => {
      window.clearInterval(ticker);
      document.removeEventListener("visibilitychange", onHide);
      window.removeEventListener("pagehide", onPageHide);
      flush(true);
    };
  }, [learnerId]);
}
