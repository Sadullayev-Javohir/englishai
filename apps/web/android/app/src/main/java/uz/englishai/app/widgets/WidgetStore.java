package uz.englishai.app.widgets;

import android.content.Context;
import android.content.SharedPreferences;

public final class WidgetStore {
    private static final String PREFS = "englishai_widgets";
    private static final String[] SCORE_KEYS = {"speaking", "listening", "reading", "writing", "grammar", "vocabulary"};

    private WidgetStore() {}

    public static void save(Context context, WidgetSnapshot snapshot, int streak, int overallScore, int weeklyMinutes) {
        SharedPreferences.Editor editor = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit();
        int[] scores = snapshot.skillScores();
        boolean[] completed = snapshot.completedSkills();
        for (int index = 0; index < WidgetSnapshot.SKILL_COUNT; index++) {
            editor.putInt("score_" + SCORE_KEYS[index], scores[index]);
            editor.putBoolean("done_" + SCORE_KEYS[index], completed[index]);
        }
        editor.putInt("completed_count", snapshot.completedCount());
        editor.putInt("leaderboard_rank", snapshot.leaderboardRank());
        editor.putString("league_name", snapshot.leagueName());
        editor.putInt("streak", Math.max(0, streak));
        editor.putInt("overall_score", Math.max(0, Math.min(100, overallScore)));
        editor.putInt("weekly_minutes", Math.max(0, weeklyMinutes));
        editor.apply();
    }

    public static WidgetSnapshot read(Context context) {
        SharedPreferences preferences = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
        int[] scores = new int[WidgetSnapshot.SKILL_COUNT];
        boolean[] completed = new boolean[WidgetSnapshot.SKILL_COUNT];
        for (int index = 0; index < WidgetSnapshot.SKILL_COUNT; index++) {
            scores[index] = preferences.getInt("score_" + SCORE_KEYS[index], 0);
            completed[index] = preferences.getBoolean("done_" + SCORE_KEYS[index], false);
        }
        return WidgetSnapshot.create(
            scores,
            completed,
            preferences.getInt("completed_count", 0),
            preferences.getInt("leaderboard_rank", 0),
            preferences.getString("league_name", null));
    }

    public static int streak(Context context) {
        return context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getInt("streak", 0);
    }

    public static int overallScore(Context context) {
        return context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getInt("overall_score", 0);
    }

    public static int weeklyMinutes(Context context) {
        return context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getInt("weekly_minutes", 0);
    }
}
