import { cn } from "@/lib/cn";

interface ProgressBarProps {
  value: number; // 0..1
  className?: string;
  barClassName?: string;
  label?: string;
}

export function ProgressBar({ value, className, barClassName, label = "Progress" }: ProgressBarProps) {
  const pct = Math.round(Math.max(0, Math.min(1, value)) * 100);
  return (
    <div
      className={cn("w-full bg-surface-container-high h-2 rounded-full overflow-hidden", className)}
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={pct}
    >
      <div
        className={cn("bg-progress h-full rounded-full transition-[width] duration-500 ease-out", barClassName)}
        style={{ width: `${pct}%` }}
      />
    </div>
  );
}
