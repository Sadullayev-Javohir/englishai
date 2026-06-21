import { useState } from "react";
import { CefrLevel } from "@/api/types";
import { api } from "@/api/client";
import { cefrShort } from "@/lib/labels";
import { cn } from "@/lib/cn";
import { ILLUSTRATION_GRADIENTS, hashString } from "@/lib/illustrationGradients";

interface TopicImageProps {
  topicId: string;
  title: string;
  level: CefrLevel;
  category?: string;
  slot?: number;
  hideLevelBadge?: boolean;
  /** Suppress the title caption drawn on the generated placeholder art. Use where the
      title is already shown next to the image (e.g. the level-map cards) so it never
      renders twice. */
  hideTitle?: boolean;
  className?: string;
}


export function TopicImage({
  topicId,
  title,
  level,
  slot = 0,
  hideLevelBadge,
  hideTitle,
  className,
}: TopicImageProps) {
  const [failed, setFailed] = useState(false);
  const artIndex = hashString(`${topicId}:${slot}`) % ILLUSTRATION_GRADIENTS.length;

  return (
    <div className={cn("relative overflow-hidden bg-surface-container", className)}>
      {failed ? (
        <div
          data-safe-art={`topic-${artIndex}`}
          className={cn(
            "relative flex h-full w-full items-end overflow-hidden bg-ea-primary p-md text-ea-on-primary",
            ILLUSTRATION_GRADIENTS[artIndex],
          )}
        >
          <span aria-hidden className="absolute -right-8 -top-10 h-28 w-28 rounded-full bg-white/20" />
          <span aria-hidden className="absolute right-10 top-6 h-12 w-16 rotate-6 rounded-[45%] bg-white/15 ring-2 ring-white/15" />
          <span aria-hidden className="absolute -bottom-12 -left-8 h-32 w-52 rotate-[-8deg] rounded-[50%] bg-black/20" />
          <span aria-hidden className="absolute bottom-9 left-8 h-16 w-20 rotate-[-12deg] rounded-[45%] bg-white/10 ring-2 ring-white/15" />
          {!hideTitle && (
            <span className="relative line-clamp-3 font-title-md text-title-md leading-snug text-white drop-shadow-[0_2px_3px_rgba(0,0,0,0.35)]">
              {title}
            </span>
          )}
        </div>
      ) : (
        <img
          src={api.images.topicUrl(topicId, slot)}
          alt={title}
          loading="lazy"
          decoding="async"
          className="h-full w-full object-cover"
          onError={() => setFailed(true)}
        />
      )}
      {!hideLevelBadge && (
        <span className="absolute left-2 top-2 rounded-full bg-black/45 px-2 py-0.5 font-label-md text-label-md text-white ring-1 ring-white/25">
          {cefrShort(level)}
        </span>
      )}
    </div>
  );
}
