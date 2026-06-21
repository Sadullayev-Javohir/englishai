import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";
import { Mascot } from "@/components/game";
import { CATALOG_ACCENT, CATALOG_GREETING } from "./catalogTokens";
import type { CatalogModule } from "./catalogTokens";

interface CatalogHeaderProps {
  module: CatalogModule;
  /** Module title (e.g. "Video darslar"). */
  title: string;
  /** Module subtitle / hint. */
  subtitle?: string;
  /** "12/30 o'rganildi" style progress pill. Show when both are present. */
  learned?: number;
  total?: number;
}

/**
 * Catalog header (spec §3): module title + parrot greeter + a progress pill
 * ("12/30 o'rganildi"). Sits above the 3D card grid. Built fresh on the clean
 * `game/` primitives.
 */
export function CatalogHeader({
  module,
  title,
  subtitle,
  learned,
  total,
}: CatalogHeaderProps) {
  const accent = CATALOG_ACCENT[module];
  const showProgress = learned != null && total != null;
  const greeting = CATALOG_GREETING[module];

  return (
    <header className="mb-lg">
      <div className="flex flex-wrap items-center gap-sm md:flex-nowrap md:gap-lg">
        <Mascot size={48} className="animate-ea-float shrink-0 md:h-14 md:w-14" />
        <div className="min-w-0 flex-1 basis-[calc(100%-3.5rem)] sm:basis-0">
          <h1 className="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-text-primary leading-tight">
            {title}
          </h1>
          {subtitle && (
            <p className="font-body-md text-body-md text-text-secondary mt-xs">{subtitle}</p>
          )}
        </div>
        {showProgress ? (
          <span
            className={cn(
              "order-3 inline-flex max-w-full items-center gap-1.5 rounded-full px-3 py-1.5 sm:order-none sm:shrink-0",
              "bg-ea-surface/90  font-duo font-extrabold text-[14px] text-text-primary",
              "",
            )}
          >
            <Icon name="auto_stories" filled className={cn("text-[16px]", accent.text)} />
            {`${learned}/${total} o'rganildi`}
          </span>
        ) : (
          <span
            className={cn(
              "order-3 inline-flex max-w-full items-center gap-1 rounded-full px-3 py-1.5 font-duo font-extrabold text-[14px] text-white sm:order-none sm:shrink-0",
              accent.bg,
            )}
          >
            <Icon name="star" filled className="text-[16px]" />
            {moduleLabel(module)}
          </span>
        )}
      </div>

      <div className="mt-md flex max-w-full items-start gap-sm rounded-[20px] bg-ea-surface/90 px-4 py-2   sm:inline-flex">
        <Icon name="pets" filled className={cn("mt-0.5 shrink-0 text-[18px]", accent.text)} />
        <span className="min-w-0 break-words font-body-md text-text-primary">{greeting}</span>
      </div>
    </header>
  );
}

function moduleLabel(module: CatalogModule): string {
  switch (module) {
    case "video":
      return "Video";
    case "reading":
      return "Reading";
    case "books":
      return "Books";
    case "grammar":
      return "Grammar";
    case "writing":
      return "Writing";
    case "listening":
      return "Listening";
  }
}
