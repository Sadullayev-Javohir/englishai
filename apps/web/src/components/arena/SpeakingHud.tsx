import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

export interface SpeakingHudProps {
  hearts: number;
  maxHearts?: number;
  xp: number;
  streak?: number;
  onExit: () => void;
  title?: string;
  className?: string;
}

/**
 * Frosted pill HUD that floats over the jungle: a ghost exit button (+ optional title)
 * on the left, and hearts / XP / streak badges on the right. Compact and high-contrast.
 */
export function SpeakingHud({
  hearts,
  maxHearts = 5,
  xp,
  streak,
  onExit,
  title,
  className,
}: SpeakingHudProps) {
  const safeHearts = Math.max(0, Math.min(maxHearts, hearts));

  return (
    <div
      className={cn(
        "flex items-center justify-between gap-3 rounded-[24px] bg-ea-surface/90 px-3 py-2",
        "  font-duo",
        className,
      )}
    >
      {/* Left: exit + title */}
      <div className="flex min-w-0 items-center gap-2">
        <button
          type="button"
          onClick={onExit}
          aria-label="Exit"
          className={cn(
            "flex h-9 w-9 shrink-0 items-center justify-center rounded-full",
            "bg-black/5 text-text-secondary transition-colors hover:bg-black/10",
            "",
          )}
        >
          <Icon name="close" className="text-xl" />
        </button>
        {title && (
          <span className="truncate font-extrabold text-text-primary">{title}</span>
        )}
      </div>

      {/* Right: stats */}
      <div className="flex shrink-0 items-center gap-2">
        {/* Hearts */}
        <div className="flex items-center gap-1 rounded-full bg-ea-danger/15 px-2.5 py-1.5">
          <Icon name="heart" filled className="text-ea-danger text-lg" />
          <span className="font-extrabold text-ea-danger">{safeHearts}</span>
        </div>
        {/* XP */}
        <div className="flex items-center gap-1 rounded-full bg-ea-orange-500/15 px-2.5 py-1.5">
          <span className="text-ea-orange-600 text-lg leading-none">⚡</span>
          <span className="font-extrabold text-ea-orange-600">{xp} XP</span>
        </div>
        {/* Streak */}
        {typeof streak === "number" && (
          <div className="flex items-center gap-1 rounded-full bg-ea-orange-500/15 px-2.5 py-1.5">
            <span className="text-ea-orange-600 text-lg leading-none">🔥</span>
            <span className="font-extrabold text-ea-orange-600">{streak}</span>
          </div>
        )}
      </div>
    </div>
  );
}
