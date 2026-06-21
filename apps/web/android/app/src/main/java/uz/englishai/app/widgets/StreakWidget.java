package uz.englishai.app.widgets;

import android.content.Context;
import android.widget.RemoteViews;
import uz.englishai.app.R;

public final class StreakWidget extends EnglishAiWidgetProvider {
    @Override protected RemoteViews render(Context context, WidgetSnapshot snapshot) {
        RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.widget_streak);
        views.setTextViewText(R.id.widget_streak_count, String.valueOf(WidgetStore.streak(context)));
        views.setOnClickPendingIntent(R.id.widget_root, launchApp(context, "/home", 104));
        return views;
    }
}
