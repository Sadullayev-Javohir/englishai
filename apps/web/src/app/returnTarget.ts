const RETURN_TARGET_KEY = "englishai:return-target";

const GATE_PATHS = new Set([
  "/",
  "/hero",
  "/login",
  "/username",
  "/welcome",
  "/assessment",
  "/placement",
  "/placement/result",
  "/onboarding/goal",
  "/app/vocabulary/review",
]);

export function normalizeReturnTarget(value: string): string | null {
  if (!value.startsWith("/") || value.startsWith("//")) return null;

  try {
    const url = new URL(value, window.location.origin);
    if (url.origin !== window.location.origin || GATE_PATHS.has(url.pathname)) return null;
    return `${url.pathname}${url.search}${url.hash}`;
  } catch {
    return null;
  }
}

export function rememberReturnTarget(value: string): void {
  const target = normalizeReturnTarget(value);
  if (target) sessionStorage.setItem(RETURN_TARGET_KEY, target);
}

export function getReturnTarget(): string | null {
  const stored = sessionStorage.getItem(RETURN_TARGET_KEY);
  const target = stored ? normalizeReturnTarget(stored) : null;
  if (!target && stored) sessionStorage.removeItem(RETURN_TARGET_KEY);
  return target;
}

export function clearReturnTarget(): void {
  sessionStorage.removeItem(RETURN_TARGET_KEY);
}

export function returnTargetOr(fallback: string): string {
  return getReturnTarget() ?? fallback;
}
