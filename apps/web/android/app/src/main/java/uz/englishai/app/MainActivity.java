package uz.englishai.app;

import android.os.Bundle;
import android.view.View;
import android.webkit.WebSettings;
import android.webkit.WebView;
import com.getcapacitor.BridgeActivity;
import uz.englishai.app.widgets.WidgetUpdatePlugin;

public class MainActivity extends BridgeActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        registerPlugin(WidgetUpdatePlugin.class);
        super.onCreate(savedInstanceState);
        LauncherIconManager.markOpened(this);
        WebView webView = getBridge().getWebView();
        webView.setOverScrollMode(View.OVER_SCROLL_NEVER);
        WebSettings settings = webView.getSettings();
        settings.setTextZoom(100);
    }

    @Override
    public void onResume() {
        super.onResume();
        LauncherIconManager.markOpened(this);
    }
}
