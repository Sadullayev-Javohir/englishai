import { motion, useReducedMotion } from "framer-motion";
import { cn } from "@/lib/cn";

export type LiveVoiceVisualState = "idle" | "listening" | "thinking" | "speaking";

const STATE_COLORS: Record<LiveVoiceVisualState, string[]> = {
  idle: ["#0B1020", "#4F46E5", "#17172A"],
  listening: ["#4338CA", "#818CF8", "#17172A"],
  thinking: ["#3730A3", "#A5B4FC", "#17172A"],
  speaking: ["#4F46E5", "#818CF8", "#3730A3"],
};

interface LiveVoiceBackdropProps {
  state: LiveVoiceVisualState;
  /** Normalized audio energy (0..1). Drives the speaking-scene scale and glow. */
  intensity?: number;
}

/**
 * Visual state layer for a live AI conversation. It sits behind the transcript and changes its
 * palette for listening / thinking / speaking. While real TTS audio plays, `intensity` makes the
 * three depth-separated orbs breathe with the tutor's actual voice energy.
 */
export function LiveVoiceBackdrop({ state, intensity = 0 }: LiveVoiceBackdropProps) {
  const reduceMotion = useReducedMotion();
  const level = Math.min(1, Math.max(0, intensity));
  const colors = STATE_COLORS[state];
  const active = state !== "idle";

  return (
    <div
      data-live-voice-scene
      data-voice-state={state}
      aria-hidden="true"
      className={cn(
        "pointer-events-none absolute inset-0 overflow-hidden rounded-[20px] transition-colors duration-700 sm:rounded-[28px]",
        active ? "opacity-100" : "opacity-70",
      )}
      style={{
        "--voice-intensity": String(Number(level.toFixed(2))),
        background: `radial-gradient(circle at 50% 85%, ${colors[1]}55 0%, transparent 52%), linear-gradient(145deg, ${colors[0]} 0%, ${colors[2]} 100%)`,
      } as React.CSSProperties}
    >
      {colors.map((color, index) => {
        const size = 230 + index * 125;
        const energyScale = 1 + level * (0.12 + index * 0.045);
        return (
          <motion.span
            data-voice-orb
            key={color}
            className="absolute left-1/2 top-1/2 rounded-full blur-3xl mix-blend-screen"
            style={{
              width: size,
              height: size,
              marginLeft: -size / 2,
              marginTop: -size / 2,
              background: `radial-gradient(circle, ${color}cc 0%, ${color}44 45%, transparent 72%)`,
              opacity: 0.18 + index * 0.06 + level * 0.35,
            }}
            animate={reduceMotion ? { scale: energyScale } : {
              scale: state === "speaking"
                ? energyScale
                : [0.92 + index * 0.03, 1.05 + index * 0.02, 0.92 + index * 0.03],
              x: state === "listening" ? [0, 22 - index * 8, 0] : [0, -16 + index * 9, 0],
              y: state === "thinking" ? [0, -24 + index * 7, 0] : [0, 14 - index * 5, 0],
            }}
            transition={state === "speaking"
              ? { duration: 0.12, ease: "linear" }
              : { duration: 4.5 + index, repeat: Infinity, ease: "easeInOut" }}
          />
        );
      })}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,transparent_0%,rgba(2,18,13,0.15)_58%,rgba(2,18,13,0.58)_100%)]" />
      <div className="absolute inset-0 opacity-20 [background-image:linear-gradient(rgba(255,255,255,0.08)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.08)_1px,transparent_1px)] [background-size:44px_44px] [mask-image:linear-gradient(to_bottom,transparent,black_35%,black)]" />
    </div>
  );
}
