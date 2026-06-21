import { useCallback, useEffect, useRef, useState } from "react";
import { uz } from "@/content/uz";
import { WordTooltip } from "@/components/video/WordTooltip";
import { WordDetailSheet } from "@/components/video/WordDetailSheet";

/**
 * Shared hover-popover / click-sheet machinery for a target vocabulary word, factored out of
 * <HighlightedPassage> so the passage AND the "Yangi so'zlar" word cards behave identically:
 * hover shows the {@link WordTooltip} (Uzbek meaning + IPA + viseme mouth + native audio), click
 * opens the full {@link WordDetailSheet}. One source of truth for the interaction.
 *
 * `getTranslation` resolves the vetted Uzbek meaning for a word (rule 11); callers pass a lookup
 * over their target word set. Returns:
 *  - `bind(word, example)` → event handlers to spread on the word's element, plus a stable
 *    `data-*`-free className-agnostic interaction surface.
 *  - `overlay` → the tooltip + sheet portals to render once per host component.
 */
export function useWordPopover(getTranslation: (word: string) => string | null) {
  const [hovered, setHovered] = useState<{ word: string; example: string; anchor: DOMRect } | null>(
    null,
  );
  const [selected, setSelected] = useState<{ word: string; example: string } | null>(null);

  const openTimer = useRef<number | null>(null);
  const closeTimer = useRef<number | null>(null);

  const clearTimers = useCallback(() => {
    if (openTimer.current) window.clearTimeout(openTimer.current);
    if (closeTimer.current) window.clearTimeout(closeTimer.current);
    openTimer.current = null;
    closeTimer.current = null;
  }, []);

  const hoverWord = useCallback((word: string, example: string, anchor: DOMRect) => {
    if (closeTimer.current) window.clearTimeout(closeTimer.current);
    if (openTimer.current) window.clearTimeout(openTimer.current);
    openTimer.current = window.setTimeout(() => setHovered({ word, example, anchor }), 180);
  }, []);

  const scheduleClose = useCallback(() => {
    if (openTimer.current) window.clearTimeout(openTimer.current);
    closeTimer.current = window.setTimeout(() => setHovered(null), 140);
  }, []);

  const openDetail = useCallback(
    (word: string, example: string) => {
      clearTimers();
      setHovered(null);
      setSelected({ word, example });
    },
    [clearTimers],
  );

  useEffect(() => () => clearTimers(), [clearTimers]);

  // Handlers to spread onto any element that should reveal the word popover.
  const bind = useCallback(
    (word: string, example: string) => ({
      onMouseEnter: (e: React.MouseEvent) =>
        hoverWord(word, example, e.currentTarget.getBoundingClientRect()),
      onMouseLeave: scheduleClose,
      onClick: () => openDetail(word, example),
    }),
    [hoverWord, scheduleClose, openDetail],
  );

  const overlay = (
    <>
      {hovered && (
        <WordTooltip
          word={hovered.word}
          exampleSentence={hovered.example}
          translation={getTranslation(hovered.word)}
          anchor={hovered.anchor}
          onOpenDetail={() => openDetail(hovered.word, hovered.example)}
          onPointerEnter={() => {
            if (closeTimer.current) window.clearTimeout(closeTimer.current);
          }}
          onPointerLeave={scheduleClose}
        />
      )}
      {selected && (
        <WordDetailSheet
          word={selected.word}
          exampleSentence={selected.example}
          translation={getTranslation(selected.word)}
          exampleLabel={uz.videoWord.inThisText}
          enableRecording={false}
          onClose={() => setSelected(null)}
        />
      )}
    </>
  );

  return { bind, overlay };
}
