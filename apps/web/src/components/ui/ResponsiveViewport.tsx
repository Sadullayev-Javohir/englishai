import { cn } from "@/lib/cn";

type PageWidth = "narrow" | "content" | "wide" | "full";

const WIDTHS: Record<PageWidth, string> = {
  narrow: "max-w-3xl",
  content: "max-w-[1100px]",
  // The shell already spends up to 248px on the sidebar, so a 1440px cap never
  // bound below a 1688px viewport - the "wide" page was effectively unbounded.
  // 1180px is the real reading limit and matches `--ea-stage-lg`.
  wide: "max-w-[1180px]",
  full: "max-w-none",
};

interface ResponsiveViewportProps extends React.HTMLAttributes<HTMLDivElement> {
  width?: PageWidth;
  padded?: boolean;
}

/** Owns viewport height and safe-area behavior without imposing page width. */
export function ResponsiveViewport({ className, children, ...rest }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div className={cn("min-h-responsive-viewport w-full min-w-0", className)} {...rest}>
      {children}
    </div>
  );
}

/** Shared centered page boundary with responsive shell gutters. */
export function PageContainer({
  width = "content",
  padded = true,
  className,
  children,
  ...rest
}: ResponsiveViewportProps) {
  return (
    <div
      className={cn("mx-auto w-full min-w-0", WIDTHS[width], padded && "px-page-gutter", className)}
      {...rest}
    >
      {children}
    </div>
  );
}

/** How the stage fills the vertical space it was given. */
export type StageAlign = "focus" | "flow";
/** Which chrome the stage has to subtract to know its available height. */
export type StageFrame = "frame" | "shell" | "auto";
/** Canonical content widths - see `--ea-stage-*` in index.css. */
export type StageWidth = "sm" | "md" | "lg" | "full";

interface StageProps extends React.HTMLAttributes<HTMLDivElement> {
  align?: StageAlign;
  frame?: StageFrame;
  width?: StageWidth;
}

/**
 * The shared page body: centered horizontally always, centered vertically only
 * when the content is shorter than the space available.
 *
 * `align="focus"` for a screen that is one self-contained step - a dars qadami,
 * a result, an empty/error state, a login form. `align="flow"` (the default) for
 * anything that scrolls: catalogs, dashboards, long prose.
 *
 * Picking `focus` for a long page is not destructive - `.ea-stage` is built on
 * `min-height`, so a tall stage simply grows and the centering becomes a no-op
 * instead of clipping the top away. But say what the screen actually is.
 */
export function Stage({
  align = "flow",
  frame = "auto",
  width = "lg",
  className,
  children,
  ...rest
}: StageProps) {
  return (
    <div
      className={cn(
        "ea-stage",
        `ea-stage--${align}`,
        frame !== "auto" && `ea-stage--${frame}`,
        `ea-stage--${width}`,
        className
      )}
      {...rest}
    >
      {children}
    </div>
  );
}
