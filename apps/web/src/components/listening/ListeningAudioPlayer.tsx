import { useEffect, useRef, useState } from "react";
import { Pause, Play, RotateCcw } from "lucide-react";
import { api } from "@/api/client";
import { uz } from "@/content/uz";

const BARS = [18, 32, 24, 40, 28, 48, 32, 22, 38, 28, 46, 32, 20, 42, 30, 24, 38, 48, 28, 20, 36, 26, 44, 30];
function timestamp(seconds: number) {
  const value = Number.isFinite(seconds) ? Math.max(0, Math.floor(seconds)) : 0;
  return `${Math.floor(value / 60)}:${String(value % 60).padStart(2, "0")}`;
}

/** One native audio element per stage; leaving the stage stops playback. */
export function ListeningAudioPlayer({ topicId, title, onListened, onPlaybackError }: {
  topicId: string; title: string; onListened?: () => void; onPlaybackError?: () => void;
}) {
  const audioRef = useRef<HTMLAudioElement>(null);
  const [status, setStatus] = useState<"loading" | "ready" | "playing" | "paused" | "ended" | "error">("loading");
  const [position, setPosition] = useState(0);
  const [duration, setDuration] = useState(0);
  const [speed, setSpeed] = useState(1);
  useEffect(() => {
    const audio = audioRef.current;
    return () => { audio?.pause(); };
  }, []);
  async function play(restart = false) {
    const audio = audioRef.current;
    if (!audio) return;
    if (restart || status === "ended") { audio.currentTime = 0; setPosition(0); }
    if (status === "error") audio.load();
    try { await audio.play(); } catch { setStatus("error"); onPlaybackError?.(); }
  }
  const label = status === "playing" ? uz.listening.pause : status === "paused" ? uz.listening.resume : uz.listening.play;
  return (
    <div className="listening-player" data-testid="listening-audio-controls">
      <audio ref={audioRef} src={api.listening.audioUrl(topicId)} preload="auto" aria-label={`${title} — ${uz.listeningTopics.audioTitle}`}
        onLoadStart={() => setStatus("loading")}
        onCanPlay={() => setStatus(value => value === "playing" ? value : "ready")}
        onLoadedMetadata={event => setDuration(Number.isFinite(event.currentTarget.duration) ? event.currentTarget.duration : 0)}
        onTimeUpdate={event => setPosition(event.currentTarget.currentTime)}
        onPlay={() => { setStatus("playing"); onListened?.(); }}
        onPause={() => setStatus(value => value === "ended" ? value : "paused")}
        onEnded={() => { setStatus("ended"); setPosition(duration); }}
        onError={() => { setStatus("error"); onPlaybackError?.(); }}
      />
      <div className="listening-player__transport">
        <button type="button" className="listening-player__play" aria-label={label} disabled={status === "loading" || status === "error"}
          onClick={() => { if (status === "playing") audioRef.current?.pause(); else void play(); }}>
          {status === "playing" ? <Pause size={22} /> : <Play size={22} />}
        </button>
        <div className="listening-player__seek">
          <div className="listening-player__wave" aria-hidden="true">
            {BARS.map((height, index) => <span key={index} style={{ height: `${height}px` }} className={index / BARS.length <= position / (duration || 1) ? "is-played" : ""} />)}
          </div>
          <input type="range" min={0} max={duration || 1} step={0.1} value={position} disabled={!duration || status === "error"} aria-label={uz.listening.seekLabel}
            aria-valuetext={`${timestamp(position)} / ${timestamp(duration)}`}
            onChange={event => { const time = Number(event.target.value); if (audioRef.current) audioRef.current.currentTime = time; setPosition(time); }} />
        </div>
        <button type="button" className="listening-player__speed" aria-label={`Tinglash tezligi ${speed}x`} onClick={() => {
          const next = speed === 1 ? 0.75 : speed === 0.75 ? 1.25 : 1;
          setSpeed(next); if (audioRef.current) audioRef.current.playbackRate = next;
        }}>{speed}×</button>
      </div>
      <div className="listening-player__meta">
        <span>{timestamp(position)} / {timestamp(duration)}</span>
        <button type="button" disabled={status === "loading"} onClick={() => void play(true)}><RotateCcw size={13} />{uz.listening.replay}</button>
      </div>
      {status === "error" && <p role="alert" className="listening-player__error">{uz.listening.audioError}</p>}
    </div>
  );
}
