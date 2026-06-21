package uz.englishai.app.widgets;

import android.content.Context;
import android.widget.RemoteViews;
import uz.englishai.app.R;

public final class LeaderboardWidget extends EnglishAiWidgetProvider {
    @Override protected RemoteViews render(Context context, WidgetSnapshot snapshot) {
        RemoteViews views = new RemoteViews(context.getPackageName(), R.layout.widget_leaderboard);
        views.setTextViewText(R.id.widget_league, snapshot.leagueName());
        views.setTextViewText(R.id.widget_rank, snapshot.leaderboardRank() > 0 ? "#" + snapshot.leaderboardRank() : "-");
        views.setOnClickPendingIntent(R.id.widget_root, launchApp(context, "/leaderboard", 103));
        return views;
    }
}
