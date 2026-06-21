import { describe, expect, it, vi } from "vitest";
import { buildHomeViewModel } from "./homeViewModel";

vi.mock("@/api/nativeCapabilities", () => ({ getNativeCapabilities: () => ({ video: true }) }));

const topicModules = [
  { module: "Vocabulary", score: 90, passed: true, unlocked: true, achievedAt: null },
  { module: "Grammar", score: 74, passed: false, unlocked: true, achievedAt: null },
  { module: "Reading", score: 0, passed: false, unlocked: false, achievedAt: null },
  { module: "Writing", score: 0, passed: false, unlocked: false, achievedAt: null },
  { module: "Speaking", score: 0, passed: false, unlocked: false, achievedAt: null },
  { module: "Listening", score: 0, passed: false, unlocked: false, achievedAt: null },
];

describe("buildHomeViewModel", () => {
  it("counts distinct core skills instead of completed tasks or topic mastery", () => {
    const model = buildHomeViewModel({
      name: "Ali",
      activeTopic: { id: "travel", title: "Travel", modules: topicModules },
      dailyGoalTarget: 4,
      todayCompletedTasks: 9,
      skillsCompletedToday: ["Vocabulary", "Grammar", "Reading", "Vocabulary", "FirstLesson", "Video"],
    });
    expect(model.todayCompletedSkills).toBe(3);
    expect(model.completed).toBe(1);
    expect(model.dailyGoalTarget).toBe(4);
  });

  it("never counts more than six skills and uses zero when no daily skills are returned", () => {
    expect(buildHomeViewModel({
      name: "Ali",
      skillsCompletedToday: [...topicModules.map(item => item.module), "Grammar", "Roleplay"],
    }).todayCompletedSkills).toBe(6);
    expect(buildHomeViewModel({ name: "Ali", todayCompletedTasks: 8 }).todayCompletedSkills).toBe(0);
    expect(buildHomeViewModel({ name: "Ali", skillsCompletedToday: [] }).todayCompletedSkills).toBe(0);
  });

  it("derives one consistent plan from partial API data", () => {
    const model = buildHomeViewModel({
      name: "Dilnoza",
      activeTopic: { id: "travel", title: "Travel", modules: topicModules },
      practiceWordCount: 4,
      savedCount: 12,
      streakAtRisk: true,
      goalMet: false,
      weekSeconds: 5_460,
      last7Days: [
        { day: "2026-07-21", seconds: 600 },
        { day: "2026-07-22", seconds: 1_260 },
      ],
    });
    expect(model.completed).toBe(1);
    expect(model.remaining).toBe(5);
    expect(model.nextAction.to).toBe("/app/grammar/topic/travel");
    expect(model.nextAction.key).toBe("Grammar");
    expect(model.daily.find((skill) => skill.key === "Reading")?.locked).toBe(true);
    expect(model.streakAtRisk).toBe(true);
    expect(model.modules.some((module) => module.to === "/app/speaking/role-talk")).toBe(true);
    expect(model.weeklyMinutes).toBe(91);
    expect(model.weeklyProgress).toEqual([0, 0, 0, 0, 0, 10, 21]);
  });

  it("falls back to the first skill when there is no active roadmap topic", () => {
    const model = buildHomeViewModel({ name: "Ali", goalMet: true });
    expect(model.nextAction.key).toBe("Vocabulary");
    expect(model.streakAtRisk).toBe(false);
    expect(model.weeklyProgress).toHaveLength(7);
    expect(model.weeklyMinutes).toBe(0);
  });

  it("uses the canonical topic skill order and advances to the unlocked frontier", () => {
    const expectedSequence = ["Vocabulary", "Grammar", "Reading", "Writing", "Speaking", "Listening"];

    expectedSequence.forEach((skill, completedCount) => {
      const modules = expectedSequence.map((module, index) => ({
        module,
        score: index < completedCount ? 90 : 0,
        passed: index < completedCount,
        unlocked: index <= completedCount,
        achievedAt: null,
      }));
      const model = buildHomeViewModel({
        name: "Ali",
        activeTopic: { id: "travel", title: "Travel", modules },
      });

      expect(model.nextAction.key).toBe(skill);
    });
  });

  it("moves Home to the next roadmap topic when the previous topic is mastered", () => {
    const model = buildHomeViewModel({
      name: "Ali",
      activeTopic: {
        id: "work",
        title: "Work",
        modules: topicModules.map((module, index) => ({
          ...module,
          score: 0,
          passed: false,
          unlocked: index === 0,
        })),
      },
    });

    expect(model.completed).toBe(0);
    expect(model.nextAction.key).toBe("Vocabulary");
    expect(model.nextAction.to).toBe("/app/vocabulary/topic/work");
    expect(model.activeTopicTitle).toBe("Work");
  });

  it("keeps Writing as the shared Home and Levels frontier", () => {
    const modules = topicModules.map((module) => ({
      ...module,
      passed: ["Vocabulary", "Grammar", "Reading"].includes(module.module),
      unlocked: ["Vocabulary", "Grammar", "Reading", "Writing"].includes(module.module),
    }));
    const model = buildHomeViewModel({
      name: "Ali",
      activeTopic: { id: "travel", title: "Travel", modules },
    });

    expect(model.nextAction.key).toBe("Writing");
    expect(model.nextAction.to).toBe("/writing/task/travel");
    expect(model.nextAction.skillKey).toBe("writing");
    expect(model.daily.find((skill) => skill.key === "Writing")?.current).toBe(true);
  });

  it("promotes the pending library or video recommendation above the next topic", () => {
    const library = buildHomeViewModel({ name: "Ali", fallbackKind: "books" });
    const video = buildHomeViewModel({ name: "Ali", fallbackKind: "video" });

    expect(library.nextAction.to).toBe("/books");
    expect(library.nextAction.fallbackKind).toBe("books");
    expect(video.nextAction.to).toBe("/video");
    expect(video.nextAction.fallbackKind).toBe("video");
  });
});
