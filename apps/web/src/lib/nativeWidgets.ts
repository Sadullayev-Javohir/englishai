import { Capacitor, registerPlugin } from "@capacitor/core";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { buildNativeWidgetSnapshot } from "./widgetSnapshot";

interface EnglishAiWidgetsPlugin {
  update(options: {
    scores: number[];
    completed: boolean[];
    completedCount: number;
    leaderboardRank: number;
    leagueName: string;
    streak: number;
    overallScore: number;
    weeklyMinutes: number;
  }): Promise<{ updated: boolean }>;
}

const EnglishAiWidgets = registerPlugin<EnglishAiWidgetsPlugin>("EnglishAiWidgets");

/** Refreshes public widget display values. Auth tokens and personal text never enter widget storage. */
export async function syncNativeWidgets(): Promise<void> {
  if (!Capacitor.isNativePlatform() || Capacitor.getPlatform() !== "android") return;
  const learnerId = getLearnerId();
  try {
    const today = new Date().toISOString().slice(0, 10);
    const [gamification, overview, leaderboard, studyStats] = await Promise.all([
      api.gamification.status(learnerId),
      api.learning.overview(learnerId),
      api.gamification.leaderboard(learnerId),
      api.learning.studyStats(learnerId, today),
    ]);
    const snapshot = buildNativeWidgetSnapshot(gamification, overview, leaderboard, studyStats.weekSeconds / 60);
    await EnglishAiWidgets.update({ ...snapshot, streak: gamification.currentStreak });
  } catch {
    // Widgets retain their last valid snapshot when offline or signed out.
  }
}
