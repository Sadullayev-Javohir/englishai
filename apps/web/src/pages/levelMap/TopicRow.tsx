import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { SKILL_STEPS } from "@/lib/skillSteps";
import { cn } from "@/lib/cn";
import type { VocabularyTopicSummaryDto } from "@/api/types";
import { useSkillStates } from "./skillStates";
import type { TopicState } from "./chapters";
import { TopicSkillsPanel } from "./TopicSkillsPanel";

interface TopicRowProps {
  topic: VocabularyTopicSummaryDto;
  index: number;
  state: TopicState;
  expanded: boolean;
  onToggle: () => void;
}

export function TopicRow({ topic, index, state, expanded, onToggle }: TopicRowProps) {
  const locked = state === "locked";
  const { stateOf } = useSkillStates(topic.modules);

  return (
    <li
      data-roadmap-topic={topic.id}
      data-roadmap-active={state === "active" || undefined}
      className="lvmap-topic"
    >
      <button
        type="button"
        disabled={locked}
        onClick={onToggle}
        aria-expanded={locked ? undefined : expanded}
        className={cn("lvmap-card", `lvmap-card--${state}`)}
      >
        <span className="lvmap-card__state" aria-hidden>
          {state === "mastered" ? (
            <Icon name="check" filled className="text-[20px]" />
          ) : state === "active" ? (
            <Icon name="play_arrow" filled className="text-[20px]" />
          ) : locked ? (
            <Icon name="lock" className="text-[18px]" />
          ) : (
            index + 1
          )}
        </span>

        <span className="lvmap-card__body">
          <span className="lvmap-card__title">{topic.title}</span>
          <span className="lvmap-card__subtitle">{topic.titleUz}</span>
          <span className="lvmap-card__skills" aria-label="Olti ko‘nikma holati">
            {SKILL_STEPS.map((step) => {
              const skillState = stateOf(step);
              return (
                <span
                  key={step.key}
                  className={cn(
                    "lvmap-skill-dot",
                    `lvmap-skill-dot--${skillState}`,
                    `lvmap-skill--${step.key}`,
                  )}
                  title={uz.skillPath.steps[step.key].title}
                >
                  <Icon
                    name={skillState === "done" ? "check" : step.icon}
                    className="text-[11px]"
                  />
                </span>
              );
            })}
          </span>
        </span>

        <span className="lvmap-card__aside">
          <span className={cn("lvmap-badge", `lvmap-badge--${state}`)}>
            {state === "mastered"
              ? "6 / 6"
              : `${topic.passedModuleCount} / ${topic.requiredModuleCount}`}
          </span>
          {!locked && (
            <Icon
              name="expand_more"
              className={cn("lvmap-card__caret", expanded && "is-open")}
            />
          )}
        </span>
      </button>

      {expanded && !locked && <TopicSkillsPanel topic={topic} />}
    </li>
  );
}
