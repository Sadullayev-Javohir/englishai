import { uz } from "@/content/uz";
import type { VocabularyTopicSummaryDto } from "@/api/types";

/** Per-topic node state on the road. */
export type TopicState = "mastered" | "active" | "locked" | "upcoming";

/** Chapter (bo'lim) state, derived from the topics it holds. */
export type ChapterState = "mastered" | "active" | "locked" | "upcoming";

export interface Chapter {
  /** Server category code, e.g. "family_people" - unique per level, so it doubles as the React key. */
  category: string;
  /** 1-based position of this chapter within the level. */
  position: number;
  /** Index of this chapter's first topic within the whole level, so node numbering stays continuous. */
  startIndex: number;
  topics: VocabularyTopicSummaryDto[];
  masteredCount: number;
  state: ChapterState;
}

export interface LevelRoad {
  chapters: Chapter[];
  /**
   * Index of the level's active topic - the first one that is not yet mastered. -1 when every
   * topic is mastered. This is the authoritative place where learning resumes (the API returns
   * topics in learning order), and it drives both the node state and which chapter opens first.
   */
  activeIndex: number;
}

/** State of a single topic node, given the level's active index. */
export function topicState(
  index: number,
  topic: VocabularyTopicSummaryDto,
  activeIndex: number,
): TopicState {
  if (topic.isMastered) return "mastered";
  if (topic.isLocked) return "locked";
  if (index === activeIndex) return "active";
  return "upcoming";
}

/**
 * Splits a level's topics into chapters.
 *
 * Every CEFR level ships exactly ten categories of five topics, and the API returns them in the
 * level's learning order (Sequence), so each *contiguous run* of one category is one chapter.
 * Grouping by run rather than by key keeps the learning order intact even if a level ever
 * interleaves categories - it would simply render more, smaller chapters instead of reordering
 * the road under the learner.
 */
export function buildLevelRoad(topics: VocabularyTopicSummaryDto[]): LevelRoad {
  const activeIndex = topics.findIndex((t) => !t.isMastered);
  const chapters: Chapter[] = [];

  topics.forEach((topic, index) => {
    const last = chapters[chapters.length - 1];
    if (last && last.category === topic.category) {
      last.topics.push(topic);
      return;
    }
    chapters.push({
      category: topic.category,
      position: chapters.length + 1,
      startIndex: index,
      topics: [topic],
      masteredCount: 0,
      state: "upcoming",
    });
  });

  for (const chapter of chapters) {
    chapter.masteredCount = chapter.topics.filter((t) => t.isMastered).length;
    const end = chapter.startIndex + chapter.topics.length;
    const holdsActive = activeIndex >= chapter.startIndex && activeIndex < end;

    chapter.state = chapter.masteredCount === chapter.topics.length
      ? "mastered"
      : holdsActive
        ? "active"
        : chapter.topics.every((t) => t.isLocked)
          ? "locked"
          : "upcoming";
  }

  return { chapters, activeIndex };
}

/** Prettifies a raw category code as a last resort when no Uzbek template exists for it. */
function prettifyCategory(category: string): string {
  const spaced = category.replace(/_/g, " ");
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
}

/**
 * Vetted Uzbek chapter name for a server category code (rule 11). All 60 codes across A1-C2 are
 * templated in `uz.levelMap.chapters`; the prettified English code is the fallback so a newly
 * seeded category never renders blank.
 */
export function chapterLabel(category: string): string {
  return uz.levelMap.chapters[category] ?? prettifyCategory(category);
}
