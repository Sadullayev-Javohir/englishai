import { useMemo } from "react";
import { SKILL_STEPS, type SkillStep } from "@/lib/skillSteps";
import type { VocabularyTopicSummaryDto } from "@/api/types";

export type SkillState = "done" | "current" | "locked" | "todo";

/**
 * Maps a topic's K.5 module checklist onto the canonical six-step order.
 * `SKILL_STEPS` (lib/skillSteps.ts) stays the single source of truth for both the
 * order and the navigation target, so the roadmap can never drift from SkillPath.
 */
export function useSkillStates(modules: VocabularyTopicSummaryDto["modules"]) {
  return useMemo(() => {
    const byModule = new Map(modules.map((m) => [m.module, m]));
    const currentKey = SKILL_STEPS.find((s) => {
      const m = byModule.get(s.module);
      return m?.unlocked && !m.passed;
    })?.key;

    const stateOf = (step: SkillStep): SkillState => {
      const m = byModule.get(step.module);
      if (m?.passed) return "done";
      if (!m?.unlocked) return "locked";
      if (step.key === currentKey) return "current";
      return "todo";
    };

    return { stateOf };
  }, [modules]);
}
