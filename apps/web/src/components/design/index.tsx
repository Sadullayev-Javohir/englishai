import { createPortal } from "react-dom";
import { forwardRef, useEffect, useId, useRef } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import "./overlays.responsive.css";

export type DesignTone = "standard" | "primary" | "performance" | "dark" | "success" | "danger";
export type DesignSize = "sm" | "md" | "lg";

const BUTTON_TONES: Record<DesignTone, string> = {
  standard: "ea-button--standard",
  primary: "ea-button--primary",
  performance: "ea-button--performance",
  dark: "ea-button--dark",
  success: "ea-button--success",
  danger: "ea-button--danger",
};

export interface AppButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  tone?: DesignTone;
  size?: DesignSize;
  fullWidth?: boolean;
  loading?: boolean;
  leadingIcon?: string;
  trailingIcon?: string;
}

export function AppButton({
  tone = "primary",
  size = "md",
  fullWidth,
  loading,
  leadingIcon,
  trailingIcon,
  className,
  children,
  disabled,
  ...rest
}: AppButtonProps) {
  return (
    <button
      className={cn("ea-button", `ea-button--${size}`, BUTTON_TONES[tone], fullWidth && "ea-button--full", className)}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...rest}
    >
      {loading ? <Icon name="progress_activity" className="ea-button__spinner" /> : leadingIcon ? <Icon name={leadingIcon} /> : null}
      <span>{children}</span>
      {!loading && trailingIcon ? <Icon name={trailingIcon} /> : null}
    </button>
  );
}

export interface AppIconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  icon: string;
  label: string;
  tone?: DesignTone;
  size?: DesignSize;
  loading?: boolean;
}

export function AppIconButton({ icon, label, tone = "standard", size = "md", loading, className, disabled, ...rest }: AppIconButtonProps) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      aria-busy={loading || undefined}
      disabled={disabled || loading}
      className={cn("ea-icon-button", `ea-icon-button--${size}`, `ea-icon-button--${tone}`, className)}
      {...rest}
    >
      <Icon name={loading ? "progress_activity" : icon} className={loading ? "ea-button__spinner" : undefined} />
    </button>
  );
}

export interface DesignCardProps extends React.HTMLAttributes<HTMLElement> {
  as?: "div" | "section" | "article" | "button";
  tone?: DesignTone;
  padding?: "none" | DesignSize;
  interactive?: boolean;
  disabled?: boolean;
  type?: "button" | "submit" | "reset";
}

export function DesignCard({ as: Tag = "div", tone = "standard", padding = "md", interactive, className, children, ...rest }: DesignCardProps) {
  return (
    <Tag className={cn("ea-card", `ea-card--${tone}`, `ea-card--pad-${padding}`, interactive && "ea-card--interactive", className)} {...rest}>
      {children}
    </Tag>
  );
}

export interface DesignImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  fit?: "cover" | "contain";
}

export function DesignImage({ fit = "cover", className, alt, ...rest }: DesignImageProps) {
  return <img className={cn("responsive-media", fit === "contain" && "object-contain", className)} alt={alt} {...rest} />;
}

export type DesignTextTone = "title" | "heading" | "body" | "label" | "caption";

export interface DesignTextProps extends React.HTMLAttributes<HTMLElement> {
  as?: "h1" | "h2" | "h3" | "p" | "span" | "strong";
  tone?: DesignTextTone;
}

const TEXT_CLASSES: Record<DesignTextTone, string> = {
  title: "text-responsive-title",
  heading: "text-responsive-heading",
  body: "text-responsive-body",
  label: "text-responsive-label",
  caption: "text-responsive-caption",
};

export function DesignText({ as: Tag = "p", tone = "body", className, children, ...rest }: DesignTextProps) {
  return <Tag className={cn(TEXT_CLASSES[tone], className)} {...rest}>{children}</Tag>;
}

interface HeaderProps extends React.HTMLAttributes<HTMLElement> {
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: React.ReactNode;
}

