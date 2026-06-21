package uz.englishai.app.widgets;

import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;
import uz.englishai.app.MainActivity;
import uz.englishai.app.R;

public abstract class EnglishAiWidgetProvider extends AppWidgetProvider {
    protected abstract RemoteViews render(Context context, WidgetSnapshot snapshot);

    @Override
    public void onUpdate(Context context, AppWidgetManager manager, int[] appWidgetIds) {
        WidgetSnapshot snapshot = WidgetStore.read(context);
        for (int appWidgetId : appWidgetIds) {
            RemoteViews views = render(context, snapshot);
            manager.updateAppWidget(appWidgetId, views);
        }
    }

    protected static PendingIntent launchApp(Context context, String route, int requestCode) {
        Intent intent = new Intent(context, MainActivity.class)
            .setData(android.net.Uri.parse("uz.englishai.app://" + route.replaceFirst("^/", "")))
            .setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        return PendingIntent.getActivity(context, requestCode, intent, PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
    }

    public static void updateAll(Context context) {
        AppWidgetManager manager = AppWidgetManager.getInstance(context);
        Class<?>[] providers = {DailySkillsWidget.class, SkillStatsWidget.class, LeaderboardWidget.class, StreakWidget.class};
        for (Class<?> provider : providers) {
            ComponentName component = new ComponentName(context, provider);
            int[] ids = manager.getAppWidgetIds(component);
            if (ids.length > 0) {
                Intent update = new Intent(context, provider)
                    .setAction(AppWidgetManager.ACTION_APPWIDGET_UPDATE)
                    .putExtra(AppWidgetManager.EXTRA_APPWIDGET_IDS, ids);
                context.sendBroadcast(update);
            }
        }
    }
}
