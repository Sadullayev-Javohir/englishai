import { cn } from "@/lib/cn";

/**
 * Parrot mascot for the onboarding flow (Jungle Academy spec §3).
 *
 * Renders the parrot-mascot.png asset with a gentle CSS float animation.
 * Reduced-motion users get a static image.
 */

type ParrotSize = "sm" | "md" | "lg" | "xl";

const SIZE_MAP: Record<ParrotSize, string> = {
  sm: "w-16 h-16",
  md: "w-24 h-24",
  lg: "w-32 h-32",
  xl: "w-40 h-40",
};

interface ParrotProps {
  size?: ParrotSize;
  className?: string;
}

export function Parrot({ size = "md", className }: ParrotProps) {
  return (
    <div
      aria-hidden
      className={cn(
        "animate-ea-float",
        SIZE_MAP[size],
        className,
      )}
    >
      <img
        src="/assets/parrot-mascot.png"
        alt=""
        className="h-full w-full object-contain drop-shadow-[0_8px_16px_rgba(0,0,0,0.25)]"
        decoding="async"
      />
    </div>
  );
}
