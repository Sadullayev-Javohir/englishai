import { motion } from "framer-motion";
import { cn } from "@/lib/cn";

/**
 * Big celebratory "+XP" / "Ajoyib!" pop that bounces in. Render inside
 * <AnimatePresence> so it can exit cleanly.
 */
export function RewardPop({
  label,
  className,
}: {
  label: string;
  className?: string;
}) {
  return (
    <motion.div
      initial={{ scale: 0.4, opacity: 0, y: 10 }}
      animate={{ scale: 1, opacity: 1, y: 0 }}
      exit={{ scale: 0.6, opacity: 0 }}
      transition={{ type: "spring", stiffness: 400, damping: 17 }}
      className={cn(
        "inline-flex items-center gap-2 rounded-full bg-ea-green-600 px-5 py-2.5",
        "font-duo font-extrabold text-white text-body-md",
        className,
      )}
    >
      {label}
    </motion.div>
  );
}