export function PageHeader({ eyebrow, title, description, actions, className, ...rest }: HeaderProps) {
  return (
    <header className={cn("ea-page-header", className)} {...rest}>
      <div className="ea-page-header__copy">
        {eyebrow && <span className="ea-eyebrow">{eyebrow}</span>}
        <h1>{title}</h1>
        {description && <p>{description}</p>}
      </div>
      {actions && <div className="ea-page-header__actions">{actions}</div>}
    </header>
  );
}

export function SectionHeader({ eyebrow, title, description, actions, className, ...rest }: HeaderProps) {
  return (
    <header className={cn("ea-section-header", className)} {...rest}>
      <div>
        {eyebrow && <span className="ea-eyebrow">{eyebrow}</span>}
        <h2>{title}</h2>
        {description && <p>{description}</p>}
      </div>
      {actions && <div className="ea-section-header__actions">{actions}</div>}
    </header>
  );
}

interface StatCardProps extends Omit<DesignCardProps, "children"> {
  label: string;
  value: React.ReactNode;
  icon?: string;
  detail?: React.ReactNode;
}

export function StatCard({ label, value, icon, detail, tone = "standard", className, ...rest }: StatCardProps) {
  return (
    <DesignCard tone={tone} className={cn("ea-stat-card", className)} {...rest}>
      <div className="ea-stat-card__head">
        <span>{label}</span>
        {icon && <span className="ea-stat-card__icon"><Icon name={icon} /></span>}
      </div>
      <strong>{value}</strong>
      {detail && <div className="ea-stat-card__detail">{detail}</div>}
    </DesignCard>
  );
}

interface ChartCardProps extends DesignCardProps {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  chartLabel: string;
  children: React.ReactNode;
}

export function ChartCard({ title, description, actions, chartLabel, children, tone = "performance", className, ...rest }: ChartCardProps) {
  return (
    <DesignCard tone={tone} className={cn("ea-chart-card", className)} {...rest}>
      <SectionHeader title={title} description={description} actions={actions} />
      <div className="ea-chart-card__canvas" role="img" aria-label={chartLabel}>{children}</div>
    </DesignCard>
  );
}

const FOCUSABLE_SELECTOR = [
  "a[href]",
  "button:not([disabled])",
  "input:not([disabled])",
  "select:not([disabled])",
  "textarea:not([disabled])",
  "[tabindex]:not([tabindex='-1'])",
].join(",");

let overlayCount = 0;
let previousBodyOverflow = "";

export interface OverlayProps {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  className?: string;
  closeLabel?: string;
  closeOnBackdrop?: boolean;
  closeOnEscape?: boolean;
  showClose?: boolean;
  initialFocusRef?: React.RefObject<HTMLElement | null>;
  panelProps?: React.HTMLAttributes<HTMLElement>;
  portalTarget?: Element | DocumentFragment | null;
}

function useOverlay({ open, onClose, closeOnEscape = true, initialFocusRef }: OverlayProps, panelRef: React.RefObject<HTMLElement>) {
  const returnFocusRef = useRef<HTMLElement | null>(null);
  const onCloseRef = useRef(onClose);

  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    if (!open) return;
    if (!returnFocusRef.current) returnFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    if (overlayCount === 0) {
      previousBodyOverflow = document.body.style.overflow;
      document.body.style.overflow = "hidden";
    }
    overlayCount += 1;

    const focusPanel = () => {
      const panel = panelRef.current;
      if (!panel) return;
      const target = initialFocusRef?.current ?? panel.querySelector<HTMLElement>("[autofocus]") ?? panel.querySelector<HTMLElement>(FOCUSABLE_SELECTOR) ?? panel;
      target.focus({ preventScroll: true });
    };
    const frame = window.requestAnimationFrame(focusPanel);
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && closeOnEscape) {
        event.preventDefault();
        onCloseRef.current();
        return;
      }
      if (event.key !== "Tab") return;
      const panel = panelRef.current;
      if (!panel) return;
      const focusable = Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)).filter((element) => !element.hidden && element.getAttribute("aria-hidden") !== "true");
      if (focusable.length === 0) {
        event.preventDefault();
        panel.focus();
        return;
      }
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && (document.activeElement === first || document.activeElement === panel)) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => {
      window.cancelAnimationFrame(frame);
      document.removeEventListener("keydown", onKeyDown);
      overlayCount = Math.max(0, overlayCount - 1);
      if (overlayCount === 0) document.body.style.overflow = previousBodyOverflow;
      const returnTarget = returnFocusRef.current;
      window.requestAnimationFrame(() => returnTarget?.focus({ preventScroll: true }));
    };
  }, [closeOnEscape, initialFocusRef, open, panelRef]);
}

