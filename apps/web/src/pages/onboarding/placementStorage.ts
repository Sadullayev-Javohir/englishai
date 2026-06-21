import type { PlacementResultDto } from "@/api/types";

export const PLACEMENT_RESULT_KEY = "englishai.placement.result";
const SESSION_KEY = "englishai.placement.session";
const memory = new Map<string, string>();

function read(key: string): string | null {
  let readable = false;
  for (const storage of [() => localStorage, () => sessionStorage]) {
    try {
      const value = storage().getItem(key);
      readable = true;
      if (value !== null) return value;
    } catch { /* try the other storage area */ }
  }
  return readable ? null : memory.get(key) ?? null;
}
function write(key: string, value: string | null) {
  if (value === null) memory.delete(key); else memory.set(key, value);
  for (const storage of [() => localStorage, () => sessionStorage]) {
    try {
      if (value === null) storage().removeItem(key); else storage().setItem(key, value);
    } catch { /* A blocked/full storage area must not fail a successful API request. */ }
  }
}

/** Per-account state: another signed-in learner must never inherit a session or result. */
export function placementStorage(learnerId: string) {
  const sessionKey = `${SESSION_KEY}.${learnerId}`;
  const resultKey = `${PLACEMENT_RESULT_KEY}.${learnerId}`;
  const migratedKey = `${sessionKey}.legacy-checked`;
  return {
    readSession: () => {
      const scoped = read(sessionKey);
      if (scoped || read(migratedKey)) return scoped;
      // Old clients stored one global id. Only migrate real session ids; the server
      // verifies ownership before any question is displayed or the id is adopted.
      const legacy = read(SESSION_KEY);
      return legacy && /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(legacy) ? legacy : null;
    },
    saveSession: (id: string | null) => {
      write(sessionKey, id);
      write(migratedKey, "1");
      if (id && read(SESSION_KEY) === id) write(SESSION_KEY, null);
    },
    saveResult: (result: PlacementResultDto) => write(resultKey, JSON.stringify(result)),
    readResult: (): PlacementResultDto | null => {
      try {
        const value = read(resultKey);
        const result: unknown = value ? JSON.parse(value) : null;
        return isPlacementResult(result) ? result : null;
      } catch { return null; }
    },
    readDraft: (sessionId: string, itemId: string) => read(`${sessionKey}.${sessionId}.${itemId}.draft`) ?? "",
    saveDraft: (sessionId: string, itemId: string, text: string | null) => write(`${sessionKey}.${sessionId}.${itemId}.draft`, text),
  };
}

export function isPlacementResult(value: unknown): value is PlacementResultDto {
  if (!value || typeof value !== "object") return false;
  const result = value as PlacementResultDto;
  const validLevel = (level: number) => Number.isInteger(level) && level >= 1 && level <= 6;
  const validScore = (score: number) => typeof score === "number" && Number.isFinite(score) && score >= 0 && score <= 100;
  return validLevel(result.overallLevel) && validScore(result.overallScore)
    && Array.isArray(result.stageResults) && result.stageResults.length > 0 && result.stageResults.length <= 6
    && new Set(result.stageResults.map(stage => stage?.stage)).size === result.stageResults.length
    && result.stageResults.every(stage => stage && validLevel(stage.stage) && validLevel(stage.level) && validScore(stage.score));
}
