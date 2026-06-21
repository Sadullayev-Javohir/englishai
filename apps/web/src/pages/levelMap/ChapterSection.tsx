import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import { chapterLabel, topicState, type Chapter } from "./chapters";
import { TopicRow } from "./TopicRow";

interface ChapterSectionProps {
  chapter: Chapter;
  /** Total chapters in the level, for the "3-si / 10" caption. */
  total: number;
  /** Index of the level's active topic, or -1 when everything is mastered. */
  activeIndex: number;
  expanded: boolean;
  onToggle: () => void;
  expandedTopicId: string | null;
  onToggleTopic: (topicId: string) => void;
}

export function ChapterSection({
  chapter,
  total,
  activeIndex,
  expanded,
  onToggle,
  expandedTopicId,
  onToggleTopic,
}: ChapterSectionProps) {
  const c = uz.levelMap.chapter;
  const count = chapter.topics.length;

  const stateNote =
    chapter.state === "mastered"
      ? c.done
      : chapter.state === "active"
        ? c.active
        : chapter.state === "locked"
          ? c.locked
          : c.upcoming;

  return (
    <section className={cn("lvmap-chapter", `lvmap-chapter--${chapter.state}`)}>
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={expanded}
        aria-label={`${chapterLabel(chapter.category)} - ${expanded ? c.collapse : c.expand}`}
        className="lvmap-chapter__banner"
      >
        <span className="lvmap-chapter__ordinal">
          {chapter.state === "mastered" ? (
            <Icon name="check" filled className="text-[19px]" />
          ) : chapter.state === "locked" ? (
            <Icon name="lock" className="text-[17px]" />
          ) : (
            chapter.position
          )}
        </span>

        <span className="min-w-0">
          <span className="lvmap-chapter__title">{chapterLabel(chapter.category)}</span>
          <span className="lvmap-chapter__meta">
            <span className="lvmap-chapter__meta-count">
              {c.countLabel(chapter.position, total)} ·{" "}
            </span>
            {c.progress(chapter.masteredCount, count)} · {stateNote}
          </span>
        </span>

        <span className="lvmap-chapter__right">
          <span className="lvmap-chapter__dots" aria-hidden>
            {chapter.topics.map((t, i) => (
              <span
                key={t.id}
                className={cn("lvmap-chapter__dot", i < chapter.masteredCount && "lvmap-chapter__dot--on")}
              />
            ))}
          </span>
          <Icon name="expand_more" className="lvmap-chapter__caret text-[20px]" />
        </span>
      </button>

      {expanded && (
        <ol className="lvmap-chapter__rows">
          {chapter.topics.map((topic, i) => {
            const levelIndex = chapter.startIndex + i;
            return (
              <TopicRow
                key={topic.id}
                topic={topic}
                index={levelIndex}
                state={topicState(levelIndex, topic, activeIndex)}
                expanded={expandedTopicId === topic.id}
                onToggle={() => onToggleTopic(topic.id)}
              />
            );
          })}
        </ol>
      )}
    </section>
  );
}
