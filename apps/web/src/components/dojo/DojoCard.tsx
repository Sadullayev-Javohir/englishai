import { cn } from "@/lib/cn";

export type DojoTone = "neutral" | "red" | "blue" | "yellow" | "green";

interface DojoCardProps extends React.HTMLAttributes<HTMLDivElement> {
  tone?: DojoTone;
  children: React.ReactNode;
}

const TONE_RING: Record<DojoTone, string> = {
  neutral: "",
  red: "ring-1 ring-ea-danger/15",
  blue: "ring-1 ring-ea-primary/15",
  yellow: "ring-1 ring-ea-orange-500/20",
  green: "ring-1 ring-ea-green-600/15",
};

/**
 * 3D jungle-dojo content card (pronunciation dojo screen). Same chunky-lip
 * language as the game layer's PopCard, with an optional tone ring so a card
 * can read as an error/info/success section without a loud background fill.
 */
export function DojoCard({ tone = "neutral", className, children, ...rest }: DojoCardProps) {
  return (
    <div
      className={cn(
        "rounded-card bg-ea-surface/95 p-md",
        TONE_RING[tone],
        className,
      )}
      {...rest}
    >
      {children}
    </div>
  );
}
