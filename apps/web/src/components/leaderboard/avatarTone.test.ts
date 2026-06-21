import { describe, expect, it } from "vitest";
import { avatarTone } from "./avatarTone";

describe("avatarTone", () => {
  it("returns a stable tone for the same learner", () => {
    expect(avatarTone("learner-1")).toEqual(avatarTone("learner-1"));
  });

  it("distributes different learners across tones", () => {
    const tones = new Set(Array.from({ length: 20 }, (_, index) => avatarTone(`learner-${index}`).background));
    expect(tones.size).toBeGreaterThan(1);
  });
});
