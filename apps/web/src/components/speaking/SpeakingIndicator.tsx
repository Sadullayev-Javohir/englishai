// Staggered start offsets so the bars ripple instead of moving in lockstep. Seven bars read as a
// proper voice waveform; the varied delays give it an organic, dictaphone-like motion.
const BAR_DELAYS = ["-0.6s", "-0.15s", "-0.45s", "0s", "-0.3s", "-0.5s", "-0.2s"];

interface SpeakingIndicatorProps {
  /** Diameter of the indicator disc in px (the bars scale with it). */
  size?: number;
}

/**
 * "AI is speaking" animation for the live conversation - a dictaphone-style voice waveform:
 * high-contrast equalizer bars rippling inside a primary disc, ringed by a soft pulsing halo. Deliberately shows
 * NO parrot/logo: it stands in for the mic button while the tutor talks, so it must read purely as
 * "audio is playing", not as a mascot sitting on top of the mic. The viseme mouth stays on the
 * Pronunciation Detail screen, where lip position is the point.
 */
export function SpeakingIndicator({ size = 64 }: SpeakingIndicatorProps) {
  return (
    <div className="relative flex items-center justify-center" style={{ width: size, height: size }}>
      {/* Soft pulsing halo - the outward "sound" ripple. */}
      <span className="absolute inset-0 rounded-full bg-primary-container/25 animate-ping" />
      {/* Solid green disc the waveform sits on. */}
      <span className="absolute inset-0 rounded-full bg-primary-container shadow-lg" />
      {/* Equalizer bars centred in the disc - the tutor's "voice". */}
      <div
        className="relative z-10 flex items-end gap-[3px]"
        style={{ height: size * 0.42 }}
        aria-hidden
      >
        {BAR_DELAYS.map((delay, i) => (
          <span
            key={i}
            className="animate-soundbar rounded-full bg-on-primary-container"
            style={{ width: Math.max(3, size * 0.05), height: "100%", animationDelay: delay }}
          />
        ))}
      </div>
    </div>
  );
}
