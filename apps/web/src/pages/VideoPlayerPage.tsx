import { memo, useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { ArrowRight, Captions, ChevronDown, ChevronUp, ListChecks, Radio, Sparkles } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { uz } from "@/content/uz";
import { useDocumentTitle } from "@/app/documentTitle";
import { api } from "@/api/client";
import { EnergyOutcome, TranscriptStatus } from "@/api/types";
import type { EnergyDto, TranscriptSegmentDto, VideoLessonDto } from "@/api/types";
import { useAsync } from "@/lib/useAsync";
import { activeSegmentIndex, useYouTubePlayer } from "@/lib/useYouTubePlayer";
import { sanitizeVideoTranscript } from "@/lib/videoTranscript";
import { cefrShort, formatDuration } from "@/lib/labels";
import { cn } from "@/lib/cn";
import { WordDetailSheet } from "@/components/video/WordDetailSheet";
import { useVideoExplainConversation } from "@/components/video/useVideoExplainConversation";
import { ExplainChatPanel } from "@/components/video/ExplainChatPanel";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { publishAssistantContext } from "@/components/assistantContext";
import { VideoPlayerHeader } from "@/components/video/VideoPlayerHeader";
import { useEnergy } from "@/components/game/EnergyProvider";
import { captionTokens, spokenWordIndex } from "@/lib/videoKaraoke";
import "./VideoPlayerPage.css";

const PLAYER_CONTAINER_ID = "video-lesson-player";
const timestamp = (seconds: number) => formatDuration(seconds).padStart(5, "0");

/** Pen 72 WEB/MOBILE. YouTube owns all playback controls; learning tools stay outside it. */
export function VideoPlayerPage() {
  const { id = "" } = useParams();
  const { data, loading, error } = useAsync(() => api.video.lesson(id), [id]);
  const { energy, consume, openEnergyModal } = useEnergy();
  const [energyGate, setEnergyGate] = useState<"checking" | "allowed" | "blocked">("checking");
  const [energyAttempt, setEnergyAttempt] = useState(0);
  useDocumentTitle(data?.title, uz.nav.video);
  const viewportRef = useRef<HTMLElement>(null);

  useLayoutEffect(() => {
    document.documentElement.classList.add("video-player-route-active");
    document.body.classList.add("video-player-route-active");
    const viewport = window.visualViewport;
    const resize = () => {
      if (!viewport || !viewportRef.current) return;
      viewportRef.current.style.setProperty("--video-viewport-height", `${viewport.height}px`);
      viewportRef.current.dataset.keyboard = String(window.innerHeight - viewport.height > 150);
    };
    resize();
    viewport?.addEventListener("resize", resize);
    return () => {
      document.documentElement.classList.remove("video-player-route-active");
      document.body.classList.remove("video-player-route-active");
      viewport?.removeEventListener("resize", resize);
    };
  }, []);

  useEffect(() => {
    const videoId = data?.youTubeVideoId;
    if (!videoId) return;
    let cancelled = false;
    setEnergyGate("checking");
    void consume("video", videoId).then((result) => {
      if (cancelled) return;
      if (result.outcome === EnergyOutcome.Insufficient) {
        setEnergyGate("blocked");
        openEnergyModal({
          action: "video",
          resume: () => setEnergyAttempt((attempt) => attempt + 1),
        });
      } else {
        setEnergyGate("allowed");
      }
    }).catch(() => {
      if (!cancelled) setEnergyGate("blocked");
    });
    return () => { cancelled = true; };
  }, [consume, data?.youTubeVideoId, energyAttempt, openEnergyModal]);

  return (
    <section ref={viewportRef} className="video-player-page">
      <VideoPlayerHeader energy={energy} />
      {loading || (data && energyGate !== "allowed") ? <ModulePageLoader icon="smart_display" accent="red" label={uz.videoCatalog.opening} />
        : <PlayerView key={id} lessonId={id} initialLesson={data ?? null} failed={Boolean(error) || !data} energy={energy} />}
    </section>
  );
}

function PlayerView({ lessonId, initialLesson, failed, energy }: {
  lessonId: string; initialLesson: VideoLessonDto | null; failed: boolean; energy: EnergyDto | null;
}) {
  const navigate = useNavigate();
  const [lesson, setLesson] = useState(() => initialLesson ? sanitizeVideoTranscript(initialLesson) : null);
  const transcriptStreaming = lesson?.transcriptStatus === TranscriptStatus.Pending || lesson?.transcriptStatus === TranscriptStatus.Partial;
  useTranscriptPolling(lessonId, transcriptStreaming, setLesson);
  const rawVideoId = lesson?.youTubeVideoId ?? lessonId;
  const videoId = /^[A-Za-z0-9_-]{11}$/.test(rawVideoId) ? rawVideoId : "";
  const { currentTime, seekTo, pause } = useYouTubePlayer(videoId, PLAYER_CONTAINER_ID);
  const transcript = useMemo(() => lesson?.transcript ?? [], [lesson]);
  const activeIndex = activeSegmentIndex(transcript, currentTime);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [showTranslation, setShowTranslation] = useState(true);
  const [selectedWord, setSelectedWord] = useState<{ word: string; example: string } | null>(null);
  const [chatOpen, setChatOpen] = useState(false);
  const [aiContextPinned, setAiContextPinned] = useState(false);
  const selected = transcript[selectedIndex] ?? transcript[0];

  useEffect(() => { if (!aiContextPinned && activeIndex >= 0) setSelectedIndex(activeIndex); }, [activeIndex, aiContextPinned]);
  const selectSentence = useCallback((index: number) => {
    const segment = transcript[index];
    if (!segment) return;
    setShowTranslation(shown => index === selectedIndex ? !shown : true);
    setAiContextPinned(false);
    setSelectedIndex(index);
    seekTo(segment.startSeconds);
  }, [seekTo, selectedIndex, transcript]);
  const openWord = useCallback((word: string, example: string, index: number) => {
    pause(); setSelectedIndex(index); setSelectedWord({ word, example });
  }, [pause]);
  const getChatContext = useCallback(() => selected
    ? `[${timestamp(selected.startSeconds)}] ${selected.englishText}` : lesson?.title ?? "", [lesson?.title, selected]);
  const chat = useVideoExplainConversation(lessonId, getChatContext, String(selected?.startSeconds ?? "none"));
  const askAi = useCallback((question?: string) => {
    setSelectedWord(null);
    setChatOpen(true);
    setAiContextPinned(true);
    void chat.send(question ?? `Shu jumlani o‘zbekcha tushuntiring: ma’nosi, ishlatilishi va grammatikasi. ${selected?.englishText ?? ""}`);
  }, [chat, selected?.englishText]);

  useEffect(() => publishAssistantContext({
    area: "video", resourceId: lessonId, title: lesson?.title ?? uz.nav.video,
    context: [lesson ? `Topic: ${lesson.topic}` : "", lesson ? `Level: ${cefrShort(lesson.level)}` : "",
      transcript.length ? `Full transcript:\n${transcript.map(segment => segment.englishText).join(" ")}` : ""].filter(Boolean).join("\n"),
    focusText: selected?.englishText ?? "", route: `/video/${lessonId}/play`,
  }), [lesson, lessonId, selected?.englishText, transcript]);

  const title = lesson?.title ?? uz.nav.video;
  const channel = lesson?.channel || "YouTube";
  const sourceUrl = videoId ? `https://www.youtube.com/watch?v=${videoId}` : undefined;
  const level = lesson ? cefrShort(lesson.level) : "";
  const goToQuiz = () => { pause(); navigate(`/video/${lessonId}/quiz`); };

  return (
    <>
      <div className={cn("video-player-main", chatOpen && "is-ai-open")}>
        <div className="video-player-watch">
          {videoId ? <div className="video-player-stage"><div className="video-player-frame"><div className="video-player-embed"><div id={PLAYER_CONTAINER_ID} /></div></div></div>
            : <p role="status" className="video-player-unavailable">{uz.videoPlayer.videoUnavailable}</p>}
          <div className="video-player-info">
            <h1 className="video-player-title">{title}</h1>
            <div className="video-player-source">
              <p className="video-player-metadata"><span className="video-player-desktop-meta">{[channel, level, lesson?.durationSeconds ? formatDuration(lesson.durationSeconds) : null].filter(Boolean).join("  ·  ")}</span>
                <span className="video-player-mobile-meta">{level}{energy?.outcome === EnergyOutcome.Consumed ? "  ·  −1 energiya  ·  Qayta ko‘rish bepul" : "  ·  Video ochildi"}</span></p>
              {sourceUrl && <a href={sourceUrl} target="_blank" rel="noopener noreferrer" className="video-player-source-link"><span className="video-player-mobile-meta">YouTube ↗</span><span className="video-player-desktop-meta">{uz.videoPlayer.openYouTube} ↗</span></a>}
            </div>
          </div>
        </div>
        <section aria-labelledby="video-practice-captions" className="video-player-captions">
          <div className="video-player-captions__heading">
            <h2 id="video-practice-captions">Transcript</h2>
            <button type="button" onClick={goToQuiz} disabled={!transcript.length} className="video-player-mobile-quiz" aria-label="Quiz"><ListChecks size={18} aria-hidden /></button>
            <button type="button" aria-label={showTranslation ? uz.videoPlayer.hideTranslation : uz.videoPlayer.showTranslation} aria-pressed={showTranslation}
              onClick={() => setShowTranslation(shown => !shown)} className="video-player-translation-toggle" disabled={!transcript.length}><Captions size={21} aria-hidden /></button>
          </div>
          <p className="video-player-captions__hint">Tarjimani ko‘rish uchun jumlani bosing.</p>
          {transcript.length ? <PracticeCaptions transcript={transcript} selectedIndex={selectedIndex} currentTime={currentTime} showTranslation={showTranslation}
            onSelect={selectSentence} onWord={openWord} onAskAi={() => askAi()} contextPinned={aiContextPinned} onFollow={() => setAiContextPinned(false)} />
            : <p role="status" className="video-player-unavailable">{failed || lesson?.transcriptStatus === TranscriptStatus.Unavailable ? uz.videoPlayer.transcriptUnavailable : uz.videoPlayer.transcriptPending}</p>}
          {transcriptStreaming && transcript.length > 0 && <p role="status" className="video-player-streaming">{uz.videoPlayer.transcriptLoadingMore}</p>}
        </section>
        <div className="video-player-actions"><button type="button" onClick={goToQuiz} disabled={!transcript.length} className="video-player-quiz-button">Video testiga o‘tish</button></div>
        <button type="button" className="video-player-ai-toggle" onClick={() => setChatOpen(true)} disabled={!selected}><Sparkles size={18} aria-hidden />AI’dan so‘rash<ArrowRight size={16} aria-hidden /></button>
        <div className="video-player-contextual-ai">
          <ExplainChatPanel conversation={chat} variant="contextual" selectedSentence={selected?.englishText} timestamp={selected ? timestamp(selected.startSeconds) : ""}
            onClose={() => { setChatOpen(false); setAiContextPinned(false); }} onInteract={() => setAiContextPinned(true)} focusInputOnOpen={false} />
        </div>
      </div>
      {selectedWord && <WordDetailSheet word={selectedWord.word} exampleSentence={selectedWord.example}
        translation={lesson?.glossary?.find(entry => entry.word.toLowerCase() === selectedWord.word)?.uzbekMeaning ?? null}
        onAskAi={askAi} onClose={() => setSelectedWord(null)} />}
    </>
  );
}

/** A measured, paged transcript. Only an oversized individual caption may scroll internally. */
const PracticeCaptions = memo(function PracticeCaptions({ transcript, selectedIndex, currentTime, showTranslation, onSelect, onWord, onAskAi, contextPinned, onFollow }: {
  transcript: TranscriptSegmentDto[]; selectedIndex: number; currentTime: number; showTranslation: boolean;
  onSelect: (index: number) => void; onWord: (word: string, example: string, index: number) => void; onAskAi: () => void;
  contextPinned: boolean; onFollow: () => void;
}) {
  const listRef = useRef<HTMLOListElement>(null);
  const [capacity, setCapacity] = useState(() => typeof window !== "undefined" && window.innerWidth <= 700 ? 3 : 4);
  const [manualStart, setManualStart] = useState<number | null>(null);
  const start = Math.min(manualStart ?? Math.max(0, selectedIndex - capacity + 1), Math.max(0, transcript.length - capacity));
  const end = Math.min(transcript.length, start + capacity);
  const following = manualStart === null && !contextPinned;

  useLayoutEffect(() => {
    const list = listRef.current;
    if (!list || typeof ResizeObserver === "undefined") return;
    let lastWidth = 0;
    let lastHeight = 0;
    const observer = new ResizeObserver(() => {
      const { width, height } = list.getBoundingClientRect();
      if (!width || !height) return;
      if (Math.abs(width - lastWidth) > 10 || Math.abs(height - lastHeight) > 35) {
        lastWidth = width; lastHeight = height;
        setCapacity(window.innerWidth <= 700 ? 3 : 4);
      }
    });
    observer.observe(list);
    return () => observer.disconnect();
  }, []);

  useLayoutEffect(() => {
    const list = listRef.current;
    if (!list) return;
    const rows = Array.from(list.children) as HTMLElement[];
    const measure = () => {
      if (!list.clientHeight) return;
      const gap = Number.parseFloat(getComputedStyle(list).rowGap) || 0;
      const needed = rows.reduce((total, row) => total + row.scrollHeight, 0) + Math.max(0, rows.length - 1) * gap;
      if (needed > list.clientHeight + 1 && capacity > 1) setCapacity(size => Math.max(1, size - 1));
    };
    measure();
    if (typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(measure);
    rows.forEach(row => observer.observe(row));
    return () => observer.disconnect();
  }, [capacity, end, selectedIndex, showTranslation, start, transcript]);

  return <>
    <ol ref={listRef} className={cn("video-practice-list", capacity === 1 && "is-single-caption")} aria-label="Video transkripti">
      {transcript.slice(start, end).map((segment, offset) => <PracticeCaption key={`${segment.startSeconds}-${start + offset}`}
        segment={segment} index={start + offset} total={transcript.length} selected={start + offset === selectedIndex}
        activeWord={spokenWordIndex(segment, currentTime)} showTranslation={showTranslation} onSelect={onSelect} onWord={onWord} onAskAi={onAskAi} />)}
    </ol>
    <div className="video-transcript-navigation">
      <button type="button" className="video-transcript-follow" aria-label="Videoga mos kuzatish" aria-pressed={following} onClick={() => { setManualStart(null); onFollow(); }}>
        <Radio size={16} aria-hidden /><span className="video-player-desktop-meta">{following ? "Videoga mos" : "Videoga qaytish"} · {timestamp(transcript[start].startSeconds)}–{timestamp(transcript[end - 1].startSeconds)}</span>
        <span className="video-player-mobile-meta">Jumlani bosing — tarjima ochiladi</span>
      </button>
      <button type="button" aria-label="Oldingi jumlalar" disabled={start === 0} onClick={() => setManualStart(Math.max(0, start - capacity))}><ChevronUp size={17} aria-hidden /></button>
      <button type="button" aria-label="Keyingi jumlalar" disabled={end >= transcript.length} onClick={() => setManualStart(start + capacity)}><ChevronDown size={17} aria-hidden /></button>
    </div>
  </>;
});

const PracticeCaption = memo(function PracticeCaption({ segment, index, total, selected, activeWord, showTranslation, onSelect, onWord, onAskAi }: {
  segment: TranscriptSegmentDto; index: number; total: number; selected: boolean; activeWord: number; showTranslation: boolean;
  onSelect: (index: number) => void; onWord: (word: string, example: string, index: number) => void; onAskAi: () => void;
}) {
  const tokens = useMemo(() => captionTokens(segment), [segment]);
  return <li data-caption-index={index} aria-posinset={index + 1} aria-setsize={total} aria-current={selected ? "true" : undefined}
    onClick={() => onSelect(index)} className={cn("video-practice-line", selected && "is-selected")}>
    <button type="button" className="video-practice-timestamp" aria-label={`${timestamp(segment.startSeconds)} — ${segment.englishText}`}
      aria-expanded={selected && showTranslation} aria-controls={selected ? `caption-translation-${index}` : undefined}>
      {timestamp(segment.startSeconds)}{selected && <span> · TANLANGAN JUMLA</span>}
    </button>
    <p className="video-practice-text">{tokens.map((token, tokenIndex) => token.wordIndex >= 0
      ? <button key={tokenIndex} type="button" onClick={event => {
        event.stopPropagation();
        onSelect(index);
      }} onDoubleClick={event => {
        event.stopPropagation();
        onWord(token.text.toLowerCase().replace(/^[^a-z]+|[^a-z]+$/g, ""), segment.englishText, index);
      }} onKeyDown={event => {
        if (event.altKey && event.key === "Enter") {
          event.preventDefault();
          onWord(token.text.toLowerCase().replace(/^[^a-z]+|[^a-z]+$/g, ""), segment.englishText, index);
        }
      }} title="Tarjima uchun bosing. So‘z ma’nosi: ikki marta bosing yoki Alt+Enter."
        aria-keyshortcuts="Alt+Enter"
        className={cn("video-practice-word", token.wordIndex === activeWord && "is-speaking")} data-speaking={token.wordIndex === activeWord || undefined}>{token.text}</button>
      : <span key={tokenIndex}>{token.text}</span>)}</p>
    {selected && <>
      <div id={`caption-translation-${index}`} hidden={!showTranslation} className="video-practice-translation"><span>O‘ZBEKCHA</span><p>{segment.uzbekTranslation || uz.videoPlayer.translationPending}</p></div>
      <button type="button" className="video-practice-ask" onClick={event => { event.stopPropagation(); onAskAi(); }}><Sparkles size={16} aria-hidden />AI’dan so‘rash<ArrowRight size={14} aria-hidden /></button>
    </>}
  </li>;
});

/** Keep the existing streamed-transcript behavior without delaying the native player. */
function useTranscriptPolling(lessonId: string, streaming: boolean, setLesson: (lesson: VideoLessonDto) => void) {
  useEffect(() => {
    if (!streaming) return;
    let cancelled = false;
    let inFlight = false;
    let idlePolls = 0;
    let lastCount = 0;
    const timer = window.setInterval(async () => {
      if (inFlight) return;
      inFlight = true;
      try {
        const fresh = await api.video.lesson(lessonId);
        if (cancelled) return;
        if (fresh.transcript.length > lastCount) {
          lastCount = fresh.transcript.length;
          idlePolls = 0;
          setLesson(sanitizeVideoTranscript(fresh));
        } else {
          idlePolls += 1;
        }
        if (fresh.transcriptStatus === TranscriptStatus.Available || fresh.transcriptStatus === TranscriptStatus.Unavailable) {
          setLesson(sanitizeVideoTranscript(fresh));
          window.clearInterval(timer);
        }
      } catch {
        idlePolls += 1;
      } finally {
        inFlight = false;
      }
      if (idlePolls >= 24) window.clearInterval(timer);
    }, 2500);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [lessonId, streaming, setLesson]);
}
