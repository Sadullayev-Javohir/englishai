import { ReviewStage } from "@/api/types";

/**
 * Shared SRS (spaced-repetition) helpers for the vocabulary "saved words" surfaces.
 *
 * A word walks the 3 / 7 / 21 day ladder before it is mastered. The domain enum
 * (Domain.Vocabulary.ReviewStage) encodes how many of those milestones a word has already
 * cleared: Day3=0 words still owe the day-3 review, Day7=1 have cleared day-3, Day21=2 have
 * cleared day-3 and day-7, and Mastered=3 have cleared all three. So the stage index doubles as
 * the count of ticked milestones - that is what drives the 3/7/21 tick chips on the cards.
 */

export const REVIEW_MILESTONE_DAYS = [3, 7, 21] as const;
export type ReviewMilestoneDay = (typeof REVIEW_MILESTONE_DAYS)[number];

export interface ReviewMilestone {
  days: ReviewMilestoneDay;
  /** The learner has already passed this milestone review. */
  done: boolean;
  /** The next, still-pending milestone the learner is working toward (none once mastered). */
  current: boolean;
}

/** Minimal shape the SRS helpers need - both VocabularyItemDto and DueReviewDto satisfy it. */
export interface SrsWordLike {
  stage: ReviewStage;
  nextReviewAt?: string | null;
}

export type SavedWordStatus = "mastered" | "due" | "planned";

/** How many 3/7/21 milestones a word has cleared (stage index, capped at the ladder length). */
export function clearedMilestones(stage: ReviewStage): number {
  return Math.min(stage, REVIEW_MILESTONE_DAYS.length);
}

/** The three 3/7/21 chips for a single stage, marked done / current / upcoming. */
export function stageMilestones(stage: ReviewStage): ReviewMilestone[] {
  const cleared = clearedMilestones(stage);
  return REVIEW_MILESTONE_DAYS.map((days, index) => ({
    days,
    done: index < cleared,
    current: index === cleared,
  }));
}

export function isMastered(stage: ReviewStage): boolean {
  return stage === ReviewStage.Mastered;
}

/** A non-mastered word whose scheduled review time has arrived. */
export function isDue(word: SrsWordLike, now: number = Date.now()): boolean {
  return (
    word.stage !== ReviewStage.Mastered &&
    Boolean(word.nextReviewAt && new Date(word.nextReviewAt).getTime() <= now)
  );
}

export function wordStatus(word: SrsWordLike, now: number = Date.now()): SavedWordStatus {
  if (isMastered(word.stage)) return "mastered";
  if (isDue(word, now)) return "due";
  return "planned";
}

export interface TopicAggregate {
  total: number;
  dueCount: number;
  masteredCount: number;
  /** Least-advanced stage across the topic's words - the milestone level the whole set shares. */
  minStage: ReviewStage;
  /** Whether every word in the topic is mastered. */
  allMastered: boolean;
  /** Topic-level 3/7/21 chips: a milestone is "done" only when every word has cleared it. */
  milestones: ReviewMilestone[];
}

/**
 * Rolls a topic's saved words up into a single 3/7/21 progress view. A topic milestone chip only
 * ticks once every word in the topic has passed it (so the card never over-reports), which is
 * exactly stageMilestones(minStage).
 */
export function topicAggregate(words: SrsWordLike[], now: number = Date.now()): TopicAggregate {
  if (words.length === 0) {
    return {
      total: 0,
      dueCount: 0,
      masteredCount: 0,
      minStage: ReviewStage.Day3,
      allMastered: false,
      milestones: stageMilestones(ReviewStage.Day3),
    };
  }
  const minStage = words.reduce<ReviewStage>(
    (lowest, word) => (word.stage < lowest ? word.stage : lowest),
    ReviewStage.Mastered,
  );
  const masteredCount = words.filter((word) => isMastered(word.stage)).length;
  return {
    total: words.length,
    dueCount: words.filter((word) => isDue(word, now)).length,
    masteredCount,
    minStage,
    allMastered: masteredCount === words.length,
    milestones: stageMilestones(minStage),
  };
}
