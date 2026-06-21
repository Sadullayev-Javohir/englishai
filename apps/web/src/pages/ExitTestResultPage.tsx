import { useEffect, useMemo } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import type { FinalizeLevelExitTestResult } from "@/api/types";
import { TestStage } from "@/api/types";
import { setStoredLevel } from "@/app/session";
import { cefrShort, cefrLong } from "@/lib/labels";
import { DuoButton } from "@/components/game/DuoButton";
import { Icon } from "@/components/ui/Icon";
import { Confetti } from "@/lesson/Confetti";
import "./ExitTestResultPage.css";

type StageTone = "green" | "yellow" | "blue" | "purple" | "orange";

const STAGE_META: { stage: TestStage; label: string; icon: string; tone: StageTone }[] = [
  { stage: TestStage.Vocabulary, label: uz.placement.stageLabels.vocabulary, icon: "menu_book", tone: "green" },
  { stage: TestStage.Grammar, label: uz.placement.stageLabels.grammar, icon: "rule", tone: "yellow" },
  { stage: TestStage.Listening, label: uz.placement.stageLabels.listening, icon: "headphones", tone: "blue" },
  { stage: TestStage.Reading, label: uz.placement.stageLabels.reading, icon: "auto_stories", tone: "purple" },
  { stage: TestStage.Writing, label: uz.placement.stageLabels.writing, icon: "edit", tone: "orange" },
  { stage: TestStage.Speaking, label: uz.placement.stageLabels.speaking, icon: "mic", tone: "purple" },
];

const fadeUp = (index: number) => ({
  initial: { opacity: 0, y: 18 },
  animate: { opacity: 1, y: 0 },
  transition: { delay: 0.08 + index * 0.06, duration: 0.38, ease: "easeOut" },
});

export function ExitTestResultPage() {
  const location = useLocation();
  const result = (location.state as { result?: FinalizeLevelExitTestResult } | null)?.result;

  if (!result) {
    const t = uz.levelMap.exitResult;
    return (
      <main className="exit-result-page exit-result-page--missing">
        <section role="alert" className="exit-result-missing">
          <span className="exit-result-missing__icon"><Icon name="info" className="text-[30px]" /></span>
          <p className="exit-result-eyebrow">Chiqish testi</p>
          <h1>{t.missingTitle}</h1>
          <p className="exit-result-missing__copy">{t.missingText}</p>
          <Link className="exit-result-link-button" to="/levels">
            <Icon name="map" className="text-[20px]" />
            {t.missingBack}
          </Link>
        </section>
      </main>
    );
  }

  return <ExitTestResultContent result={result} />;
}

