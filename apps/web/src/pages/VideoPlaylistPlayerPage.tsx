import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { ArrowLeft, ArrowRight, ChevronDown, ListVideo, Sparkles } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { api } from "@/api/client";
import { EnergyOutcome, type VideoPlaylistDto } from "@/api/types";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import { activeSegmentIndex, useYouTubePlayer } from "@/lib/useYouTubePlayer";
import { cefrShort, formatDuration } from "@/lib/labels";
import { VideoPlayerHeader } from "@/components/video/VideoPlayerHeader";
import { useEnergy } from "@/components/game/EnergyProvider";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { useVideoExplainConversation } from "@/components/video/useVideoExplainConversation";
import { ExplainChatPanel } from "@/components/video/ExplainChatPanel";
import { PlaylistTranscript } from "@/components/video/PlaylistTranscript";
import { usePlaylistTranscript } from "@/components/video/usePlaylistTranscript";
import { formatLongDuration } from "./VideoFullSearchPage";
import "./VideoPlaylistPlayerPage.css";

const PLAYER_CONTAINER_ID = "video-playlist-native-player";
const timestamp = (seconds: number) => formatDuration(seconds).padStart(5, "0");

/** Pen 72 with a playlist below the reading pane; on phones the queue is a collapsible panel. */
export function VideoPlaylistPlayerPage() {
  const { playlistId = "", episodeNumber = "" } = useParams();
  const viewportRef = useRef<HTMLElement>(null);
  const { data: playlist, loading, error } = useAsync(() => api.video.playlist(playlistId), [playlistId], Boolean(playlistId));
  const requestedIndex = Math.max(0, Number.parseInt(episodeNumber, 10) - 1 || 0);
  const index = playlist ? Math.min(requestedIndex, Math.max(0, playlist.items.length - 1)) : 0;
  const item = playlist?.items[index];
  useDocumentTitle(item?.title, "Playlist");

  useLayoutEffect(() => {
    const viewport = window.visualViewport;
    const resize = () => {
      viewportRef.current?.style.setProperty("--playlist-viewport", `${viewport?.height ?? window.innerHeight}px`);
      if (viewportRef.current) viewportRef.current.dataset.keyboard = String(window.innerHeight - (viewport?.height ?? window.innerHeight) > 150);
    };
    resize();
    viewport?.addEventListener("resize", resize);
    window.addEventListener("resize", resize);
    return () => {
      viewport?.removeEventListener("resize", resize);
      window.removeEventListener("resize", resize);
    };
  }, []);

  return <section className="video-playlist-player" ref={viewportRef}>
    {loading ? <><VideoPlayerHeader energy={null} /><ModulePageLoader icon="playlist_play" accent="purple" /></>
      : playlist && item ? <PlaylistEpisode key={`${playlist.id}:${item.youTubeVideoId}`} playlist={playlist} index={index} />
        : <><VideoPlayerHeader energy={null} /><main className="video-playlist-player__missing">
          <h1>Playlist qismi topilmadi</h1><p>{error ? "Playlistni yuklab bo‘lmadi." : "Bu qism endi mavjud emas."}</p>
          <Link to="/video">Video katalogiga qaytish</Link>
        </main></>}
  </section>;
}

