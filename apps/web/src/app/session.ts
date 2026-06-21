// Local session helper. Auth is Google-only (see app/auth.tsx): on sign-in the
// authenticated account id is written here via setLearnerId, so every learner-scoped
// call site keeps using getLearnerId() unchanged. The legacy random-id fallback below
// only applies if getLearnerId is ever read before auth resolves (it should not be,
// since the whole app is gated behind sign-in).
const LEARNER_KEY = "englishai.learnerId";
const LEVEL_KEY = "englishai.level";

function uuid(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return "00000000-0000-4000-8000-000000000000".replace(/[018]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "8" ? (r & 0x3) | 0x8 : r;
    return v.toString(16);
  });
}

export function getLearnerId(): string {
  let id = localStorage.getItem(LEARNER_KEY);
  if (!id) {
    id = uuid();
    localStorage.setItem(LEARNER_KEY, id);
  }
  return id;
}

/** Binds the authenticated account id as the learner id (called by the auth provider). */
export function setLearnerId(id: string): void {
  localStorage.setItem(LEARNER_KEY, id);
}

// The chosen level is cached per account so a different Google account signing in on the
// same browser never inherits the previous user's level (which would skip onboarding).
function levelKey(): string {
  return `${LEVEL_KEY}.${localStorage.getItem(LEARNER_KEY) ?? "anon"}`;
}

export function getStoredLevel(): number | null {
  const raw = localStorage.getItem(levelKey());
  return raw ? Number(raw) : null;
}

export function setStoredLevel(level: number): void {
  localStorage.setItem(levelKey(), String(level));
}

/**
 * Whether onboarding is complete. The server is the source of truth (a profile exists);
 * the locally cached level is an extra signal so a just-finished onboarding (placement or
 * "Start from A1") is recognised immediately without re-fetching the session.
 */
export function hasOnboarded(serverOnboarded: boolean): boolean {
  return serverOnboarded || getStoredLevel() !== null;
}

/** Clears local session state (used on logout). */
export function clearSession(): void {
  const learnerId = localStorage.getItem(LEARNER_KEY);
  localStorage.removeItem(LEARNER_KEY);
  if (learnerId) {
    localStorage.removeItem(`${LEVEL_KEY}.${learnerId}`);
    localStorage.removeItem(`${GOAL_SEEN_KEY}.${learnerId}`);
  }
}

// The goal-onboarding screen is shown once per account, right after the level-choice flow. We
// remember that the learner has seen it (chosen a goal or skipped) so the goal gate never loops them
// back after a "skip" (which legitimately leaves the goal Unspecified). Per-account, like the level.
const GOAL_SEEN_KEY = "englishai.goalPromptSeen";

function goalSeenKey(): string {
  return `${GOAL_SEEN_KEY}.${localStorage.getItem(LEARNER_KEY) ?? "anon"}`;
}

export function hasSeenGoalPrompt(): boolean {
  return localStorage.getItem(goalSeenKey()) === "1";
}

export function markGoalPromptSeen(): void {
  localStorage.setItem(goalSeenKey(), "1");
}
