/**
 * Brand illustration gradients.
 *
 * Topic and word thumbnails need VARIETY - a hash of the id picks one so two
 * cards side by side never look identical - but before this file that variety
 * came from ten unrelated raw Tailwind hues (emerald, sky, rose, amber, zinc,
 * ...), which is the single biggest source of "every page has different
 * colours" on the catalogue screens.
 *
 * These eight are built exclusively from the canonical `--ea-*` accent ramps,
 * so the set still reads as eight distinct thumbnails while every one of them
 * belongs to the palette. All are deep enough for white text and a white level
 * badge to sit on them.
 *
 * Order matters: adjacent entries are deliberately different hue families, so a
 * sequential list of topics cycles through contrasting thumbnails.
 */
export const ILLUSTRATION_GRADIENTS = [
  "from-ea-green-600 via-ea-green-600 to-ea-green-900",
  "from-ea-blue-600 via-ea-blue-600 to-ea-blue-800",
  "from-ea-purple-600 via-ea-purple-600 to-ea-purple-800",
  "from-ea-orange-500 via-ea-orange-600 to-ea-orange-800",
  "from-ea-danger via-ea-danger to-ea-danger-700",
  "from-ea-primary-deep via-ea-green-600 to-ea-green-900",
  "from-ea-yellow-700 via-ea-orange-600 to-ea-orange-800",
  "from-ea-muted via-ea-muted-deep to-ea-ink-deep",
] as const;

/** Stable hash so a given id always maps to the same gradient. */
export function hashString(value: string): number {
  let hash = 0;
  for (let index = 0; index < value.length; index += 1) {
    hash = (hash * 31 + value.charCodeAt(index)) | 0;
  }
  return Math.abs(hash);
}

/** Pick the gradient for an id (optionally offset by a slot within a grid). */
export function gradientFor(id: string, slot = 0): string {
  return ILLUSTRATION_GRADIENTS[(hashString(id) + slot) % ILLUSTRATION_GRADIENTS.length];
}
