import { describe, expect, it } from "vitest";
import { CefrLevel } from "@/api/types";
import type { VocabularyTopicSummaryDto } from "@/api/types";
import { buildLevelRoad, chapterLabel, topicState } from "./chapters";

function topic(
  id: string,
  category: string,
  overrides: Partial<VocabularyTopicSummaryDto> = {},
): VocabularyTopicSummaryDto {
  return {
    id,
    title: id,
    titleUz: id,
    category,
    level: CefrLevel.A1,
    isFilled: true,
    learned: false,
    isStarted: false,
    passedModuleCount: 0,
    requiredModuleCount: 6,
    isMastered: false,
    isLocked: false,
    requiresPro: false,
    modules: [],
    ...overrides,
  };
}

/** Ten categories of five, in learning order - the shape every CEFR level ships. */
function fullLevel(): VocabularyTopicSummaryDto[] {
  const categories = [
    "family_people", "home_routine", "food_drink", "school", "animals_pets",
    "body_health", "clothes_weather", "places_town", "free_time_toys", "time_numbers",
  ];
  return categories.flatMap((category) =>
    Array.from({ length: 5 }, (_, i) => topic(`${category}-${i}`, category)),
  );
}

describe("buildLevelRoad", () => {
  it("splits a full level into ten chapters of five, preserving learning order", () => {
    const road = buildLevelRoad(fullLevel());

    expect(road.chapters).toHaveLength(10);
    expect(road.chapters.every((c) => c.topics.length === 5)).toBe(true);
    expect(road.chapters.map((c) => c.position)).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
    // startIndex keeps node numbering continuous across chapters (1..50).
    expect(road.chapters.map((c) => c.startIndex)).toEqual([0, 5, 10, 15, 20, 25, 30, 35, 40, 45]);
    expect(road.chapters[0].category).toBe("family_people");
    expect(road.chapters[9].category).toBe("time_numbers");
  });

  it("returns no chapters for an empty level", () => {
    const road = buildLevelRoad([]);
    expect(road.chapters).toEqual([]);
    expect(road.activeIndex).toBe(-1);
  });

  it("groups a single-category level into one chapter", () => {
    const road = buildLevelRoad([topic("a", "school"), topic("b", "school")]);
    expect(road.chapters).toHaveLength(1);
    expect(road.chapters[0].topics).toHaveLength(2);
  });

  it("splits non-contiguous runs rather than reordering the road", () => {
    const road = buildLevelRoad([
      topic("a", "school"),
      topic("b", "food_drink"),
      topic("c", "school"),
    ]);
    // Learning order is authoritative: three runs, not two merged buckets.
    expect(road.chapters.map((c) => c.category)).toEqual(["school", "food_drink", "school"]);
  });

  it("points activeIndex at the first non-mastered topic", () => {
    const topics = fullLevel();
    topics[0].isMastered = true;
    topics[1].isMastered = true;

    expect(buildLevelRoad(topics).activeIndex).toBe(2);
  });

  it("reports -1 when every topic is mastered", () => {
    const topics = fullLevel().map((t) => ({ ...t, isMastered: true }));
    expect(buildLevelRoad(topics).activeIndex).toBe(-1);
  });

  it("derives chapter state from its topics", () => {
    const topics = fullLevel();
    // Chapter 1 fully done, chapter 2 holds the active topic, chapter 3 all locked.
    for (let i = 0; i < 5; i++) topics[i].isMastered = true;
    for (let i = 10; i < 15; i++) topics[i].isLocked = true;

    const { chapters } = buildLevelRoad(topics);

    expect(chapters[0].state).toBe("mastered");
    expect(chapters[0].masteredCount).toBe(5);
    expect(chapters[1].state).toBe("active");
    expect(chapters[2].state).toBe("locked");
    expect(chapters[3].state).toBe("upcoming");
  });

  it("prefers mastered over active when the whole chapter is complete", () => {
    const topics = [topic("a", "school", { isMastered: true })];
    expect(buildLevelRoad(topics).chapters[0].state).toBe("mastered");
  });
});

describe("topicState", () => {
  const t = topic("a", "school");

  it("reports mastered ahead of every other state", () => {
    expect(topicState(3, { ...t, isMastered: true, isLocked: true }, 3)).toBe("mastered");
  });

  it("reports locked before active", () => {
    expect(topicState(3, { ...t, isLocked: true }, 3)).toBe("locked");
  });

  it("marks only the level's active index as active", () => {
    expect(topicState(3, t, 3)).toBe("active");
    expect(topicState(4, t, 3)).toBe("upcoming");
  });
});

describe("chapterLabel", () => {
  it("uses the vetted Uzbek template for a known category (rule 11)", () => {
    expect(chapterLabel("family_people")).toBe("Oila va odamlar");
    expect(chapterLabel("technology_ai")).toBe("Texnologiya va sun'iy intellekt");
  });

  it("falls back to the prettified code for an unseeded category", () => {
    expect(chapterLabel("brand_new_topic")).toBe("Brand new topic");
  });
});
