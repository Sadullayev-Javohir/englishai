import { useEffect, useRef } from "react";
import { cn } from "@/lib/cn";

const BAR_COUNT = 10;
/** Every Nth frequency bin is sampled per bar (skips the low sub-bass bins that barely move for
 * a voice signal, and spreads the rest across the visible bars). */
const BIN_STRIDE = 4;

/**
 * A live, real-amplitude frequency-bar visualizer for an active microphone stream - used by the
 * video shadowing mode so the learner sees they're actually being picked up while speaking (unlike
 * the canned CSS pulse used elsewhere in the app, e.g. `PronunciationAttempt`/`SpeakingPage`'s mic
 * button, this one is driven by real `AnalyserNode` frequency data).
 *
 * Bar heights are written directly to each bar's DOM style inside a `requestAnimationFrame` loop
 * instead of React state, the same reasoning as `SpeakingPage`'s `startSilenceMonitor` RAF loop -
 * pushing amplitude data through React state would re-render at up to 60fps for no benefit.
 */
export function MicFrequencyBars({ stream, className }: { stream: MediaStream | null; className?: string }) {
  const barRefs = useRef<(HTMLSpanElement | null)[]>([]);

  useEffect(() => {
    if (!stream) return;

    const AudioCtx: typeof AudioContext | undefined =
      window.AudioContext ??
      (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtx) return;

    const ctx = new AudioCtx();
    const source = ctx.createMediaStreamSource(stream);
    const analyser = ctx.createAnalyser();
    analyser.fftSize = 128;
    analyser.smoothingTimeConstant = 0.7;
    source.connect(analyser);
    const data = new Uint8Array(analyser.frequencyBinCount);

    let raf = 0;
    const tick = () => {
      analyser.getByteFrequencyData(data);
      for (let i = 0; i < BAR_COUNT; i++) {
        const bin = Math.min(data.length - 1, i * BIN_STRIDE + 2);
        const level = data[bin] / 255; // 0..1
        const bar = barRefs.current[i];
        if (bar) bar.style.transform = `scaleY(${Math.max(0.08, level)})`;
      }
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);

    return () => {
      cancelAnimationFrame(raf);
      source.disconnect();
      void ctx.close();
    };
  }, [stream]);

  return (
    <div className={cn("flex h-10 items-center justify-center gap-1", className)} aria-hidden="true">
      {Array.from({ length: BAR_COUNT }, (_, i) => (
        <span
          key={i}
          ref={(el) => {
            barRefs.current[i] = el;
          }}
          className="h-full w-1.5 origin-center rounded-full bg-ea-orange-500 transition-none"
          style={{ transform: "scaleY(0.08)" }}
        />
      ))}
    </div>
  );
}
