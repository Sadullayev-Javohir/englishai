/**
 * Game layer barrel export (Jungle Academy redesign).
 *
 * All Duolingo-style 3D components live here - the single import source
 * for every screen. Import from `@/components/game` instead of `@/components/duo`
 * (the old "dabdala" layer) or inline styles.
 *
 * Spec: design-export/gamification-prompts/00-INDEX.md
 * Rebuild mandate (§0): these are CLEAN rebuilds, not copies of the old layer.
 */

export { DuoButton } from "./DuoButton";
export type { DuoButtonProps, DuoColor } from "./DuoButton";

export { PopCard } from "./PopCard";
export type { PopCardProps } from "./PopCard";

export { JungleBackground } from "./JungleBackground";

// Global top HUD (replaces the old sidebar).
export { GlobalHud, GhostExit, LessonHud } from "./GlobalHud";
export type { GlobalHudProps } from "./GlobalHud";

export { AppHud } from "./AppHud";

export { HeartsProvider, useHeartsState, HEARTS_MAX } from "./HeartsProvider";
export { StatPill } from "./StatPill";

export { Hearts, XpBadge } from "./GamifiedBits";
export { ProgressDuo } from "./ProgressDuo";

export { Confetti } from "./Confetti";
export { RewardPop } from "./RewardPop";

export { Mascot, SpeechBubble } from "./Mascot";
export { Friend, MascotBubble } from "./MascotBubble";
export { MascotGuide } from "./MascotGuide";
export { WrongShake } from "./WrongShake";

export { CleanParallax } from "./CleanParallax";
export { JungleParallax } from "./JungleParallax";

// Shared 3D jungle lesson shell (video / books / grammar / writing / listening).
export { LessonFrame, useLessonHud } from "./LessonFrame";
export type { LessonModule } from "./LessonFrame";
export { ExerciseOption } from "./ExerciseOption";
export type { ExerciseAccent, ExerciseOptionProps, ExerciseState } from "./ExerciseOption";

export { LearningPath } from "./LearningPath";
export type { PathNode } from "./LearningPath";
export { StreakFlame, XPBadge } from "./StreakFlame";

export { ProgressRing } from "@/components/ui/ProgressRing";
