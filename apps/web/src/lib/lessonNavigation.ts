import type { Location } from "react-router-dom";
import type { SkillStepKey } from "@/lib/skillSteps";

export type LessonOrigin = "levels" | "catalog";

export interface LessonNavigationState {
  lessonOrigin?: LessonOrigin;
  vocabularyTopicId?: string;
  topicTitle?: string;
  topicCode?: string;
  roleplayScenario?: string;
}

const CATALOG_ROUTES: Record<SkillStepKey, string> = {
  vocabulary: "/app/vocabulary/topics",
  grammar: "/app/grammar",
  reading: "/reading",
  writing: "/writing",
  speaking: "/app/speaking",
  listening: "/listening",
};

export function lessonOriginFrom(location: Pick<Location, "state">): LessonOrigin {
  const state = location.state as LessonNavigationState | null;
  return state?.lessonOrigin === "levels" ? "levels" : "catalog";
}

export function lessonState(origin: LessonOrigin, state?: Omit<LessonNavigationState, "lessonOrigin">) {
  return { ...state, lessonOrigin: origin } satisfies LessonNavigationState;
}

export function lessonBackTarget(skill: SkillStepKey, origin: LessonOrigin): string {
  return origin === "levels" ? "/levels" : CATALOG_ROUTES[skill];
}
