import { motion } from "framer-motion";
import { cn } from "@/lib/cn";

export type IdeaColor = "yellow" | "blue" | "green" | "purple";

export interface IdeaCardProps {
  imageUrl?: string;
  emoji?: string;
  title: string;
  starter?: string;
  color?: IdeaColor;
  className?: string;
}

const COLOR_MAP: Record<IdeaColor, string> = {
  yellow: "bg-ea-orange-500/15 text-ea-orange-600",
  blue: "bg-ea-primary/15 text-ea-primary",
  green: "bg-ea-green-600/15 text-ea-green-600",
  purple: "bg-ea-primary/15 text-ea-primary",
};

/**
 * A 3D "conversation idea" card: a thumbnail (image or emoji block), a bold question
 * title, and a smaller starter sentence. Lifts on hover for the playful game feel.
 */
export function IdeaCard({
  imageUrl,
  emoji,
  title,
  starter,
  color = "yellow",
  className,
}: IdeaCardProps) {
  return (
    <motion.div
      transition={{ type: "spring", stiffness: 400, damping: 22 }}
      className={cn(
        "rounded-card bg-ea-surface/95 p-4",
        "",
        className,
      )}
    >
      {/* Thumbnail */}
      <div className="mb-3 h-28 w-full overflow-hidden rounded-xl">
        {imageUrl ? (
          <img
            src={imageUrl}
            alt=""
            className="h-full w-full object-cover"
          />
        ) : (
          <div
            className={cn(
              "flex h-full w-full items-center justify-center rounded-xl text-4xl",
              COLOR_MAP[color],
            )}
          >
            {emoji ?? "💡"}
          </div>
        )}
      </div>

      <h3 className="font-duo font-extrabold text-text-primary text-body-lg leading-tight">
        {title}
      </h3>
      {starter && (
        <p className="mt-1 text-secondary text-body-md leading-snug">{starter}</p>
      )}
    </motion.div>
  );
}
