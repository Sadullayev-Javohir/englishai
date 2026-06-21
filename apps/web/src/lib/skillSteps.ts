import type { NavigateFunction } from "react-router-dom";
import { lessonState, type LessonOrigin } from "@/lib/lessonNavigation";

export type SkillStepKey =
  | "vocabulary"
  | "grammar"
  | "writing"
  | "speaking"
  | "listening"
  | "reading";

export interface SkillStep {
  key: SkillStepKey;
  icon: string;
  /** Canonical SkillType module name as returned by the completion checklist (K.5). */
  module: string;
}

// Canonical order of the topic-centric learning path. Every skill links back to the SAME topic so
// the learner practises it with the words just studied - the project's core "interconnected skills"
// goal. The order is recommended and topic mastery requires every skill to reach at least 75%. This
// list is the single source of truth for both the SkillPath hub and the Level Map roadmap dropdown,
// and must match the backend RequiredModules.
export const SKILL_STEPS: SkillStep[] = [
  { key: "vocabulary", icon: "menu_book", module: "Vocabulary" },
  { key: "grammar", icon: "rule", module: "Grammar" },
  { key: "reading", icon: "auto_stories", module: "Reading" },
  { key: "writing", icon: "edit_note", module: "Writing" },
  { key: "speaking", icon: "record_voice_over", module: "Speaking" },
  { key: "listening", icon: "headphones", module: "Listening" },
];

/**
 * Opens a skill for a specific vocabulary topic, so the learner practises it with that topic's
 * words. Vocabulary opens the topic page itself (which hosts the SkillPath hub); the other skills
 * carry the topic via route param or router state, matching how each module reads it.
 */
export function openSkill(
  navigate: NavigateFunction,
  step: SkillStep,
  topicId: string,
  topicTitle: string,
  origin: LessonOrigin = "catalog",
): void {
  switch (step.key) {
    case "vocabulary":
      navigate(`/app/vocabulary/topic/${topicId}`, { state: lessonState(origin) });
      break;
    case "grammar":
      navigate(`/app/grammar/topic/${topicId}`, { state: lessonState(origin) });
      break;
    case "writing":
      navigate(`/writing/task/${topicId}`, { state: lessonState(origin, { vocabularyTopicId: topicId, topicTitle }) });
      break;
    case "speaking":
      navigate(`/app/speaking/topic/${encodeURIComponent(topicId)}`, { state: lessonState(origin, { topicTitle }) });
      break;
    case "reading":
      navigate("/reading", { state: lessonState(origin, { vocabularyTopicId: topicId, topicTitle }) });
      break;
    case "listening":
      navigate(`/listening/topic/${topicId}`, { state: lessonState(origin) });
      break;
  }
}
