package uz.englishai.app.widgets;

import android.content.Context;
import android.widget.RemoteViews;
import uz.englishai.app.R;

public final class SkillStatsWidget extends EnglishAiWidgetProvider {
    private static final int[] SCORE_IDS = {R.id.skill_1, R.id.skill_2, R.id.skill_3, R.id.skill_4, R.id.skill_5, R.id.skill_6};
    private static final String[] LABELS = {"Gapirish", "Tinglash", "O‘qish", "Yozish", "Grammatika", "Lug‘at"};

    @Override protected RemoteViews render(Context context, WidgetSnapshot snapshot) {
        RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.widget_skill_stats);
        int[] scores = snapshot.skillScores();
        views.setTextViewText(R.id.widget_overall, "Umumiy progress  " + WidgetStore.overallScore(context) + "%");
        views.setTextViewText(R.id.widget_weekly, "Bu hafta  " + WidgetStore.weeklyMinutes(context) + " daqiqa");
        for (int index = 0; index < SCORE_IDS.length; index++) {
            views.setTextViewText(SCORE_IDS[index], LABELS[index] + "  ·  " + scores[index] + "%");
        }
        views.setOnClickPendingIntent(R.id.widget_root, launchApp(context, "/progress", 102));
        return views;
    }
}
