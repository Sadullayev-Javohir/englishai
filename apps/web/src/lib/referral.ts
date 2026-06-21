// Referral-code capture. A shared link looks like `https://englishai.uz/?ref=AB12CD`. We stash
// the code the moment the app loads (before the user even signs in) so it survives the Google
// OAuth round-trip, then attach it to the sign-in call. The backend only honours it for a
// brand-new account and silently ignores anything blank/unknown, so a stale code is harmless.

const STORAGE_KEY = "englishai.referralCode";

// Matches the backend ReferralCode shape (6 chars, unambiguous uppercase alphabet) but stays
// lenient on input so we can normalize before storing.
const CODE_INPUT = /^[A-Za-z0-9]{4,12}$/;

/** Reads `?ref=` from the current URL (if any) and stores a normalized code for later sign-in. */
export function captureReferralFromUrl(): void {
  try {
    const params = new URLSearchParams(window.location.search);
    const raw = params.get("ref");
    if (!raw) return;

    const trimmed = raw.trim();
    if (!CODE_INPUT.test(trimmed)) return;

    localStorage.setItem(STORAGE_KEY, trimmed.toUpperCase());
  } catch {
    // Private-mode / disabled storage - a referral is a nice-to-have, never block the app.
  }
}

/** The stored referral code, or null if none was captured. */
export function getStoredReferralCode(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

/**
 * Stores a referral code the user typed in by hand (e.g. a friend sent them the code, not a
 * link). Normalizes and validates it the same way `?ref=` capture does; returns true when the
 * code looked valid and was stored, false otherwise so the UI can flag a bad code.
 */
export function setStoredReferralCode(raw: string): boolean {
  try {
    const trimmed = raw.trim();
    if (!CODE_INPUT.test(trimmed)) return false;
    localStorage.setItem(STORAGE_KEY, trimmed.toUpperCase());
    return true;
  } catch {
    return false;
  }
}

/** Clears the stored code once it has been used (or is no longer relevant). */
export function clearStoredReferralCode(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // ignore
  }
}
