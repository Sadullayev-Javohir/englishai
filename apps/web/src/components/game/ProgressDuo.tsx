import { motion } from "framer-motion";
import { cn } from "@/lib/cn";

/**
 * Chunky Duolingo-style progress bar with a bouncy spring fill. Used for the
 * lesson progress at the top of the topic lesson.
 */
export function ProgressDuo({
  value,
  max = 100,
  color = "green",
  className,
}: {
  value: number;
  max?: number;
  color?: "green" | "blue" | "yellow" | "purple" | "red";
  className?: string;
}) {
  const pct = Math.max(0, Math.min(100, (value / max) * 100));
  const barColor = {
    green: "bg-ea-green-600",
    blue: "bg-ea-primary",
    yellow: "bg-ea-orange-500",
    purple: "bg-ea-primary",
    red: "bg-ea-danger",
  }[color];

  return (
    <div className={cn("w-full", className)}>
      <div className="w-full h-4 rounded-full bg-ea-surface-soft overflow-hidden">
        <motion.div
          className={cn("h-full rounded-full", barColor)}
          initial={{ width: 0 }}
          animate={{ width: `${pct}%` }}
          transition={{ type: "spring", stiffness: 140, damping: 20 }}
        />
      </div>
    </div>
  );
}
