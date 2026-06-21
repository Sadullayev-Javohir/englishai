import { motion } from "framer-motion";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";
import { CefrLevel } from "@/api/types";
import { cefrShort } from "@/lib/labels";
import type { CatalogModule } from "./catalogTokens";
import { CATALOG_ACCENT } from "./catalogTokens";

export interface ShelfCardProps {
  /** Module accent (drives badge + chip color). */
  module: CatalogModule;
  /** Topic/book cover: an image (img url or element). Full-bleed at the top. */
  cover: React.ReactNode;
  /** CEFR level shown as a corner badge. */
  level: CefrLevel;
  /** Main title (English-first per the design system). */
  title: string;
  /** Short subtitle line (e.g. Uzbek label, author, category). */
  subtitle?: string;
  /** XP reward shown as a chip. */
  xp?: number;
  /** Optional pre-title row content (icon chip + level badge live here by default). */
  /** Locked (sequential gate) - dims, disables open, gentle shake on tap. */
  locked?: boolean;
  /** Pro / paywalled - stays tappable but opens the paywall instead of the lesson. */
  pro?: boolean;
  /** Already completed - green check footer. */
  done?: boolean;
  /** Called on tap. For locked cards this is ignored; for pro cards the caller wires paywall. */
  onOpen?: () => void;
  /** Accessible label for the cover image. */
  coverAlt?: string;
  className?: string;
}

/**
 * The single 3D "shelf" card shared by every content catalog (spec §3). One card language
 * across Video / Reading / Books / Grammar / Writing / Listening so the app reads as one game:
 * cover image, CEFR level badge (corner), title + subtitle, XP reward chip, hover-lift with a
 * chunky bottom "lip", and tap-sink. Locked cards shake gently and never break navigation; pro
 * cards show a ⭐ paywall affordance but stay tappable (the caller opens the paywall).
 *
 * Built fresh on the clean `game/` primitives - NOT copied from the deprecated `src/components/duo/*`.
 */
export function ShelfCard({
  module,
  cover,
  level,
  title,
  subtitle,
  xp,
  locked,
  pro,
  done,
  onOpen,
  coverAlt,
  className,
}: ShelfCardProps) {
  const accent = CATALOG_ACCENT[module];

  // Card state → interactive styling.
  const interactive = !locked;
  const isDisabled = locked;

  function handleClick() {
    if (isDisabled) return;
    onOpen?.();
  }

  return (
    <motion.button
      type="button"
      disabled={isDisabled}
      onClick={handleClick}
      transition={{ type: "spring", stiffness: 600, damping: 26 }}
      aria-label={coverAlt ?? title}
      className={cn(
        "group relative flex flex-col text-left rounded-[1.5rem] overflow-hidden",
        "bg-ea-surface/95 ",
        // The signature Duolingo "lip": a thick bottom shadow gives physical depth. Colorless
        // so every module shares the look; the module accent only tints the badge/chip.
        "",
        interactive && " transition-all",
        " ",
        locked && "opacity-70 cursor-default",
        className,
      )}
    >
      {/* ── Cover (full-bleed) ── */}
      <div className="relative">
        {cover}
        {/* CEFR level badge - corner, frosted accent. */}
        <span
          className={cn(
            "absolute top-2 left-2 z-10 inline-flex items-center gap-1 rounded-full px-2.5 py-[3px]",
            "bg-black/45 text-white font-duo font-extrabold text-[12px] ",
          )}
        >
          {cefrShort(level)}
        </span>
        {/* Pro badge (⭐) - keeps the card tappable, signals paywall. */}
        {pro && !locked && (
          <span
            className={cn(
              "absolute top-2 right-2 z-10 inline-flex items-center gap-1 rounded-full px-2.5 py-[3px]",
              "bg-ea-orange-500 text-ea-on-primary font-duo font-extrabold text-[12px] shadow-sm",
            )}
          >
            <Icon name="star" filled className="text-[12px]" />
            Pro
          </span>
        )}
        {/* Locked overlay - soft scrim + lock; card still in the grid, never navigates. */}
        {locked && (
          <div className="absolute inset-0 z-10 flex items-center justify-center bg-black/30">
            <div className="flex flex-col items-center gap-1 text-white">
              <Icon name="lock" filled className="text-[26px] drop-shadow" />
              <span className="font-caption text-caption font-semibold drop-shadow">
                {cefrShort(level)}
              </span>
            </div>
          </div>
        )}
      </div>

      {/* ── Body ── */}
      <div className="flex flex-1 flex-col p-responsive-card">
        <div className="flex items-start justify-between gap-sm">
          <h3 className="font-headline-md text-headline-md md:font-headline-md md:text-headline-md text-text-primary line-clamp-2 leading-snug">
            {title}
          </h3>
          {xp != null && (
            <span
              className={cn(
                "shrink-0 inline-flex items-center gap-1 rounded-full px-2.5 py-[3px] font-duo font-extrabold text-[13px]",
                accent.bgSoft,
                accent.text,
              )}
            >
              <Icon name="star" filled className="text-[14px]" />
              {xp}
            </span>
          )}
        </div>
        {subtitle && (
          <p className="mt-xs font-caption text-caption text-text-secondary line-clamp-1">{subtitle}</p>
        )}

        {/* Footer: status chip (locked / pro / done) or the clear CTA. */}
        <div className="mt-auto pt-md">
          {locked ? (
            <span className="inline-flex items-center gap-xs font-caption text-caption text-text-secondary">
              <Icon name="lock" filled className="text-[14px]" />
              Qulfdan chiqarish uchun oldingisini tugating
            </span>
          ) : pro ? (
            <span className="inline-flex items-center gap-xs font-caption text-caption text-ea-orange-600 font-semibold">
              <Icon name="workspace_premium" filled className="text-[14px]" />
              Davom etish uchun Pro
            </span>
          ) : done ? (
            <span className="inline-flex items-center gap-xs font-caption text-caption text-success font-semibold">
              <Icon name="check_circle" filled className="text-[14px]" />
              Tugatildi
            </span>
          ) : (
            <span
              className={cn(
                "inline-flex items-center gap-1 font-duo font-bold text-[14px]",
                accent.text,
              )}
            >
              {uzCta(module)}
              <Icon name="arrow_forward" className="text-[16px]" />
            </span>
          )}
        </div>
      </div>
    </motion.button>
  );
}

/** Vetted CTA label per module (rule 11 - no free-form Uzbek in components). */
function uzCta(module: CatalogModule): string {
  switch (module) {
    case "video":
      return "Ko'rish";
    case "reading":
      return "O'qish";
    case "books":
      return "O'qish";
    case "grammar":
      return "Boshlash";
    case "writing":
      return "Yozish";
    case "listening":
      return "Tinglash";
  }
}
