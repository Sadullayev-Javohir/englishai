import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

interface MascotBubbleProps {
  children: ReactNode;
  /** Tail points to the parrot on the left. */
  side?: "left" | "right";
  className?: string;
}

/**
 * MascotBubble - the parrot's speech bubble (white rounded card with a little notch tail).
 * Used for nudges ("Bu so'zni ayting") and the end-of-lesson celebration line.
 */
export function MascotBubble({ children, side = "left", className }: MascotBubbleProps) {
  return (
    <div
      className={cn(
        "relative bg-ea-surface rounded-2xl px-md py-sm font-body-md text-body-md text-text-primary shadow-md",
        className,
      )}
    >
      {children}
      <span
        aria-hidden
        className={cn(
          "absolute top-1/2 -translate-y-1/2 w-3 h-3 bg-ea-surface rotate-45",
          side === "left" ? "-left-1.5" : "-right-1.5",
        )}
      />
    </div>
  );
}
