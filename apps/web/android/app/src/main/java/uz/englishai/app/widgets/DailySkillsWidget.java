package uz.englishai.app.widgets;

import android.content.Context;
import android.widget.RemoteViews;
import uz.englishai.app.R;

public final class DailySkillsWidget extends EnglishAiWidgetProvider {
    private static final int[] CHECK_IDS = {R.id.skill_1, R.id.skill_2, R.id.skill_3, R.id.skill_4, R.id.skill_5, R.id.skill_6};
    private static final String[] LABELS = {"Gapirish", "Tinglash", "O‘qish", "Yozish", "Grammatika", "Lug‘at"};

    @Override protected RemoteViews render(Context context, WidgetSnapshot snapshot) {
        RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.widget_daily_skills);
        views.setTextViewText(R.id.widget_title, "Bugungi 6 skill");
        views.setTextViewText(R.id.widget_summary, snapshot.completedCount() + "/6 bajarildi");
        boolean[] done = snapshot.completedSkills();
        for (int index = 0; index < CHECK_IDS.length; index++) {
            views.setTextViewText(CHECK_IDS[index], (done[index] ? "✓  " : "○  ") + LABELS[index]);
            views.setTextColor(CHECK_IDS[index], context.getColor(done[index] ? R.color.widget_green : R.color.widget_text));
        }
        views.setOnClickPendingIntent(R.id.widget_root, launchApp(context, "/home", 101));
        return views;
    }
}
