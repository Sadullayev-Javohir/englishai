import { cn } from "@/lib/cn";

/**
 * Loading placeholder block. Use in place of content while a screen fetches, to avoid
 * layout shift. Compose several to mimic the final layout (title bar, lines, avatar).
 */
export function Skeleton({ className }: { className?: string }) {
  return (
    <div
      aria-hidden="true"
      className={cn("animate-pulse rounded-lg bg-surface-container-high", className)}
    />
  );
}
