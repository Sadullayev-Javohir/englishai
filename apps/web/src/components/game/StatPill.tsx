import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";

/** Flat status control shared by the app HUD variants. */

interface StatPillProps {
  icon: React.ReactNode;
  label: string;
  title: string;
  tone?: "orange" | "yellow" | "blue" | "purple" | "red";
  className?: string;
  /** SPA navigation target (rendered as a router link). Prefer over `to`. */
  to?: string;
  /** Click handler (e.g. navigate()) - used for SPA nav without a full reload. */
  onClick?: () => void;
}

const TONE_TEXT: Record<NonNullable<StatPillProps["tone"]>, string> = {
  orange: "text-[var(--ea-primary)]",
  yellow: "text-[var(--ea-primary)]",
  blue: "text-[var(--ea-primary)]",
  purple: "text-[var(--ea-primary)]",
  red: "text-[var(--ea-danger)]",
};

export function StatPill({ icon, label, title, tone = "yellow", className, to, onClick }: StatPillProps) {
  const inner = (
    <span
      title={title}
      className={cn(
        "flex items-center gap-1.5 rounded-full border border-[var(--ea-border)] bg-[var(--ea-surface)] px-3 py-1.5",
        "font-duo font-extrabold",
        TONE_TEXT[tone],
        onClick && "cursor-pointer",
        className,
      )}
    >
      <span className="text-[16px] leading-none" aria-hidden>
        {icon}
      </span>
      <span className="text-[14px] leading-none tabular-nums">{label}</span>
    </span>
  );

  if (onClick) {
    return (
      <button
        type="button"
        onClick={onClick}
        aria-label={title}
        className="rounded-full outline-none focus-visible:ring-2 focus-visible:ring-[var(--ea-focus)] focus-visible:ring-offset-2"
      >
        {inner}
      </button>
    );
  }
  if (to) {
    return (
      <Link to={to} className="rounded-full outline-none focus-visible:ring-2 focus-visible:ring-[var(--ea-focus)] focus-visible:ring-offset-2">
        {inner}
      </Link>
    );
  }
  return inner;
}
