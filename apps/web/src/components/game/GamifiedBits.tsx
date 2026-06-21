import { motion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";

/**
 * Small game HUD bits for the topic lesson: Hearts (lives) and the XP pill.
 * Frosted white pills that sit above the jungle path. Hearts deplete as the
 * learner answers exercises wrong; XP ticks up with every correct answer.
 */

export function Hearts({
  count,
  max = 5,
  className,
}: {
  count: number;
  max?: number;
  className?: string;
}) {
  return (
    <div
      className={cn(
        // Tighter gap/padding below `sm` so five hearts plus the XP badge still
        // fit on one HUD row on a 360-390px phone.
        "inline-flex items-center gap-0.5 rounded-[14px] border-2 border-white/15 bg-ea-danger-700 px-1.5 py-1.5 text-white  sm:gap-1 sm:px-3",
        className,
      )}
    >
      {Array.from({ length: max }).map((_, i) => {
        const filled = i < count;
        return (
          <motion.span
            key={i}
            animate={filled ? { scale: [1, 1.25, 1] } : { opacity: 0.9, scale: 1 }}
            transition={{ duration: 0.3 }}
          >
            <Icon
              name={filled ? "heart" : "heart_broken"}
              filled
              className={cn(
                "text-[18px] md:text-[20px]",
                filled ? "text-ea-danger" : "text-white/35"
              )}
              style={{ fill: filled ? "var(--ea-danger)" : "rgba(255,255,255,0.25)" }}
            />
          </motion.span>
        );
      })}
    </div>
  );
}

export function XpBadge({
  xp,
  className,
}: {
  xp: number;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "inline-flex items-center gap-1 rounded-[14px] border-2 border-white/15 bg-ea-yellow-700 px-2 py-1.5 text-white  sm:gap-1.5 sm:px-3",
        className,
      )}
    >
      <Icon name="star" filled className="text-ea-orange-600 text-[18px]" />
      <span className="font-duo font-extrabold text-[15px] text-white leading-none">
        {xp}
      </span>
      <span className="font-caption text-caption text-white/75 leading-none hidden md:inline">
        XP
      </span>
    </div>
  );
}
