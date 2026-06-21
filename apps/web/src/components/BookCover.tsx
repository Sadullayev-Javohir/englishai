import { CefrLevel } from "@/api/types";
import { cefrShort } from "@/lib/labels";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

// Per-level gradient for the generated placeholder cover (used when no licensed photo is
// available). Full literal class names so Tailwind's scanner keeps them in the build.
const LEVEL_GRADIENT: Record<CefrLevel, string> = {
  [CefrLevel.A1]: "bg-ea-primary",
  [CefrLevel.A2]: "bg-ea-primary",
  [CefrLevel.B1]: "bg-ea-primary",
  [CefrLevel.B2]: "bg-ea-primary",
  [CefrLevel.C1]: "bg-ea-primary",
  [CefrLevel.C2]: "bg-ea-primary",
};

interface BookCoverProps {
  title: string;
  level: CefrLevel;
  coverImageUrl: string | null;
  className?: string;
}

/**
 * A book cover: the licensed photo when one was resolved (Wikimedia), otherwise a generated
 * gradient placeholder with the title and a modern book icon - copyright-safe and always
 * present (docs/development-guide.md rule 12). No emoji glyphs; a Lucide icon keeps the look cohesive.
 */
export function BookCover({ title, level, coverImageUrl, className }: BookCoverProps) {
  if (coverImageUrl) {
    return (
      <div className={cn("relative overflow-hidden bg-surface-container", className)}>
        <img src={coverImageUrl} alt={title} loading="lazy" className="w-full h-full object-cover" />
        <span className="absolute top-2 left-2 px-2 py-0.5 rounded-full bg-black/45 font-duo font-extrabold text-caption text-white">
          {cefrShort(level)}
        </span>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "relative flex min-h-0 flex-col items-center justify-center p-sm text-center min-[390px]:p-md",
        LEVEL_GRADIENT[level],
        className,
      )}
    >
      <span className="absolute top-2 left-2 px-2 py-0.5 rounded-full bg-black/25 font-duo font-extrabold text-caption text-white">
        {cefrShort(level)}
      </span>
      <Icon name="auto_stories" filled className="mb-xs text-[28px] text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.3)] min-[390px]:mb-sm min-[390px]:text-[34px]" />
      <span className="line-clamp-4 break-words font-headline-md text-[14px] leading-snug text-white drop-shadow min-[390px]:text-headline-md">
        {title}
      </span>
    </div>
  );
}
