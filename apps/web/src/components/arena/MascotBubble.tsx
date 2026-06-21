import { cn } from "@/lib/cn";

export type BubbleSide = "left" | "right";
export type BubbleVariant = "tutor" | "learner";

export interface MascotBubbleProps {
  side: BubbleSide;
  children: React.ReactNode;
  speaking?: boolean;
  variant?: BubbleVariant;
  className?: string;
}

/**
 * A 3D chat bubble on the live "chat stage". Left = tutor (parrot side), right =
 * learner. White/95 card with a chunky bottom lip so it reads as a physical
 * speech bubble. A small rotated-square tail points toward the speaker.
 */
export function MascotBubble({
  side,
  children,
  speaking,
  variant,
  className,
}: MascotBubbleProps) {
  const left = side === "left";
  return (
    <div className={cn("flex w-full", left ? "justify-start" : "justify-end")}>
      <div className={cn("relative w-[80%] max-w-[80%]", className)}>
        <div
          className={cn(
            "rounded-card bg-ea-surface/95 p-md",
            "font-body-md text-text-primary",
            left ? "rounded-bl-md" : "rounded-br-md",
            speaking && "ring-2 ring-ea-primary/60",
            variant === "learner" && "ring-2 ring-ea-green-600/60",
          )}
        >
          {children}
        </div>
        {/* tail */}
        <span
          aria-hidden
          className={cn(
            "absolute -bottom-2 h-4 w-4 rotate-45 bg-ea-surface/95",
            left ? "left-6" : "right-6",
          )}
        />
      </div>
    </div>
  );
}