function OverlayContent({ title, description, children, footer, className, closeLabel = "Yopish", onClose, closeOnBackdrop = true, showClose = true, panelProps, sheet, panelRef }: OverlayProps & { sheet?: boolean; panelRef: React.RefObject<HTMLElement> }) {
  const titleId = useId();
  const descriptionId = useId();
  return (
    <div className={cn("ea-overlay", "ea-responsive-overlay", sheet ? "ea-overlay--sheet" : "ea-overlay--modal")}>
      <button className="ea-overlay__backdrop" type="button" aria-label={closeLabel} onClick={closeOnBackdrop ? onClose : undefined} tabIndex={closeOnBackdrop ? 0 : -1} />
      <section
        {...panelProps}
        ref={panelRef}
        className={cn("ea-overlay__panel", className, panelProps?.className)}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        tabIndex={-1}
      >
        {sheet && <span className="ea-overlay__handle" aria-hidden="true" />}
        <header className="ea-overlay__header">
          <div>
            <h2 id={titleId}>{title}</h2>
            {description && <p id={descriptionId}>{description}</p>}
          </div>
          {showClose && <AppIconButton icon="close" label={closeLabel} onClick={onClose} />}
        </header>
        <div className="ea-overlay__body">{children}</div>
        {footer && <footer className="ea-overlay__footer">{footer}</footer>}
      </section>
    </div>
  );
}

export function DesignModal(props: OverlayProps) {
  const panelRef = useRef<HTMLElement>(null);
  useOverlay(props, panelRef);
  if (!props.open || typeof document === "undefined") return null;
  return createPortal(<OverlayContent {...props} panelRef={panelRef} />, props.portalTarget ?? document.body);
}

export function DesignSheet(props: OverlayProps) {
  const panelRef = useRef<HTMLElement>(null);
  useOverlay(props, panelRef);
  if (!props.open || typeof document === "undefined") return null;
  return createPortal(<OverlayContent {...props} sheet panelRef={panelRef} />, props.portalTarget ?? document.body);
}

interface DesignConfirmProps extends Omit<OverlayProps, "children" | "footer"> {
  message: React.ReactNode;
  confirmLabel: string;
  cancelLabel?: string;
  destructive?: boolean;
  loading?: boolean;
  onConfirm: () => void;
}

export function DesignConfirm({ message, confirmLabel, cancelLabel = "Bekor qilish", destructive, loading, onConfirm, ...props }: DesignConfirmProps) {
  return (
    <DesignModal
      {...props}
      footer={
        <>
          <AppButton size="lg" className="ea-confirm__button" tone="standard" onClick={props.onClose}>{cancelLabel}</AppButton>
          <AppButton size="lg" className="ea-confirm__button" tone={destructive ? "danger" : "primary"} loading={loading} onClick={onConfirm}>{confirmLabel}</AppButton>
        </>
      }
    >
      <div className="ea-confirm__message">{message}</div>
    </DesignModal>
  );
}

interface DesignToastProps extends React.HTMLAttributes<HTMLDivElement> {
  open: boolean;
  title: string;
  message?: string;
  tone?: "standard" | "success" | "danger";
  onClose?: () => void;
  closeLabel?: string;
}

export function DesignToast({ open, title, message, tone = "standard", onClose, closeLabel = "Yopish", className, ...rest }: DesignToastProps) {
  if (!open || typeof document === "undefined") return null;
  return createPortal(
    <div className="ea-toast-region ea-responsive-toast-region" aria-live={tone === "danger" ? "assertive" : "polite"} aria-atomic="true">
      <div className={cn("ea-toast", `ea-toast--${tone}`, className)} role={tone === "danger" ? "alert" : "status"} {...rest}>
        <Icon name={tone === "success" ? "check_circle" : tone === "danger" ? "error" : "info"} />
        <div><strong>{title}</strong>{message && <p>{message}</p>}</div>
        {onClose && <AppIconButton icon="close" label={closeLabel} size="sm" onClick={onClose} />}
      </div>
    </div>,
    document.body,
  );
}

