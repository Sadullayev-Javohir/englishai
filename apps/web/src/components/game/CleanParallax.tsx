import { useEffect, useRef } from "react";
import { cn } from "@/lib/cn";

interface CleanParallaxProps {
  className?: string;
  /** Intensity of the pointer-driven parallax in px. */
  intensity?: number;
  children?: React.ReactNode;
}

/**
 * Clean jungle depth layer for the Level Map (spec §2.1, last paragraph). The old
 * `JungleParallax` is NOT reused - this is a fresh, tasteful static depth with a
 * subtle CSS-driven parallax that reacts to pointer movement. Sits ABOVE
 * JUNGLE.jpg as a decorative foreground (leaves / canopy silhouettes). All motion
 * is pointer-based and disabled under prefers-reduced-motion (CSS).
 */
export function CleanParallax({
  className,
  intensity = 18,
  children,
}: CleanParallaxProps) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const onMove = (e: PointerEvent) => {
      const { innerWidth: w, innerHeight: h } = window;
      const dx = (e.clientX / w - 0.5) * 2;
      const dy = (e.clientY / h - 0.5) * 2;
      el.style.setProperty("--px", `${dx * intensity}px`);
      el.style.setProperty("--py", `${dy * intensity}px`);
    };
    window.addEventListener("pointermove", onMove);
    return () => window.removeEventListener("pointermove", onMove);
  }, [intensity]);

  return (
    <div
      ref={ref}
      aria-hidden
      className={cn(
        "pointer-events-none absolute inset-0 overflow-hidden",
        className,
      )}
      style={{
        // Driven by --px/--py set on pointer move; CSS transition smooths it.
        transform: "translate3d(var(--px,0), var(--py,0), 0)",
        transition: "transform 200ms ease-out",
        // Soft canopy glow from the top to fake depth without heavy assets.
        background:
          "radial-gradient(120% 80% at 50% -10%, rgba(11,61,46,0.35), transparent 60%)",
      }}
    >
      {children}
    </div>
  );
}
