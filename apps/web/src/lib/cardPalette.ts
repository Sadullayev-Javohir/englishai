/**
 * Compatibility palette for lesson flows that still use board identifiers.
 * Every board resolves to the canonical Professional Indigo flat surface.
 */

export type CardAccent =
  | "board1"
  | "board2"
  | "board3"
  | "board4"
  | "board5"
  | "board6"
  | "board7"
  | "board8"
  | "board9"
  | "board10";

export const CARD_STYLE: Record<CardAccent, { face: string; lip: string }> = {
  board1: { face: "bg-ea-primary", lip: "" },
  board2: { face: "bg-ea-primary", lip: "" },
  board3: { face: "bg-ea-primary", lip: "" },
  board4: { face: "bg-ea-primary", lip: "" },
  board5: { face: "bg-ea-primary", lip: "" },
  board6: { face: "bg-ea-primary", lip: "" },
  board7: { face: "bg-ea-primary", lip: "" },
  board8: { face: "bg-ea-primary", lip: "" },
  board9: { face: "bg-ea-primary", lip: "" },
  board10: { face: "bg-ea-primary", lip: "" },
};

// Ten-tone cycle - one unique hue per grid cell (no repeats).
export const ACCENT_CYCLE: CardAccent[] = [
  "board1",
  "board2",
  "board3",
  "board4",
  "board5",
  "board6",
  "board7",
  "board8",
  "board9",
  "board10",
];

export const accentFor = (i: number): CardAccent => ACCENT_CYCLE[i % ACCENT_CYCLE.length];

/** A uniform flat card shell class string for compatibility call sites. */
export const cardClass = (ac: CardAccent): string =>
  ["rounded-card border border-ea-primary text-ea-on-primary shadow-none", CARD_STYLE[ac].face, CARD_STYLE[ac].lip].join(" ");
