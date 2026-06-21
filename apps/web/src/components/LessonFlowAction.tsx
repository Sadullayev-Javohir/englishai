import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import type { TopicCompletionDto } from "@/api/types";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { SKILL_STEPS, openSkill, type SkillStepKey } from "@/lib/skillSteps";
import { CefrLevel } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { Confetti, DuoButton } from "@/components/game";
import { DesignModal } from "@/components/design";
import { uz } from "@/content/uz";
import { lessonOriginFrom, lessonState } from "@/lib/lessonNavigation";

interface LessonFlowActionProps {
  current: SkillStepKey;
  topicId: string;
  topicTitle: string;
  completion: TopicCompletionDto | null;
  passed: boolean;
  className?: string;
}

export function LessonFlowAction({
  current,
  topicId,
  topicTitle,
  completion,
  passed,
  className,
}: LessonFlowActionProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const origin = lessonOriginFrom(location);
  const [showFinished, setShowFinished] = useState(false);
  const currentModule = SKILL_STEPS.find((step) => step.key === current)?.module;
  const currentModuleState = currentModule
    ? completion?.modules.find((module) => module.module === currentModule)
    : undefined;
  const authoritativePassed = currentModuleState?.passed ?? passed;
  const currentAttempted = Boolean(currentModuleState?.achievedAt);
  const nextStep = completion
    ? SKILL_STEPS.find((step) => {
        const module = completion.modules.find((item) => item.module === step.module);
        return module?.unlocked && !module.passed;
      })
    : undefined;
  const canFinishTopic = Boolean(completion?.isMastered);

  const level = completion ? CefrLevel[completion.level as keyof typeof CefrLevel] : undefined;
  const { data: map } = useAsync(
    () => (showFinished && level ? api.levels.map(getLearnerId(), level) : Promise.resolve(null)),
    [showFinished, level],
  );

  const { sectionNumber, nextTopic, mapReady } = useMemo(() => {
    const topics = map?.topics ?? [];
    const index = topics.findIndex((topic) => topic.id === topicId);
    return {
      sectionNumber: index >= 0 ? index + 1 : null,
      nextTopic: index >= 0 ? topics[index + 1] : undefined,
      mapReady: Boolean(map),
    };
  }, [map, topicId]);

  function continueFlow() {
    if (!currentAttempted) return;
    if (nextStep) {
      openSkill(navigate, nextStep, topicId, topicTitle, origin);
      return;
    }
    if (canFinishTopic) setShowFinished(true);
  }

  return (
    <>
      <div className={className}>
        <DuoButton color={authoritativePassed ? "green" : "blue"} fullWidth onClick={continueFlow} disabled={!currentAttempted}>
          <span className="inline-flex items-center gap-2">
            <Icon name={authoritativePassed ? "arrow_forward" : "refresh"} filled className="text-[20px]" />
            <span>{currentAttempted
              ? nextStep
                ? uz.lessonFlow.continueTo(nextStepLabel(nextStep.key))
                : canFinishTopic
                  ? uz.lessonFlow.finishSection
                  : uz.lessonFlow.nextStepFallback
              : uz.lessonFlow.retryHint}</span>
          </span>
        </DuoButton>
      </div>

      <DesignModal
        open={showFinished}
        onClose={() => setShowFinished(false)}
        title={sectionNumber
          ? uz.lessonFlow.sectionComplete(sectionNumber)
          : uz.lessonFlow.sectionCompleteFallback}
        description={uz.lessonFlow.completedBody}
        closeLabel={uz.common.close}
        className="max-w-[560px]"
        footer={
          <div className="grid w-full gap-3 sm:grid-cols-2">
            <DuoButton color="blue" fullWidth onClick={() => navigate("/levels")}>
              {uz.lessonFlow.roadmap}
            </DuoButton>
            <DuoButton
              color="yellow"
              fullWidth
              disabled={!mapReady}
              onClick={() => navigate(
                nextTopic ? `/app/vocabulary/topic/${nextTopic.id}` : "/levels",
                nextTopic ? { state: lessonState(origin) } : undefined,
              )}
            >
              {!mapReady
                ? uz.common.loading
                : nextTopic
                  ? uz.lessonFlow.startNextSection
                  : uz.lessonFlow.continueAction}
            </DuoButton>
          </div>
        }
      >
        <Confetti show={showFinished} count={100} />
        <div className="text-center">
          <span className="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-[24px] border border-[var(--ea-purple-300)] bg-[var(--ea-purple-50)] text-[var(--ea-purple-800)] ">
            <Icon name="emoji_events" filled className="text-[42px]" />
          </span>
          <p className="font-caption text-sm font-extrabold uppercase tracking-[0.16em] text-[var(--ea-muted)]">
            {uz.lessonFlow.completedTitle}
          </p>
          <div className="mt-5 rounded-[22px] border border-[var(--ea-blue-300)] bg-[var(--ea-blue-50)] p-4 text-left text-[var(--ea-text)] ">
            <p className="font-caption font-bold uppercase tracking-wide text-[var(--ea-blue-800)]">
              {!mapReady
                ? uz.lessonFlow.nextSectionDetecting
                : nextTopic
                  ? uz.lessonFlow.nextSectionLabel
                  : uz.lessonFlow.levelFinishLabel}
            </p>
            <p className="mt-1 font-duo text-xl font-extrabold">
              {!mapReady ? uz.common.loading : nextTopic ? nextTopic.title : uz.lessonFlow.backToRoadmap}
            </p>
            {nextTopic?.titleUz && <p className="mt-1 font-caption text-[var(--ea-muted)]">{nextTopic.titleUz}</p>}
          </div>
        </div>
      </DesignModal>
    </>
  );
}

function nextStepLabel(key: SkillStepKey): string {
  return uz.lessonFlow.steps[key];
}
