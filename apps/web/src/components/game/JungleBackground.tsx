import { cn } from "@/lib/cn";
import { staticAsset } from "@/lib/staticAssets";

/**
 * Global jungle backdrop - the single fixed photo layer every game screen sits on
 * (spec 00-INDEX §2.1). Mounted ONCE in AppShell, behind all content (-z-10).
 *
 * Implements the mandated 2-layer scrim so text stays legible:
 *   - flat darken (bg-black/35), AND
 *   - green canopy → deep-jungle vertical gradient.
 * A subtle scale+blur kills hard photo edges. Content always sits in an elevated
 * card on top of this - never raw text on the photo.
 */
export function JungleBackground({ className }: { className?: string }) {
  return (
    <div
      aria-hidden
      className={cn(
        "fixed inset-0 -z-10 h-screen w-screen overflow-hidden",
        className,
      )}
    >
      <img
        src={staticAsset("/assets/jungle.jpg")}
        alt=""
        className="fixed inset-0 h-screen w-screen scale-105 object-cover blur-[1px]"
        draggable={false}
      />
      {/* Layer 1: flat darken for baseline legibility. */}
      <div className="absolute inset-0 bg-black/35" />
      {/* Layer 2: green canopy top → deep jungle bottom gradient. */}
      <div className="absolute inset-0 bg-gradient-to-b from-ea-green-900/45 via-transparent to-ea-ink-deep/75" />
    </div>
  );
}
