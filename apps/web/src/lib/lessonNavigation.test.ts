import { describe, expect, it, vi } from "vitest";
import { lessonBackTarget, lessonOriginFrom } from "./lessonNavigation";
import { openSkill, SKILL_STEPS } from "./skillSteps";

describe("lesson navigation", () => {
  it("uses the skill catalog when no trusted origin exists", () => {
    expect(lessonOriginFrom({ state: null })).toBe("catalog");
    expect(lessonBackTarget("vocabulary", "catalog")).toBe("/app/vocabulary/topics");
    expect(lessonBackTarget("speaking", "catalog")).toBe("/app/speaking");
  });

  it("returns every skill to levels for a levels-origin lesson", () => {
    for (const step of SKILL_STEPS) {
      expect(lessonBackTarget(step.key, "levels")).toBe("/levels");
    }
  });

  it("propagates levels origin through every skill route", () => {
    const navigate = vi.fn();

    for (const step of SKILL_STEPS) {
      openSkill(navigate, step, "topic-1", "Travel", "levels");
    }

    for (const [, options] of navigate.mock.calls) {
      expect(options?.state?.lessonOrigin).toBe("levels");
    }
  });
});
