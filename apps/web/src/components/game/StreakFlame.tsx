import { motion } from "framer-motion";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

/**
 * Small reusable Duolingo-style gamified bits. Canonical game-layer copy
 * (migrated from duo/*). ProgressDuo / WrongShake / RewardPop live in their own
 * game/* files; this holds StreakFlame and XPBadge.
 */

export function StreakFlame({ count, className }: { count: number; className?: string }) {
  return (
    <motion.div
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full bg-ea-orange-500/15 px-3 py-1.5",
        className,
      )}
      whileHover={{ scale: 1.05 }}
    >
      <motion.span
        animate={{ scale: [1, 1.18, 1] }}
        transition={{ duration: 1.1, repeat: Infinity, ease: "easeInOut" }}
        className="inline-flex text-ea-orange-600"
      >
        <Icon name="local_fire_department" filled className="text-xl" />
      </motion.span>
      <span className="font-duo font-extrabold text-ea-orange-600">{count}</span>
    </motion.div>
  );
}

export function XPBadge({ xp, className }: { xp: number; className?: string }) {
  return (
    <div
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full bg-ea-orange-500/15 px-3 py-1.5",
        className,
      )}
    >
      <Icon name="diamond" filled className="text-ea-orange-600 text-lg" />
      <span className="font-duo font-extrabold text-ea-orange-600">{xp} XP</span>
    </div>
  );
}
