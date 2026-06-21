import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { Button } from "@/components/ui/Button";
import { Spinner } from "@/components/ui/Spinner";
import { TopicSkillsOverview } from "@/components/TopicSkillsOverview";
import { LessonFlowAction } from "@/components/LessonFlowAction";
import { CefrLevel } from "@/api/types";
import { cn } from "@/lib/cn";
import { lessonOriginFrom, lessonState } from "@/lib/lessonNavigation";
import type { SkillStepKey } from "@/lib/skillSteps";
import { markHomeTopicCompleted } from "@/pages/home-concepts/homeRecommendation";

/**
 * Always-visible six-stage status for the topic a skill page is showing, plus a "go to the next
 * topic" action. Unlike rendering <TopicSkillsOverview> only on a fresh quiz result, this fetches
 * the topic's K.5 completion on load, so every core lesson page (reading, writing, speaking, listening)
 * shows at its bottom which skills are done and which are still left - even before the learner
 * submits this skill's quiz.
 *
 * The next-topic button is enabled only once all six stages are passed (the topic is mastered),
 * matching the sequential-topic lock; otherwise it is disabled with a hint. When mastered it sends
 * the learner to the next topic in the level (or back to the level roadmap if this is the last one).
 *
 * `refreshKey` lets the host bump a re-fetch after the learner passes this skill's quiz on the same
 * page, so the overview turns green and the button unlocks without a reload.
 */
export function TopicSkillsFooter({
  topicId,
  topicTitle,
  refreshKey = 0,
  currentSkill,
  currentPassed,
  actionClassName,
  overviewClassName,
  showOverviewContinueAction = true,
}: {
  topicId: string;
  topicTitle?: string;
  refreshKey?: number;
  currentSkill?: SkillStepKey;
  currentPassed?: boolean;
  actionClassName?: string;
  overviewClassName?: string;
  showOverviewContinueAction?: boolean;
}) {
  const navigate = useNavigate();
  const location = useLocation();
  const origin = lessonOriginFrom(location);
  const learnerId = getLearnerId();

  const { data: completion, loading } = useAsync(
    () => api.vocabulary.topicCompletion(topicId, learnerId),
    [topicId, learnerId, refreshKey],
  );

  // The ordered topic list for this topic's level, so once the topic is mastered we can send the
  // learner straight to the next topic. Loaded off the completion's level (numeric enum keyed by
  // its short name, e.g. "B1").
  const level = completion ? CefrLevel[completion.level as keyof typeof CefrLevel] : undefined;
  const { data: map } = useAsync(
    () => (level ? api.levels.map(learnerId, level) : Promise.resolve(null)),
    [learnerId, level],
  );

  const mastered = completion?.isMastered ?? false;
  useEffect(() => {
    if (mastered) markHomeTopicCompleted(learnerId, topicId);
  }, [learnerId, mastered, topicId]);

  if (loading || !completion) {
    return (
      <div className="mt-xl py-lg flex justify-center">
        <Spinner />
      </div>
    );
  }

  // The next topic in this level (if any) - unlocked once the current topic is mastered.
  const topics = map?.topics ?? [];
  const idx = topics.findIndex((t) => t.id === topicId);
  const nextTopic = idx >= 0 ? topics[idx + 1] : undefined;

  function goNext() {
    if (!mastered) return;
    if (nextTopic) {
      navigate(`/app/vocabulary/topic/${nextTopic.id}`, { state: lessonState(origin) });
    } else {
      // Last topic of the level - send the learner to the level roadmap (exit test lives there).
      navigate("/levels");
    }
  }

  return (
    <section className="mt-xl space-y-sm">
        <TopicSkillsOverview completion={completion} topicId={topicId} topicTitle={topicTitle} className={overviewClassName} showContinueAction={showOverviewContinueAction} />
        {currentSkill ? (
          <LessonFlowAction
            current={currentSkill}
            topicId={topicId}
            topicTitle={topicTitle ?? ""}
            completion={completion}
            passed={Boolean(currentPassed)}
            className={actionClassName}
          />
        ) : (
        <Button
          variant="accent"
          fullWidth
          icon={mastered ? "arrow_forward" : "lock"}
          disabled={!mastered}
          onClick={goNext}
          className={cn("!rounded-[15px]", actionClassName)}
        >
          {nextTopic || !mastered ? uz.skillPath.nextModule : uz.skillPath.nextModuleFinish}
        </Button>
        )}
        {!currentSkill && !mastered && (
          <p className="font-caption text-caption text-text-secondary text-center mt-xs">
            {uz.skillPath.nextModuleLockedHint}
          </p>
        )}
    </section>
  );
}
