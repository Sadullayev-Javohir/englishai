import { useState } from "react";
import { uz } from "@/content/uz";
import { ErrorCategory, ProgressInsightSource, SkillType } from "@/api/types";
import type { ProgressErrorDetailDto, ProgressInsightDto } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import { errorCategoryLabel, formatDate, skillLabel } from "@/lib/labels";

const SKILL_ROUTE: Record<SkillType, string> = {
  [SkillType.Speaking]: "/app/speaking",
  [SkillType.Listening]: "/listening",
  [SkillType.Reading]: "/reading",
  [SkillType.Writing]: "/writing",
  [SkillType.Grammar]: "/app/grammar",
  [SkillType.Vocabulary]: "/app/vocabulary/topics",
};

/** Single source of truth for the "skill to strengthen" headline. When the page passes the
 * lowest visible skill score, the AI panel agrees with the on-screen scores instead of
 * naming a different skill from the backend list. */
export interface FocusSkill {
  skill: SkillType;
  score: number;
}

/** Each recurring-error type routes to the place that actually helps fix it — targeted practice,
 * not a generic catalog: pronunciation → word drills, vocabulary/spelling → SRS review. */
function errorRoute(category: ErrorCategory): string {
  if (category === ErrorCategory.Pronunciation) return "/app/speaking/practice-words";
  if (category === ErrorCategory.Vocabulary || category === ErrorCategory.Spelling) return "/app/vocabulary/review";
  return "/app/grammar";
}

/** Picks the most informative recent example for an error category: one that shows the actual
 * mistake (learner vs expected answer) if available, otherwise one with a prompt. */
function bestExample(examples: ProgressErrorDetailDto[] | undefined): ProgressErrorDetailDto | null {
  if (!examples || examples.length === 0) return null;
  return examples.find((ex) => ex.expectedAnswer && ex.learnerAnswer) ?? examples.find((ex) => ex.prompt) ?? null;
}

/** The concrete "you wrote X, the answer is Y" line that turns an abstract error count into
 * something the learner can actually learn from. */
function MissAnswers({ example }: { example: ProgressErrorDetailDto }) {
  if (!example.learnerAnswer && !example.expectedAnswer) return null;
  const ai = uz.progress.ai;
  return (
    <span className="pp-miss__answers">
      {example.learnerAnswer && <span className="pp-miss__wrong"><Icon name="close" className="text-[14px]" />{ai.yourAnswer}: <b>{example.learnerAnswer}</b></span>}
      {example.expectedAnswer && <span className="pp-miss__right"><Icon name="check" className="text-[14px]" />{ai.correctAnswer}: <b>{example.expectedAnswer}</b></span>}
    </span>
  );
}

