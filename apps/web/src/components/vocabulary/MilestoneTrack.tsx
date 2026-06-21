import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import type { ReviewMilestone } from "@/lib/srsStage";
import { uz } from "@/content/uz";
import "./MilestoneTrack.css";

interface MilestoneTrackProps {
  milestones: ReviewMilestone[];
  /** Every step cleared - render the mastered crown instead of a pending "current" step. */
  mastered?: boolean;
  /** Pulse the pending step because its review is due right now. */
  due?: boolean;
  size?: "sm" | "md";
  /** Show the "3 / 7 / 21 kun jadvali" caption above the chips. */
  showCaption?: boolean;
  className?: string;
}

/**
 * The 3 / 7 / 21 spaced-repetition ladder as three tickable chips. A cleared step shows a check,
 * the pending step is highlighted (and pulses when its review is due), and a fully mastered word
 * caps the row with a crown. Purely presentational - callers pass milestones from
 * {@link stageMilestones} / {@link topicAggregate}.
 */
export function MilestoneTrack({
  milestones,
  mastered = false,
  due = false,
  size = "md",
  showCaption = false,
  className,
}: MilestoneTrackProps) {
  const label = uz.mySavedWords.milestoneAria(
    milestones.filter((step) => step.done).length,
    milestones.length,
  );
  return (
    <div className={cn("ms-track", `ms-track--${size}`, mastered && "is-mastered", className)}>
      {showCaption && <span className="ms-track__caption">{uz.mySavedWords.milestoneCaption}</span>}
      <ol className="ms-track__steps" aria-label={label}>
        {milestones.map((step) => {
          const state = step.done ? "done" : step.current && !mastered ? "current" : "todo";
          return (
            <li
              key={step.days}
              className="ms-track__step"
              data-state={state}
              data-due={state === "current" && due ? "true" : undefined}
            >
              <span className="ms-track__badge" aria-hidden="true">
                {step.done ? <Icon name="check" filled /> : step.days}
              </span>
              <span className="ms-track__day">{uz.mySavedWords.milestoneDay(step.days)}</span>
            </li>
          );
        })}
        {mastered && (
          <li className="ms-track__step ms-track__step--crown" data-state="mastered">
            <span className="ms-track__badge" aria-hidden="true"><Icon name="verified" filled /></span>
            <span className="ms-track__day">{uz.mySavedWords.mastered}</span>
          </li>
        )}
      </ol>
    </div>
  );
}
