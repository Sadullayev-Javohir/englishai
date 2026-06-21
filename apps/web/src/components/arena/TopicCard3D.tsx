import { motion } from "framer-motion";
import { cn } from "@/lib/cn";
import { TopicImage } from "@/components/TopicImage";
import { CefrLevel } from "@/api/types";

export type TopicColor = "blue" | "red" | "purple" | "green" | "yellow";

export interface TopicCard3DProps {
  title: string;
  subtitle?: string;
  topicId?: string;
  /** CEFR code (e.g. "B1") or enum - accepted as string. */
  level?: string | CefrLevel;
  color?: TopicColor;
  icon?: string;
  onClick: () => void;
  /** Stagger index - drives the entrance spring delay. */
  index?: number;
  className?: string;
}

// Gradient header used when no topicId image is supplied. Kept as literal classes
// so Tailwind's scanner keeps them in the build.
const GRADIENT: Record<TopicColor, string> = {
  blue: "bg-ea-primary",
  red: "bg-ea-primary",
  purple: "bg-ea-primary",
  green: "bg-ea-primary",
  yellow: "bg-ea-primary",
};

const ICON_TINT: Record<TopicColor, string> = {
  blue: "text-white/90",
  red: "text-white/90",
  purple: "text-white/90",
  green: "text-white/90",
  yellow: "text-white/90",
};

/**
 * 3D grid card for a speaking topic. When `topicId` is provided it shows the licensed
 * topic photo via <TopicImage>; otherwise it falls back to a color-tinted gradient
 * header with the `icon`. The `index` prop staggers the spring-in entrance.
 */
export function TopicCard3D({
  title,
  subtitle,
  topicId,
  level,
  color = "blue",
  icon,
  onClick,
  index,
  className,
}: TopicCard3DProps) {
  // TopicImage requires a CefrLevel; the card accepts a CEFR enum or code string.
  const safeLevel = (level as CefrLevel) ?? CefrLevel.A1;

  return (
    <motion.button
      type="button"
      onClick={onClick}
      initial={{ opacity: 0, y: 16, scale: 0.96 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{
        type: "spring",
        stiffness: 380,
        damping: 24,
        delay: typeof index === "number" ? index * 0.06 : 0,
      }}
      className={cn(
        "flex flex-col overflow-hidden rounded-card bg-ea-surface/95 text-left",
        " ",
        "outline-none",
        className,
      )}
    >
      {/* Header */}
      <div className={cn("relative h-24 w-full md:h-32", topicId ? "" : GRADIENT[color])}>
        {topicId ? (
          <TopicImage
            topicId={topicId}
            title={title}
            level={safeLevel}
            className="h-24 w-full md:h-32"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center">
            {icon && <span className={cn("text-4xl", ICON_TINT[color])}>{icon}</span>}
          </div>
        )}
      </div>

      {/* Body */}
      <div className="flex flex-col gap-1 p-4">
        <h3 className="font-duo font-extrabold text-text-primary text-body-lg line-clamp-2">
          {title}
        </h3>
        {subtitle && (
          <p className="text-secondary text-body-md line-clamp-2">{subtitle}</p>
        )}
      </div>
    </motion.button>
  );
}
