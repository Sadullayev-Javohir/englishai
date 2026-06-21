import { cn } from "@/lib/cn";

/**
 * Clean jungle backdrop for the immersive placement / lesson screens.
 *
 * Renders public/assets/jungle.jpg under a 2-layer scrim so white UI text stays
 * legible, per the gamification redesign spec (§2 Background & Shell):
 *   - flat darken (bg-black/40), AND
 *   - green canopy → deep-jungle vertical gradient.
 * A subtle scale + blur kills hard photo edges. The screen that mounts this owns
 * its own chrome (thin top progress + peeking parrot) - this layer adds NO HUD.
 *
 * This is a fresh, on-brand layer. It is intentionally NOT a copy of the deprecated
 * src/components/duo/JungleBackground (which carried garish framing).
 */
export function JungleBackground({ className }: { className?: string }) {
  return (
    <div
      aria-hidden
      className={cn("absolute inset-0 -z-10 h-full w-full overflow-hidden", className)}
    >
      <img
        src="/assets/jungle.jpg"
        alt=""
        className="absolute inset-0 h-full w-full scale-105 object-cover blur-[1px]"
        draggable={false}
        decoding="async"
      />
      {/* Layer 1: flat darken for baseline legibility. */}
      <div className="absolute inset-0 bg-black/40" />
      {/* Layer 2: green canopy top → deep jungle bottom gradient. */}
      <div className="absolute inset-0 bg-gradient-to-b from-ea-green-900/40 via-transparent to-ea-ink-deep/80" />
    </div>
  );
}
