import { motion } from "framer-motion";
import { CefrLevel } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { TopicImage } from "@/components/TopicImage";
import { useWordVoice } from "@/lib/useWordVoice";
import { cn } from "@/lib/cn";
import { CARD_STYLE, type CardAccent } from "@/lib/cardPalette";

/**
 * 3D flashcard for the vocabulary SRS review redesign (spec §Review.1).
 *
 * Locked prop contract - other agents depend on these EXACT signatures:
 *   word, translation, exampleSentence, partOfSpeech, sourceTopicId,
 *   flipped (controlled by parent), onReveal (called on tap when not flipped).
 *
 * A flip card with a chunky bottom lip. The front is tappable to reveal;
 * the back shows the translation + example and a hint to rate. The visible
 * face is driven by the parent via `flipped`. Tap-to-flip is only active on
 * the front face (the back face is for rating).
 */

interface Flashcard3DProps {
  word: string;
  translation: string;
  exampleSentence: string | null;
  partOfSpeech: string | null;
  sourceTopicId: string | null;
  /** 3D board accent so the flashcard is a chunky coloured surface, never white. */
  accent?: CardAccent;
  flipped: boolean;
  onReveal: () => void;
}

export function Flashcard3D({
  word,
  translation,
  exampleSentence,
  partOfSpeech,
  sourceTopicId,
  accent = "board2",
  flipped,
  onReveal,
}: Flashcard3DProps) {
  const speak = useWordVoice();


  return (
    <div className="w-full [perspective:1200px]">
      <motion.div
        animate={{ rotateY: flipped ? 180 : 0 }}
        transition={{ type: "spring", stiffness: 260, damping: 24 }}
        className="relative min-h-[290px] w-full sm:min-h-[340px] [@media(max-height:600px)]:min-h-[260px]"
        style={{ transformStyle: "preserve-3d" }}
      >
        {/* ── FRONT ─────────────────────────────────────────────────────── */}
        <motion.div
          whileTap={flipped ? undefined : { scale: 0.985 }}
          transition={{ type: "spring", stiffness: 400, damping: 28 }}
          role="button"
          tabIndex={flipped ? -1 : 0}
          aria-label={`Reveal translation for ${word}`}
          onClick={flipped ? undefined : onReveal}
          onKeyDown={(e) => {
            if (flipped) return;
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              onReveal();
            }
          }}
          style={{ backfaceVisibility: "hidden" }}
          className={cn(
            "absolute inset-0 flex cursor-pointer flex-col overflow-hidden rounded-card border border-white/25 text-white select-none",
            CARD_STYLE[accent].face,
            CARD_STYLE[accent].lip,
          )}
        >
          {/* Card art: topic image when available, otherwise a parrot tile */}
          {sourceTopicId ? (
            <TopicImage
              topicId={sourceTopicId}
              title={word}
              level={CefrLevel.A1}
              className="h-24 w-full shrink-0 sm:h-32 [@media(max-height:600px)]:h-20"
            />
          ) : (
            <div className="flex h-24 w-full shrink-0 items-center justify-center bg-white/20 sm:h-32 [@media(max-height:600px)]:h-20">
              <Icon name="auto_stories" className="text-[44px] text-white" />
            </div>
          )}

          <div className="flex flex-1 flex-col items-center justify-center gap-2 px-4 pb-5 pt-3 sm:gap-3 sm:px-6 sm:pb-8 sm:pt-4">
            <h2 className="max-w-full break-words text-center font-duo text-[32px] font-extrabold leading-tight text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.35)] sm:text-[40px]">
              {word}
            </h2>

            {partOfSpeech && (
              <span className="rounded-full bg-white/25 px-3 py-1 font-duo text-label-md font-bold text-white">
                {partOfSpeech}
              </span>
            )}

            <button
              type="button"
              aria-label={`Play pronunciation of ${word}`}
              onClick={(e) => {
                e.stopPropagation();
                speak(word);
              }}
              className="mt-1 flex h-12 w-12 items-center justify-center rounded-full bg-white/25 text-white ring-2 ring-white/40 shadow-sm transition-transform "
            >
              <Icon name="volume_up" className="text-[24px]" />
            </button>
          </div>
        </motion.div>

        {/* ── BACK ──────────────────────────────────────────────────────── */}
        <div
          style={{ transform: "rotateY(180deg)", backfaceVisibility: "hidden" }}
          className={cn(
            "absolute inset-0 flex flex-col items-center justify-center gap-3 overflow-y-auto rounded-card border border-white/25 px-4 py-5 text-center text-white select-none sm:gap-4 sm:px-6",
            CARD_STYLE[accent].face,
            CARD_STYLE[accent].lip,
          )}
        >
          <span className="font-duo text-label-md font-bold uppercase tracking-wide text-white/80">
            Tarjima
          </span>
          <p className="max-w-full break-words text-center font-duo text-[28px] font-extrabold leading-tight text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.35)] sm:text-[34px]">
            {translation}
          </p>

          {exampleSentence && (
            <p className="max-w-[28ch] break-words text-center font-duo text-sm font-semibold italic text-white/90 sm:text-body-md">
              “{exampleSentence}”
            </p>
          )}

          <span className="mt-1 rounded-full bg-white/25 px-3 py-1 font-duo text-label-md font-bold text-white">
            Pastda baho bering ↓
          </span>
        </div>
      </motion.div>
    </div>
  );
}