function PlaylistEpisode({ playlist, index }: { playlist: VideoPlaylistDto; index: number }) {
  const navigate = useNavigate();
  const item = playlist.items[index];
  const { energy, consume, openEnergyModal } = useEnergy();
  const [energyGate, setEnergyGate] = useState<"checking" | "allowed" | "blocked">("checking");
  const [energyAttempt, setEnergyAttempt] = useState(0);
  const { data: opened, error: openError, reload: retryOpen } = useAsync(
    () => api.video.open({ ...item, lessonId: null, topic: "full-playlist", level: 2, hasClosedCaptions: false }),
    [item.youTubeVideoId, energyGate],
    energyGate === "allowed",
  );
  const { lesson: streamedLesson, state: transcriptState, retry: retryTranscript } = usePlaylistTranscript(opened);
  const { currentTime, endedVideoId, isPlaying, play, seekTo } = useYouTubePlayer(
    energyGate === "allowed" ? item.youTubeVideoId : "", PLAYER_CONTAINER_ID);
  const transcript = streamedLesson?.transcript ?? [];
  const activeIndex = activeSegmentIndex(transcript, currentTime);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [showTranslation, setShowTranslation] = useState(true);
  const [chatOpen, setChatOpen] = useState(false);
  const [queueOpen, setQueueOpen] = useState(false);
  const selected = transcript[selectedIndex] ?? transcript[0];
  // AI context stays on the requested sentence even if native playback advances behind it.
  const [aiIndex, setAiIndex] = useState<number | null>(null);
  const aiSentence = transcript[aiIndex ?? selectedIndex] ?? selected;
  const getContext = useCallback(() => aiSentence?.englishText ?? item.title, [aiSentence?.englishText, item.title]);
  const chat = useVideoExplainConversation(opened?.id ?? "", getContext, String(aiSentence?.startSeconds ?? "none"));
  const [pendingQuestion, setPendingQuestion] = useState<{ index: number; text: string } | null>(null);
  const autoAdvancedRef = useRef(false);
  const queueRef = useRef<HTMLDivElement>(null);
  const activeQueueRef = useRef<HTMLButtonElement>(null);
  const episodePath = useCallback((next: number) => `/video/playlists/${encodeURIComponent(playlist.id)}/${String(next + 1).padStart(2, "0")}`, [playlist.id]);
  const previousIndex = index > 0 ? index - 1 : null;
  const nextIndex = index + 1 < playlist.items.length ? index + 1 : null;
  const autoPlayStorageKey = `englishai.video-playlist.autoplay.${playlist.id}`;
  // Retain the intent through StrictMode's effect replay; the player hook clears its pending
  // command during cleanup, even after the first effect has consumed the storage marker.
  const shouldAutoPlay = useRef(window.sessionStorage.getItem(autoPlayStorageKey) === item.youTubeVideoId);

  useEffect(() => {
    let cancelled = false;
    setEnergyGate("checking");
    void consume("video", item.youTubeVideoId).then((result) => {
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
    }).catch(() => { if (!cancelled) setEnergyGate("blocked"); });
    return () => { cancelled = true; };
  }, [consume, energyAttempt, item.youTubeVideoId, openEnergyModal]);

  useEffect(() => { if (activeIndex >= 0) setSelectedIndex(activeIndex); }, [activeIndex]);
  useEffect(() => {
    if (!shouldAutoPlay.current) return;
    window.sessionStorage.removeItem(autoPlayStorageKey);
    play();
  }, [autoPlayStorageKey, item.youTubeVideoId, play]);
  useEffect(() => {
    if (endedVideoId !== item.youTubeVideoId || nextIndex === null || autoAdvancedRef.current) return;
    autoAdvancedRef.current = true;
    window.sessionStorage.setItem(autoPlayStorageKey, playlist.items[nextIndex].youTubeVideoId);
    navigate(episodePath(nextIndex));
  }, [autoPlayStorageKey, endedVideoId, episodePath, item.youTubeVideoId, navigate, nextIndex, playlist.items]);
  useLayoutEffect(() => {
    const queue = queueRef.current;
    const active = activeQueueRef.current;
    if (!queue || !active || !queue.clientHeight) return;
    const top = active.getBoundingClientRect().top - queue.getBoundingClientRect().top + queue.scrollTop;
    queue.scrollTop = Math.max(0, top - (queue.clientHeight - active.offsetHeight) / 2);
  }, [queueOpen]);
  useEffect(() => {
    if (!pendingQuestion || aiIndex !== pendingQuestion.index || !opened) return;
    void chat.send(pendingQuestion.text);
    setPendingQuestion(null);
  }, [aiIndex, chat, opened, pendingQuestion]);

  function selectSegment(segmentIndex: number) {
    const segment = transcript[segmentIndex];
    if (!segment) return;
    setShowTranslation(shown => segmentIndex === selectedIndex ? !shown : true);
    setSelectedIndex(segmentIndex);
    seekTo(segment.startSeconds);
  }
  function askAboutSegment(segmentIndex: number) {
    const segment = transcript[segmentIndex];
    if (!segment) return;
    setAiIndex(segmentIndex);
    setChatOpen(true);
    setQueueOpen(false);
    setPendingQuestion({ index: segmentIndex, text: segment.englishText });
  }
  function toggleQueue() { setQueueOpen(open => !open); setChatOpen(false); }

  if (energyGate !== "allowed") {
    return <><VideoPlayerHeader energy={energy} /><ModulePageLoader icon="smart_display" accent="purple" label="Energiya tekshirilmoqda" /></>;
  }

  return <>
    <VideoPlayerHeader energy={energy} />
    <main className="video-playlist-player__content">
      <div className={`video-playlist-player__layout${queueOpen ? " is-queue-open" : ""}${chatOpen ? " is-ai-open" : ""}`}>
        <div className="video-playlist-player__learning">
          <section className="video-playlist-player__captions" aria-labelledby="playlist-caption-title">
            <div className="video-playlist-player__caption-heading"><h2 id="playlist-caption-title">Transcript</h2></div>
            <p className="video-playlist-player__hint">Tarjimani ko‘rish uchun jumlani bosing.</p>
            <PlaylistTranscript transcript={transcript} selectedIndex={selectedIndex} currentTime={currentTime} showTranslation={showTranslation}
              unavailable={transcriptState === "unavailable"}
              loadState={openError ? "error" : transcriptState}
              onRetry={openError ? retryOpen : retryTranscript}
              aiSending={chat.sending} onSelect={selectSegment} onAskAi={askAboutSegment} onToggleTranslation={() => setShowTranslation(shown => !shown)} />
          </section>
          <div className="video-playlist-player__lesson-actions">
            <button type="button" disabled={!opened || !transcript.length} onClick={() => opened && navigate(`/video/${opened.id}/quiz`)}>Video testiga o‘tish</button>
          </div>
          <section className="video-playlist-player__queue" aria-labelledby="playlist-queue-title">
            <div className="video-playlist-player__queue-heading">
              <h2 id="playlist-queue-title"><span className="video-playlist-player__queue-desktop-title"><ListVideo size={19} aria-hidden />Playlist</span>
                <button type="button" className="video-playlist-player__queue-toggle" onClick={toggleQueue} aria-expanded={queueOpen} aria-controls="playlist-episode-queue"><ListVideo size={18} aria-hidden />Playlist <small>{index + 1} / {playlist.items.length}</small><ChevronDown size={16} aria-hidden /></button>
              </h2>
              <Link className="video-playlist-player__all-episodes" to={`/video/playlists/${encodeURIComponent(playlist.id)}`}>Barchasi</Link>
              <div className="video-playlist-player__queue-mobile-nav">
                <button type="button" aria-label="Oldingi qism" disabled={previousIndex === null} onClick={() => previousIndex !== null && navigate(episodePath(previousIndex))}><ArrowLeft size={17} aria-hidden /></button>
                <button type="button" aria-label="Keyingi qism" disabled={nextIndex === null} onClick={() => nextIndex !== null && navigate(episodePath(nextIndex))}><ArrowRight size={17} aria-hidden /></button>
              </div>
            </div>
            <p className="video-playlist-player__queue-description" title={playlist.title}>{playlist.title} · {playlist.items.length} qism · {formatLongDuration(playlist.totalDurationSeconds)}</p>
            <div ref={queueRef} id="playlist-episode-queue" className="video-playlist-player__queue-list">
              {playlist.items.map((queueItem, queueIndex) => <button key={queueItem.youTubeVideoId} ref={queueIndex === index ? activeQueueRef : undefined}
                type="button" className={queueIndex === index ? "is-active" : ""} aria-label={queueItem.title} aria-current={queueIndex === index ? "true" : undefined}
                onClick={() => { if (queueIndex === index) setQueueOpen(false); else navigate(episodePath(queueIndex)); }}>
                <small>{String(queueIndex + 1).padStart(2, "0")}</small><span><strong>{queueItem.title}</strong>
                  <em className={queueIndex === index && isPlaying ? "is-playing" : undefined}>{queueIndex === index && <i aria-hidden />}<span>{queueIndex === index ? "Hozir ijro etilmoqda" : "To‘liq epizod"}</span> · {formatDuration(queueItem.durationSeconds)}</em>
                </span><ArrowRight size={16} aria-hidden />
              </button>)}
            </div>
          </section>
        </div>
        <div className="video-playlist-player__media">
          <div className="video-playlist-player__stage"><div className="video-playlist-player__frame"><div id={PLAYER_CONTAINER_ID} /></div></div>
          <div className="video-playlist-player__episode-heading">
            <h1>{String(index + 1).padStart(2, "0")} · {item.title}</h1>
            <div><span>{item.channel} · {streamedLesson ? cefrShort(streamedLesson.level) : "Inglizcha"} · {formatDuration(item.durationSeconds)}</span>
              <a href={`https://www.youtube.com/watch?v=${item.youTubeVideoId}`} target="_blank" rel="noopener noreferrer">YouTube ↗</a></div>
          </div>
          <nav className="video-playlist-player__episode-nav" aria-label="Qismlar orasida navigatsiya">
            <button type="button" disabled={previousIndex === null} onClick={() => previousIndex !== null && navigate(episodePath(previousIndex))}><ArrowLeft size={17} aria-hidden /> Oldingi</button>
            <span>{String(index + 1).padStart(2, "0")} / {playlist.items.length}</span>
            <button type="button" disabled={nextIndex === null} onClick={() => nextIndex !== null && navigate(episodePath(nextIndex))}>Keyingi <ArrowRight size={17} aria-hidden /></button>
          </nav>
          <button type="button" className="video-playlist-player__ai-toggle" disabled={!selected} onClick={() => { setChatOpen(true); setQueueOpen(false); }}><Sparkles size={18} aria-hidden />AI’dan so‘rash<ArrowRight size={16} aria-hidden /></button>
          <div className="video-playlist-player__ai">
            <ExplainChatPanel conversation={chat} variant="contextual" selectedSentence={aiSentence?.englishText} timestamp={aiSentence ? timestamp(aiSentence.startSeconds) : ""}
              onClose={() => setChatOpen(false)} focusInputOnOpen={false} />
          </div>
        </div>
      </div>
    </main>
  </>;
}
