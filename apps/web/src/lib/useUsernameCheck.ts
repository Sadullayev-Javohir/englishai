import { useEffect, useState } from "react";
import { api, ApiError } from "@/api/client";
import { USERNAME_MAX_LENGTH, USERNAME_MIN_LENGTH, USERNAME_PATTERN } from "@/api/types";
import { uz } from "@/content/uz";

export type UsernameStatus =
  | { kind: "idle" }
  | { kind: "invalid"; message: string }
  | { kind: "checking" }
  | { kind: "available" }
  | { kind: "taken" }
  | { kind: "error" };

const DEBOUNCE_MS = 400;

/** Trim + lowercase, mirroring the backend's UsernameRules.Normalize. */
export function normalizeUsername(raw: string): string {
  return raw.trim().toLowerCase();
}

/** Local format validation matching Domain.Identity.UsernameRules. Returns null when valid. */
export function usernameFormatError(value: string): string | null {
  const v = normalizeUsername(value);
  if (v.length === 0) return uz.username.empty;
  if (v.length < USERNAME_MIN_LENGTH) return uz.username.tooShort(USERNAME_MIN_LENGTH);
  if (v.length > USERNAME_MAX_LENGTH) return uz.username.tooLong(USERNAME_MAX_LENGTH);
  if (!USERNAME_PATTERN.test(v)) return uz.username.invalidChars;
  return null;
}

/**
 * Live (debounced) username availability check. `currentUsername` - the user's existing handle
 * if any - is treated as always-fine so an edit form never flags the unchanged value as taken.
 * Format is validated locally first so we only hit the server for well-formed candidates.
 */
export function useUsernameCheck(value: string, currentUsername?: string | null): UsernameStatus {
  const [status, setStatus] = useState<UsernameStatus>({ kind: "idle" });

  useEffect(() => {
    const normalized = normalizeUsername(value);

    // Empty, or unchanged from the current handle → nothing to report.
    if (normalized.length === 0 || (currentUsername && normalized === currentUsername)) {
      setStatus({ kind: "idle" });
      return;
    }

    const formatError = usernameFormatError(value);
    if (formatError) {
      setStatus({ kind: "invalid", message: formatError });
      return;
    }

    setStatus({ kind: "checking" });
    let cancelled = false;
    const timer = window.setTimeout(async () => {
      try {
        const result = await api.auth.usernameAvailable(normalized);
        if (cancelled) return;
        setStatus(
          result.isValidFormat
            ? { kind: result.isAvailable ? "available" : "taken" }
            : { kind: "invalid", message: uz.username.invalidChars },
        );
      } catch (err) {
        if (cancelled) return;
        if (!(err instanceof ApiError)) throw err;
        setStatus({ kind: "error" });
      }
    }, DEBOUNCE_MS);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [value, currentUsername]);

  return status;
}

/** Whether the candidate may be submitted (free, or unchanged from the current handle). */
export function canSubmitUsername(
  status: UsernameStatus,
  value: string,
  currentUsername?: string | null,
): boolean {
  const normalized = normalizeUsername(value);
  if (currentUsername && normalized === currentUsername) return true;
  return status.kind === "available";
}
