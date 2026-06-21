// Native (Capacitor) session token storage.
//
// On the web the session is an HttpOnly cookie the browser sends automatically. Inside the
// native shell the app runs from capacitor://localhost and calls the API cross-origin, where
// that cookie is unreliable - so native instead keeps the JWT here and sends it as an
// Authorization: Bearer *** (see client.ts) and for authenticated realtime hubs.
//
// The token is persisted with @capacitor/preferences (survives app restarts) and mirrored in a
// module-level cache so request() can read it synchronously. loadStoredToken() must run once at
// startup, before the first authenticated call, to warm that cache.
import { Capacitor } from "@capacitor/core";
import { Preferences } from "@capacitor/preferences";

const TOKEN_KEY = "englishai.authToken";

/** True only inside the Android/iOS shell; false in any browser (web build is unaffected). */
export function isNativePlatform(): boolean {
  return Capacitor.isNativePlatform();
}

let cachedToken: string | null = null;

/** Loads the persisted token into the in-memory cache. Call once before the first API call. */
export async function loadStoredToken(): Promise<void> {
  if (!isNativePlatform()) return;
  const { value } = await Preferences.get({ key: TOKEN_KEY });
  cachedToken = value ?? null;
}

/** The current session token, or null. Synchronous - reads the cache warmed at startup. */
export function getAuthToken(): string | null {
  return cachedToken;
}

/** Persists the session token (after sign-in) and updates the cache. */
export async function setAuthToken(token: string): Promise<void> {
  cachedToken = token;
  if (!isNativePlatform()) return;
  await Preferences.set({ key: TOKEN_KEY, value: token });
}

/** Drops the stored token (on sign-out / 401). */
export async function clearAuthToken(): Promise<void> {
  cachedToken = null;
  if (!isNativePlatform()) return;
  await Preferences.remove({ key: TOKEN_KEY });
}
