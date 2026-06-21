import { useCallback, useEffect, useRef, useState } from "react";

/**
 * Minimal typing for the bits of the YouTube IFrame Player API we use (current time + seek).
 * The API replaces a target <div> with an <iframe> and drives playback from JS, which is what
 * lets the interactive transcript follow and scrub the video (PROJECT-SPEC B.3, Bosqich 3).
 */
interface YTPlayer {
  getCurrentTime?: () => number;
  getDuration?: () => number;
  getPlayerState?: () => number;
  getIframe?: () => HTMLIFrameElement;
  playVideo?: () => void;
  pauseVideo?: () => void;
  seekTo?: (seconds: number, allowSeekAhead: boolean) => void;
  destroy?: () => void;
  setVolume?: (volume: number) => void;
  getVolume?: () => number;
  mute?: () => void;
  unMute?: () => void;
  isMuted?: () => boolean;
  getPlaybackRate?: () => number;
  setPlaybackRate?: (suggestedRate: number) => void;
}

/** YT.PlayerState.PLAYING - the player is actively playing (vs. paused/buffering/ended). */
const YT_STATE_PLAYING = 1;
/** YT.PlayerState.ENDED - playback reached the end (YouTube shows its own "up next" grid then). */
const YT_STATE_ENDED = 0;

interface YTPlayerOptions {
  width?: number | string;
  height?: number | string;
  videoId: string;
  playerVars?: Record<string, number | string>;
  events?: {
    onReady?: (event: { target: YTPlayer }) => void;
    onStateChange?: (event: { target: YTPlayer; data: number }) => void;
  };
}

/**
 * Some Android WebViews keep YouTube's default 640px iframe compositing surface even when the
 * surrounding responsive card is narrower - the video image then paints past the right edge of
 * the card and covers the overlay controls sitting there. A percentage in the iframe's HTML width
 * attribute is not handled consistently by those WebViews, so write measured pixel dimensions
 * into the attributes while retaining responsive CSS sizing.
 *
 * Exported for unit testing: the one thing this must never do is fall back to `"100%"`, which is
 * exactly the state that re-arms the 640px surface.
 */
export function fitIframeToContainer(player: YTPlayer, container: HTMLElement | null) {
  const iframe = player.getIframe?.();
  if (!iframe) return;
  iframe.title ||= "YouTube video pleyeri";
  iframe.allowFullscreen = true;

  // Measure the responsive wrapper first, then the iframe itself, then the viewport - anything
  // rather than handing the WebView a percentage.
  const containerBounds = container?.getBoundingClientRect();
  const iframeBounds = iframe.getBoundingClientRect?.();
  const measuredWidth = Math.round(
    container?.clientWidth
    || containerBounds?.width
    || iframeBounds?.width
    || document.documentElement.clientWidth
    || 0,
  );
  const measuredHeight = Math.round(
    container?.clientHeight
    || containerBounds?.height
    || iframeBounds?.height
    // A 16:9 estimate still beats a percentage the WebView will ignore.
    || measuredWidth * 9 / 16,
  );

  if (measuredWidth > 0) iframe.width = String(measuredWidth);
  if (measuredHeight > 0) iframe.height = String(measuredHeight);
  Object.assign(iframe.style, {
    position: "absolute",
    inset: "0",
    display: "block",
    boxSizing: "border-box",
    width: "100%",
    maxWidth: "100%",
    height: "100%",
    maxHeight: "100%",
    border: "0",
  });
}

interface YTNamespace {
  Player: new (target: string | HTMLElement, options: YTPlayerOptions) => YTPlayer;
}

declare global {
  interface Window {
    YT?: YTNamespace;
    onYouTubeIframeAPIReady?: () => void;
  }
}

let apiPromise: Promise<void> | null = null;

function warmYouTubeConnections() {
  const origins = ["https://www.youtube.com", "https://www.google.com", "https://i.ytimg.com"];
  for (const origin of origins) {
    if (document.head.querySelector(`link[rel="preconnect"][href="${origin}"]`)) continue;
    const link = document.createElement("link");
    link.rel = "preconnect";
    link.href = origin;
    link.crossOrigin = "anonymous";
    document.head.appendChild(link);
  }
}

