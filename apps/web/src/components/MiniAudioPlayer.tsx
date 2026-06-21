import { useRef, useState } from "react";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";

/**
 * A compact, self-contained audio player meant to be embedded inside each listening quiz
 * question so the learner can re-hear the clip without scrolling back up to the big player
 * (PROJECT-SPEC Faza 4: the comprehension check is listening-first, but a re-listen affordance
 * per question keeps the clip close to the question). It plays the same cached, server-synthesized
 * clip as the main player; mount it with a `key` (e.g. the question id) so each instance is isolated.
 */
export function MiniAudioPlayer({
  audioUrl,
  label,
  className,
}: {
  /** Absolute URL of the synthesized clip (api.listening.audioUrl(topicId)). */
  audioUrl: string;
  /** Accessible label / tooltip for the play button. */
  label: string;
  className?: string;
}) {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [error, setError] = useState(false);

  function toggle() {
    const el = audioRef.current;
    if (!el) return;
    if (el.paused) {
      // Always restart from the top so a re-listen replays the whole clip, not a leftover spot.
      el.currentTime = 0;
      el.play().catch(() => setError(true));
    } else {
      el.pause();
    }
  }

  const pct = duration > 0 ? Math.min(100, (currentTime / duration) * 100) : 0;

  return (
    <div
      className={cn(
        "flex items-center gap-sm bg-surface-container-high rounded-full pl-xs pr-sm py-xs",
        className,
      )}
    >
      <button
        type="button"
        onClick={toggle}
        aria-label={label}
        className={cn(
          "w-10 h-10 shrink-0 rounded-full flex items-center justify-center transition-transform",
          error
            ? "bg-error/10 text-error"
            : "bg-primary-container text-on-primary-container hover:scale-105",
        )}
      >
        <Icon
          name={isPlaying ? "pause" : error ? "error" : "play_arrow"}
          filled={!error}
          className="text-[22px]"
        />
      </button>
      <div className="flex-1 min-w-[64px] h-1.5 rounded-full bg-border overflow-hidden">
        <div
          className="h-full bg-primary-container rounded-full transition-[width] duration-150"
          style={{ width: `${pct}%` }}
        />
      </div>
      <audio
        ref={audioRef}
        src={audioUrl}
        preload="none"
        onError={() => setError(true)}
        onLoadedMetadata={(e) => setDuration(e.currentTarget.duration || 0)}
        onTimeUpdate={(e) => setCurrentTime(e.currentTarget.currentTime)}
        onPlay={() => setIsPlaying(true)}
        onPause={() => setIsPlaying(false)}
        onEnded={() => {
          setIsPlaying(false);
          setCurrentTime(0);
        }}
      />
    </div>
  );
}
