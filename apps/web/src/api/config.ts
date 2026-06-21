// Base URL for all backend calls (REST + SignalR + media URLs).
//
// Web build: served from the same origin as the API, so this stays empty and every
// request uses a relative path (e.g. "/api/..."). In dev, Vite proxies /api and /hubs
// to the backend (see vite.config.ts); in prod the SPA is served same-origin.
//
// Mobile build (Capacitor): the web assets are served from capacitor://localhost, so a
// relative "/api/..." would hit the on-device file server instead of the backend. The
// mobile build is produced with VITE_API_BASE_URL set to the absolute production API
// origin (e.g. https://englishai.uz), which is prepended to every backend path here.
const RAW_BASE = (import.meta.env.VITE_API_BASE_URL ?? "").trim();

// Normalise: drop a trailing slash so callers can always concatenate "/api/...".
export const API_BASE = RAW_BASE.replace(/\/+$/, "");

/** Prefixes a backend path (e.g. "/api/...", "/hubs/...", "/health") with API_BASE. */
export function apiUrl(path: string): string {
  return `${API_BASE}${path}`;
}
