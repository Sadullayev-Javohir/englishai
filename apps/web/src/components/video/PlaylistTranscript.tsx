import { useLayoutEffect, useRef, useState } from "react";
import { Captions, CaptionsOff, ChevronDown, ChevronUp, LoaderCircle, Radio, RefreshCw, Sparkles } from "lucide-react";
import type { TranscriptSegmentDto } from "@/api/types";
import { formatDuration } from "@/lib/labels";
import { captionTokens, spokenWordIndex } from "@/lib/videoKaraoke";
import type { TranscriptLoadState } from "./usePlaylistTranscript";

const timestamp = (seconds: number) => formatDuration(seconds).padStart(5, "0");

interface PlaylistTranscriptProps {
  transcript: TranscriptSegmentDto[];
  selectedIndex: number;
  currentTime: number;
  showTranslation: boolean;
  unavailable: boolean;
  loadState?: TranscriptLoadState;
  onRetry?: () => void;
  aiSending: boolean;
  onSelect: (index: number) => void;
  onAskAi: (index: number) => void;
  onToggleTranslation: () => void;
}

/** A small timed window, not thousands of scrolling rows. Only an oversized single row scrolls. */
export function PlaylistTranscript({
  transcript, selectedIndex, currentTime, showTranslation, unavailable, aiSending,
  onSelect, onAskAi, onToggleTranslation, loadState = "loading", onRetry,
}: PlaylistTranscriptProps) {
  const listRef = useRef<HTMLOListElement>(null);
  const [capacity, setCapacity] = useState(3);
  const [manualStart, setManualStart] = useState<number | null>(null);
  const hasTranscript = transcript.length > 0;
  const start = Math.min(manualStart ?? Math.max(0, selectedIndex - capacity + 1), Math.max(0, transcript.length - capacity));
  const end = Math.min(start + capacity, transcript.length);
  const lastSelected = useRef(selectedIndex);

  useLayoutEffect(() => {
    if (lastSelected.current !== selectedIndex) {
      lastSelected.current = selectedIndex;
      setManualStart(null);
    }
  }, [selectedIndex]);

  useLayoutEffect(() => {
    const list = listRef.current;
    if (!list || typeof ResizeObserver === "undefined") return;
    let lastHeight = 0;
    let lastWidth = 0;
    const observer = new ResizeObserver(() => {
      const { width, height } = list.getBoundingClientRect();
      if (!width || !height) return;
      if (Math.abs(height - lastHeight) > 20 || Math.abs(width - lastWidth) > 15) {
        lastHeight = height;
        lastWidth = width;
        setCapacity(window.innerWidth <= 700 ? 3 : 4);
      }
    });
    observer.observe(list);
    return () => observer.disconnect();
  }, [hasTranscript]);

  useLayoutEffect(() => {
    const list = listRef.current;
    if (!list || !list.clientHeight || capacity <= 1) return;
    const gap = Number.parseFloat(getComputedStyle(list).rowGap) || 0;
    const rows = Array.from(list.children) as HTMLElement[];
    const needed = rows.reduce((total, row) => total + row.scrollHeight, 0) + Math.max(0, rows.length - 1) * gap;
    if (needed > list.clientHeight + 1) setCapacity(value => value - 1);
  }, [capacity, end, selectedIndex, showTranslation, start, transcript]);

  if (!transcript.length) {
    const failed = loadState === "error";
    const loading = !unavailable && !failed;
    const StatusIcon = unavailable ? CaptionsOff : failed ? RefreshCw : LoaderCircle;
    const label = unavailable ? "Bu video uchun EnglishAI subtitrlari mavjud emas"
      : failed ? "Subtitrlarni yuklab bo‘lmadi. Qayta urinib ko‘ring."
        : loadState === "delayed" ? "YouTube subtitrlarini olish kutilganidan uzoqroq davom etmoqda…"
          : "Subtitrlar yuklanmoqda…";
    return <div className={`video-playlist-player__caption-pending${unavailable ? " is-unavailable" : ""}`} role="status"
      aria-live="polite" aria-busy={loading} aria-label={label}>
      <span className="video-playlist-player__loading-icon"><StatusIcon size={32} className={loading ? "video-playlist-player__spinner" : undefined} aria-hidden /></span>
      <p>{label}</p>
      {loading && <div className="video-playlist-player__loading-lines" aria-hidden><i /><i /><i /></div>}
      {(failed || loadState === "delayed") && onRetry && <button type="button" className="video-playlist-player__transcript-retry" onClick={onRetry}><RefreshCw size={14} aria-hidden />Qayta urinish</button>}
    </div>;
  }

  return <>
    <button type="button" className="video-playlist-player__translation-toggle" aria-pressed={showTranslation}
      aria-label={showTranslation ? "Tarjimani yashirish" : "Tarjimani ko‘rsatish"} onClick={onToggleTranslation}><Captions size={20} aria-hidden /></button>
    <ol ref={listRef} aria-label="Video transkripti" className={`video-playlist-player__caption-list${capacity === 1 ? " is-single-caption" : ""}`}>
      {transcript.slice(start, end).map((segment, offset) => {
        const index = start + offset;
        const current = index === selectedIndex;
        const activeWord = current ? spokenWordIndex(segment, currentTime) : -1;
        return <li key={`${index}:${segment.startSeconds}`} className={`video-playlist-player__caption-row${current ? " is-current" : ""}`}
          data-caption-index={index} data-current={current || undefined} aria-current={current ? "true" : undefined}
          aria-posinset={index + 1} aria-setsize={transcript.length}>
          <button type="button" className="video-playlist-player__caption-select" aria-label={`${timestamp(segment.startSeconds)} — ${segment.englishText}`}
            aria-expanded={current && showTranslation} aria-controls={current ? `playlist-translation-${index}` : undefined} onClick={() => onSelect(index)}>
            <time>{timestamp(segment.startSeconds)}{current && <span> · TANLANGAN JUMLA</span>}</time>
            <strong>{captionTokens(segment).map((token, tokenIndex) => <span key={tokenIndex}
              className={token.wordIndex >= 0 && token.wordIndex === activeWord ? "is-speaking" : undefined}>{token.text}</span>)}</strong>
          </button>
          {current && <p id={`playlist-translation-${index}`} hidden={!showTranslation} className="video-playlist-player__translation">
            <span>O‘ZBEKCHA</span>{segment.uzbekTranslation || "Tarjima tayyorlanmoqda…"}
          </p>}
          <button type="button" className="video-playlist-player__caption-ai" aria-label={`"${segment.englishText}" ni AI bilan tushunish`}
            disabled={aiSending} onClick={() => onAskAi(index)}><Sparkles size={14} aria-hidden /><span>AI’dan so‘rash</span></button>
        </li>;
      })}
    </ol>
    <div className="video-playlist-player__transcript-nav">
      <button type="button" className="video-playlist-player__follow" aria-label="Videoga mos kuzatish" aria-pressed={manualStart === null} onClick={() => setManualStart(null)}>
        <Radio size={14} aria-hidden /><span>{manualStart === null ? "Jumlani bosing — tarjima ochiladi" : "Videoga qaytish"}</span>
      </button>
      <button type="button" aria-label="Oldingi jumlalar" disabled={start === 0} onClick={() => setManualStart(Math.max(0, start - capacity))}><ChevronUp size={16} aria-hidden /></button>
      <button type="button" aria-label="Keyingi jumlalar" disabled={end >= transcript.length} onClick={() => setManualStart(start + capacity)}><ChevronDown size={16} aria-hidden /></button>
    </div>
  </>;
}
