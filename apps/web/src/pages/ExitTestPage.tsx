import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { motion, useReducedMotion } from "framer-motion";
import { uz } from "@/content/uz";
import { getLearnerId, getStoredLevel } from "@/app/session";
import { CefrLevel } from "@/api/types";
import { cefrShort } from "@/lib/labels";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { AdaptiveTestRunner } from "@/components/AdaptiveTestRunner";
import { ParrotLogo } from "@/components/ParrotLogo";
import { api } from "@/api/client";
import { enterSecureAssessment, exitSecureAssessment, supportsFullscreenAssessment } from "@/lib/secureAssessment";
import type { SecureAssessmentMode } from "@/lib/secureAssessment";
import "./ExitTestPage.css";

const ACTIVE_EXIT_TEST_SESSION_KEY = "englishai.exit-test.session";

const POINTS = [
  { icon: "schedule", title: uz.levelMap.exitPointTimeTitle, text: uz.levelMap.exitPointTimeText, tone: "green" },
  { icon: "school", title: uz.levelMap.exitPointSkillTitle, text: uz.levelMap.exitPointSkillText, tone: "purple" },
  { icon: "trending_up", title: uz.levelMap.exitPointLevelTitle, text: uz.levelMap.exitPointLevelText, tone: "blue" },
] as const;

const container = { hidden: {}, show: { transition: { staggerChildren: 0.07, delayChildren: 0.08 } } };
const item = {
  hidden: { opacity: 0, y: 16 },
  show: { opacity: 1, y: 0, transition: { type: "spring" as const, stiffness: 260, damping: 25 } },
};

export function ExitTestPage() {
  const navigate = useNavigate();
  const reduceMotion = useReducedMotion();
  const [searchParams] = useSearchParams();
  const [started, setStarted] = useState(false);
  const [secureError, setSecureError] = useState(false);
  // Fullscreen where the browser has it, kiosk lock everywhere else (see lib/kioskLock.ts).
  const [secureMode, setSecureMode] = useState<SecureAssessmentMode>("fullscreen");
  const cached = getStoredLevel();
  const requestedLevel = Number(searchParams.get("level"));
  const previewLevel = requestedLevel >= CefrLevel.A1 && requestedLevel <= CefrLevel.C1
    ? requestedLevel as CefrLevel
    : null;
  const currentLevel: CefrLevel = previewLevel ?? (cached != null ? (Math.min(Math.max(cached, 1), 6) as CefrLevel) : CefrLevel.A1);
  const nextLevel = Math.min(currentLevel + 1, 6) as CefrLevel;

  if (started) {
    return (
      <AdaptiveTestRunner
        variant="exit-test"
        secure
        secureMode={secureMode}
        getStoredSessionId={() => sessionStorage.getItem(ACTIVE_EXIT_TEST_SESSION_KEY)}
        persistSessionId={(sessionId) => {
          if (sessionId) sessionStorage.setItem(ACTIVE_EXIT_TEST_SESSION_KEY, sessionId);
          else sessionStorage.removeItem(ACTIVE_EXIT_TEST_SESSION_KEY);
        }}
        start={async () => {
          const response = await api.levels.exitTest.start(getLearnerId(), true, previewLevel ?? undefined);
          return { sessionId: response.sessionId, firstItem: response.firstItem };
        }}
        onComplete={async (sessionId) => {
          const result = await api.levels.exitTest.finalize(sessionId);
          navigate("/levels/exit-test/result", { state: { result }, replace: true });
        }}
        onClose={() => navigate("/levels")}
      />
    );
  }

  return (
    <main className="exit-test-page">
      <motion.section className="exit-test-intro" variants={container} initial={reduceMotion ? "show" : "hidden"} animate="show" aria-labelledby="exit-test-title">
        <motion.div variants={item} className="exit-test-intro__brand" aria-hidden="true"><ParrotLogo size={76} /></motion.div>
        <motion.div variants={item} className="exit-test-intro__copy">
          <span className="exit-test-intro__eyebrow">{uz.levelMap.exitTitle}</span>
          <h1 id="exit-test-title">{uz.levelMap.exitIntroTitle}</h1>
          <p>{uz.levelMap.exitIntroSubtitle}</p>
        </motion.div>
        <motion.section variants={item} className="exit-test-levels" aria-label={uz.levelMap.exitStakesTitle}>
          <div><span>{uz.levelMap.exitCurrentLabel}</span><strong>{cefrShort(currentLevel)}</strong></div>
          <span className="exit-test-levels__arrow" aria-hidden="true"><Icon name="arrow_forward" /></span>
          <div className="exit-test-levels__next"><span>{uz.levelMap.exitNextLabel}</span><strong>{cefrShort(nextLevel)}</strong></div>
        </motion.section>
        <motion.div variants={item} className="exit-test-points">
          {POINTS.map((point) => (
            <article key={point.title} className={`exit-test-point exit-test-point--${point.tone}`}>
              <span className="exit-test-point__icon" aria-hidden="true"><Icon name={point.icon} /></span>
              <div><h2>{point.title}</h2><p>{point.text}</p></div>
            </article>
          ))}
        </motion.div>
        <motion.div variants={item} className="exit-test-intro__actions">
          {/* Phones without the Fullscreen API run the kiosk lock instead; say what that means. */}
          {!supportsFullscreenAssessment() ? <p>{uz.placement.secure.kioskHint}</p> : null}
          {secureError ? <p role="alert">{uz.placement.secure.startError}</p> : null}
          <Button fullWidth size="lg" icon="fullscreen" onClick={async () => {
            setSecureError(false);
            try {
              setSecureMode(await enterSecureAssessment());
            } catch {
              setSecureError(true);
              return;
            }
            setStarted(true);
          }}>{sessionStorage.getItem(ACTIVE_EXIT_TEST_SESSION_KEY) ? uz.placement.secure.continueCta : uz.levelMap.exitStartCta}</Button>
          <Button fullWidth size="lg" variant="ghost" icon="arrow_back" onClick={async () => { await exitSecureAssessment(); navigate("/levels"); }}>{uz.common.back}</Button>
        </motion.div>
      </motion.section>
    </main>
  );
}