function ExitTestResultContent({ result }: { result: FinalizeLevelExitTestResult }) {
  const navigate = useNavigate();

  useEffect(() => {
    if (result.advanced) setStoredLevel(result.newLevel);
  }, [result.advanced, result.newLevel]);

  const t = uz.levelMap.exitResult;
  const stageMap = useMemo(
    () => new Map(result.result.stageResults.map((stageResult) => [stageResult.stage, stageResult])),
    [result.result.stageResults],
  );
  const failedStages = new Set(result.failedStages ?? []);
  const subtitle = result.advanced
    ? t.advancedSubtitle(cefrShort(result.newLevel))
    : result.passed
      ? t.passedNotAdvancedSubtitle(result.masteredSkillCount, result.requiredMasteredSkills)
      : t.failedSubtitle;
  const statusLabel = result.advanced
    ? "Yangi daraja ochildi"
    : result.passed
      ? "Testdan o'tildi"
      : "Mashq tavsiya qilinadi";
  const recommendation = result.advanced
    ? `${cefrShort(result.newLevel)} daraja yo'li ochildi. Yangi mavzularni darajalar xaritasidan davom ettiring.`
    : result.passed
      ? `Natija saqlandi. Darajani oshirish uchun yana ${Math.max(0, result.requiredMasteredSkills - result.masteredSkillCount)} ta ko'nikmani mustahkamlang.`
      : `${cefrShort(result.testedLevel)} mavzularini takrorlang, past natijali ko'nikmalarni mashq qiling va testni qayta topshiring.`;

  return (
    <main className={`exit-result-page ${result.passed ? "is-passed" : "is-failed"}`}>
      {result.passed && <Confetti fireKey={1} count={120} className="exit-result-confetti" />}
      <div className="exit-result-shell">
        <motion.section {...fadeUp(0)} className="exit-result-hero">
          <div className="exit-result-hero__copy">
            <span className="exit-result-status">
              <Icon name={result.passed ? "verified" : "refresh"} className="text-[18px]" />
              {statusLabel}
            </span>
            <p className="exit-result-eyebrow">Chiqish testi natijasi</p>
            <h1>{result.passed ? t.passedTitle : t.failedTitle}</h1>
            <p className="exit-result-hero__subtitle">{subtitle}</p>
            <div className="exit-result-hero__actions">
              <DuoButton color={result.passed ? "green" : "blue"} size="lg" icon="map" onClick={() => navigate("/levels")}>
                {t.backToLevels}
              </DuoButton>
              {!result.passed && (
                <DuoButton color="purple" size="lg" icon="refresh" onClick={() => navigate("/levels/exit-test")}>
                  {t.retry}
                </DuoButton>
              )}
            </div>
          </div>
          <div className="exit-result-level" aria-label={`Umumiy natija ${result.result.overallScore}%`}>
            <span className="exit-result-level__label">Umumiy natija</span>
            <strong>{result.result.overallScore}%</strong>
            <span className="exit-result-level__cefr">{cefrLong(result.result.overallLevel)}</span>
            <div className="exit-result-level__track" aria-hidden="true">
              <span style={{ width: `${Math.max(0, Math.min(100, result.result.overallScore))}%` }} />
            </div>
          </div>
        </motion.section>

        <motion.section {...fadeUp(1)} className="exit-result-stats" aria-label="Natija statistikasi">
          <article className="exit-result-stat exit-result-stat--green"><span className="exit-result-stat__icon"><Icon name="target" className="text-[24px]" /></span><span><small>O'tish chegarasi</small><strong>{result.minimumOverallScore}%</strong></span></article>
          <article className="exit-result-stat exit-result-stat--blue"><span className="exit-result-stat__icon"><Icon name="workspace_premium" className="text-[24px]" /></span><span><small>Joriy daraja</small><strong>{cefrShort(result.newLevel)}</strong></span></article>
          <article className="exit-result-stat exit-result-stat--purple"><span className="exit-result-stat__icon"><Icon name="bolt" className="text-[24px]" /></span><span><small>Mustahkam ko'nikma</small><strong>{result.masteredSkillCount}/{result.requiredMasteredSkills}</strong></span></article>
        </motion.section>

        <div className="exit-result-grid">
          <motion.section {...fadeUp(2)} className="exit-result-card exit-result-breakdown">
            <header className="exit-result-section-header">
              <div><p className="exit-result-eyebrow">Batafsil tahlil</p><h2>{t.breakdownTitle}</h2></div>
              <span className="exit-result-section-badge">6 ko'nikma</span>
            </header>
            <div className="exit-result-skill-list">
              {STAGE_META.map((meta) => {
                const stageResult = stageMap.get(meta.stage);
                if (!stageResult) return null;
                const score = Math.max(0, Math.min(100, stageResult.score));
                const minimum = meta.stage === TestStage.Writing || meta.stage === TestStage.Speaking
                  ? result.productiveStageFloor
                  : result.minimumStageScore;
                const failed = failedStages.has(meta.stage);
                return (
                  <article key={meta.stage} className={`exit-result-skill exit-result-skill--${meta.tone}${failed ? " is-failed" : " is-passed"}`}>
                    <span className="exit-result-skill__icon"><Icon name={meta.icon} className="text-[22px]" /></span>
                    <span className="exit-result-skill__copy">
                      <span><strong>{meta.label}</strong><small>{cefrShort(stageResult.level)} · min {minimum}%</small></span>
                      <span className="exit-result-skill__track" aria-hidden="true"><span style={{ width: `${score}%` }} /></span>
                    </span>
                    <strong className="exit-result-skill__score">{failed ? "↓ " : "✓ "}{score}%</strong>
                  </article>
                );
              })}
            </div>
          </motion.section>

          <motion.aside {...fadeUp(3)} className="exit-result-card exit-result-recommendation">
            <span className="exit-result-recommendation__icon"><Icon name={result.advanced ? "rocket_launch" : result.passed ? "trending_up" : "school"} className="text-[28px]" /></span>
            <p className="exit-result-eyebrow">Tavsiya</p>
            <h2>{result.advanced ? "Keyingi darajani boshlang" : result.passed ? "Ko'nikmalarni mustahkamlang" : "Qayta urinishga tayyorlaning"}</h2>
            <p>{recommendation}</p>
            <div className="exit-result-recommendation__steps">
              <div><Icon name="check_circle" className="text-[20px]" /><span><strong>Natija saqlandi</strong><small>Progress profilingizda yangilandi</small></span></div>
              <div><Icon name="conversion_path" className="text-[20px]" /><span><strong>Aniq keyingi qadam</strong><small>Darajalar xaritasidan davom eting</small></span></div>
            </div>
            <DuoButton color={result.passed ? "green" : "blue"} size="lg" fullWidth icon="map" onClick={() => navigate("/levels")}>
              {t.backToLevels}
            </DuoButton>
          </motion.aside>
        </div>
      </div>
    </main>
  );
}
