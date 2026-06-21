import { useLocation, useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import type { TopicCompletionDto } from "@/api/types";
import { lessonOriginFrom } from "@/lib/lessonNavigation";
import { SKILL_STEPS, openSkill, type SkillStep } from "@/lib/skillSteps";
import { AppButton, DesignCard } from "@/components/design";
import "./TopicSkillsOverview.css";

type StepState = "done" | "current" | "locked" | "todo" | "retry";

// Each of the 6 skills gets its own distinct accent so the overview reads as six different cards
// instead of a uniform blue. Only the accent hue is fixed here; the soft background tint is derived
// from the accent mixed with the current surface in CSS, so it stays readable in BOTH light and dark
// themes (a hardcoded light tint used to leave light text on a light card in dark mode).
const SKILL_ACCENT: Record<string, string> = {
  vocabulary: "#0f8f83",
  grammar: "#7c3aed",
  reading: "#2563eb",
  writing: "#e45b4f",
  speaking: "#16875d",
  listening: "#b66a08",
};
const accentForSkill = (key: string): string => SKILL_ACCENT[key] || SKILL_ACCENT.vocabulary;

/**
 * A compact, presentational six-stage status list for a single topic, driven by the
 * <see cref="TopicCompletionDto"/> a skill's result screen already returns (K.5). Unlike
 * <c>SkillPath</c> it does not fetch - it renders whatever completion it is handed, so it can be
 * dropped onto any skill's result view to answer "what's done, what's left, where next". The first
 * unlocked-but-unpassed skill is highlighted as the recommended next step and is openable inline.
 */
export function TopicSkillsOverview({
  completion,
  topicId,
  topicTitle,
  className,
  showContinueAction = true,
}: {
  completion: TopicCompletionDto;
  topicId: string;
  topicTitle?: string;
  className?: string;
  showContinueAction?: boolean;
}) {
  const navigate = useNavigate();
  const location = useLocation();
  const origin = lessonOriginFrom(location);

  const passed = new Set(completion.modules.filter((m) => m.passed).map((m) => m.module));
  const attempted = new Set(completion.modules.filter((m) => m.achievedAt).map((m) => m.module));
  const unlocked = new Set(completion.modules.filter((m) => m.unlocked).map((m) => m.module));

  function isUnlocked(step: SkillStep): boolean {
    return unlocked.has(step.module);
  }

  const currentKey = SKILL_STEPS.find((s) => isUnlocked(s) && !passed.has(s.module))?.key;

  function open(step: SkillStep) {
    if (!isUnlocked(step)) return;
    if (passed.has(step.module) && step.key !== currentKey) {
      // Already passed - still let the learner revisit it.
    }
    openSkill(navigate, step, topicId, topicTitle ?? "", origin);
  }

  function stateOf(step: SkillStep): StepState {
    if (passed.has(step.module)) return "done";
    if (!isUnlocked(step)) return "locked";
    if (attempted.has(step.module)) return "retry";
    if (step.key === currentKey) return "current";
    return "todo";
  }

  const nextStep = SKILL_STEPS.find((s) => s.key === currentKey);

  return (
    <DesignCard as="section" className={cn("topic-skills-overview", className)}>
      <div className="topic-skills-overview__heading">
        <div>
          <span className="topic-skills-overview__eyebrow">Mavzu yo‘li</span>
          <h2>{uz.skillPath.overviewTitle}</h2>
        </div>
        <span className="topic-skills-overview__progress">
          {uz.skillPath.progress(completion.passedModuleCount, completion.requiredModuleCount)}
        </span>
      </div>
      <p className="topic-skills-overview__hint">
        {completion.isMastered ? uz.skillPath.moduleCompleteHint : uz.skillPath.overviewHint}
      </p>

      <ul className="topic-skills-overview__grid">
        {SKILL_STEPS.map((step) => {
          const copy = uz.skillPath.steps[step.key];
          const state = stateOf(step);
          const locked = state === "locked";
          const interactive = !locked;
          const accent = accentForSkill(step.key);
          return (
            <li key={step.key}>
              <DesignCard
                as="button"
                type="button"
                disabled={!interactive}
                onClick={() => open(step)}
                padding="sm"
                interactive={interactive}
                className={cn(
                  "topic-skills-overview__step",
                  `is-${state}`,
                  interactive ? "is-interactive" : "cursor-default",
                )}
                style={{ "--skill-accent": accent } as React.CSSProperties}
              >
                <span className="topic-skills-overview__icon">
                  {state === "done" ? (
                    <Icon name="check" className="text-[16px]" />
                  ) : locked ? (
                    <Icon name="lock" className="text-[16px]" />
                  ) : (
                    <Icon name={step.icon} className="text-[16px]" />
                  )}
                </span>
                <span className="topic-skills-overview__copy">
                  <span className="topic-skills-overview__title">
                    {copy.title}
                  </span>
                  <span className="topic-skills-overview__state">
                    {state === "done"
                      ? uz.skillPath.done
                      : state === "current"
                        ? uz.skillPath.next
                        : locked
                          ? uz.skillPath.locked
                          : uz.skillPath.todo}
                  </span>
                </span>
              </DesignCard>
            </li>
          );
        })}
      </ul>

      {/* One-tap jump to the next unfinished skill. */}
      {showContinueAction && !completion.isMastered && nextStep && (
        <AppButton
          onClick={() => open(nextStep)}
          trailingIcon="arrow_forward"
          className="topic-skills-overview__continue"
        >
          <span className="topic-skills-overview__continue-copy">
            <small>{uz.skillPath.continueNext}</small>
            <span aria-hidden className="topic-skills-overview__continue-space">&nbsp;</span>
            <strong>{uz.skillPath.steps[nextStep.key].title}</strong>
          </span>
        </AppButton>
      )}
    </DesignCard>
  );
}
