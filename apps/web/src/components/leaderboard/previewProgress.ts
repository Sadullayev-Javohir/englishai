import {
  CefrLevel,
  SkillType,
  type LeaderboardEntryDto,
  type PublicLearnerProgressDto,
} from "@/api/types";

const SKILLS = [
  SkillType.Speaking,
  SkillType.Listening,
  SkillType.Reading,
  SkillType.Writing,
  SkillType.Grammar,
  SkillType.Vocabulary,
];

export function previewLearnerProgress(
  entry: LeaderboardEntryDto,
  today: string,
): PublicLearnerProgressDto | null {
  if (!entry.learnerId.startsWith("preview-")) return null;

  const seed = [...entry.learnerId].reduce((sum, char) => sum + char.charCodeAt(0), 0);
  const level = Math.min(CefrLevel.C2, Math.max(CefrLevel.A1, 1 + Math.floor(entry.score / 700))) as CefrLevel;
  const skillScores = SKILLS.map((skill, index) => ({
    skill,
    score: Math.min(96, 48 + ((seed + index * 13) % 43)),
    sampleCount: 8 + ((seed + index * 7) % 22),
  }));
  const last7Days = Array.from({ length: 7 }, (_, index) => {
    const day = new Date(`${today}T00:00:00Z`);
    day.setUTCDate(day.getUTCDate() - (6 - index));
    return { day: day.toISOString().slice(0, 10), seconds: 900 + ((seed + index * 811) % 4200) };
  });
  const growth = Array.from({ length: 8 }, (_, weekIndex) =>
    SKILLS.map((skill, skillIndex) => {
      const week = new Date(`${today}T00:00:00Z`);
      week.setUTCDate(week.getUTCDate() - (7 - weekIndex) * 7);
      return {
        weekEnding: week.toISOString(),
        skill,
        score: Math.max(20, skillScores[skillIndex].score - (7 - weekIndex) * 2),
      };
    }),
  ).flat();
  const weekSeconds = last7Days.reduce((sum, bucket) => sum + bucket.seconds, 0);
  const totalSeconds = weekSeconds * 8;
  const topicsTotal = 24;
  const topicsLearned = Math.min(topicsTotal, 8 + (seed % 15));

  return {
    learnerId: entry.learnerId,
    displayName: entry.displayName,
    pictureUrl: entry.pictureUrl,
    isPremium: entry.isPremium,
    lifetimeXp: entry.score,
    rank: entry.rank,
    overallLevel: level,
    skillScores,
    topicsTotal,
    topicsLearned,
    topicsMastered: Math.max(0, topicsLearned - 3),
    studyStats: {
      todaySeconds: last7Days[6].seconds,
      weekSeconds,
      monthSeconds: Math.round(totalSeconds / 2),
      yearSeconds: totalSeconds,
      totalSeconds,
      activeDays: 34 + (seed % 55),
      currentDayStreak: 4 + (seed % 12),
      averageSecondsPerActiveDay: 2100 + (seed % 1800),
      longestDaySeconds: Math.max(...last7Days.map((bucket) => bucket.seconds)),
      last7Days,
      last12Months: [],
      bySkill: [],
      yearHeatmap: [],
    },
    gamification: {
      todayCompletedTasks: 2 + (seed % 4),
      dailyGoalTarget: 6,
      isGoalMet: seed % 3 === 0,
      currentStreak: 4 + (seed % 12),
      longestStreak: 16 + (seed % 30),
      isStreakAtRisk: false,
      skillsCompletedToday: [],
    },
    growth,
  };
}
