import { AnimatePresence, motion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";

interface RewardPopProps {
  /** Bump to show a +XP / +points pop. 0 = hidden. */
  fireKey: number;
  label: string;
  color?: "green" | "yellow" | "blue";
}

/**
 * A small floating "+XP" pill that pops up and fades near a correct answer (RewardPop).
 * Purely celebratory; does not interfere with layout.
 */
export function RewardPop({ fireKey, label, color = "green" }: RewardPopProps) {
  const tone =
    color === "yellow"
      ? "text-ea-orange-600"
      : color === "blue"
        ? "text-ea-primary"
        : "text-ea-green-600";
  return (
    <AnimatePresence>
      {fireKey > 0 && (
        <motion.div
          key={fireKey}
          initial={{ y: 0, opacity: 0, scale: 0.8 }}
          animate={{ y: -28, opacity: 1, scale: 1 }}
          exit={{ opacity: 0 }}
          transition={{ type: "spring", stiffness: 400, damping: 22 }}
          className={`pointer-events-none absolute -top-2 left-1/2 -translate-x-1/2 z-30 flex items-center gap-1 font-duo font-extrabold ${tone}`}
        >
          <Icon name="star" filled className="text-[18px]" />
          {label}
        </motion.div>
      )}
    </AnimatePresence>
  );
}
