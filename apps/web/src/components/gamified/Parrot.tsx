import { motion, useReducedMotion } from "framer-motion";
import { cn } from "@/lib/cn";

interface ParrotProps {
  size?: number;
  float?: boolean;
  /** Pulses the parrot to read as "talking" while the tutor's TTS audio plays. */
  speaking?: boolean;
  className?: string;
}

/** Friendly floating parrot mascot for empty states / decorations (fresh asset render). */
export function Parrot({ size = 112, float = true, speaking = false, className }: ParrotProps) {
  const reduce = useReducedMotion();
  const img = (
    <img
      src="/assets/parrot-mascot.png"
      alt=""
      aria-hidden
      className={cn(
        "object-contain drop-shadow-[0_8px_16px_rgba(0,0,0,0.35)]",
        className,
      )}
      style={{ width: size, height: size }}
    />
  );

  const speakingAnimation =
    speaking && !reduce ? { scale: [1, 1.06, 1] } : undefined;
  const speakingTransition = speaking
    ? { duration: 0.5, repeat: Infinity, ease: "easeInOut" as const }
    : undefined;

  if (!float) {
    if (!speakingAnimation) return img;
    return (
      <motion.div
        animate={speakingAnimation}
        transition={speakingTransition}
        style={{ width: size, height: size }}
      >
        {img}
      </motion.div>
    );
  }

  return (
    <motion.div
      animate={
        reduce
          ? undefined
          : { y: [0, -10, 0], ...(speakingAnimation ?? {}) }
      }
      transition={speaking ? speakingTransition : { duration: 3, repeat: Infinity, ease: "easeInOut" }}
      style={{ width: size, height: size }}
    >
      {img}
    </motion.div>
  );
}
