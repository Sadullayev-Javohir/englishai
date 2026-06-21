import { beforeEach, describe, expect, it, vi } from "vitest";

const listeners = new Map<string, (payload?: unknown) => void>();
const minimizeApp = vi.fn();
const setOverlaysWebView = vi.fn();
const setStyle = vi.fn();

vi.mock("@capacitor/app", () => ({
  App: {
    addListener: vi.fn(async (eventName: string, listener: (payload?: unknown) => void) => {
      listeners.set(eventName, listener);
      return { remove: vi.fn() };
    }),
    minimizeApp,
  },
}));

vi.mock("@capacitor/keyboard", () => ({
  Keyboard: {
    addListener: vi.fn(async (eventName: string, listener: (payload?: unknown) => void) => {
      listeners.set(eventName, listener);
      return { remove: vi.fn() };
    }),
  },
}));

vi.mock("@capacitor/status-bar", () => ({
  StatusBar: { setOverlaysWebView, setStyle, setBackgroundColor: vi.fn().mockResolvedValue(undefined) },
  Style: { Dark: "DARK" },
}));

vi.mock("./nativeAuth", () => ({ isNativePlatform: () => true }));

describe("initNativeUi", () => {
  beforeEach(() => {
    vi.resetModules();
    listeners.clear();
    minimizeApp.mockClear();
    setOverlaysWebView.mockClear();
    setStyle.mockClear();
    document.documentElement.removeAttribute("data-native-app");
    document.documentElement.removeAttribute("data-native-active");
    document.documentElement.removeAttribute("data-native-keyboard-open");
    document.body.innerHTML = "";
  });

  it("marks the native document and tracks keyboard and app lifecycle state", async () => {
    const { initNativeUi } = await import("./nativeUi");
    await initNativeUi();

    expect(document.documentElement.hasAttribute("data-native-app")).toBe(true);
    expect(document.documentElement.hasAttribute("data-native-active")).toBe(true);
    expect(setOverlaysWebView).toHaveBeenCalledWith({ overlay: false });

    listeners.get("keyboardWillShow")?.();
    expect(document.documentElement.hasAttribute("data-native-keyboard-open")).toBe(true);
    listeners.get("keyboardWillHide")?.();
    expect(document.documentElement.hasAttribute("data-native-keyboard-open")).toBe(false);

    listeners.get("appStateChange")?.({ isActive: false });
    expect(document.documentElement.hasAttribute("data-native-active")).toBe(false);
  });

  it("closes an active dialog before navigating on hardware back", async () => {
    const close = document.createElement("button");
    close.setAttribute("aria-label", "Close dialog");
    const onClose = vi.fn();
    close.addEventListener("click", onClose);
    const dialog = document.createElement("div");
    dialog.setAttribute("role", "dialog");
    dialog.append(close);
    document.body.append(dialog);

    const { initNativeUi } = await import("./nativeUi");
    await initNativeUi();
    listeners.get("backButton")?.({ canGoBack: false });

    expect(onClose).toHaveBeenCalledOnce();
    expect(minimizeApp).not.toHaveBeenCalled();
  });

  it("minimizes at the root when there is no overlay or history entry", async () => {
    const { initNativeUi } = await import("./nativeUi");
    await initNativeUi();
    const originalLength = window.history.length;

    listeners.get("backButton")?.({ canGoBack: false });

    if (originalLength <= 1) expect(minimizeApp).toHaveBeenCalledOnce();
  });
});
