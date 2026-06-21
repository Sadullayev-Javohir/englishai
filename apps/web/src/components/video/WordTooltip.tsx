import { useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { playWordVoice } from "@/lib/audio";
import { Icon } from "@/components/ui/Icon";
import { Spinner } from "@/components/ui/Spinner";
import { VisemeMouth } from "@/components/speaking/VisemeMouth";
import "./wordOverlays.css";

const MAX_WIDTH = 288;
const VIEWPORT_MARGIN = 8;

interface WordTooltipProps {
  /** The hovered word, already normalised for the pronunciation lookup. */
  word: string;
  /** The transcript line the word came from - its real in-video example sentence. */
  exampleSentence: string;
  /** A vetted Uzbek meaning from the lesson glossary, if the word is in it (rule 11). */
  translation: string | null;
  /** Viewport rect of the hovered word, so the card anchors to it. */
  anchor: DOMRect;
  /** Opens the full word-detail sheet (the richer, click/tap target). */
  onOpenDetail: () => void;
  /** Keeps the card open while the pointer is over it; clears on leave. */
  onPointerEnter: () => void;
  onPointerLeave: () => void;
}

/**
 * A hover popover for a transcript word (PROJECT-SPEC B.3, Bosqich 3): its Uzbek meaning,
 * IPA + a 2D mouth (visemes), and the word in its real in-video sentence. Reuses the Speaking
 * word-detail endpoint (CMU-backed, so any English word resolves). Rendered in a portal so it
 * is never clipped by the scrollable transcript card. On touch devices (no hover) the learner
 * taps the word instead, which opens the full <c>WordDetailSheet</c>.
 */
export function WordTooltip({
  word,
  exampleSentence,
  translation,
  anchor,
  onOpenDetail,
  onPointerEnter,
  onPointerLeave,
}: WordTooltipProps) {
  const { data, loading } = useAsync(() => api.speaking.wordDetail(word), [word]);
  const cardRef = useRef<HTMLDivElement>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [pos, setPos] = useState<{ left: number; top: number; placeAbove: boolean } | null>(null);
  // Bumped on each tap of the speaker so the mouth replays in sync with the audio (it also
  // plays once when the card first shows the word). Starts at 1 so the initial render animates.
  const [playKey, setPlayKey] = useState(1);

  function listen() {
    audioRef.current ??= new Audio();
    // Prefer the Azure reference clip (works where the browser has no TTS voices); fall back to
    // the browser voice. The viseme mouth replays alongside it.
    playWordVoice(data?.audioBase64, word, audioRef.current);
    setPlayKey((k) => k + 1);
  }

  // Position the card against the word: centred horizontally (clamped to the viewport) and
  // above the word when there is room, otherwise below. Measured after render so a tall card
  // (with the mouth + example) never spills off-screen.
  useLayoutEffect(() => {
    const height = cardRef.current?.offsetHeight ?? 240;
    const width = Math.min(MAX_WIDTH, Math.max(0, window.innerWidth - VIEWPORT_MARGIN * 2));
    const left = Math.min(
      Math.max(VIEWPORT_MARGIN, anchor.left + anchor.width / 2 - width / 2),
      Math.max(VIEWPORT_MARGIN, window.innerWidth - width - VIEWPORT_MARGIN),
    );
    const maxTop = Math.max(VIEWPORT_MARGIN, window.innerHeight - height - VIEWPORT_MARGIN);
    const placeAbove = anchor.top > height + VIEWPORT_MARGIN;
    const top = placeAbove
      ? Math.max(VIEWPORT_MARGIN, anchor.top - height - VIEWPORT_MARGIN)
      : Math.max(VIEWPORT_MARGIN, Math.min(anchor.bottom + VIEWPORT_MARGIN, maxTop));
    setPos({ left, top, placeAbove });
  }, [anchor, data, loading]);

  return createPortal(
    <div
      ref={cardRef}
      role="tooltip"
      onMouseEnter={onPointerEnter}
      onMouseLeave={onPointerLeave}
      style={{
        position: "fixed",
        left: pos?.left ?? anchor.left,
        top: pos?.top ?? anchor.bottom + 8,
        width: `min(${MAX_WIDTH}px, calc(100vw - ${VIEWPORT_MARGIN * 2}px))`,
        visibility: pos ? "visible" : "hidden",
        zIndex: 60,
      }}
      className="word-tooltip-card space-y-sm"
    >
      <div className="flex items-center justify-between gap-sm">
        <div>
          <p className="font-caption text-caption font-bold uppercase tracking-[0.16em] text-ea-blue-200">Word power</p>
          <span className="font-duo text-headline-md font-extrabold capitalize text-white drop-shadow">{word}</span>
        </div>
        <button
          aria-label={uz.videoWord.listen}
          onClick={listen}
          className="word-tooltip-card__listen"
        >
          <Icon name="volume_up" className="text-[22px]" />
        </button>
      </div>

      {loading ? (
        <div className="flex justify-center py-md">
          <Spinner />
        </div>
      ) : !data ? (
        <p className="font-body-md text-body-md text-white/80">{uz.videoWord.notFound}</p>
      ) : (
        <>
          <div className="word-tooltip-card__meaning">
            <p className="word-tooltip-card__ipa font-body-md text-body-md">{data.ipa}</p>
            {translation && (
              <p className="mt-1 font-duo text-headline-md font-extrabold text-white">{translation}</p>
            )}
          </div>

          <div className="word-tooltip-card__mouth flex items-center gap-md p-sm">
            <VisemeMouth frames={data.visemes} playKey={playKey} size={56} />
            <p className="flex-1 font-caption text-caption text-ea-blue-50">
              <span className="font-label-md text-label-md text-white">{uz.videoWord.mouthPosition}</span>
              <br />
              {data.phonemes.map((p) => p.phoneme).join(" · ")}
            </p>
          </div>

          <div className="word-tooltip-card__example p-sm">
            <p className="font-body-md text-body-md italic leading-snug text-white">"{exampleSentence}"</p>
          </div>
        </>
      )}

      <button
        onClick={onOpenDetail}
        className="word-tooltip-card__details"
      >
        {uz.videoWord.details}
        <Icon name="arrow_forward" className="text-[16px]" />
      </button>
    </div>,
    document.body,
  );
}
