import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import { useLessonGuidancePreference } from "@/lib/lessonGuidance";

type LessonStageMode = "focus" | "flow" | "practice" | "fullscreen";
type LessonStageWidth = "sm" | "md" | "lg" | "full";

interface LessonStageFrameProps extends React.HTMLAttributes<HTMLDivElement> {
  header?: React.ReactNode;
  footer?: React.ReactNode;
  topRightAction?: React.ReactNode;
  mode?: LessonStageMode;
  width?: LessonStageWidth;
  bodyClassName?: string;
  headerClassName?: string;
  footerClassName?: string;
}

export function LessonStageFrame({
  header,
  footer,
  topRightAction,
  mode = "focus",
  width = "md",
  className,
  bodyClassName,
  headerClassName,
  footerClassName,
  children,
  ...rest
}: LessonStageFrameProps) {
  return (
    <div
      className={cn(
        "ea-lesson-stage",
        `ea-lesson-stage--${mode}`,
        `ea-lesson-stage--${width}`,
        className,
      )}
      {...rest}
    >
      {topRightAction != null && (
        <div className="ea-lesson-stage__top-right-action" data-lesson-stage-top-right-action>
          {topRightAction}
        </div>
      )}
      {header != null && (
        <header className={cn("ea-lesson-stage__header", headerClassName)} data-lesson-stage-header>
          {header}
        </header>
      )}
      <main className={cn("ea-lesson-stage__body", bodyClassName)} data-lesson-stage-body>
        {children}
      </main>
      {footer != null && (
        <footer className={cn("ea-lesson-stage__footer", footerClassName)} data-lesson-stage-footer>
          {footer}
        </footer>
      )}
    </div>
  );
}

interface LessonFeedbackSlotProps extends React.HTMLAttributes<HTMLDivElement> {
  compact?: boolean;
}

export function LessonFeedbackSlot({ compact = false, className, children, ...rest }: LessonFeedbackSlotProps) {
  return (
    <div
      className={cn("ea-lesson-feedback-slot", compact && "ea-lesson-feedback-slot--compact", className)}
      {...rest}
    >
      {children}
    </div>
  );
}

interface LessonGuidanceProps extends React.HTMLAttributes<HTMLElement> {
  title: string;
  items: readonly string[];
  icon?: string;
}

export function LessonGuidance({
  title,
  items,
  icon = "tips_and_updates",
  className,
  ...rest
}: LessonGuidanceProps) {
  const { visible, setVisible } = useLessonGuidancePreference();
  if (items.length === 0) return null;

  if (!visible) {
    return (
      <div className={cn("ea-lesson-guidance-toggle-row", className)}>
        <button
          type="button"
          className="ea-lesson-guidance-toggle"
          aria-label={uz.lessonGuidance.open}
          onClick={() => setVisible(true)}
        >
          <Icon name="tips_and_updates" filled />
          <span>{uz.lessonGuidance.open}</span>
        </button>
      </div>
    );
  }

  return (
    <aside
      className={cn(
        "ea-lesson-guidance",
        className,
      )}
      aria-label={title}
      {...rest}
    >
      <span className="ea-lesson-guidance__icon" aria-hidden="true">
        <Icon name={icon} filled />
      </span>
      <div className="ea-lesson-guidance__content">
        <div className="ea-lesson-guidance__heading">
          <h2 className="ea-lesson-guidance__title">{title}</h2>
          <button
            type="button"
            className="ea-lesson-guidance__close"
            aria-label={uz.lessonGuidance.close}
            onClick={() => setVisible(false)}
          >
            <Icon name="close" />
          </button>
        </div>
        <ul className="ea-lesson-guidance__list">
          {items.map((item) => <li key={item}>{item}</li>)}
        </ul>
      </div>
    </aside>
  );
}