export function AiInsightPanel({ insight, onNavigate, focusSkill = null }: {
  insight: ProgressInsightDto;
  onNavigate: (route: string) => void;
  focusSkill?: FocusSkill | null;
}) {
  const [expanded, setExpanded] = useState(false);
  const ai = uz.progress.ai;
  const { snapshot, insight: analysis } = insight;
  // Prefer the score the learner can see on this page (lowest of the six skills); fall back to
  // the backend's prioritised list only when the page does not supply one.
  const headlineSkill: FocusSkill | null =
    focusSkill ??
    (analysis.skillsToStrengthen[0]
      ? { skill: analysis.skillsToStrengthen[0].skill, score: analysis.skillsToStrengthen[0].score }
      : null);
  const primaryError = analysis.recurringErrors[0] ?? null;
  const primaryExample = primaryError
    ? bestExample(snapshot.errorsLast30Days.find((item) => item.category === primaryError.category)?.recentExamples)
    : null;
  const isLive = analysis.source === ProgressInsightSource.Hermes && !analysis.isFallback;
  const hasErrorDetail = analysis.recurringErrors.length > 0;

  return (
    <section className="pp-ai pp-panel" aria-labelledby="progress-ai-heading">
      <div className="pp-ai__top">
        <div className="pp-ai__id">
          <span className="pp-ai__badge"><Icon name="auto_awesome" className="text-[25px]" /></span>
          <div className="min-w-0">
            <div className="pp-ai__meta">
              <span className={cn("pp-ai__status", isLive ? "is-live" : "is-fallback")}>
                <span className="pp-ai__dot" />
                {isLive ? ai.poweredBy : ai.fallback}
              </span>
              <span className="pp-ai__stamp">{ai.generatedAt(formatDate(insight.generatedAt.slice(0, 10)))}</span>
            </div>
            <h2 id="progress-ai-heading">{ai.heading}</h2>
            <p className="pp-ai__lead">{ai.overall(analysis.overallCode)}</p>
          </div>
        </div>
        <button type="button" className="pp-cta" onClick={() => onNavigate(analysis.targetRoute)}>
          {ai.startAction}
          <Icon name="arrow_forward" />
        </button>
      </div>

      <div className="pp-ai__cards">
        {headlineSkill && (
          <button type="button" className="pp-ai-card" onClick={() => onNavigate(SKILL_ROUTE[headlineSkill.skill])}>
            <small><Icon name="target" className="text-[16px]" style={{ color: "var(--pp-primary)" }} />{ai.focusSkills}</small>
            <span className="pp-ai-card__val"><strong>{skillLabel(headlineSkill.skill)}</strong><b>{Math.round(headlineSkill.score)}/100</b></span>
            <span className="pp-ai-card__go">{ai.openSkill}<Icon name="arrow_forward" className="text-[15px]" /></span>
          </button>
        )}
        {primaryError && (
          <button type="button" className="pp-ai-card" onClick={() => onNavigate(errorRoute(primaryError.category))}>
            <small><Icon name="error" className="text-[16px]" style={{ color: "var(--pp-danger)" }} />{ai.recurringErrors}</small>
            <span className="pp-ai-card__val"><strong>{errorCategoryLabel(primaryError.category)}</strong><b className="is-danger">{ai.errorTimes(primaryError.countLast30Days)}</b></span>
            {primaryExample?.expectedAnswer ? (
              <span className="pp-ai-card__miss"><s>{primaryExample.learnerAnswer}</s><Icon name="arrow_forward" className="text-[13px]" /><b>{primaryExample.expectedAnswer}</b></span>
            ) : (
              <span className="pp-ai-card__go">{ai.openSkill}<Icon name="arrow_forward" className="text-[15px]" /></span>
            )}
          </button>
        )}
        <div className="pp-ai-card" data-static="true">
          <small><Icon name="routine" className="text-[16px]" style={{ color: "var(--pp-primary)" }} />{ai.studyHabit}</small>
          <p className="pp-ai-card__habit">{ai.habit(analysis.habitCode)}</p>
        </div>
      </div>

      {hasErrorDetail && (
        <>
          <button type="button" className="pp-ai__toggle" aria-expanded={expanded} onClick={() => setExpanded((value) => !value)}>
            <Icon name={expanded ? "expand_less" : "expand_more"} className="text-[20px]" />
            {expanded ? ai.hideDetails : ai.showDetails}
          </button>

          {expanded && (
            <div className="pp-ai__misses">
              {analysis.recurringErrors.map((error) => {
                const example = bestExample(snapshot.errorsLast30Days.find((item) => item.category === error.category)?.recentExamples);
                return (
                  <button key={error.category} type="button" className="pp-miss" onClick={() => onNavigate(errorRoute(error.category))}>
                    <div className="pp-miss__head">
                      <span className="pp-ai-row__icon is-danger"><Icon name="error_outline" className="text-[18px]" /></span>
                      <strong>{errorCategoryLabel(error.category)}</strong>
                      <span className="pp-ai-row__tag is-danger">{ai.errorCount(error.countLast30Days)}</span>
                    </div>
                    {example && (
                      <div className="pp-miss__body">
                        {example.prompt && <p className="pp-miss__q">{example.prompt}</p>}
                        <MissAnswers example={example} />
                      </div>
                    )}
                    <span className="pp-miss__cta">{ai.openSkill}<Icon name="arrow_forward" className="text-[15px]" /></span>
                  </button>
                );
              })}
            </div>
          )}
        </>
      )}
    </section>
  );
}
