import { API_BASE } from "@/api/config";

/**
 * A notification/broadcast link resolved into either an internal SPA route (open *inside* the
 * app) or a genuinely external URL (open in the system browser).
 */
export type NotificationTarget =
  | { kind: "internal"; path: string }
  | { kind: "external"; url: string };

/**
 * Origins that belong to *this* app.
 *  - Web build: it runs on the production origin directly (window.location.origin).
 *  - Mobile build (Capacitor): the app is served from capacitor://localhost, but the production
 *    site is baked into API_BASE (e.g. https://englishai.uz) at build time.
 * Both are treated as "us" so that a link to our own site is never bounced out to Chrome.
 */
function ownOrigins(): string[] {
  const origins = new Set<string>();
  if (typeof window !== "undefined" && window.location?.origin) {
    origins.add(window.location.origin);
  }
  if (API_BASE) {
    try {
      origins.add(new URL(API_BASE).origin);
    } catch {
      // API_BASE malformed - ignore it.
    }
  }
  return [...origins];
}

/**
 * Decides how a notification link should be opened.
 *
 * A super-admin usually broadcasts a link copied straight from the address bar - e.g.
 * `https://englishai.uz/video/abc/play`. That is an *internal* page of our own app even though
 * it is written as an absolute URL. Handing such a link to the Capacitor Browser plugin would
 * open our own site inside Chrome, kicking the user out of the native app entirely. So any
 * absolute URL that points at our own site origin is rewritten to its path and treated as
 * internal; only URLs on a foreign origin (youtube.com, a blog, …) stay external.
 *
 * Returns null for an empty/unusable link so callers can no-op.
 */
export function resolveNotificationTarget(link: string): NotificationTarget | null {
  const raw = link.trim();
  if (raw.length === 0) return null;

  // Relative link - always internal.
  if (raw.startsWith("/")) return { kind: "internal", path: raw };

  // Absolute http(s) link: internal if it points at our own site, else external.
  if (/^https?:\/\//i.test(raw)) {
    let parsed: URL;
    try {
      parsed = new URL(raw);
    } catch {
      return null;
    }
    if (ownOrigins().includes(parsed.origin)) {
      return { kind: "internal", path: `${parsed.pathname}${parsed.search}${parsed.hash}` };
    }
    return { kind: "external", url: raw };
  }

  // Any other scheme (mailto:, tel:, custom app scheme) - leave it to the system.
  return { kind: "external", url: raw };
}
