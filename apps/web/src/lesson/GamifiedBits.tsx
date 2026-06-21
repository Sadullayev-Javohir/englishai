import { AnimatePresence, motion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";
import { MAX_HEARTS } from "./lessonSession";

interface HeartsProps {
  hearts: number;
  className?: string;
}

/**
 * Hearts pill (GamifiedBits): full red hearts for remaining lives, empty/cracked for lost.
 * A heart that was just lost plays a quick crack + fade so the penalty reads instantly.
 */
export function Hearts({ hearts, className }: HeartsProps) {
  return (
    <div
      className={`inline-flex items-center gap-0.5 rounded-full bg-ea-surface/85 px-3 py-1.5 shadow-sm ${className ?? ""}`}
      aria-label={`${hearts} ta yurak qoldi`}
    >
      {Array.from({ length: MAX_HEARTS }, (_, i) => {
        const alive = i < hearts;
        return (
          <motion.span
            key={i}
            animate={
              !alive
                ? { scale: [1, 1.3, 1], rotate: [0, -12, 0] }
                : { scale: 1, rotate: 0 }
            }
            transition={{ duration: 0.4 }}
            className="inline-flex"
          >
            <Icon
              name={alive ? "heart" : "heart_broken"}
              filled
              className={`text-[22px] ${alive ? "text-ea-danger" : "text-black/25"}`}
            />
          </motion.span>
        );
      })}
    </div>
  );
}

interface XpBitsProps {
  xp: number;
  className?: string;
}

/** XP counter (GamifiedBits): gem + animated number that ticks up as XP is earned. */
export function XpBits({ xp, className }: XpBitsProps) {
  return (
    <div
      className={`inline-flex items-center gap-1.5 rounded-full bg-ea-surface/85 px-3 py-1.5 shadow-sm font-duo font-extrabold text-ea-primary ${className ?? ""}`}
    >
      <Icon name="star" filled className="text-[18px] text-ea-primary" />
      <AnimatePresence mode="popLayout">
        <motion.span
          key={xp}
          initial={{ y: -8, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: 8, opacity: 0 }}
          transition={{ duration: 0.25 }}
        >
          {xp}
        </motion.span>
      </AnimatePresence>
    </div>
  );
}
