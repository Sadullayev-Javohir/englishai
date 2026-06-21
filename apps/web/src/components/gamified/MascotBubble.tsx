import { cn } from "@/lib/cn";

interface MascotBubbleProps {
  children: React.ReactNode;
  className?: string;
}

/** Friendly speech bubble used by empty states (parrot "speaks" the hint). */
export function MascotBubble({ children, className }: MascotBubbleProps) {
  return (
    <div
      className={cn(
        "relative rounded-2xl bg-ea-surface/95 p-5 ",
        "font-duo text-body-md text-ea-text",
        className,
      )}
    >
      {children}
      {/* Downward tail under the bubble. */}
      <span className="absolute -bottom-2 left-8 h-4 w-4 rotate-45 rounded-[3px] bg-ea-surface/95" />
    </div>
  );
}
