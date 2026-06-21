/**
 * Cross-browser Fullscreen API access.
 *
 * The unprefixed spec is missing on iOS Safari/WKWebView entirely and several Android WebViews
 * still expose only the `webkit*` names, so every caller in the app goes through this module
 * instead of touching `document.fullscreenEnabled` / `element.requestFullscreen` directly.
 */

type PrefixedDocument = Document & {
  webkitFullscreenElement?: Element | null;
  webkitFullscreenEnabled?: boolean;
  webkitExitFullscreen?: () => Promise<void> | void;
};

type PrefixedElement = Element & {
  webkitRequestFullscreen?: (options?: FullscreenOptions) => Promise<void> | void;
};

function prefixedDocument(): PrefixedDocument | null {
  return typeof document === "undefined" ? null : (document as PrefixedDocument);
}

/** The element currently displayed fullscreen, under either spelling of the API. */
export function fullscreenElement(): Element | null {
  const doc = prefixedDocument();
  if (!doc) return null;
  return doc.fullscreenElement ?? doc.webkitFullscreenElement ?? null;
}

/**
 * Whether `element` (the document element by default) can plausibly be made fullscreen. Browsers
 * can still refuse at request time - `requestFullscreenOn` reports the real outcome.
 */
export function fullscreenSupported(element?: Element | null): boolean {
  const doc = prefixedDocument();
  if (!doc) return false;
  const target = (element ?? doc.documentElement) as PrefixedElement | null;
  if (!target) return false;
  const enabled = Boolean(doc.fullscreenEnabled || doc.webkitFullscreenEnabled);
  const requestable = Boolean(target.requestFullscreen || target.webkitRequestFullscreen);
  return enabled && requestable;
}

/**
 * Requests fullscreen for `element` and resolves with whether the browser actually entered it.
 * Never throws: callers decide their own fallback (kiosk mode, CSS pseudo-fullscreen).
 */
export async function requestFullscreenOn(element: Element | null): Promise<boolean> {
  const doc = prefixedDocument();
  if (!doc || !element) return false;
  if (fullscreenElement()) return true;

  const target = element as PrefixedElement;
  if (target.requestFullscreen) {
    try {
      // `navigationUI: "hide"` keeps the browser chrome out of an exam/video surface; browsers
      // that don't know the option reject, so retry without it before giving up.
      await target.requestFullscreen({ navigationUI: "hide" });
    } catch {
      try {
        await target.requestFullscreen();
      } catch {
        /* fall through to the prefixed attempt below */
      }
    }
  }
  if (!fullscreenElement() && target.webkitRequestFullscreen) {
    try {
      await target.webkitRequestFullscreen();
    } catch {
      /* no fullscreen available - the caller falls back */
    }
  }
  return Boolean(fullscreenElement());
}

/** Leaves fullscreen if the document is in it; a no-op (and never throws) otherwise. */
export async function exitFullscreenIfActive(): Promise<void> {
  const doc = prefixedDocument();
  if (!doc || !fullscreenElement()) return;
  try {
    if (doc.exitFullscreen) await doc.exitFullscreen();
    else await doc.webkitExitFullscreen?.();
  } catch {
    /* navigation must never be blocked by a failed exit */
  }
}

/** Subscribes to both spellings of the change event; returns the unsubscribe function. */
export function onFullscreenChange(listener: () => void): () => void {
  if (typeof document === "undefined") return () => {};
  document.addEventListener("fullscreenchange", listener);
  document.addEventListener("webkitfullscreenchange", listener);
  return () => {
    document.removeEventListener("fullscreenchange", listener);
    document.removeEventListener("webkitfullscreenchange", listener);
  };
}
