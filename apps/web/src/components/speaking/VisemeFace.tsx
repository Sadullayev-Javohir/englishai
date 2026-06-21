import { useEffect, useRef, useState } from "react";

export interface VisemeFrameLike {
  visemeId: number;
  offsetMs: number;
}

/** Azure defines exactly 22 visemes (0 = silence .. 21), each with one official reference
 * photo (see `public/assets/visemes/SOURCE.md`). Every English word maps onto this same
 * fixed set, so these 22 images are all a word's mouth animation will ever need. */
const VISEME_COUNT = 22;
const SILENCE_ID = 0;

function visemeSrc(id: number): string {
  const clamped = Math.min(Math.max(Math.round(id), 0), VISEME_COUNT - 1);
  return `/assets/visemes/viseme-${clamped}.jpg`;
}

let prefetched = false;
/** Warms the browser cache for all 22 shapes once per session, so the very first playback of
 * any word never stalls on a cold image fetch mid-animation. */
function prefetchVisemeImages() {
  if (prefetched || typeof window === "undefined") return;
  prefetched = true;
  for (let id = 0; id < VISEME_COUNT; id++) {
    const img = new window.Image();
    img.src = visemeSrc(id);
  }
}

interface Pose {
  id: number;
  nextId: number;
  blend: number;
}

const NEUTRAL_POSE: Pose = { id: SILENCE_ID, nextId: SILENCE_ID, blend: 0 };

/** Finds the shape at `ms` on the (already offset-sorted) frame track, plus how far into the
 * transition to the following shape we are - used to cross-dissolve between the two reference
 * photos instead of hard-cutting between them. */
function poseAt(frames: VisemeFrameLike[], ms: number): Pose {
  const first = frames[0];
  if (ms <= first.offsetMs) return { id: first.visemeId, nextId: first.visemeId, blend: 0 };

  const last = frames[frames.length - 1];
  if (ms >= last.offsetMs) return { id: last.visemeId, nextId: last.visemeId, blend: 0 };

  for (let i = 0; i < frames.length - 1; i++) {
    const a = frames[i];
    const b = frames[i + 1];
    if (ms >= a.offsetMs && ms < b.offsetMs) {
      const span = b.offsetMs - a.offsetMs || 1;
      return { id: a.visemeId, nextId: b.visemeId, blend: (ms - a.offsetMs) / span };
    }
  }
  return { id: last.visemeId, nextId: last.visemeId, blend: 0 };
}

interface VisemeFaceProps {
  /** Azure viseme id/offset track for the current word; empty/null renders the neutral pose. */
  frames?: VisemeFrameLike[] | null;
  /** Changes on every new utterance to (re)start playback; falsy = idle/neutral pose. */
  playKey?: string | number | null;
  /** Playback speed: 1 = native; < 1 plays the mouth slower (for "speak slowly"), matching the
   * rate the caller applied to the reference clip. */
  rate?: number;
  size?: number;
}

/**
 * Animated mouth built from Azure's own 22 official viseme reference photos (Microsoft Learn,
 * "Get facial position with viseme" - see `public/assets/visemes/SOURCE.md`), driven by the
 * per-word viseme id/offset track the backend already produces for any English word (curated
 * list + CMU dictionary fallback). Cross-dissolves between the current and next shape so the
 * mouth reads as continuous motion rather than a slideshow.
 *
 * Timing is a plain `performance.now()` clock, deliberately NOT tied to the `<audio>` element's
 * own `currentTime`: that seemed like a better sync source, but a stalled/erroring clip (e.g.
 * the character-code-based fallback used when no real TTS voice is configured) can leave
 * `currentTime` frozen while `paused`/`ended` stay false, which made the loop read the same
 * timestamp forever - the mouth got stuck open with no way out. A wall clock always advances,
 * so `ms` always eventually crosses `lastOffset` and the loop always finishes on its own.
 */
export function VisemeFace({ frames, playKey, rate = 1, size = 96 }: VisemeFaceProps) {
  const framesRef = useRef<VisemeFrameLike[]>([]);
  const [pose, setPose] = useState<Pose>(NEUTRAL_POSE);

  useEffect(() => {
    prefetchVisemeImages();
  }, []);

  useEffect(() => {
    framesRef.current = frames && frames.length > 0 ? [...frames].sort((a, b) => a.offsetMs - b.offsetMs) : [];
  }, [frames]);

  useEffect(() => {
    const track = framesRef.current;
    if (!playKey || track.length === 0) {
      setPose(NEUTRAL_POSE);
      return;
    }

    const start = performance.now();
    const lastOffset = track[track.length - 1].offsetMs;
    let raf = 0;

    const tick = () => {
      const ms = (performance.now() - start) * (rate || 1);
      if (ms > lastOffset + 250) {
        setPose(NEUTRAL_POSE);
        return; // done - loop stops itself, nothing left running to get stuck
      }
      setPose(poseAt(track, ms));
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [playKey, rate]);

  const showBlend = pose.blend > 0.03 && pose.nextId !== pose.id;

  return (
    <div
      role="img"
      aria-label="Tovush talaffuzida og'iz harakati"
      style={{ width: size, height: size }}
      className="relative overflow-hidden rounded-2xl bg-surface-container-low"
    >
      <img
        src={visemeSrc(pose.id)}
        alt=""
        draggable={false}
        className="absolute inset-0 h-full w-full object-cover"
      />
      {showBlend && (
        <img
          src={visemeSrc(pose.nextId)}
          alt=""
          draggable={false}
          className="absolute inset-0 h-full w-full object-cover"
          style={{ opacity: pose.blend }}
        />
      )}
    </div>
  );
}
