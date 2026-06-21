import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

/**
 * Small 3D benefit pills for the welcome hero (Jungle Academy spec §3).
 *
 * Each pill shows a shared icon + short label, rendered as a frosted
 * white pill with a tiny bottom lip so it looks pressable / game-like.
 *
 * Three pills are rendered by default: XP, Streak, League.
 */

interface Bit {
  icon: "bolt" | "local_fire_department" | "emoji_events";
  label: string;
  tone: "yellow" | "orange" | "purple";
}

const BITS: Bit[] = [
  { icon: "bolt", label: "XP", tone: "yellow" },
  { icon: "local_fire_department", label: "Streak", tone: "orange" },
  { icon: "emoji_events", label: "Liga", tone: "purple" },
];

const TONE_TEXT: Record<Bit["tone"], string> = {
  yellow: "text-ea-orange-600",
  orange: "text-ea-orange-600",
  purple: "text-ea-primary",
};

interface GamifiedBitsProps {
  className?: string;
}

export function GamifiedBits({ className }: GamifiedBitsProps) {
  return (
    <div className={cn("flex items-center justify-center gap-2", className)}>
      {BITS.map((bit) => (
        <span
          key={bit.label}
          className={cn(
            "inline-flex items-center gap-1 rounded-full bg-ea-surface/85 px-3 py-1.5",
            " font-duo font-extrabold text-[13px]",
            TONE_TEXT[bit.tone],
          )}
        >
          <Icon name={bit.icon} filled className="text-[18px]" />
          <span>{bit.label}</span>
        </span>
      ))}
    </div>
  );
}
