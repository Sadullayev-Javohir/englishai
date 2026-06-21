import { motion } from "framer-motion";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

/**
 * Duolingo-style learning path - a winding vertical trail of circular lesson
 * nodes. The current node bobs; locked nodes are greyed with a lock; completed
 * nodes show a check. Tap the active node to start the lesson.
 * Pure presentational: feed it `nodes` + `onSelect`.
 * Canonical game-layer copy (migrated from duo/*).
 */

export interface PathNode {
  id: string;
  label: string;
  icon: string;
  state: "completed" | "active" | "locked";
  /** Nudges the node left/right to create the snake path. -1 left, 0 center, 1 right. */
  offset?: number;
}

interface LearningPathProps {
  nodes: PathNode[];
  onSelect?: (node: PathNode) => void;
}

const OFFSET_CLASS: Record<number, string> = {
  [-1]: "md:translate-x-[-64px] translate-x-[-24px]",
  [0]: "translate-x-0",
  [1]: "md:translate-x-[64px] translate-x-[24px]",
};

const STATE_STYLE: Record<
  PathNode["state"],
  { ring: string; fill: string; text: string; iconFilled: boolean }
> = {
  completed: {
    ring: "border-ea-green-600/30",
    fill: "bg-ea-green-600",
    text: "text-white",
    iconFilled: true,
  },
  active: {
    ring: "border-ea-orange-500/40",
    fill: "bg-ea-orange-500",
    text: "text-white",
    iconFilled: true,
  },
  locked: {
    ring: "border-ea-primary",
    fill: "bg-ea-surface-soft",
    text: "text-text-muted",
    iconFilled: false,
  },
};

export function LearningPath({ nodes, onSelect }: LearningPathProps) {
  return (
    <div className="flex flex-col items-center gap-3 md:gap-5 py-4">
      {nodes.map((node, i) => {
        const s = STATE_STYLE[node.state];
        const interactive = node.state !== "locked";
        return (
          <motion.div
            key={node.id}
            className={cn("relative z-10", OFFSET_CLASS[node.offset ?? 0])}
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{
              delay: i * 0.05,
              type: "spring",
              stiffness: 300,
              damping: 22,
            }}
          >
            {node.state === "active" && (
              <span className="absolute inset-0 -z-10 animate-ea-pulse-ring rounded-full bg-ea-primary/20" />
            )}
            <motion.button
              type="button"
              disabled={!interactive}
              onClick={() => interactive && onSelect?.(node)}
              whileHover={interactive ? { scale: 1.05 } : undefined}
              whileTap={interactive ? { scale: 0.98 } : undefined}
              transition={{ type: "spring", stiffness: 500, damping: 28 }}
              aria-label={node.label}
              className={cn(
                "relative flex items-center justify-center rounded-full",
                "w-[78px] h-[78px] md:w-[92px] md:h-[92px]",
                "border-2 font-duo",
                s.ring,
                s.fill,
                s.text,
                !interactive && "cursor-not-allowed",
              )}
            >
              {node.state === "locked" ? (
                <Icon name="lock" className="text-[26px]" />
              ) : node.state === "completed" ? (
                <Icon name="check" filled className="text-[34px]" />
              ) : (
                <Icon name={node.icon} filled className="text-[34px]" />
              )}
            </motion.button>
            {(node.state === "active" || node.state === "locked") && (
              <p
                className={cn(
                  "mt-2 text-center font-duo font-bold text-xs md:text-sm",
                  node.state === "locked"
                    ? "text-text-muted"
                    : "text-ea-orange-600",
                )}
              >
                {node.label}
              </p>
            )}
          </motion.div>
        );
      })}
    </div>
  );
}
