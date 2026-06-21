import { cn } from "@/lib/cn";
import { Icon } from "./Icon";

interface EmptyStateProps {
  icon?: string;
  title: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
}

/**
 * Calm, centered empty state for lists/screens with no data yet. A muted icon in a soft
 * circle, a title, an optional line of guidance, and an optional action button.
 */
export function EmptyState({ icon = "info", title, description, action, className }: EmptyStateProps) {
  return (
    <div className={cn("flex flex-col items-center justify-center text-center py-2xl px-lg", className)}>
      <div className="mb-md flex h-16 w-16 items-center justify-center rounded-full bg-surface-container text-text-muted">
        <Icon name={icon} className="text-[28px]" />
      </div>
      <h3 className="font-headline-md text-headline-md text-text-primary">{title}</h3>
      {description && <p className="mt-xs max-w-sm text-body-md text-text-secondary">{description}</p>}
      {action && <div className="mt-lg">{action}</div>}
    </div>
  );
}
