import { describe, expect, it } from "vitest";
import { leagueForXp, leagueProgressForXp } from "./useLeagueTier";

describe("XP league policy", () => {
  it.each([
    [0, "bronze"], [499, "bronze"], [500, "silver"], [1_499, "silver"],
    [1_500, "gold"], [2_999, "gold"], [3_000, "platinum"],
    [5_999, "platinum"], [6_000, "diamond"],
  ] as const)("maps %i XP to %s", (xp, expected) => {
    expect(leagueForXp(xp)).toBe(expected);
  });

  it("reports progress and remaining XP for the next league", () => {
    expect(leagueProgressForXp(1_800)).toMatchObject({
      tier: "gold", xpIntoTier: 300, xpForTier: 1_500, xpToNext: 1_200, progressPercent: 20,
    });
  });

  it("marks diamond as completed with no next league", () => {
    expect(leagueProgressForXp(7_000)).toMatchObject({
      tier: "diamond", next: null, xpToNext: 0, progressPercent: 100,
    });
  });
});
