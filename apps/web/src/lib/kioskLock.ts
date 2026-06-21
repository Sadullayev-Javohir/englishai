/**
 * Kiosk lock - the secure-assessment fallback for browsers with no Fullscreen API (iOS Safari,
 * iOS WKWebView / the Capacitor shell, locked-down Android WebViews).
 *
 * Instead of refusing to start the test there, the document itself is pinned to the viewport:
 * no scrolling, no pinch-zoom, no selection, no context menu. Integrity monitoring then relies on
 * `visibilitychange`/`pagehide` (see secureAssessment.ts), which fire reliably on both platforms
 * when the learner switches app or tab.
 *
 * The visual side lives in index.css under `.secure-assessment-kiosk` - deliberately a class on
 * html/body rather than a fixed overlay element, so no scroll container is introduced between the
 * test surface and the viewport.
 */

const KIOSK_CLASS = "secure-assessment-kiosk";
const KIOSK_VIEWPORT = "width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover";

/** Set while locked; calling it restores every mutation in reverse. */
let release: (() => void) | null = null;

function blockEvent(event: Event) {
  event.preventDefault();
}

function blockMultiTouch(event: TouchEvent) {
  // Single-finger touches still reach the test surface (buttons, internal scrolling); only
  // pinch gestures are cancelled.
  if (event.touches.length > 1) event.preventDefault();
}

export function isKioskLocked(): boolean {
  return release !== null;
}

/** Locks the document to the viewport. Safe to call repeatedly - only the first call applies. */
export function applyKioskLock(): void {
  if (release || typeof document === "undefined") return;

  const root = document.documentElement;
  const { body } = document;
  const scrollY = window.scrollY;
  const viewportMeta = document.querySelector<HTMLMetaElement>('meta[name="viewport"]');
  const previousViewport = viewportMeta?.getAttribute("content") ?? null;

  root.classList.add(KIOSK_CLASS);
  body.classList.add(KIOSK_CLASS);
  viewportMeta?.setAttribute("content", KIOSK_VIEWPORT);

  const passiveBlockers: Array<[string, EventListener]> = [
    ["contextmenu", blockEvent],
    ["selectstart", blockEvent],
    ["dblclick", blockEvent],
    // iOS-only pinch gestures; harmless to register elsewhere.
    ["gesturestart", blockEvent],
    ["gesturechange", blockEvent],
  ];
  for (const [type, listener] of passiveBlockers) {
    document.addEventListener(type, listener, { capture: true });
  }
  const touchListener = blockMultiTouch as EventListener;
  document.addEventListener("touchmove", touchListener, { capture: true, passive: false });

  release = () => {
    for (const [type, listener] of passiveBlockers) {
      document.removeEventListener(type, listener, { capture: true });
    }
    document.removeEventListener("touchmove", touchListener, { capture: true });
    root.classList.remove(KIOSK_CLASS);
    body.classList.remove(KIOSK_CLASS);
    if (previousViewport === null) viewportMeta?.removeAttribute("content");
    else viewportMeta?.setAttribute("content", previousViewport);
    // Pinning html/body drops the scroll position; only restore it when there was one.
    if (scrollY > 0) window.scrollTo(0, scrollY);
  };
}

/** Undoes `applyKioskLock` exactly. A no-op when nothing is locked. */
export function releaseKioskLock(): void {
  const restore = release;
  release = null;
  restore?.();
}
