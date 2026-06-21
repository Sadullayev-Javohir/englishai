import { act, cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { enterSecureAssessment, useSecureAssessment } from "./secureAssessment";
import { isKioskLocked, releaseKioskLock } from "./kioskLock";

const { reportIntegrityViolation } = vi.hoisted(() => ({ reportIntegrityViolation: vi.fn() }));
vi.mock("@/api/client", () => ({
  api: { placement: { reportIntegrityViolation } },
  apiErrorDetails: () => null,
}));

let fullscreenElement: Element | null = null;
Object.defineProperty(document, "fullscreenElement", { configurable: true, get: () => fullscreenElement });

const requestFullscreen = vi.fn(async () => {
  fullscreenElement = document.documentElement;
  document.dispatchEvent(new Event("fullscreenchange"));
});

/** Re-installs the default (fullscreen-capable desktop) environment. */
function installFullscreenApi() {
  Object.defineProperty(document, "fullscreenEnabled", { configurable: true, value: true });
  Object.defineProperty(document.documentElement, "requestFullscreen", { configurable: true, value: requestFullscreen });
}

/** Removes the Fullscreen API entirely - iOS Safari / WKWebView. */
function withoutFullscreenApi() {
  Object.defineProperty(document, "fullscreenEnabled", { configurable: true, value: false });
  delete (document.documentElement as unknown as Record<string, unknown>).requestFullscreen;
}

Object.defineProperty(document, "exitFullscreen", {
  configurable: true,
  value: vi.fn(async () => {
    fullscreenElement = null;
    document.dispatchEvent(new Event("fullscreenchange"));
  }),
});
installFullscreenApi();

/** Pretends the device is a phone/tablet, where `blur` must not count as a violation. */
function asTouchDevice() {
  Object.defineProperty(navigator, "maxTouchPoints", { configurable: true, value: 5 });
}

afterEach(() => {
  cleanup();
  releaseKioskLock();
  reportIntegrityViolation.mockReset();
  requestFullscreen.mockClear();
  fullscreenElement = document.documentElement;
  installFullscreenApi();
  Object.defineProperty(navigator, "maxTouchPoints", { configurable: true, value: 0 });
});

describe("enterSecureAssessment", () => {
  it("uses the real Fullscreen API when the browser has one", async () => {
    fullscreenElement = null;

    await expect(enterSecureAssessment()).resolves.toBe("fullscreen");
    expect(requestFullscreen).toHaveBeenCalledTimes(1);
    expect(isKioskLocked()).toBe(false);
  });

  it("falls back to the kiosk lock instead of failing when there is no Fullscreen API", async () => {
    fullscreenElement = null;
    withoutFullscreenApi();

    await expect(enterSecureAssessment()).resolves.toBe("kiosk");
    expect(isKioskLocked()).toBe(true);
    expect(document.documentElement.classList.contains("secure-assessment-kiosk")).toBe(true);
  });
});

describe("useSecureAssessment", () => {
  it("deduplicates simultaneous browser events into one incident", async () => {
    fullscreenElement = document.documentElement;
    reportIntegrityViolation.mockResolvedValue({ violationCount: 1, invalidated: false });
    const { result } = renderHook(() => useSecureAssessment("session-1", true));

    act(() => {
      window.dispatchEvent(new Event("blur"));
      fullscreenElement = null;
      document.dispatchEvent(new Event("fullscreenchange"));
    });

    await waitFor(() => expect(reportIntegrityViolation).toHaveBeenCalledTimes(1));
    expect(result.current.warningOpen).toBe(true);
  });

  it("invalidates when the server reports the session invalidated", async () => {
    fullscreenElement = document.documentElement;
    reportIntegrityViolation
      .mockResolvedValueOnce({ violationCount: 1, invalidated: false })
      .mockResolvedValueOnce({ violationCount: 2, invalidated: true });
    const onInvalidated = vi.fn();
    const { result } = renderHook(() => useSecureAssessment("session-1", true, onInvalidated));

    act(() => window.dispatchEvent(new Event("blur")));
    await waitFor(() => expect(result.current.warningOpen).toBe(true));
    await act(() => result.current.returnToTest());
    act(() => window.dispatchEvent(new Event("blur")));

    await waitFor(() => expect(result.current.invalidated).toBe(true));
    expect(onInvalidated).toHaveBeenCalledTimes(1);
  });

  it("suppresses intentional fullscreen exit", async () => {
    fullscreenElement = document.documentElement;
    const { result } = renderHook(() => useSecureAssessment("session-1", true));

    await act(() => result.current.leaveSecureAssessment());

    expect(reportIntegrityViolation).not.toHaveBeenCalled();
  });

  it("ignores window blur on touch devices but still reports backgrounding", async () => {
    asTouchDevice();
    reportIntegrityViolation.mockResolvedValue({ violationCount: 1, invalidated: false });
    const visibilityState = vi.spyOn(document, "visibilityState", "get").mockReturnValue("hidden");
    const { result } = renderHook(() => useSecureAssessment("session-1", true, undefined, "kiosk"));

    act(() => window.dispatchEvent(new Event("blur")));
    expect(reportIntegrityViolation).not.toHaveBeenCalled();

    act(() => document.dispatchEvent(new Event("visibilitychange")));

    await waitFor(() => expect(reportIntegrityViolation).toHaveBeenCalledTimes(1));
    expect(reportIntegrityViolation.mock.calls[0][2]).toBe("visibility_hidden");
    expect(result.current.warningOpen).toBe(true);
    visibilityState.mockRestore();
  });

  it("reports pagehide as backgrounding", async () => {
    reportIntegrityViolation.mockResolvedValue({ violationCount: 1, invalidated: false });
    renderHook(() => useSecureAssessment("session-1", true, undefined, "kiosk"));

    act(() => window.dispatchEvent(new Event("pagehide")));

    await waitFor(() => expect(reportIntegrityViolation).toHaveBeenCalledTimes(1));
    expect(reportIntegrityViolation.mock.calls[0][2]).toBe("visibility_hidden");
  });

  it("ignores fullscreen exit on touch devices", async () => {
    // Android Chrome drops fullscreen for the soft keyboard (Writing) and the microphone
    // prompt (Speaking) - the last two questions of the test, neither of them cheating.
    asTouchDevice();
    fullscreenElement = document.documentElement;
    renderHook(() => useSecureAssessment("session-1", true));

    act(() => {
      fullscreenElement = null;
      document.dispatchEvent(new Event("fullscreenchange"));
    });

    await Promise.resolve();
    expect(reportIntegrityViolation).not.toHaveBeenCalled();
  });

  it("does not report the fullscreen exit a permission prompt causes", async () => {
    fullscreenElement = document.documentElement;
    const { result } = renderHook(() => useSecureAssessment("session-1", true));

    await act(() => result.current.withoutFullscreenWatch(async () => {
      fullscreenElement = null;
      document.dispatchEvent(new Event("fullscreenchange"));
    }));

    expect(reportIntegrityViolation).not.toHaveBeenCalled();
    // Fullscreen is gone for good, so the kiosk lock takes over as the secure surface.
    expect(isKioskLocked()).toBe(true);
  });

  it("keeps watching fullscreen when the prompt leaves the page in fullscreen", async () => {
    fullscreenElement = document.documentElement;
    reportIntegrityViolation.mockResolvedValue({ violationCount: 1, invalidated: false });
    const { result } = renderHook(() => useSecureAssessment("session-1", true));

    await act(() => result.current.withoutFullscreenWatch(async () => undefined));
    act(() => {
      fullscreenElement = null;
      document.dispatchEvent(new Event("fullscreenchange"));
    });

    await waitFor(() => expect(reportIntegrityViolation).toHaveBeenCalledTimes(1));
    expect(reportIntegrityViolation.mock.calls[0][2]).toBe("fullscreen_exit");
  });

  it("does not watch fullscreen changes in kiosk mode", async () => {
    fullscreenElement = null;
    renderHook(() => useSecureAssessment("session-1", true, undefined, "kiosk"));

    act(() => document.dispatchEvent(new Event("fullscreenchange")));

    await Promise.resolve();
    expect(reportIntegrityViolation).not.toHaveBeenCalled();
  });

  it("releases a kiosk lock left behind when the test unmounts", async () => {
    fullscreenElement = null;
    withoutFullscreenApi();
    const { unmount } = renderHook(() => useSecureAssessment("session-1", true, undefined, "kiosk"));
    await act(() => enterSecureAssessment().then(() => undefined));
    expect(isKioskLocked()).toBe(true);

    unmount();

    expect(isKioskLocked()).toBe(false);
    expect(document.body.classList.contains("secure-assessment-kiosk")).toBe(false);
  });
});
