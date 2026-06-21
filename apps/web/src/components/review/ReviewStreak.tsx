import { motion } from "framer-motion";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

/**
 * Streak flame chip for the vocabulary SRS review HUD (spec §Review.4).
 *
 * Locked prop contract - other agents depend on these EXACT signatures:
 *   count (current consecutive-correct review streak), className?.
 *
 * Compact pill showing a flame icon + count. Pulses/glows when count >= 2
 * (amber flame); muted when count === 0.
 */

interface ReviewStreakProps {
  count: number;
  className?: string;
}

export function ReviewStreak({ count, className }: ReviewStreakProps) {
  const active = count >= 2;
  const empty = count === 0;

  return (
    <motion.div
      animate={active ? { scale: [1, 1.12, 1] } : { scale: 1 }}
      transition={
        active
          ? { duration: 1.6, repeat: Infinity, ease: "easeInOut" }
          : { type: "spring", stiffness: 300, damping: 24 }
      }
      className={cn(
        "flex items-center gap-1.5 rounded-full px-3 py-1.5 font-duo font-extrabold",
        "bg-white/15",
        empty ? "text-[var(--ea-muted)]" : "text-ea-orange-600",
        className,
      )}
      aria-label={empty ? "No review streak" : `Review streak ${count}`}
    >
      <Icon
        name="local_fire_department"
        filled={active}
        className={cn("shrink-0", empty ? "text-[16px]" : "text-[20px]")}
      />
      <span className="text-label-md">{count}</span>
    </motion.div>
  );
}