/** Loads the IFrame API <script> once and resolves when the global `YT` is ready. */
function loadIframeApi(): Promise<void> {
  if (window.YT?.Player) return Promise.resolve();
  if (apiPromise) return apiPromise;

  warmYouTubeConnections();
  apiPromise = new Promise<void>((resolve) => {
    const previous = window.onYouTubeIframeAPIReady;
    window.onYouTubeIframeAPIReady = () => {
      previous?.();
      resolve();
    };
    const script = document.createElement("script");
    script.src = "https://www.youtube.com/iframe_api";
    script.async = true;
    document.head.appendChild(script);
  });
  return apiPromise;
}

/**
 * Mounts a YouTube player into the element with id <paramref name="containerId"/> and tracks
 * its current playback time, exposing a `seekTo` for click-to-jump. Polls because the IFrame
 * API has no time-update event.
 */
export function useYouTubePlayer(videoId: string, containerId: string) {
  const [currentTime, setCurrentTime] = useState(0);
  // Total length and whether playback is live - surfaced so an external control bar (the full-size
  // transcript) can show the position/duration and offer play/pause without its own polling.
  const [duration, setDuration] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  // Once true, playback has started at least once - used to switch from the "not started yet"
  // thumbnail poster to the live video frame (and to keep showing that frame, not a poster, once
  // the user pauses mid-video).
  const [hasStarted, setHasStarted] = useState(false);
  const [ended, setEnded] = useState(false);
  // The `ended` flag can still be true for one render while a parent swaps to a different
  // video id. Keep the id that emitted the native event so playlist pages never skip an item.
  const [endedVideoId, setEndedVideoId] = useState<string | null>(null);
  const [volume, setVolumeState] = useState(100);
  const [muted, setMutedState] = useState(false);
  const [playbackRate, setPlaybackRateState] = useState(1);
  const playerRef = useRef<YTPlayer | null>(null);
  const readyRef = useRef(false);
  const pendingPlaybackRef = useRef<"play" | "pause" | null>(null);
  const pendingSeekRef = useRef<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    let interval: ReturnType<typeof setInterval> | undefined;
    let resizeObserver: ResizeObserver | undefined;
    let resizeFrame = 0;
    const fitTimers: number[] = [];
    // Resolved lazily on every use: the IFrame API replaces the mount node with its own iframe,
    // and the node is absent entirely on the renders where the video id is not yet known. A
    // container captured once at effect start would be null in those cases, silently disabling
    // both the ResizeObserver and the pixel sizing below.
    const resolveContainer = () => document.getElementById(containerId)?.parentElement ?? null;
    const fitPlayer = (player = playerRef.current) => {
      if (cancelled || !player) return;
      fitIframeToContainer(player, resolveContainer());
    };
    const schedulePlayerFit = () => {
      window.cancelAnimationFrame(resizeFrame);
      resizeFrame = window.requestAnimationFrame(() => fitPlayer());
    };
    const attachResizeObserver = () => {
      const container = resolveContainer();
      if (!container || typeof ResizeObserver === "undefined") return;
      resizeObserver = new ResizeObserver(schedulePlayerFit);
      resizeObserver.observe(container);
    };

    setCurrentTime(0);
    setDuration(0);
    setIsPlaying(false);
    setHasStarted(false);
    setEnded(false);
    setEndedVideoId(null);
    readyRef.current = false;
    pendingPlaybackRef.current = null;
    pendingSeekRef.current = null;

    // Without a video id there is no mount node to build the player on (see `videoUnavailable`
    // in VideoPlayerPage); constructing one against a missing element leaves a broken instance.
    if (!videoId) return;

    void loadIframeApi().then(() => {
      if (cancelled || !window.YT) return;
      playerRef.current = new window.YT.Player(containerId, {
        width: "100%",
        height: "100%",
        videoId,
        // EnglishAI's learning tools stay outside the unmodified YouTube player.
        // YouTube owns playback controls, captions, settings, branding and fullscreen.
        playerVars: {
          controls: 1,
          disablekb: 0,
          fs: 1,
          playsinline: 1,
          origin: window.location.origin,
        },
        events: {
          onReady: (event) => {
            if (cancelled) return;
            readyRef.current = true;
            fitPlayer(event.target);
            schedulePlayerFit();
            // Fallback for WebViews without ResizeObserver, and for the frames that only settle
            // once YouTube's own layout has run.
            fitTimers.push(
              window.setTimeout(() => fitPlayer(event.target), 300),
              window.setTimeout(() => fitPlayer(event.target), 1000),
            );
            if (!resizeObserver) attachResizeObserver();
            if (pendingSeekRef.current !== null) {
              event.target.seekTo?.(pendingSeekRef.current, true);
              pendingSeekRef.current = null;
            }
            const pendingPlayback = pendingPlaybackRef.current;
            pendingPlaybackRef.current = null;
            if (pendingPlayback === "play") event.target.playVideo?.();
            if (pendingPlayback === "pause") event.target.pauseVideo?.();
          },
          onStateChange: (event) => {
            if (cancelled) return;
            fitPlayer(event.target);
            setIsPlaying(event.data === YT_STATE_PLAYING);
            if (event.data === YT_STATE_PLAYING) setHasStarted(true);
            setEnded(event.data === YT_STATE_ENDED);
            setEndedVideoId(event.data === YT_STATE_ENDED ? videoId : null);
          },
        },
      });
      attachResizeObserver();
      window.addEventListener("resize", schedulePlayerFit);
      window.addEventListener("orientationchange", schedulePlayerFit);
      // Polled fairly tightly (the IFrame API has no time-update event) so the transcript can
      // follow the audio at the word level, not just the line level. The same tick keeps the
      // duration and native settings fresh for the separate shadowing exercise.
      interval = setInterval(() => {
        const player = playerRef.current;
        const nativeRate = player?.getPlaybackRate?.();
        if (typeof nativeRate === "number") setPlaybackRateState(nativeRate);
        const nativeVolume = player?.getVolume?.();
        if (typeof nativeVolume === "number") setVolumeState(nativeVolume);
        const nativeMuted = player?.isMuted?.();
        if (typeof nativeMuted === "boolean") setMutedState(nativeMuted);
        const time = player?.getCurrentTime?.();
        if (typeof time === "number") setCurrentTime(time);
        const total = player?.getDuration?.();
        if (typeof total === "number" && total > 0) setDuration(total);
        const state = player?.getPlayerState?.();
        if (typeof state === "number") {
          setIsPlaying(state === YT_STATE_PLAYING);
          if (state === YT_STATE_PLAYING) setHasStarted(true);
          setEnded(state === YT_STATE_ENDED);
          setEndedVideoId(state === YT_STATE_ENDED ? videoId : null);
        }
      }, 100);
    });

    return () => {
      cancelled = true;
      window.cancelAnimationFrame(resizeFrame);
      for (const timer of fitTimers) window.clearTimeout(timer);
      resizeObserver?.disconnect();
      window.removeEventListener("resize", schedulePlayerFit);
      window.removeEventListener("orientationchange", schedulePlayerFit);
      if (interval) clearInterval(interval);
      playerRef.current?.destroy?.();
      playerRef.current = null;
      readyRef.current = false;
      pendingPlaybackRef.current = null;
      pendingSeekRef.current = null;
    };
  }, [videoId, containerId]);

  const seekTo = useCallback((seconds: number) => {
    if (!playerRef.current || !readyRef.current) {
      pendingSeekRef.current = seconds;
      return;
    }
    playerRef.current?.seekTo?.(seconds, true);
  }, []);

  const togglePlay = useCallback(() => {
    const player = playerRef.current;
    if (!player) {
      pendingPlaybackRef.current = "play";
      return;
    }
    if (!readyRef.current) {
      pendingPlaybackRef.current = pendingPlaybackRef.current === "play" ? null : "play";
      return;
    }
    if (player.getPlayerState?.() === YT_STATE_PLAYING) player.pauseVideo?.();
    else player.playVideo?.();
  }, []);

  const play = useCallback(() => {
    if (!playerRef.current || !readyRef.current) {
      pendingPlaybackRef.current = "play";
      return;
    }
    playerRef.current.playVideo?.();
  }, []);

  const pause = useCallback(() => {
    if (!playerRef.current || !readyRef.current) {
      pendingPlaybackRef.current = "pause";
      return;
    }
    playerRef.current.pauseVideo?.();
  }, []);

  const getCurrentTime = useCallback(() => playerRef.current?.getCurrentTime?.() ?? 0, []);

  const setVolume = useCallback((next: number) => {
    const clamped = Math.max(0, Math.min(100, Math.round(next)));
    const player = playerRef.current;
    setVolumeState(clamped);
    player?.setVolume?.(clamped);
    if (clamped === 0) {
      player?.mute?.();
      setMutedState(true);
    } else {
      player?.unMute?.();
      setMutedState(false);
    }
  }, []);

  const toggleMute = useCallback(() => {
    const player = playerRef.current;
    if (!player) return;
    if (player.isMuted?.()) {
      player.unMute?.();
      setMutedState(false);
    } else {
      player.mute?.();
      setMutedState(true);
    }
  }, []);

  const setPlaybackRate = useCallback((rate: number) => {
    const next = Math.max(0.5, Math.min(2, rate));
    playerRef.current?.setPlaybackRate?.(next);
    setPlaybackRateState(next);
  }, []);

  return {
    currentTime,
    duration,
    isPlaying,
    hasStarted,
    ended,
    endedVideoId,
    seekTo,
    togglePlay,
    play,
    pause,
    getCurrentTime,
    volume,
    muted,
    setVolume,
    toggleMute,
    playbackRate,
    setPlaybackRate,
  };
}

