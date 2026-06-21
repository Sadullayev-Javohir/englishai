import { cn } from "@/lib/cn";
import { Icon } from "./Icon";

type Tone = "success" | "warning" | "info" | "accent" | "streak" | "neutral";

interface PillProps {
  tone?: Tone;
  icon?: string;
  iconFilled?: boolean;
  children: React.ReactNode;
  className?: string;
}

// Badge tones map to the brief's badge palette (green / orange / blue) plus status.
const TONES: Record<Tone, string> = {
  success: "bg-success-bg text-success",
  warning: "bg-warning-bg text-on-tertiary-fixed",
  info: "bg-info-bg text-on-info-bg",
  accent: "bg-secondary-container text-on-secondary-container",
  streak: "bg-surface text-streak-active border border-border",
  neutral: "bg-surface-container text-text-secondary",
};

export function Pill({ tone = "neutral", icon, iconFilled, children, className }: PillProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-xs px-md py-xs rounded-full font-label-md text-label-md",
        TONES[tone],
        className,
      )}
    >
      {icon && <Icon name={icon} filled={iconFilled} className="text-[18px]" />}
      {children}
    </span>
  );
}
