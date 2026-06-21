import { useEffect, useRef, useState } from "react";
import { Headphones, LoaderCircle, Mic, Pause, Play, RotateCcw } from "lucide-react";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import "./PlacementAudioPlayer.css";

type PlayerState = "idle" | "loading" | "playing" | "paused" | "ended" | "error";

/** Listening and recorded-answer playback intentionally use the same transport. */
export function AssessmentAudioPlayer({
  src, recording = false, disabled = false, durationHint = 0,
}: { src: string; recording?: boolean; disabled?: boolean; durationHint?: number }) {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [state, setState] = useState<PlayerState>("idle");
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);

  useEffect(() => {
    const audio = audioRef.current;
    return () => { audio?.pause(); };
  }, [src]);
  useEffect(() => {
    if (disabled) audioRef.current?.pause();
  }, [disabled]);

  async function togglePlayback() {
    const audio = audioRef.current;
    if (!audio || disabled || state === "loading") return;
    if (!audio.paused) { audio.pause(); return; }
    if (audio.ended) audio.currentTime = 0;
    if (state === "error") audio.load();
    setState("loading");
    try { await audio.play(); } catch { setState("error"); }
  }

  const label = state === "loading" ? uz.placement.listen.loading
    : state === "playing" ? uz.placement.listen.pause
      : state === "error" ? "Qayta urinish"
        : state === "paused" ? "Tinglashni davom ettirish"
          : state === "ended" ? uz.placement.listen.replay
            : recording ? "Yozuvni tinglash" : uz.placement.listen.play;
  const Control = state === "loading" ? LoaderCircle
    : state === "playing" ? Pause
      : state === "ended" || state === "error" ? RotateCcw : Play;
  const Kind = recording ? Mic : Headphones;
  // Some MediaRecorder WebM blobs report Infinity until playback ends. The recorder
  // can supply its measured duration; server listening clips never invent one.
  const safeDuration = Number.isFinite(duration) && duration > 0 ? duration
    : Number.isFinite(durationHint) && durationHint > 0 ? durationHint : 0;
  const safeTime = Math.max(0, Math.min(currentTime, safeDuration || currentTime));

  return (
    <section className="placement-audio" aria-label={recording ? "Ovozli javobingiz" : "Tinglash audiosi"}>
      <audio
        ref={audioRef} src={src} preload={recording ? "metadata" : "none"}
        onLoadedMetadata={event => setDuration(event.currentTarget.duration)}
        onDurationChange={event => setDuration(event.currentTarget.duration)}
        onTimeUpdate={event => setCurrentTime(event.currentTarget.currentTime)}
        onPlaying={() => setState("playing")}
        onPause={event => { if (!event.currentTarget.ended) setState("paused"); }}
        onEnded={() => setState("ended")}
        onError={() => setState("error")}
      />
      <div className="placement-audio__heading">
        <Kind size={19} aria-hidden /><h2>{recording ? "Ovozli javobingiz" : "Audioni tinglang"}</h2>
        <span>{recording ? "Yuborishdan oldin tekshiring" : "Tinglang va javob bering"}</span>
      </div>
      <div className="placement-audio__transport">
        <button type="button" className="placement-audio__control" onClick={() => void togglePlayback()} disabled={disabled || state === "loading"} aria-label={label}>
          <Control size={24} aria-hidden className={state === "loading" ? "placement-audio__spinner" : undefined} />
        </button>
        <div className="placement-audio__timeline">
          <progress max={safeDuration || 1} value={safeTime} aria-label={recording ? "Yozuv ijrosi" : "Audio ijrosi"} />
          <div><span>{formatAudioTime(safeTime)}</span><span>{safeDuration ? formatAudioTime(safeDuration) : "—:—"}</span></div>
        </div>
      </div>
      <p className="placement-audio__caption" role={state === "error" ? "alert" : "status"}>
        {state === "error" ? uz.placement.listen.error : state === "playing" ? "Ijro etilmoqda…" : label}
      </p>
    </section>
  );
}

export function formatAudioTime(value: number) {
  if (!Number.isFinite(value) || value < 0) return "0:00";
  return `${Math.floor(value / 60)}:${Math.floor(value % 60).toString().padStart(2, "0")}`;
}

/** No transcript: listening must be assessed by ear, not by reading the script. */
export function PlacementAudioPlayer({ questionId, disabled = false }: { questionId: string; disabled?: boolean }) {
  return <AssessmentAudioPlayer key={questionId} src={api.placement.audioUrl(questionId)} disabled={disabled} />;
}