/** The index of the transcript segment active at time <paramref name="seconds"/>, or -1. */
export function activeSegmentIndex(
  segments: ReadonlyArray<{ startSeconds: number; endSeconds: number }>,
  seconds: number,
): number {
  let active = -1;
  for (let i = 0; i < segments.length; i++) {
    if (segments[i].startSeconds <= seconds + 0.15) active = i;
    else break;
  }
  return active;
}

/** Matches a "word" the same way the transcript tokenizer does (letters, apostrophes, hyphens). */
const WORD_RE = /[A-Za-z][A-Za-z'-]*/g;

interface TimedWord {
  startSeconds: number;
  endSeconds: number;
}

/**
 * Which word of the line is being spoken at <paramref name="seconds"/>, as a 0-based index (or -1
 * when none is yet). When the line has real per-word timing (<paramref name="words"/>), the spoken
 * word is found directly from that timing - the index is into the `words` array. Otherwise it falls
 * back to estimating from the line span, distributing it across the words of `englishText` weighted
 * by length (the index is then into those regex-matched words).
 */
export function activeWordIndex(
  segment: { startSeconds: number; endSeconds: number; englishText: string; words?: TimedWord[] },
  seconds: number,
): number {
  const timed = segment.words;
  if (timed && timed.length > 0) {
    // The current word is the last one that has started; it stays highlighted until the next
    // word begins, giving a continuous karaoke sweep (small tolerance absorbs poll jitter).
    let index = -1;
    for (let i = 0; i < timed.length; i++) {
      if (timed[i].startSeconds <= seconds + 0.05) index = i;
      else break;
    }
    return index;
  }

  const words = segment.englishText.match(WORD_RE);
  if (!words || words.length === 0) return -1;

  const start = segment.startSeconds;
  // Guard against zero/negative spans (some caption lines share a timestamp).
  const end = Math.max(segment.endSeconds, start + 0.1);
  if (seconds < start) return -1;
  if (seconds >= end) return words.length - 1;

  const weights = words.map((w) => w.length + 1);
  const total = weights.reduce((a, b) => a + b, 0);
  const target = ((seconds - start) / (end - start)) * total;

  let acc = 0;
  for (let i = 0; i < words.length; i++) {
    acc += weights[i];
    if (target <= acc) return i;
  }
  return words.length - 1;
}
