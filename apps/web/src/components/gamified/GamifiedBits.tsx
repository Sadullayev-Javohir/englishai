import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

interface GamifiedBitsProps {
  variant?: "leaf" | "sparkle";
  className?: string;
}

/** Small decorative 3D-ish jungle bit: a leaf or sparkle tile. Used sparingly. */
export function GamifiedBits({ variant = "leaf", className }: GamifiedBitsProps) {
  const isSparkle = variant === "sparkle";
  const name = isSparkle ? "self_improvement" : "recycling";
  const accent = isSparkle ? "text-ea-orange-600" : "text-ea-green-600";

  return (
    <div
      aria-hidden
      className={cn(
        "flex h-9 w-9 items-center justify-center rounded-full bg-white/15 ring-1 ring-white/25 text-white",
        className,
      )}
    >
      <Icon name={name} className={cn("text-[18px]", accent)} />
    </div>
  );
}
