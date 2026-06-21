package uz.englishai.app;

import android.content.ComponentName;
import android.content.Context;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import androidx.annotation.NonNull;
import androidx.work.ExistingPeriodicWorkPolicy;
import androidx.work.PeriodicWorkRequest;
import androidx.work.WorkManager;
import androidx.work.Worker;
import androidx.work.WorkerParameters;
import java.util.concurrent.TimeUnit;

public final class LauncherIconManager {
    private static final String PREFS = "englishai_launcher";
    private static final String LAST_OPENED_AT = "last_opened_at";
    private static final String WORK_NAME = "englishai-launcher-icon-refresh";
    private static final long RED_AFTER_MS = TimeUnit.HOURS.toMillis(24);
    private static final long GRAY_AFTER_MS = TimeUnit.HOURS.toMillis(72);
    private static final String NORMAL = "uz.englishai.app.LauncherNormal";
    private static final String RED = "uz.englishai.app.LauncherRed";
    private static final String GRAY = "uz.englishai.app.LauncherGray";

    private LauncherIconManager() {}

    public static void markOpened(Context context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit().putLong(LAST_OPENED_AT, System.currentTimeMillis()).apply();
        setAlias(context, NORMAL);
    }

    public static void refresh(Context context) {
        SharedPreferences preferences = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
        long lastOpenedAt = preferences.getLong(LAST_OPENED_AT, System.currentTimeMillis());
        long inactiveFor = Math.max(0, System.currentTimeMillis() - lastOpenedAt);
        setAlias(context, inactiveFor >= GRAY_AFTER_MS ? GRAY : inactiveFor >= RED_AFTER_MS ? RED : NORMAL);
    }

    public static void schedule(Context context) {
        PeriodicWorkRequest request = new PeriodicWorkRequest.Builder(IconRefreshWorker.class, 12, TimeUnit.HOURS).build();
        WorkManager.getInstance(context).enqueueUniquePeriodicWork(WORK_NAME, ExistingPeriodicWorkPolicy.UPDATE, request);
    }

    private static void setAlias(Context context, String activeAlias) {
        PackageManager manager = context.getPackageManager();
        for (String alias : new String[] { NORMAL, RED, GRAY }) {
            int state = alias.equals(activeAlias)
                ? PackageManager.COMPONENT_ENABLED_STATE_ENABLED
                : PackageManager.COMPONENT_ENABLED_STATE_DISABLED;
            manager.setComponentEnabledSetting(new ComponentName(context, alias), state, PackageManager.DONT_KILL_APP);
        }
    }

    public static final class IconRefreshWorker extends Worker {
        public IconRefreshWorker(@NonNull Context context, @NonNull WorkerParameters parameters) {
            super(context, parameters);
        }

        @NonNull @Override public Result doWork() {
            refresh(getApplicationContext());
            return Result.success();
        }
    }
}
