package uz.englishai.app.widgets;

import com.getcapacitor.JSArray;
import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;

@CapacitorPlugin(name = "EnglishAiWidgets")
public class WidgetUpdatePlugin extends Plugin {
    @PluginMethod
    public void update(PluginCall call) {
        JSArray scoreArray = call.getArray("scores", new JSArray());
        JSArray completedArray = call.getArray("completed", new JSArray());
        int[] scores = new int[WidgetSnapshot.SKILL_COUNT];
        boolean[] completed = new boolean[WidgetSnapshot.SKILL_COUNT];
        for (int index = 0; index < WidgetSnapshot.SKILL_COUNT; index++) {
            try {
                scores[index] = index < scoreArray.length() ? scoreArray.getInt(index) : 0;
                completed[index] = index < completedArray.length() && completedArray.getBoolean(index);
            } catch (Exception ignored) {
                scores[index] = 0;
                completed[index] = false;
            }
        }
        WidgetSnapshot snapshot = WidgetSnapshot.create(
            scores,
            completed,
            call.getInt("completedCount", 0),
            call.getInt("leaderboardRank", 0),
            call.getString("leagueName", null));
        WidgetStore.save(
            getContext(), snapshot, call.getInt("streak", 0),
            call.getInt("overallScore", 0), call.getInt("weeklyMinutes", 0));
        EnglishAiWidgetProvider.updateAll(getContext());
        call.resolve(new JSObject().put("updated", true));
    }
}
