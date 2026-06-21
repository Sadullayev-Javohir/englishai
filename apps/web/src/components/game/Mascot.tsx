import { cn } from "@/lib/cn";
import { staticAsset } from "@/lib/staticAssets";

interface MascotProps {
  /** Size in px of the mascot image. */
  size?: number;
  /** Gentle floating idle animation. */
  float?: boolean;
  className?: string;
  alt?: string;
}

/**
 * Parrot (to'tiqush) mascot (spec §2.6). The brand's learning companion - used on
 * key screens with a SpeechBubble for encouragement. Image is the licensed asset
 * at /assets/parrot-mascot.png; we never recolor or redraw it.
 */
export function Mascot({
  size = 96,
  float = true,
  className,
  alt = "EnglishAI to'tiqushi",
}: MascotProps) {
  return (
    <img
      src={staticAsset("/assets/parrot-mascot.png")}
      alt={alt}
      width={size}
      height={size}
      draggable={false}
      className={cn(
        "object-contain drop-shadow-[0_6px_10px_rgba(0,0,0,0.25)] select-none",
        float && "animate-ea-float",
        className,
      )}
    />
  );
}

interface SpeechBubbleProps extends React.HTMLAttributes<HTMLDivElement> {
  /** Mascot sits on this side of the bubble. */
  side?: "left" | "right";
  children: React.ReactNode;
}

/**
 * Encouragement speech bubble for the mascot (spec §2.6). All visible Uzbek copy
 * MUST come from the content store (rule 11) - never hardcode free-form Uzbek here.
 */
export function SpeechBubble({
  side = "left",
  className,
  children,
  ...rest
}: SpeechBubbleProps) {
  return (
    <div className={cn("relative", className)} {...rest}>
      <div
        className={cn(
          "rounded-[20px] bg-ea-surface px-4 py-3 ",
          "font-body-md text-text-primary",
          side === "left" ? "rounded-bl-md" : "rounded-br-md",
        )}
      >
        {children}
        {/* Tail pointing to the mascot. */}
        <span
          className={cn(
            "absolute -bottom-2 h-4 w-4 rotate-45 bg-ea-surface",
            side === "left" ? "left-6" : "right-6",
          )}
        />
      </div>
    </div>
  );
}
