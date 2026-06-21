import { motion } from "framer-motion";
import { cn } from "@/lib/cn";

export type ParrotVariant = "tutor" | "friend";

export interface ParrotAvatarProps {
  size?: number;
  speaking?: boolean;
  float?: boolean;
  variant?: ParrotVariant;
  className?: string;
}

/**
 * The parrot language tutor (or a parrot "friend") as a floating 3D avatar.
 * Bounces/rotates gently while `speaking`; floats on a slow loop when `float`.
 * Used in the hub hero, mode cards, and live chat. Purely presentational.
 */
export function ParrotAvatar({
  size = 72,
  speaking = false,
  float = true,
  variant = "tutor",
  className,
}: ParrotAvatarProps) {
  return (
    <motion.img
      src="/assets/parrot-mascot.png"
      alt=""
      aria-hidden
      width={size}
      height={size}
      style={{ width: size, height: size }}
      className={cn(
        "object-contain drop-shadow-[0_8px_14px_rgba(0,0,0,0.35)]",
        variant === "tutor" && "rounded-full ring-4 ring-ea-primary/70",
        variant === "friend" && "rounded-full ring-4 ring-ea-green-600/70",
        className,
      )}
      animate={
        speaking
          ? { y: [0, -6, 0], rotate: [0, -4, 4, -2, 0], scale: [1, 1.05, 1] }
          : float
            ? { y: [0, -5, 0] }
            : undefined
      }
      transition={
        speaking
          ? { duration: 0.7, repeat: Infinity, ease: "easeInOut" }
          : { duration: 3, repeat: Infinity, ease: "easeInOut" }
      }
    />
  );
}
