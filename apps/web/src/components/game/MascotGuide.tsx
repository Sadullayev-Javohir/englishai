import { motion } from "framer-motion";
import { cn } from "@/lib/cn";
import { Mascot } from "./Mascot";

/**
 * MascotGuide - the parrot companion paired with an encouragement speech bubble
 * for the level roadmap (spec §2.6). Fresh, clean build (not copied from the old
 * "dabdala" layer). The parrot floats gently; the bubble sits beside it and can
 * point its tail toward the mascot (left/right). All visible Uzbek copy MUST come
 * from the content store (rule 11) - never hardcode free-form Uzbek here.
 */

type GuideTone = "green" | "blue" | "yellow" | "purple" | "orange" | "red";

interface MascotGuideProps {
  /** Speech text (already resolved from the content store). */
  text: string;
  /** Which side the parrot sits on. */
  side?: "left" | "right";
  /** Bubble accent tone. */
  tone?: GuideTone;
  /** Parrot size in px. */
  size?: number;
  className?: string;
}

const TONE_RING: Record<GuideTone, string> = {
  green: "border-primary/30",
  blue: "border-ea-blue-600/30",
  yellow: "border-ea-yellow-700/30",
  purple: "border-ea-green-600/30",
  orange: "border-accent/30",
  red: "border-ea-orange-600/30",
};

export function MascotGuide({
  text,
  side = "left",
  tone = "green",
  size = 64,
  className,
}: MascotGuideProps) {
  const bubble = (
    <motion.div
      initial={{ opacity: 0, scale: 0.92 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ type: "spring", stiffness: 220, damping: 22 }}
      className={cn(
        "min-w-0 flex-1",
        side === "left" ? "order-2" : "order-1",
      )}
    >
      <div
        className={cn(
          "rounded-[20px] bg-ea-surface px-4 py-3  font-body-md text-body-md text-text-primary border",
          TONE_RING[tone],
          side === "left" ? "rounded-tl-md" : "rounded-tr-md",
        )}
      >
        {text}
      </div>
    </motion.div>
  );

  const parrot = (
    <motion.div
      className={cn("shrink-0", side === "left" ? "order-1" : "order-2")}
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ type: "spring", stiffness: 200, damping: 20 }}
    >
      <Mascot size={size} float />
    </motion.div>
  );

  return (
    <div className={cn("flex items-start gap-3", className)}>
      {side === "left" ? (
        <>
          {parrot}
          {bubble}
        </>
      ) : (
        <>
          {bubble}
          {parrot}
        </>
      )}
    </div>
  );
}

export type { GuideTone };
