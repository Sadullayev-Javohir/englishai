import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

/**
 * The to'tiqush (parrot) mascot + speech bubble for the topic lesson.
 *
 * `Friend` renders a colored skill-icon circle (Duolingo-style friend token) used
 * across the level roadmap to badge each of the six skills. `MascotBubble` frames
 * any child text in a Duolingo-style white speech bubble with a little tail.
 *
 * Both are fresh, clean components for the gamification redesign.
 */

export type FriendKey =
  | "vocabulary"
  | "grammar"
  | "reading"
  | "writing"
  | "speaking"
  | "listening";

const FRIEND_ICON: Record<FriendKey, string> = {
  vocabulary: "menu_book",
  grammar: "rule",
  reading: "auto_stories",
  writing: "edit_note",
  speaking: "record_voice_over",
  listening: "headphones",
};

const FRIEND_COLOR: Record<FriendKey, string> = {
  vocabulary: "bg-ea-green-600",
  grammar: "bg-ea-green-600",
  reading: "bg-ea-yellow-700",
  writing: "bg-ea-blue-600",
  speaking: "bg-accent",
  listening: "bg-ea-green-600",
};

export function Friend({
  skill,
  size = 40,
  className,
}: {
  skill: FriendKey;
  size?: number;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center justify-center rounded-full text-white shadow-sm",
        FRIEND_COLOR[skill] ?? "bg-ea-green-600",
        className,
      )}
      style={{ width: size, height: size }}
    >
      <Icon
        name={FRIEND_ICON[skill] ?? "school"}
        className="text-[16px] md:text-[18px]"
      />
    </span>
  );
}

type BubbleTone = "green" | "blue" | "yellow" | "purple" | "orange" | "red" | "transparent";

const BUBBLE_RING: Record<BubbleTone, string> = {
  green: "border-primary/30",
  blue: "border-ea-blue-600/30",
  yellow: "border-ea-yellow-700/30",
  purple: "border-ea-green-600/30",
  orange: "border-accent/30",
  red: "border-ea-orange-600/30",
  transparent: "border-white/30",
};

export function MascotBubble({
  children,
  className,
  tail = "left",
  tone = "green",
}: {
  children: React.ReactNode;
  className?: string;
  /** Which side the speech-bubble tail points to (toward the parrot). */
  tail?: "left" | "right" | "none";
  /** Accent border tone. */
  tone?: BubbleTone;
}) {
  return (
    <div className={cn("relative", className)}>
      <div
        className={cn(
          "rounded-2xl px-4 py-3 font-body-md text-body-md shadow-md border",
          tone === "transparent"
            ? "bg-ea-green-900/70  text-white border-white/30"
            : "bg-ea-surface text-text-primary border",
          tone !== "transparent" && BUBBLE_RING[tone],
        )}
      >
        {children}
      </div>
      {tail !== "none" && (
        <span
          className={cn(
            "absolute -bottom-2 w-4 h-4 rotate-45 rounded-sm border-r border-b",
            tone === "transparent"
              ? "bg-ea-green-900/70 border-white/30"
              : "bg-ea-surface border-white/50",
            tail === "left" ? "left-6" : "right-6",
          )}
        />
      )}
    </div>
  );
}
