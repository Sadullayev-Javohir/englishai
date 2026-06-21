import { useAsync } from "@/lib/useAsync";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";

/** Sequential-unlock state of one learning-spine topic within a single skill's catalog. */
export interface TopicGate {
  /** Locked while either the topic or this skill's module is not yet eligible (K.5). */
  isLocked: boolean;
  /** Trial paywall (H.1): the topic is past the free allowance and needs a Premium plan. */
  requiresPro: boolean;
  /** This skill's module is already passed for the topic - show it as done (green "tugatildi"). */
  passed: boolean;
  /** All six lesson stages passed - the whole topic is complete. */
  isMastered: boolean;
  /** Number of passed lesson modules for the topic. */
  passedModuleCount: number;
  /** Total lesson modules required to master the topic. */
  requiredModuleCount: number;
}

const LOCKED: TopicGate = {
  isLocked: true,
  requiresPro: false,
  passed: false,
  isMastered: false,
  passedModuleCount: 0,
  requiredModuleCount: 6,
};

const LEVELS: CefrLevel[] = [
  CefrLevel.A1,
  CefrLevel.A2,
  CefrLevel.B1,
  CefrLevel.B2,
  CefrLevel.C1,
  CefrLevel.C2,
];

/**
 * Per-topic gating for a skill catalog. Every skill teaches the same 50 learning-spine topics, and
 * the Level Map already computes each topic's sequential lock and six-module checklist (K.5); this
 * hook reuses that single source of truth so each skill's topic list unlocks step by step exactly
 * like the Level Map and the vocabulary catalog - no backend change needed.
 *
 * `gateOf(topicId)` combines the topic lock with THIS skill module's `unlocked` state and reports
 * whether that module passed (so the catalog can mark finished topics green). Topics absent from
 * the loaded level map stay locked because their eligibility cannot be verified.
 *
 * @param level Pin the level whose map to load, or undefined for the learner's current level.
 * @param moduleName Canonical SkillType name: "Grammar" | "Reading" | "Listening" | "Writing" | …
 * @param allLevels Load every CEFR map for an all-levels catalog instead of only the current map.
 */
export function useSkillTopicGates(
  learnerId: string,
  level: CefrLevel | undefined,
  moduleName: string,
  allLevels = false,
): { gateOf: (topicId: string) => TopicGate; ready: boolean } {
  const { data } = useAsync(
    async () => {
      const maps = allLevels
        ? await Promise.all(LEVELS.map((candidate) => api.levels.map(learnerId, candidate)))
        : [await api.levels.map(learnerId, level)];
      return maps.flatMap((map) => map.topics);
    },
    [learnerId, level, allLevels],
    Boolean(learnerId),
  );

  const byId = new Map((data ?? []).map((topic) => [topic.id, topic]));
  function gateOf(topicId: string): TopicGate {
    const topic = byId.get(topicId);
    if (!topic) return LOCKED;
    const module = topic.modules.find((m) => m.module === moduleName);
    return {
      isLocked: topic.isLocked || module?.unlocked !== true,
      requiresPro: topic.requiresPro,
      passed: !!module?.passed,
      isMastered: topic.isMastered,
      passedModuleCount: topic.passedModuleCount,
      requiredModuleCount: topic.requiredModuleCount,
    };
  }
  return { gateOf, ready: !!data };
}
