import { motion } from "framer-motion";
import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

interface ShakeBoxProps {
  children: ReactNode;
  /** When this value changes, the box plays one wrong-answer shake. */
  trigger: number;
  className?: string;
}

/**
 * Wraps any element and plays a horizontal "wrong" shake
 * from tailwind.config.js) whenever `trigger` increments. Used for incorrect tiles so the
 * feedback is physical, not just a color swap.
 */
export function ShakeBox({ children, trigger, className }: ShakeBoxProps) {
  return (
    <motion.div
      key={trigger}
      animate={trigger > 0 ? { x: [0, -10, 10, -8, 8, -4, 4, 0] } : { x: 0 }}
      transition={{ duration: 0.5, ease: "easeInOut" }}
      className={cn(className)}
    >
      {children}
    </motion.div>
  );
}
