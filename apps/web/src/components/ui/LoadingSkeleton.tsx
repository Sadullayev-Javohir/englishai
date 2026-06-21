import { useLayoutEffect } from "react";
import { cn } from "@/lib/cn";
import { acquireRouteLoadingLock } from "@/lib/routeLoadingLock";

export type LoadingSkeletonVariant = "page" | "catalog" | "detail" | "dashboard" | "table" | "list" | "form" | "inline";

interface LoadingSkeletonProps {
  variant?: LoadingSkeletonVariant;
  rows?: number;
  cards?: number;
  className?: string;
  label?: string;
  embedded?: boolean;
  dataModuleLoader?: "embedded" | "page";
}

function Blocks({ count, className }: { count: number; className: string }) {
  return <>{Array.from({ length: count }, (_, index) => <span className={className} key={index} />)}</>;
}

export function LoadingSkeleton({
  variant = "page",
  rows = 5,
  cards = 6,
  className,
  label = "Yuklanmoqda",
  embedded = false,
  dataModuleLoader,
}: LoadingSkeletonProps) {
  useLayoutEffect(() => {
    if (variant === "inline" || embedded) return;
    return acquireRouteLoadingLock();
  }, [embedded, variant]);

  if (variant === "inline") {
    return <span className={cn("ea-skeleton-inline", className)} role="status" aria-label={label} aria-busy="true"><span /></span>;
  }

  return (
    <section
      className={cn("ea-loading-skeleton", `ea-loading-skeleton--${variant}`, embedded && "is-embedded", className)}
      role="status"
      aria-label={label}
      aria-busy="true"
      aria-live="polite"
      data-module-loader={dataModuleLoader}
    >
      <span className="sr-only">{label}</span>
      <div className="ea-loading-skeleton__heading" aria-hidden="true">
        <span className="ea-skeleton-block ea-loading-skeleton__eyebrow" />
        <span className="ea-skeleton-block ea-loading-skeleton__title" />
        <span className="ea-skeleton-block ea-loading-skeleton__subtitle" />
      </div>
      {variant === "dashboard" ? (
        <>
          <div className="ea-loading-skeleton__stats" aria-hidden="true"><Blocks count={4} className="ea-skeleton-block ea-loading-skeleton__stat" /></div>
          <div className="ea-loading-skeleton__dashboard" aria-hidden="true"><span className="ea-skeleton-block ea-loading-skeleton__chart" /><div className="ea-loading-skeleton__stack"><Blocks count={3} className="ea-skeleton-block ea-loading-skeleton__stack-card" /></div></div>
        </>
      ) : variant === "table" ? (
        <div className="ea-loading-skeleton__table" aria-hidden="true"><span className="ea-skeleton-block ea-loading-skeleton__table-head" /><Blocks count={rows} className="ea-skeleton-block ea-loading-skeleton__table-row" /></div>
      ) : variant === "detail" || variant === "form" ? (
        <div className="ea-loading-skeleton__detail" aria-hidden="true"><span className="ea-skeleton-block ea-loading-skeleton__media" /><div className="ea-loading-skeleton__lines"><Blocks count={rows} className="ea-skeleton-block ea-loading-skeleton__line" /></div></div>
      ) : variant === "list" ? (
        <div className="ea-loading-skeleton__list" aria-hidden="true"><Blocks count={rows} className="ea-skeleton-block ea-loading-skeleton__list-row" /></div>
      ) : (
        <div className="ea-loading-skeleton__grid" aria-hidden="true"><Blocks count={cards} className="ea-skeleton-block ea-loading-skeleton__card" /></div>
      )}
    </section>
  );
}
