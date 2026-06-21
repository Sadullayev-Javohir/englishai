import { motion } from "framer-motion";
import { cn } from "@/lib/cn";

/**
 * Spring-animated progress bar for the vocabulary SRS review HUD (spec §Review.3).
 *
 * Locked prop contract - other agents depend on these EXACT signatures:
 *   done (reviewed so far), due (total due), className?.
 *
 * Compact bar: translucent track, green fill with a chunky bottom lip, and a
 * `{done} / {due}` label in Nunito. Fill width animates with a spring.
 */

interface ProgressDuoProps {
  done: number;
  due: number;
  className?: string;
}

export function ProgressDuo({ done, due, className }: ProgressDuoProps) {
  const pct = due > 0 ? (done / due) * 100 : 0;

  return (
    <div className={cn("w-full", className)}>
      <div className="mb-1 flex items-center justify-between">
        <span className="font-duo text-label-md font-extrabold text-current">
          Bajarildi: {done} / {due}
        </span>
      </div>

      <div
        role="progressbar"
        aria-label="Takrorlash jarayoni"
        aria-valuemin={0}
        aria-valuemax={due}
        aria-valuenow={done}
        className="h-3 w-full overflow-hidden rounded-full bg-white/25"
      >
        <motion.div
          animate={{ width: `${pct}%` }}
          transition={{ type: "spring", stiffness: 220, damping: 30 }}
          className="h-3 rounded-full bg-ea-green-600 "
        />
      </div>
    </div>
  );
}
