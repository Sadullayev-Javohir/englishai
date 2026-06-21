import { motion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";
import { Hearts, XpBadge } from "./GamifiedBits";
import { ProgressDuo } from "./ProgressDuo";
import { cn } from "@/lib/cn";

/**
 * Lesson-mode HUD prop contract. Reused by `LessonHud` (and the existing
 * `GlobalHud` lesson HUD) so callers can type their lesson props in one place.
 */
export interface GlobalHudProps {
  progress: number;
  total: number;
  hearts: number;
  maxHearts?: number;
  xp: number;
  onExit: () => void;
  progressColor?: "green" | "blue" | "yellow" | "purple" | "red";
}

/**
 * Global lesson HUD: a chunky progress bar (lesson progress), a hearts pill
 * (lives) and the running XP total on the right, plus an "exit" ghost button on
 * the far left. Sits fixed at the top of the full-screen jungle lesson.
 */
export function GlobalHud({
  progress,
  total,
  hearts,
  maxHearts = 5,
  xp,
  onExit,
  progressColor = "green",
  className,
}: {
  progress: number;
  total: number;
  hearts: number;
  maxHearts?: number;
  xp: number;
  onExit: () => void;
  progressColor?: "green" | "blue" | "yellow" | "purple" | "red";
  className?: string;
}) {
  const pct = total > 0 ? (progress / total) * 100 : 0;

  return (
    <div className={cn("w-full", className)}>
      {/* One row at every width. The XP badge used to drop to `row-start-2` below
          the `sm` breakpoint, which on a 390px phone left it stranded on a line
          of its own while the progress counter under it was hidden - two rows of
          chrome and no lesson position. Hearts and the XP label shrink instead. */}
      <div className="grid grid-cols-[40px_minmax(0,1fr)_auto_auto] items-center gap-1.5 sm:grid-cols-[44px_minmax(0,1fr)_auto_auto] sm:gap-3">
        <button
          onClick={onExit}
          className={cn(
            "shrink-0 flex h-10 w-10 items-center justify-center rounded-[14px] border-2 border-white/20 text-white transition-all  hover:brightness-110 ",
            progressColor === "purple"
              ? "bg-ea-purple-800"
              : "bg-ea-text",
          )}
          aria-label="Chiqish"
        >
          <Icon name="close" filled className="text-[24px] text-white" />
        </button>

        <div className={cn(
          "flex-1 min-w-0 rounded-full p-1",
          progressColor === "purple"
            ? "bg-ea-purple-800 "
            : "bg-ea-text ",
        )}>
          <ProgressDuo value={pct} color={progressColor} />
        </div>

        <Hearts count={hearts} max={maxHearts} className="shrink-0" />
        <XpBadge xp={xp} className="shrink-0 justify-self-end" />
      </div>

    </div>
  );
}

/** Reusable ghost/exit button style for in-lesson dismissals. */
export function GhostExit({ onExit, label }: { onExit: () => void; label?: string }) {
  return (
    <motion.button
      whileTap={{ scale: 0.92 }}
      onClick={onExit}
      className="inline-flex items-center gap-1 rounded-full px-3 py-1.5 text-ea-muted-text font-duo font-extrabold hover:brightness-110 transition-transform"
    >
      <Icon name="close" filled className="text-[20px]" />
      {label && <span>{label}</span>}
    </motion.button>
  );
}

/**
 * In-lesson HUD variant: a frosted pill/shell pinned to the top of the
 * full-screen lesson. Shows canonical lesson progress,
 * bar), a hearts pill, an XP pill and a ghost exit button. The shell drops in
 * with a spring entrance so it feels alive on mount, and stays readable over a
 * jungle image background (white labels, white/90 frosted pills).
 */
export function LessonHud({
  progress,
  total,
  hearts,
  maxHearts = 5,
  xp,
  onExit,
}: GlobalHudProps) {
  const pct = total > 0 ? (progress / total) * 100 : 0;

  return (
    <motion.div
      initial={{ y: -28, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      transition={{ type: "spring", stiffness: 160, damping: 18 }}
      className="fixed inset-x-0 top-0 z-40 px-3 py-3 sm:px-4"
    >
      <div className="mx-auto flex w-full max-w-3xl items-center gap-2.5 rounded-2xl bg-ea-surface/85 px-3 py-2.5   sm:gap-3 sm:px-4">
        <GhostExit onExit={onExit} />

        <div className="min-w-0 flex-1">
          <ProgressDuo value={pct} color="green" />
        </div>

        <Hearts count={hearts} max={maxHearts} />
        <XpBadge xp={xp} />
      </div>

      <div className="mx-auto mt-1 flex w-full max-w-3xl items-center justify-between px-1">
        <span className="font-caption text-caption text-white drop-shadow-sm">
          {Math.round(pct)}%
        </span>
        <span className="font-caption text-caption text-white drop-shadow-sm">
          {progress} / {total}
        </span>
      </div>
    </motion.div>
  );
}
