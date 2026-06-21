import { describe, expect, it } from "vitest";
import { ReviewStage } from "@/api/types";
import {
  clearedMilestones,
  isDue,
  stageMilestones,
  topicAggregate,
  wordStatus,
} from "./srsStage";

const NOW = new Date("2026-08-15T00:00:00Z").getTime();
const past = "2026-08-01T00:00:00Z";
const future = "2099-01-01T00:00:00Z";

describe("srsStage", () => {
  it("maps stage to the number of cleared milestones", () => {
    expect(clearedMilestones(ReviewStage.Day3)).toBe(0);
    expect(clearedMilestones(ReviewStage.Day7)).toBe(1);
    expect(clearedMilestones(ReviewStage.Day21)).toBe(2);
    expect(clearedMilestones(ReviewStage.Mastered)).toBe(3);
  });

  it("ticks milestones up to the cleared count and marks the next as current", () => {
    const milestones = stageMilestones(ReviewStage.Day7);
    expect(milestones.map((m) => m.done)).toEqual([true, false, false]);
    expect(milestones.map((m) => m.current)).toEqual([false, true, false]);
    expect(milestones.map((m) => m.days)).toEqual([3, 7, 21]);

    const mastered = stageMilestones(ReviewStage.Mastered);
    expect(mastered.every((m) => m.done)).toBe(true);
    expect(mastered.some((m) => m.current)).toBe(false);
  });

  it("treats a non-mastered word past its review time as due", () => {
    expect(isDue({ stage: ReviewStage.Day3, nextReviewAt: past }, NOW)).toBe(true);
    expect(isDue({ stage: ReviewStage.Day3, nextReviewAt: future }, NOW)).toBe(false);
    expect(isDue({ stage: ReviewStage.Day3, nextReviewAt: null }, NOW)).toBe(false);
    // Mastered words never surface as due even with a stale timestamp.
    expect(isDue({ stage: ReviewStage.Mastered, nextReviewAt: past }, NOW)).toBe(false);
  });

  it("resolves the display status", () => {
    expect(wordStatus({ stage: ReviewStage.Mastered, nextReviewAt: null }, NOW)).toBe("mastered");
    expect(wordStatus({ stage: ReviewStage.Day3, nextReviewAt: past }, NOW)).toBe("due");
    expect(wordStatus({ stage: ReviewStage.Day7, nextReviewAt: future }, NOW)).toBe("planned");
  });

  it("aggregates a topic to its least-advanced shared milestone", () => {
    const agg = topicAggregate(
      [
        { stage: ReviewStage.Day21, nextReviewAt: future },
        { stage: ReviewStage.Day7, nextReviewAt: past },
        { stage: ReviewStage.Mastered, nextReviewAt: null },
      ],
      NOW,
    );
    expect(agg.total).toBe(3);
    expect(agg.dueCount).toBe(1);
    expect(agg.masteredCount).toBe(1);
    expect(agg.minStage).toBe(ReviewStage.Day7);
    expect(agg.allMastered).toBe(false);
    // Topic chips tick only what every word has cleared (min stage = Day7 => one tick).
    expect(agg.milestones.map((m) => m.done)).toEqual([true, false, false]);
  });

  it("reports a fully mastered topic", () => {
    const agg = topicAggregate(
      [
        { stage: ReviewStage.Mastered, nextReviewAt: null },
        { stage: ReviewStage.Mastered, nextReviewAt: null },
      ],
      NOW,
    );
    expect(agg.allMastered).toBe(true);
    expect(agg.dueCount).toBe(0);
    expect(agg.milestones.every((m) => m.done)).toBe(true);
  });

  it("handles an empty topic without throwing", () => {
    const agg = topicAggregate([], NOW);
    expect(agg.total).toBe(0);
    expect(agg.allMastered).toBe(false);
    expect(agg.milestones).toHaveLength(3);
  });
});
