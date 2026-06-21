import { useEffect, useState } from "react";

interface ConfettiPiece {
  id: number;
  left: number;
  delay: number;
  duration: number;
  drift: number;
  rotate: number;
  color: string;
  size: number;
}

const COLORS = ["#4F46E5", "#818CF8", "#A5B4FC", "#D97706", "#4338CA"];

interface ConfettiProps {
  /** Bump this number to fire a fresh burst (each change re-spawns the pieces). */
  fireKey: number;
  /** Number of pieces per burst. */
  count?: number;
  className?: string;
}

/**
 * Lightweight CSS confetti burst. Renders a fixed layer of falling colored squares that
 * rain down once whenever `fireKey` changes. Pure CSS animation - no canvas, no extra deps.
 */
export function Confetti({ fireKey, count = 80, className }: ConfettiProps) {
  const [pieces, setPieces] = useState<ConfettiPiece[]>([]);

  useEffect(() => {
    if (fireKey === 0) return;
    const next: ConfettiPiece[] = Array.from({ length: count }, (_, i) => ({
      id: fireKey * 1000 + i,
      left: Math.random() * 100,
      delay: Math.random() * 0.4,
      duration: 1.6 + Math.random() * 1.4,
      drift: (Math.random() - 0.5) * 240,
      rotate: 180 + Math.random() * 540,
      color: COLORS[i % COLORS.length],
      size: 7 + Math.random() * 8,
    }));
    setPieces(next);
    const t = window.setTimeout(() => setPieces([]), 3200);
    return () => window.clearTimeout(t);
  }, [fireKey, count]);

  if (pieces.length === 0) return null;

  return (
    <div className={`pointer-events-none fixed inset-0 z-[80] overflow-hidden ${className ?? ""}`}>
      {pieces.map((p) => (
        <span
          key={p.id}
          className="absolute top-[-24px] rounded-[2px] confetti-piece"
          style={{
            left: `${p.left}%`,
            width: p.size,
            height: p.size,
            background: p.color,
            // CSS vars consumed by the existing .confetti-piece animation rules.
            ["--dur" as string]: `${p.duration}s`,
            ["--delay" as string]: `${p.delay}s`,
            ["--drift" as string]: `${p.drift}px`,
            ["--spin" as string]: `${p.rotate}deg`,
          }}
        />
      ))}
    </div>
  );
}
