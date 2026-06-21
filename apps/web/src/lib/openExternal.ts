import { Browser } from "@capacitor/browser";
import { isNativePlatform } from "@/api/nativeAuth";

/**
 * Opens an external http(s) URL in a way that works on both the web app and the native APK.
 *
 * On the web a plain `window.open(_blank)` opens a new tab. Inside the Capacitor shell a new browser
 * tab has nowhere to go - `window.open` is unreliable in a WebView - so we hand the URL to the
 * Capacitor Browser plugin, which opens it in the system in-app browser (Chrome Custom Tab / Safari
 * View Controller). This is what makes a broadcast's "Havolani ochish" button work on the phone.
 */
export function openExternalUrl(url: string): void {
  if (isNativePlatform()) {
    // Fire-and-forget: the plugin call is async, but the caller doesn't need to await it.
    void Browser.open({ url });
  } else {
    window.open(url, "_blank", "noopener,noreferrer");
  }
}
