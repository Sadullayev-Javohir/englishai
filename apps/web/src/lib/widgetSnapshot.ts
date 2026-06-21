import { SkillType, type GamificationStatusDto, type LearnerOverviewDto, type LeaderboardDto } from "@/api/types";

export const WIDGET_SKILLS: readonly SkillType[] = [
  SkillType.Speaking,
  SkillType.Listening,
  SkillType.Reading,
  SkillType.Writing,
  SkillType.Grammar,
  SkillType.Vocabulary,
];

export interface NativeWidgetSnapshot {
  scores: number[];
  completed: boolean[];
  completedCount: number;
  leaderboardRank: number;
  leagueName: string;
  overallScore: number;
  weeklyMinutes: number;
}

function leagueNameForRank(rank: number): string {
  if (rank > 0 && rank <= 3) return "Oltin liga";
  if (rank > 0 && rank <= 20) return "Kumush liga";
  return "Bronza liga";
}

export function buildNativeWidgetSnapshot(
  gamification: GamificationStatusDto,
  overview: LearnerOverviewDto,
  leaderboard: LeaderboardDto,
  weeklyMinutes = 0,
): NativeWidgetSnapshot {
  const scoreBySkill = new Map(overview.skillScores.map((item) => [item.skill, item.score]));
  const completedToday = new Set(gamification.skillsCompletedToday.map((skill) => skill.toLowerCase()));
  const rank = leaderboard.currentUserEntry?.rank ?? 0;

  return {
    scores: WIDGET_SKILLS.map((skill) => Math.round(Math.max(0, Math.min(100, scoreBySkill.get(skill) ?? 0)))),
    completed: WIDGET_SKILLS.map((skill) => completedToday.has(SkillType[skill].toLowerCase())),
    completedCount: Math.max(0, Math.min(WIDGET_SKILLS.length, gamification.todayCompletedTasks)),
    leaderboardRank: Math.max(0, rank),
    leagueName: leagueNameForRank(rank),
    overallScore: Math.round(
      overview.skillScores.reduce((sum, item) => sum + item.score, 0) /
        Math.max(1, overview.skillScores.length),
    ),
    weeklyMinutes: Math.max(0, Math.round(weeklyMinutes)),
  };
}
