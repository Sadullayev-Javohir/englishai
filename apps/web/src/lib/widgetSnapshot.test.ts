import { describe, expect, it } from "vitest";
import { SkillType, type GamificationStatusDto, type LearnerOverviewDto, type LeaderboardDto } from "@/api/types";
import { buildNativeWidgetSnapshot } from "./widgetSnapshot";

const gamification: GamificationStatusDto = {
  todayCompletedTasks: 2,
  dailyGoalTarget: 6,
  isGoalMet: false,
  currentStreak: 4,
  longestStreak: 9,
  isStreakAtRisk: false,
  skillsCompletedToday: ["Speaking", "Vocabulary"],
};

const overview = {
  skillScores: [
    { skill: SkillType.Speaking, score: 82, sampleCount: 3 },
    { skill: SkillType.Vocabulary, score: 61, sampleCount: 4 },
  ],
} as LearnerOverviewDto;

const leaderboard = {
  currentUserEntry: { rank: 17 },
} as LeaderboardDto;

describe("buildNativeWidgetSnapshot", () => {
  it("maps all six skills in canonical order and keeps missing scores at zero", () => {
    expect(buildNativeWidgetSnapshot(gamification, overview, leaderboard)).toEqual({
      scores: [82, 0, 0, 0, 0, 61],
      completed: [true, false, false, false, false, true],
      completedCount: 2,
      leaderboardRank: 17,
      leagueName: "Kumush liga",
      overallScore: 72,
      weeklyMinutes: 0,
    });
  });
});
