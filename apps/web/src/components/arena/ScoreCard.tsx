import { cn } from "@/lib/cn";
import { DesignCard } from "@/components/design";

export type ScoreTone = "blue" | "red" | "purple" | "green" | "yellow";

export interface ScoreCardProps {
  tone?: ScoreTone;
  children: React.ReactNode;
  className?: string;
}

const TONE_BAND: Record<ScoreTone, string> = {
  blue: "bg-ea-primary",
  red: "bg-ea-danger",
  purple: "bg-ea-primary",
  green: "bg-ea-primary",
  yellow: "bg-ea-primary",
};

/** Canonical result card with a semantic top accent band. */
export function ScoreCard({ tone = "blue", children, className }: ScoreCardProps) {
  return (
    <DesignCard
      padding="none"
      className={cn(
        "mx-auto w-full max-w-[480px] text-center",
        className,
      )}
    >
      {/* Top accent band */}
      <div className={cn("h-2.5 w-full", TONE_BAND[tone])} />
      <div className="p-6">{children}</div>
    </DesignCard>
  );
}
