import { useState } from "react";
import { motion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";
import { TopicImage } from "@/components/TopicImage";
import { playWordVoice, speakEnglishWord } from "@/lib/audio";
import { useRef } from "react";
import { cn } from "@/lib/cn";
import type { CefrLevel, TopicWordDto } from "@/api/types";
import { uz } from "@/content/uz";

interface FlipCardProps {
  word: TopicWordDto;
  level: CefrLevel;
  topicId: string;
  topicTitle: string;
}

/**
 * 3D flashcard (FlipCard): tap to flip between the word face and the meaning face. The word
 * face shows the English word + a TTS DuoButton; the back shows the translation, example sentence,
 * and the topic image. Uses a CSS 3D transform with a springy flip (framer-motion rotateY + a
 * backface-hidden pair of faces).
 */
export function FlipCard({ word, level, topicId, topicTitle }: FlipCardProps) {
  const [flipped, setFlipped] = useState(false);
  const [playKey, setPlayKey] = useState(0);
  const audioRef = useRef<HTMLAudioElement | null>(null);

  function listen() {
    audioRef.current ??= new Audio();
    const played = playWordVoice(null, word.word, audioRef.current);
    if (!played) speakEnglishWord(word.word);
    setPlayKey((k) => k + 1);
  }

  return (
    <button
      type="button"
      onClick={() => setFlipped((f) => !f)}
      aria-label={flipped ? "Oldinga o'girish" : "Orqaga o'girish"}
      className="block w-full text-left [perspective:1400px] focus:outline-none"
    >
      <audio ref={audioRef} />
      <motion.div
        className="relative w-full [transform-style:preserve-3d]"
        animate={{ rotateY: flipped ? 180 : 0 }}
        transition={{ type: "spring", stiffness: 260, damping: 22 }}
        style={{ minHeight: 320 }}
      >
        {/* FRONT - word */}
        <Face className="absolute inset-0 [transform:rotateY(0deg)] [backface-visibility:hidden]">
          <div className="flex flex-col items-center justify-center h-full gap-md text-center">
            <span className="font-duo font-extrabold text-primary capitalize text-display-sm">
              {word.word}
            </span>
            {word.ipa && (
              <span className="font-body-md text-body-md text-text-secondary">{word.ipa}</span>
            )}
            <span className="mt-2 inline-flex items-center gap-2">
              <ListenButton onClick={listen} playKey={playKey} />
            </span>
            <span className="font-caption text-caption text-text-secondary mt-2">
              {uz.vocabularyTopics.tapToFlip}
            </span>
          </div>
        </Face>

        {/* BACK - meaning */}
        <Face className="absolute inset-0 [transform:rotateY(180deg)] [backface-visibility:hidden]">
          <div className="flex flex-col items-center justify-center h-full gap-sm text-center">
            <span className="font-headline-md text-headline-md text-tertiary capitalize">
              {word.word}
            </span>
            <span className="font-display-sm text-display-sm text-primary-container font-extrabold">
              {word.translation}
            </span>
            {word.exampleSentence && (
              <p className="font-body-md text-body-md text-text-secondary italic px-sm">
                "{word.exampleSentence}"
              </p>
            )}
            <TopicImage
              topicId={topicId}
              title={topicTitle}
              level={level}
              slot={1}
              hideLevelBadge
              className="h-24 w-40 rounded-xl mt-2"
            />
          </div>
        </Face>
      </motion.div>
    </button>
  );
}

function Face({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "rounded-card border border-white/40 bg-ea-surface/95 p-lg md:p-xl",
        className,
      )}
    >
      {children}
    </div>
  );
}

function ListenButton({ onClick, playKey }: { onClick: () => void; playKey: number }) {
  return (
    <motion.button
      type="button"
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
      key={playKey}
      whileTap={{ scale: 0.92 }}
      className="inline-flex items-center gap-2 rounded-full bg-ea-primary/10 text-ea-primary px-4 py-2 font-duo font-bold"
    >
      <Icon name="volume_up" className="text-[20px]" />
      {uz.vocabularyTopics.listen}
    </motion.button>
  );
}
