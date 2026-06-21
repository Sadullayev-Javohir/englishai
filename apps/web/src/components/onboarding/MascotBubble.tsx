import { cn } from "@/lib/cn";

/**
 * Speech / thought bubble for the parrot mascot (Jungle Academy spec §3).
 *
 * White frosted bubble with a small tail pointing down-left. Wrapping text
 * in Nunito extrabold for a playful Duolingo feel.
 */

interface MascotBubbleProps {
  text: string;
  className?: string;
}

export function MascotBubble({ text, className }: MascotBubbleProps) {
  return (
    <div className={cn("relative", className)}>
      <div className="rounded-2xl bg-ea-surface/95 px-5 py-3 ">
        <p className="font-duo font-extrabold text-[15px] leading-snug text-ea-green-900">
          {text}
        </p>
      </div>
      {/* Bubble tail */}
      <div className="absolute -bottom-[6px] left-10 h-3 w-3 rotate-45 bg-ea-surface/95" />
    </div>
  );
}
