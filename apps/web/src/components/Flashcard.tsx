import { useState } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { ReviewStage } from "@/api/types";
import type { VocabularyItemDto } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import { cn } from "@/lib/cn";

/**
 * FlipFlashcard - a single word rendered as a flippable 3D flashcard: the front shows the word
 * + a listen button, the back reveals the translation + example. Tapping the card body flips it
 * via a spring rotateY; the listen button stops propagation so it never triggers a flip.
 *
 * This is the shared, quality 3D flashcard used by the vocabulary hub's "recently learned" strip
 * and the saved-words vault. Built fresh per the gamification spec - the deprecated
 * src/components/duo/* layer is intentionally NOT imported or reused here.
 */
export function FlipFlashcard({
  item,
  onSpeak,
}: {
  item: VocabularyItemDto;
  onSpeak: (word: string) => void;
}) {
  const [flipped, setFlipped] = useState(false);
  const reduce = useReducedMotion();
  const mastered = item.stage === ReviewStage.Mastered;

  return (
    <div
      className="group relative h-[180px] [perspective:1200px] cursor-pointer"
      onClick={() => setFlipped((f) => !f)}
      role="button"
      tabIndex={0}
      aria-label={item.word}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          setFlipped((f) => !f);
        }
      }}
    >
      <motion.div
        className="relative h-full w-full [transform-style:preserve-3d] transition-transform"
        animate={{ rotateY: flipped ? 180 : 0 }}
        transition={
          reduce
            ? { duration: 0 }
            : { type: "spring", stiffness: 260, damping: 26 }
        }
      >
        {/* Front - word */}
        <div
          className={cn(
            "absolute inset-0 [backface-visibility:hidden]",
            "rounded-[24px] bg-ea-surface/95  p-5 flex flex-col justify-between",
            "",
            " transition-all",
          )}
        >
          <div className="flex items-start justify-between gap-sm">
            <span className="font-duo font-extrabold text-[22px] leading-tight text-ea-text capitalize">
              {item.word}
            </span>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                onSpeak(item.word);
              }}
              className="shrink-0 w-9 h-9 rounded-full bg-ea-primary/15 text-ea-primary flex items-center justify-center hover:bg-ea-primary/25 transition-colors"
              aria-label={uz.vocabularyTopics.listen}
            >
              <Icon name="volume_up" className="text-[20px]" />
            </button>
          </div>
          <div className="flex items-center justify-between">
            {mastered ? (
              <span className="inline-flex items-center gap-xs font-duo font-bold text-caption text-ea-green-600">
                <Icon name="verified" filled className="text-[16px]" />
                {uz.mySavedWords.mastered}
              </span>
            ) : (
              <span />
            )}
            <span className="font-duo font-bold text-caption text-ea-muted inline-flex items-center gap-xs">
              <Icon name="refresh" className="text-[15px]" />
              {uz.mySavedWords.flipHint}
            </span>
          </div>
        </div>

        {/* Back - translation + example */}
        <div
          className={cn(
            "absolute inset-0 [backface-visibility:hidden] [transform:rotateY(180deg)]",
            "rounded-[24px] p-5 flex flex-col justify-center gap-sm text-white",
            "bg-ea-primary",
          )}
        >
          <span className="font-duo font-extrabold text-[22px] leading-tight">
            {item.translation}
          </span>
          {item.exampleSentence && (
            <p className="font-duo text-body-md text-white/90 italic leading-snug line-clamp-3">
              "{item.exampleSentence}"
            </p>
          )}
        </div>
      </motion.div>
    </div>
  );
}
