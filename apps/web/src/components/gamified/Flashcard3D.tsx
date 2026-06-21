import { useState } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { ReviewStage } from "@/api/types";
import type { VocabularyItemDto } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import { cn } from "@/lib/cn";

type CardAccent =
  | "board1"
  | "board2"
  | "board3"
  | "board4"
  | "board5"
  | "board6"
  | "board7"
  | "board8"
  | "board9"
  | "board10";

const CARD_STYLE: Record<CardAccent, { face: string; lip: string }> = {
  board1: { face: "bg-ea-primary", lip: "" },
  board2: { face: "bg-ea-primary", lip: "" },
  board3: { face: "bg-ea-primary", lip: "" },
  board4: { face: "bg-ea-primary", lip: "" },
  board5: { face: "bg-ea-primary", lip: "" },
  board6: { face: "bg-ea-primary", lip: "" },
  board7: { face: "bg-ea-primary", lip: "" },
  board8: { face: "bg-ea-primary", lip: "" },
  board9: { face: "bg-ea-primary", lip: "" },
  board10: { face: "bg-ea-primary", lip: "" },
};

interface Flashcard3DProps {
  item: VocabularyItemDto;
  onSpeak: (word: string) => void;
  accent?: CardAccent;
}

/**
 * A single saved word rendered as a flippable COLORED 3D flashcard: the front shows the
 * word + a listen button, the back reveals the translation + example. Tapping the card
 * body flips it via a spring rotateY; the listen button stops propagation so it never
 * triggers a flip. Each card uses the saturated "darker" duo face color with a matching
 * deeper "lip" bottom shadow for the 3D effect, white text on both faces, and modern
 * Icon components. Built fresh for the gamification layer - the deprecated
 * src/components/duo/* layer is intentionally NOT imported or reused.
 */
export function Flashcard3D({ item, onSpeak, accent }: Flashcard3DProps) {
  const [flipped, setFlipped] = useState(false);
  const reduce = useReducedMotion();
  const mastered = item.stage === ReviewStage.Mastered;
  const style = CARD_STYLE[accent ?? "board1"];

  function flip() {
    setFlipped((f) => !f);
  }

  return (
    <div
      className="group relative h-[200px] [perspective:1200px] cursor-pointer"
      onClick={flip}
      role="button"
      tabIndex={0}
      aria-label={item.word}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          flip();
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
            "rounded-[24px] p-5 flex flex-col justify-between text-white",
            "ring-2 ring-white/30",
            style.face,
            style.lip,
            " group-hover:shadow-lg transition-all",
          )}
        >
          <div className="flex items-start justify-between gap-sm">
            <span className="font-duo font-extrabold text-[24px] leading-tight text-white capitalize">
              {item.word}
            </span>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                onSpeak(item.word);
              }}
              className="shrink-0 w-10 h-10 rounded-full bg-white/25 text-white ring-1 ring-white/40 flex items-center justify-center hover:bg-white/35 transition-colors"
              aria-label={uz.vocabularyTopics.listen}
            >
              <Icon name="volume_up" className="text-[20px]" />
            </button>
          </div>
          <div className="flex items-center justify-between">
            {mastered ? (
              <span className="inline-flex items-center gap-xs font-duo font-bold text-caption text-white">
                <Icon name="verified" filled className="text-[16px]" />
                {uz.mySavedWords.mastered}
              </span>
            ) : (
              <span />
            )}
            <span className="font-duo font-bold text-caption text-white/80 inline-flex items-center gap-xs">
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
            "ring-2 ring-white/30",
            style.face,
            style.lip,
            " group-hover:shadow-lg transition-all",
          )}
        >
          <span className="font-duo font-extrabold text-[24px] leading-tight flex items-center gap-xs">
            <Icon name="translate" className="text-[18px] text-white/70" />
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
