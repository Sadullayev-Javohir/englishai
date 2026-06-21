// Native shell UI chrome (status bar) setup. Web is unaffected - every call is guarded by
// isNativePlatform() and failures are swallowed so a missing plugin never blocks startup.
import { App } from "@capacitor/app";
import { Keyboard } from "@capacitor/keyboard";
import { StatusBar, Style } from "@capacitor/status-bar";
import { isNativePlatform } from "./nativeAuth";

let started = false;

const transientOverlaySelector = [
  '[role="dialog"]',
  '[aria-modal="true"]',
  ".ea-overlay",
  ".ea-modal-backdrop",
].join(",");

function setNativeState(name: string, enabled: boolean): void {
  document.documentElement.toggleAttribute(name, enabled);
}

function dismissTransientOverlay(): boolean {
  const overlay = document.querySelector<HTMLElement>(transientOverlaySelector);
  if (!overlay) return false;

  const closeControl = overlay.querySelector<HTMLElement>(
    '[data-native-back-close], [data-dialog-close], button[aria-label*="close" i], button[aria-label*="yop" i], button[title*="close" i], button[title*="yop" i]',
  );
  if (closeControl) {
    closeControl.click();
  } else {
    document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }));
  }
  return true;
}

function handleHardwareBack(canGoBack: boolean): void {
  if (dismissTransientOverlay()) return;
  if (canGoBack || window.history.length > 1) {
    window.history.back();
    return;
  }
  void App.minimizeApp();
}

/**
 * One-time native UI initialisation, called at app startup. The status-bar icon
 * color follows the effective application theme, and the web view
 * starts below the native status bar. This avoids OEM-specific cutout/status-bar overlap while
 * CSS safe-area handling continues to protect devices with display notches and gesture insets.
 */
export async function initNativeUi(): Promise<void> {
  if (!isNativePlatform() || started) return;
  started = true;
  setNativeState("data-native-app", true);
  setNativeState("data-native-active", true);

  try {
    await StatusBar.setOverlaysWebView({ overlay: false });
    await StatusBar.setStyle({ style: Style.Dark });
    try { await StatusBar.setBackgroundColor({ color: "#FFFCF7" }); } catch { /* Not supported on every native host. */ }

    await Promise.all([
      Keyboard.addListener("keyboardWillShow", () => setNativeState("data-native-keyboard-open", true)),
      Keyboard.addListener("keyboardWillHide", () => setNativeState("data-native-keyboard-open", false)),
      App.addListener("appStateChange", ({ isActive }) => {
        setNativeState("data-native-active", isActive);
        window.dispatchEvent(new CustomEvent(isActive ? "app:native-resume" : "app:native-pause"));
      }),
      App.addListener("backButton", ({ canGoBack }) => handleHardwareBack(canGoBack)),
    ]);
  } catch {
    started = false;
  }
}
