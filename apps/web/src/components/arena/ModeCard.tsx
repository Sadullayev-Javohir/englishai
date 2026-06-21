import { motion } from "framer-motion";
import { cn } from "@/lib/cn";
import { DuoButton } from "@/components/game/DuoButton";
import { ParrotAvatar, type ParrotVariant } from "./ParrotAvatar";

export type ModeColor = "blue" | "red" | "purple" | "green" | "yellow";

export interface ModeCardProps {
  title: string;
  hint: string;
  ctaLabel: string;
  color?: ModeColor;
  icon?: string;
  onClick: () => void;
  parrotVariant?: ParrotVariant;
  className?: string;
}

/**
 * Big lesson-select card: a parrot avatar up top, a bold title, a hint, and a full-width
 * DuoButton CTA. Hover lift / active sink with a chunky lip shadow.
 */
export function ModeCard({
  title,
  hint,
  ctaLabel,
  color = "blue",
  icon,
  onClick,
  parrotVariant = "friend",
  className,
}: ModeCardProps) {
  return (
    <motion.div
      transition={{ type: "spring", stiffness: 400, damping: 22 }}
      className={cn(
        "flex flex-col items-center gap-3 rounded-card bg-ea-surface/95 p-6 text-center",
        " ",
        className,
      )}
    >
      <ParrotAvatar size={84} variant={parrotVariant} />
      {icon && <span className="text-3xl" aria-hidden>{icon}</span>}
      <h3 className="font-duo font-extrabold text-text-primary text-headline-md">
        {title}
      </h3>
      <p className="text-secondary text-body-md">{hint}</p>
      <DuoButton color={color} fullWidth onClick={onClick}>
        {ctaLabel}
      </DuoButton>
    </motion.div>
  );
}
