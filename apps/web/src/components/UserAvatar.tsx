import { useState } from "react";
import { cn } from "@/lib/cn";

interface UserAvatarProps {
  pictureUrl?: string | null;
  name?: string | null;
  className?: string;
  imageClassName?: string;
  fallbackClassName?: string;
  alt?: string;
}

function initials(name?: string | null) {
  const parts = name?.trim().split(/\s+/).filter(Boolean) ?? [];
  if (parts.length === 0) return "U";
  return parts.slice(0, 2).map((part) => part.charAt(0)).join("").toUpperCase();
}

export function UserAvatar({ pictureUrl, name, className, imageClassName, fallbackClassName, alt = "" }: UserAvatarProps) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  const showImage = Boolean(pictureUrl) && failedUrl !== pictureUrl;

  return (
    <span className={cn("inline-grid shrink-0 place-items-center overflow-hidden", className)}>
      {showImage ? (
        <img
          src={pictureUrl ?? undefined}
          alt={alt}
          referrerPolicy="no-referrer"
          className={cn("h-full w-full bg-[var(--ea-surface)] object-cover", imageClassName)}
          onError={() => setFailedUrl(pictureUrl ?? null)}
        />
      ) : (
        <span className={cn("grid h-full w-full place-items-center", fallbackClassName)}>{initials(name)}</span>
      )}
    </span>
  );
}