export function DesignTooltip({ content, children }: { content: React.ReactNode; children: React.ReactElement }) {
  const id = useId();
  return <span className="ea-tooltip" aria-describedby={id}>{children}<span id={id} role="tooltip" className="ea-tooltip__content">{content}</span></span>;
}

interface FormFieldProps {
  label: string;
  htmlFor: string;
  help?: string;
  error?: string;
  required?: boolean;
  children: React.ReactNode;
  className?: string;
}

export function FormField({ label, htmlFor, help, error, required, children, className }: FormFieldProps) {
  return (
    <div className={cn("ea-field", error && "ea-field--error", className)}>
      <label htmlFor={htmlFor}>{label}{required && <span aria-hidden="true"> *</span>}</label>
      {children}
      {(error || help) && <p className="ea-field__message" id={`${htmlFor}-message`}><Icon name={error ? "error" : "info"} />{error || help}</p>}
    </div>
  );
}

export const formControlClass = "ea-form-control";

export const DesignInput = forwardRef<HTMLInputElement, React.InputHTMLAttributes<HTMLInputElement>>(function DesignInput(props, ref) {
  return <input {...props} ref={ref} className={cn(formControlClass, props.className)} />;
});

export const DesignTextarea = forwardRef<HTMLTextAreaElement, React.TextareaHTMLAttributes<HTMLTextAreaElement>>(function DesignTextarea(props, ref) {
  return <textarea {...props} ref={ref} className={cn(formControlClass, "ea-form-control--textarea", props.className)} />;
});

export const DesignSelect = forwardRef<HTMLSelectElement, React.SelectHTMLAttributes<HTMLSelectElement>>(function DesignSelect(props, ref) {
  return <select {...props} ref={ref} className={cn(formControlClass, props.className)} />;
});

interface ChoiceProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: React.ReactNode;
}

export function DesignCheckbox({ label, className, ...rest }: ChoiceProps) {
  return <label className={cn("ea-choice", className)}><input type="checkbox" {...rest} /><span>{label}</span></label>;
}

export function DesignRadio({ label, className, ...rest }: ChoiceProps) {
  return <label className={cn("ea-choice", className)}><input type="radio" {...rest} /><span>{label}</span></label>;
}

interface StateProps extends React.HTMLAttributes<HTMLDivElement> {
  title?: string;
  description?: string;
  icon?: string;
  action?: React.ReactNode;
}

export function DesignState({ title, description, icon, action, className, children, ...rest }: StateProps) {
  return (
    <div className={cn("ea-state", className)} {...rest}>
      {children || (icon && <span className="ea-state__icon"><Icon name={icon} /></span>)}
      {title && <h3>{title}</h3>}
      {description && <p>{description}</p>}
      {action && <div className="ea-state__action">{action}</div>}
    </div>
  );
}

export function LoadingState({ title = "Yuklanmoqda", description, className, ...rest }: StateProps) {
  return <LoadingSkeleton variant="list" rows={3} label={description || title} className={className} {...rest} />;
}

export function EmptyState({ title, description, icon = "info", action, className, ...rest }: StateProps & { title: string }) {
  return <DesignState title={title} description={description} icon={icon} action={action} className={className} {...rest} />;
}

export function ErrorState({ title = "Xatolik yuz berdi", description, action, className, ...rest }: StateProps) {
  return <DesignState role="alert" title={title} description={description} icon="error" action={action} className={cn("ea-state--error", className)} {...rest} />;
}

interface ResponsiveGridProps extends React.HTMLAttributes<HTMLDivElement> {
  minItemWidth?: number;
}

export function ResponsiveGrid({ minItemWidth = 240, className, style, ...rest }: ResponsiveGridProps) {
  return <div className={cn("ea-grid", className)} style={{ "--ea-grid-min": `${minItemWidth}px`, ...style } as React.CSSProperties} {...rest} />;
}
