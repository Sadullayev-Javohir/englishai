/**
 * Cohesive "Jungle Academy" brand palette for the app shell + home dashboard.
 *
 * Every value derives from design-system.md brand tokens (forest green #14532D,
 * warm orange #D97745 as accent-only, cream). This REPLACES the clashing Duolingo
 * neon colors (#58CC02 / #1CB0F6 / #FF4B4B / #FFC800 / #CE82FF) that used to live
 * inline in HomePage - those broke visual consistency between the shell (brand) and
 * the dashboard (neon). Now the whole app reads as one harmonious jungle world.
 *
 * Skill accents are a single earthy/leaf family (greens, teal, olive, honey) with
 * exactly ONE warm terracotta accent (Speaking) matching the brand's "accent-only"
 * budget. Goal cards reuse the same family so nothing clashes.
 */

export const JUNGLE = {
  /** Deepest jungle shadow (text on jungle, dark scrims). */
  greenDeep: "#0F3D2E",
  /** Brand primary green. */
  green: "#14532D",
  /** Mid leaf green (icon tints, hovers). */
  greenMid: "#1B7A4B",
  /** Bright leaf green (daily-hero gradient end, success states). */
  greenBright: "#2E7D50",
  /** Warm terracotta - accent ONLY (used sparingly, e.g. streak nudge). */
  orange: "#D97745",
  orangeDeep: "#C26B4A",
  /** Cream/white card surface. */
  cream: "#FFFDF7",
  /** Warm tint background. */
  tint: "#F3EEE4",
} as const;

/** Harmonious per-skill accents - one earthy leaf family + a single warm accent. */
export const SKILL_ACCENT: Record<string, string> = {
  Vocabulary: "#1B7A4B",
  Grammar: "#2E7D50",
  Writing: "#0E7C86",
  Speaking: "#C26B4A",
  Listening: "#B5862E",
  Reading: "#6B8F2E",
};

/** Rotating top-strip accents for the goal-topic cards - same family, no neon. */
export const GOAL_STRIP: string[] = [
  "#1B7A4B",
  "#2E7D50",
  "#0E7C86",
  "#6B8F2E",
  "#B5862E",
  "#C26B4A",
];

/** Chunky 3D "lip" shadows - brand green (and one warm variant for the streak highlight). */
export const LIP = {
  green: "0_8px_0_rgba(20,83,45,0.35)",
  greenSoft: "0_6px_0_rgba(20,83,45,0.20)",
  orange: "0_6px_0_rgba(217,119,69,0.35)",
} as const;
