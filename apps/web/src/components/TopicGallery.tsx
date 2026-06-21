import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { cn } from "@/lib/cn";

interface TopicGalleryProps {
  topicId: string;
  title: string;
  /** First slot to include (default 1, so the cover shown as the hero isn't repeated). */
  fromSlot?: number;
  /** Optional section heading + hint, rendered only when the gallery actually has images. */
  heading?: string;
  hint?: string;
  className?: string;
}

/**
 * A topic's image gallery: the licensed photos downloaded once and stored in the database, shown as
 * a responsive strip (docs/development-guide.md rule 12 - re-served from the database, attribution preserved). It
 * reads the manifest to learn which slots exist, so it shows only real images and renders nothing
 * when a topic has no gallery yet (e.g. before the image backfill runs) - never a broken section.
 */
export function TopicGallery({ topicId, title, fromSlot = 1, heading, hint, className }: TopicGalleryProps) {
  const { data } = useAsync(() => api.images.topicManifest(topicId), [topicId]);

  const images = (data?.images ?? []).filter((i) => i.slot >= fromSlot);
  if (images.length === 0) return null;

  const grid = (
    <div className={cn("grid grid-cols-1 gap-sm min-[480px]:grid-cols-2 md:grid-cols-3 md:gap-md", className)}>
      {images.map((img) => (
        <figure key={img.slot} className="relative aspect-[16/10] overflow-hidden rounded-xl bg-surface-container min-[480px]:aspect-[4/3]">
          <img
            src={api.images.topicUrl(topicId, img.slot)}
            alt={title}
            loading="lazy"
            className="w-full h-full object-cover"
          />
          {img.attribution && (
            <figcaption
              className="absolute inset-x-0 bottom-0 line-clamp-2 break-words bg-black/55 px-2 py-1 font-caption text-caption leading-tight text-white"
              title={img.attribution}
            >
              {img.attribution}
            </figcaption>
          )}
        </figure>
      ))}
    </div>
  );

  if (!heading && !hint) return grid;

  return (
    <section className="space-y-sm">
      {heading && <h2 className="font-headline-md text-headline-md text-primary">{heading}</h2>}
      {hint && <p className="font-caption text-caption text-text-secondary">{hint}</p>}
      {grid}
    </section>
  );
}
