import { useEffect } from "react";
import { motion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";
import { TopicSkillsFooter } from "@/components/TopicSkillsFooter";
import { Confetti } from "@/components/game";
import { AppButton, DesignCard } from "@/components/design";
import { cn } from "@/lib/cn";
import type { SkillStepKey } from "@/lib/skillSteps";
import type { TopicCompletionDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { markHomeTopicCompleted } from "@/pages/home-concepts/homeRecommendation";
import "./TopicLessonResult.css";

type Reward = {
  xp?: number;
  coins?: number;
};

export function TopicLessonResult({
  topicId,
  topicTitle,
  passed,
  title,
  subtitle,
  scoreLabel,
  scorePercent,
  refreshKey = 0,
  reward,
  details,
  onRetry,
  retryLabel,
  showRetryWhenPassed = false,
  showRetry = true,
  currentSkill,
  completion,
  showOverviewContinueAction = true,
}: {
  topicId: string;
  topicTitle?: string;
  passed: boolean;
  title: string;
  subtitle: string;
  scoreLabel: string;
  scorePercent?: number;
  refreshKey?: number;
  reward?: Reward;
  details?: React.ReactNode;
  onRetry: () => void;
  retryLabel: string;
  showRetryWhenPassed?: boolean;
  showRetry?: boolean;
  currentSkill: SkillStepKey;
  completion?: TopicCompletionDto | null;
  showOverviewContinueAction?: boolean;
}) {
  useEffect(() => {
    if (passed && completion?.isMastered) markHomeTopicCompleted(getLearnerId(), topicId);
  }, [completion?.isMastered, passed, topicId]);
  return (
    <motion.section
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      className="topic-lesson-result"
    >
      {passed && <Confetti show count={70} />}

      <DesignCard className="topic-lesson-result__card">
        <motion.div
          initial={{ scale: 0.5, rotate: -8 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={{ type: "spring", stiffness: 300, damping: 14 }}
          className={cn("topic-lesson-result__icon", passed ? "is-passed" : "is-retry")}
        >
          <Icon name={passed ? "workspace_premium" : "restart_alt"} filled />
        </motion.div>

        <div className="topic-lesson-result__copy">
          <span className="topic-lesson-result__kicker">
            <Icon name={passed ? "verified" : "refresh"} filled />
            {passed ? "Ajoyib natija" : "Yana bir bor urinib ko‘ring"}
          </span>
          <h1>{title}</h1>
          <p>{subtitle}</p>
        </div>

        <div className="topic-lesson-result__summary">
          <strong>{scoreLabel}</strong>
          {typeof scorePercent === "number" && <span>{scorePercent}%</span>}
          {(reward?.xp || reward?.coins) && (
            <div className="topic-lesson-result__rewards">
              {reward.xp ? <span className="is-xp"><Icon name="bolt" filled />+{reward.xp} XP</span> : null}
              {reward.coins ? <span className="is-coins"><Icon name="paid" filled />+{reward.coins}</span> : null}
            </div>
          )}
        </div>

        {details ? <div className="topic-lesson-result__details">{details}</div> : null}
      </DesignCard>

      {passed ? (
        <div className="topic-lesson-result__path">
          <TopicSkillsFooter
            topicId={topicId}
            topicTitle={topicTitle}
            refreshKey={refreshKey}
            currentSkill={currentSkill}
            currentPassed={passed}
            overviewClassName="topic-lesson-result__overview"
            actionClassName="topic-lesson-result__next"
            showOverviewContinueAction={showOverviewContinueAction}
          />
        </div>
      ) : null}

      {showRetry && (!passed || showRetryWhenPassed) && (
        <AppButton
          tone={passed ? "standard" : "danger"}
          fullWidth
          leadingIcon="refresh"
          onClick={onRetry}
          className="topic-lesson-result__retry"
        >
          {retryLabel}
        </AppButton>
      )}
    </motion.section>
  );
}
