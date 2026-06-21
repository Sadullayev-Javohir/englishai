import { useLocation, useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import { SKILL_STEPS, openSkill, type SkillStep } from "@/lib/skillSteps";
import { lessonOriginFrom } from "@/lib/lessonNavigation";

type Step = SkillStep;
const STEPS = SKILL_STEPS;

type StepState = "done" | "current" | "locked" | "todo";

type SkillColor = {
  face: string;
};
const SKILL_COLOR: Record<string, SkillColor> = {
  vocabulary: { face: "bg-ea-primary" },
  grammar: { face: "bg-ea-primary" },
  reading: { face: "bg-ea-primary" },
  writing: { face: "bg-ea-primary" },
  speaking: { face: "bg-ea-primary" },
  listening: { face: "bg-ea-primary" },
};
const colorForSkill = (key: string): SkillColor => SKILL_COLOR[key] || SKILL_COLOR.vocabulary;

/**
 * Cross-skill hub shown on a vocabulary topic page: it links every skill back to THIS topic and
 * tracks the learner's six-module mastery (K.5). Each skill shows done / current / not-started, the
 * first unfinished stage is highlighted as the recommended next step, and once all six are passed a
 * "topic complete" banner appears. <c>refreshKey</c> lets the host page force a re-fetch after the
 * learner finishes a module on the same page (e.g. the vocabulary quiz).
 */
export function SkillPath({
  topicId,
  topicTitle,
  refreshKey = 0,
}: {
  topicId: string;
  topicTitle: string;
  refreshKey?: number;
}) {
  const navigate = useNavigate();
  const location = useLocation();
  const origin = lessonOriginFrom(location);
  const learnerId = getLearnerId();

  const { data: completion } = useAsync(
    () => api.vocabulary.topicCompletion(topicId, learnerId),
    [topicId, learnerId, refreshKey],
  );

  const passed = new Set(
    (completion?.modules ?? []).filter((m) => m.passed).map((m) => m.module),
  );
  // Sequential gating (K.5): a skill is unlocked only once every earlier skill is passed. The
  // backend computes this; if the completion hasn't loaded yet, treat only vocabulary as unlocked.
  const unlocked = new Set(
    (completion?.modules ?? []).filter((m) => m.unlocked).map((m) => m.module),
  );
  function isUnlocked(step: Step): boolean {
    if (step.key === "vocabulary") return true;
    return completion ? unlocked.has(step.module) : false;
  }
  // The first unlocked, not-yet-passed skill is the suggested next step.
  const currentKey = STEPS.find((s) => isUnlocked(s) && !passed.has(s.module))?.key;
  const allDone = completion?.isMastered ?? false;

  function open(step: Step) {
    // Vocabulary is the page we're already on.
    if (step.key === "vocabulary") return;
    // A locked skill cannot be opened until the earlier skills are passed.
    if (!isUnlocked(step)) return;
    openSkill(navigate, step, topicId, topicTitle, origin);
  }

  function stateOf(step: Step): StepState {
    if (passed.has(step.module)) return "done";
    if (!isUnlocked(step)) return "locked";
    if (step.key === currentKey) return "current";
    return "todo";
  }

  return (
    <section className="space-y-sm">
      <div className="flex items-center justify-between gap-sm">
        <h2 className="font-headline-md text-headline-md text-primary">{uz.skillPath.title}</h2>
        {completion && (
          <span className="font-caption text-caption text-text-secondary shrink-0">
            {uz.skillPath.progress(completion.passedModuleCount, completion.requiredModuleCount)}
          </span>
        )}
      </div>
      <p className="font-caption text-caption text-text-secondary">{uz.skillPath.hint}</p>

      {/* Topic complete - all six lesson stages passed (K.5). */}
      {allDone && (
        <div className="bg-success-bg border border-success/30 rounded-xl p-lg flex items-start gap-md">
          <Icon name="trophy" filled className="text-success text-[24px] shrink-0" />
          <div className="min-w-0 flex-1">
            <h3 className="font-label-md text-label-md text-success">{uz.skillPath.moduleCompleteTitle}</h3>
            <p className="font-caption text-caption text-text-secondary mt-xs">
              {uz.skillPath.moduleCompleteHint}
            </p>
            <button
              onClick={() => navigate("/levels")}
              className="mt-md inline-flex items-center gap-xs rounded-full bg-success text-white px-lg py-sm font-label-md text-label-md hover:brightness-110"
            >
              <Icon name="arrow_forward" className="text-[18px]" />
              {uz.skillPath.nextTopic}
            </button>
          </div>
        </div>
      )}

      <ol className="space-y-sm">
        {STEPS.map((step, i) => {
          const copy = uz.skillPath.steps[step.key];
          const state = stateOf(step);
          const isHere = step.key === "vocabulary";
          const locked = state === "locked";
          const interactive = !isHere && !locked;
          const color = colorForSkill(step.key);
          return (
            <li key={step.key}>
              <button
                type="button"
                disabled={!interactive}
                onClick={() => open(step)}
                className={cn(
                  "w-full text-left flex items-center gap-md rounded-card border border-ea-primary p-md text-white shadow-none transition-colors",
                  interactive
                    ? " cursor-pointer hover:brightness-105"
                    : "cursor-default",
                  locked && "opacity-60",
                  color.face,
                )}
              >
                {/* Step index / done check / lock. */}
                <span
                  className={cn(
                    "w-9 h-9 rounded-full flex items-center justify-center font-label-md text-label-md shrink-0",
                    "bg-white/25 text-white",
                  )}
                >
                  {state === "done" ? (
                    <Icon name="check" className="text-[18px]" />
                  ) : locked ? (
                    <Icon name="lock" className="text-[18px]" />
                  ) : (
                    i + 1
                  )}
                </span>
                {/* Skill icon. */}
                <span
                  className={cn(
                    "w-9 h-9 rounded-xl flex items-center justify-center shrink-0",
                    "bg-white/25",
                  )}
                >
                  <Icon name={step.icon} className="text-white text-[20px]" />
                </span>
                <span className="flex-1 min-w-0">
                  <span className="font-label-md text-label-md text-white block drop-shadow">{copy.title}</span>
                  <span className="font-caption text-caption text-white/85 block drop-shadow">
                    {locked ? uz.skillPath.lockedHint : copy.text}
                  </span>
                </span>
                {/* State badge. */}
                <span
                  className={cn(
                    "font-caption text-caption px-sm py-0.5 rounded-full shrink-0 font-duo font-bold",
                    "bg-white/30 text-white",
                  )}
                >
                  {state === "done"
                    ? uz.skillPath.done
                    : state === "current"
                      ? isHere
                        ? uz.skillPath.here
                        : uz.skillPath.next
                      : locked
                        ? uz.skillPath.locked
                        : uz.skillPath.todo}
                </span>
                {interactive && (
                  <Icon name="arrow_forward" className="text-white text-[18px] shrink-0 drop-shadow" />
                )}
              </button>
            </li>
          );
        })}
      </ol>
    </section>
  );
}
