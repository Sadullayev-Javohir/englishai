import { motion } from "framer-motion";

interface ProgressDuoProps {
  /** 0..1 fraction complete. */
  value: number;
  className?: string;
}

/**
 * Lesson progress bar with a canonical status fill and springy width transition.
 */
export function ProgressDuo({ value, className }: ProgressDuoProps) {
  const pct = Math.max(0, Math.min(1, value)) * 100;
  return (
    <div
      className={`h-4 w-full rounded-full bg-white/40 overflow-hidden ${className ?? ""}`}
      role="progressbar"
      aria-label="Dars jarayoni"
      aria-valuenow={Math.round(pct)}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      <motion.div
        className="h-full rounded-full bg-ea-green-600"
        initial={false}
        animate={{ width: `${pct}%` }}
        transition={{ type: "spring", stiffness: 200, damping: 26 }}
      />
    </div>
  );
}
