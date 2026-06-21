interface ProgressRingProps {
  value: number; // 0..1
  size?: number;
  strokeWidth?: number;
  // Colour hooks so the ring reads on any background (e.g. a coloured/gradient banner):
  // pass Tailwind text-* colour utilities and the `currentColor` stroke follows.
  trackClassName?: string;
  arcClassName?: string;
  children?: React.ReactNode;
}

/** SVG progress ring matching the dashboard daily-goal indicator. */
export function ProgressRing({
  value,
  size = 128,
  strokeWidth = 8,
  trackClassName = "text-surface-container",
  arcClassName = "text-primary-container",
  children,
}: ProgressRingProps) {
  const radius = (size - strokeWidth) / 2;
  const circumference = 2 * Math.PI * radius;
  const clamped = Math.max(0, Math.min(1, value));
  const offset = circumference * (1 - clamped);
  const center = size / 2;

  return (
    <div className="relative flex items-center justify-center" style={{ width: size, height: size }}>
      <svg width={size} height={size}>
        <circle
          className={`${trackClassName} stroke-current`}
          cx={center}
          cy={center}
          r={radius}
          fill="transparent"
          strokeWidth={strokeWidth}
        />
        <circle
          className={`${arcClassName} stroke-current progress-ring-circle`}
          cx={center}
          cy={center}
          r={radius}
          fill="transparent"
          strokeWidth={strokeWidth}
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">{children}</div>
    </div>
  );
}
