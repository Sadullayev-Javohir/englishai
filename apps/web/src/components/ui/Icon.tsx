import { useMemo } from "react";
import { cn } from "@/lib/cn";
import { CircleHelp } from "lucide-react";
import { ICON_MAP } from "./iconMap";

interface IconProps {
  name: string;
  /** Legacy Material-Symbols flag. Lucide is an outline set (consistent stroke is the
      whole aesthetic), and blanket `fill` would hide the interior of circle-style icons
      like CircleCheck. So `filled` instead renders a slightly heavier stroke for
      emphasis (active tabs, streak flame), keeping every icon visually consistent. */
  filled?: boolean;
  className?: string;
  style?: React.CSSProperties;
  ariaHidden?: boolean;
}

/**
 * Single icon component for the whole app. Renders a Lucide icon looked up by its
 * legacy Material-Symbols name (see iconMap.ts). Call sites keep passing the old
 * `name`/`filled`/`className` API unchanged, so migrating the icon library never
 * required touching the ~57 call sites.
 *
 * Sizing: historically icons were sized with a Tailwind text-size class
 * (`text-[20px]`, `text-2xl`, …). Lucide sizes via a numeric `size` prop, so we parse
 * a `text-[Npx]` / named text-size class out of className and feed it to Lucide.
 * Defaults to 24px (Material Symbols' default) when none is present. The text-* class
 * is left on the element too so `currentColor` still inherits correctly.
 */
export function Icon({ name, filled, className, style, ariaHidden = true }: IconProps) {
  const Cmp = ICON_MAP[name] ?? CircleHelp;

  if (import.meta.env.DEV && !ICON_MAP[name]) {
    // eslint-disable-next-line no-console
    console.warn(`[Icon] no Lucide mapping for "${name}" - rendering fallback glyph.`);
  }

  const size = useMemo(() => parseIconSize(className), [className]);

  return (
    <Cmp
      aria-hidden={ariaHidden}
      className={cn("inline-block shrink-0 align-[-0.125em]", className)}
      style={style}
      size={size}
      strokeWidth={filled ? 2.5 : 2}
    />
  );
}

/** Named px sizes for the handful of Tailwind text-size tokens icons use. */
const NAMED_SIZES: Record<string, number> = {
  "text-xs": 16,
  "text-sm": 18,
  "text-base": 20,
  "text-lg": 22,
  "text-xl": 24,
  "text-2xl": 28,
  "text-3xl": 32,
};

/** Extract a pixel size from an arbitrary `text-[Npx]` value or a named text-size class. */
function parseIconSize(className?: string): number {
  if (!className) return 24;
  const arbitrary = className.match(/text-\[(\d+)px\]/);
  if (arbitrary) return Number(arbitrary[1]);
  for (const [cls, px] of Object.entries(NAMED_SIZES)) {
    if (new RegExp(`(?:^|\\s)${cls}(?:$|\\s)`).test(className)) return px;
  }
  return 24;
}
