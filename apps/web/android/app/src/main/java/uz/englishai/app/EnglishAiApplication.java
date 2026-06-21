package uz.englishai.app;

import android.app.Application;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.media.AudioAttributes;
import android.net.Uri;
import android.os.Build;

public final class EnglishAiApplication extends Application {
    public static final String LEARNING_CHANNEL_ID = "englishai_learning_v2";

    @Override
    public void onCreate() {
        super.onCreate();
        createLearningNotificationChannel();
        LauncherIconManager.schedule(this);
    }

    private void createLearningNotificationChannel() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return;
        Uri sound = Uri.parse("android.resource://" + getPackageName() + "/" + R.raw.englishai_reminder);
        AudioAttributes attributes = new AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_NOTIFICATION)
            .setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
            .build();
        NotificationChannel channel = new NotificationChannel(
            LEARNING_CHANNEL_ID,
            "EnglishAI.uz o‘rganish eslatmalari",
            NotificationManager.IMPORTANCE_HIGH);
        channel.setDescription("Kunlik mashqlar, SRS va muhim o‘rganish eslatmalari");
        channel.enableVibration(true);
        channel.setSound(sound, attributes);
        getSystemService(NotificationManager.class).createNotificationChannel(channel);
    }
}
