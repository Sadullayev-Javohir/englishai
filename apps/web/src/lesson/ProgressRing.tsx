import { motion } from "framer-motion";

interface ProgressRingProps {
  /** 0..1 fraction. */
  value: number;
  size?: number;
  stroke?: number;
  color?: string;
  trackColor?: string;
  className?: string;
  children?: React.ReactNode;
}

/**
 * Circular progress ring (ProgressRing): an SVG stroke that fills with a spring as `value`
 * rises. Used for the per-skill mastery rings in the checklist.
 */
export function ProgressRing({
  value,
  size = 44,
  stroke = 5,
  color = "#B9D7AA",
  trackColor = "rgba(255,255,255,0.35)",
  className,
  children,
}: ProgressRingProps) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const pct = Math.max(0, Math.min(1, value));
  return (
    <div className={`relative inline-flex items-center justify-center ${className ?? ""}`} style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90">
        <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke={trackColor} strokeWidth={stroke} />
        <motion.circle
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="none"
          stroke={color}
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={c}
          initial={false}
          animate={{ strokeDashoffset: c * (1 - pct) }}
          transition={{ type: "spring", stiffness: 160, damping: 24 }}
        />
      </svg>
      {children != null && (
        <span className="absolute inset-0 flex items-center justify-center font-duo font-extrabold">
          {children}
        </span>
      )}
    </div>
  );
}
