import { AnimatePresence, motion } from "framer-motion";
import { useMemo } from "react";

/**
 * Duolingo-style confetti burst. Renders a one-shot particle explosion centered
 * on the trigger. Compose this inside a success state - it auto-plays when
 * `show` flips to true and disappears after the animation.
 *
 * Fresh implementation for the topic-lesson redesign (the deprecated
 * `src/components/duo/Confetti` is intentionally not reused).
 */

interface ConfettiProps {
  show: boolean;
  /** Number of pieces. */
  count?: number;
  /** Particle palette. */
  colors?: string[];
}

const DEFAULT_COLORS = [
  "#B9D7AA",
  "#B8D7E9",
  "#FFD700",
  "#C2413B",
  "#BDA6EA",
  "#D97745",
];

export function Confetti({
  show,
  count = 36,
  colors = DEFAULT_COLORS,
}: ConfettiProps) {
  const pieces = useMemo(
    () =>
      Array.from({ length: count }).map((_, i) => {
        const angle = (Math.PI * 2 * i) / count + Math.random() * 0.5;
        const distance = 120 + Math.random() * 180;
        return {
          id: i,
          x: Math.cos(angle) * distance,
          y: Math.sin(angle) * distance - 40,
          rotate: Math.random() * 360,
          color: colors[i % colors.length],
          size: 8 + Math.random() * 8,
          round: Math.random() > 0.5,
        };
      }),
    [count, colors],
  );

  return (
    <AnimatePresence>
      {show && (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center overflow-visible">
          {pieces.map((p) => (
            <motion.span
              key={p.id}
              initial={{ x: 0, y: 0, opacity: 1, scale: 1, rotate: 0 }}
              animate={{
                x: p.x,
                y: p.y,
                opacity: [1, 1, 0],
                scale: [1, 1.1, 0.6],
                rotate: p.rotate,
              }}
              exit={{ opacity: 0 }}
              transition={{ duration: 1, ease: "easeOut" }}
              className="absolute"
              style={{
                width: p.size,
                height: p.size,
                backgroundColor: p.color,
                borderRadius: p.round ? "9999px" : "2px",
              }}
            />
          ))}
        </div>
      )}
    </AnimatePresence>
  );
}
