import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import type { UsernameStatus } from "@/lib/useUsernameCheck";

/**
 * Reusable handle input with a live status line (checking / available / taken / invalid).
 * Used by the post-sign-up setup screen and the profile-edit form so the rules and feedback
 * stay identical. Presentational: the caller owns the value, the debounced status, and submit.
 */
export function UsernameField({
  value,
  onChange,
  status,
  autoFocus,
  id = "username",
  variant = "flat",
  dark = false,
}: {
  value: string;
  onChange: (value: string) => void;
  status: UsernameStatus;
  autoFocus?: boolean;
  id?: string;
  /** "flat" keeps profile edit stable; page-specific variants only change presentation. */
  variant?: "flat" | "duo" | "setup";
  dark?: boolean;
}) {
  const tone = statusTone(status);

  const wrapperClass = cn(
    "flex items-center gap-sm px-md transition-all",
    variant === "setup"
      ? cn(
          "username-field__control",
          tone === "error" && "username-field__control--error",
          tone === "ok" && "username-field__control--success",
          status.kind === "checking" && "username-field__control--checking",
        )
      : variant === "duo"
      ? dark
        ? "rounded-button border border-ea-purple-800 bg-ea-purple-800 font-duo focus-within:border-ea-purple-300 focus-within:ring-4 focus-within:ring-ea-purple-300/40"
        : "rounded-button border border-white/35 bg-ea-primary font-duo focus-within:border-white/70 focus-within:ring-4 focus-within:ring-white/40"
      : cn(
          "rounded-xl border bg-surface focus-within:border-primary",
          tone === "error" ? "border-error" : tone === "ok" ? "border-success" : "border-border",
        ),
  );

  const inputClass = cn(
    "flex-1 bg-transparent py-md outline-none placeholder:opacity-50",
    variant === "setup"
      ? "username-field__input"
      : variant === "duo"
      ? dark
        ? "font-duo text-body-md text-ea-surface-soft placeholder:text-ea-purple-200"
        : "font-duo text-body-md text-white placeholder:text-white/70"
      : "font-body-md text-body-md text-text-primary",
  );

  const atClass = cn(
    "select-none",
    variant === "duo" ? "font-duo text-body-md" : "font-body-md text-body-md",
    variant === "setup" ? "username-field__prefix" : dark ? "text-ea-purple-200" : variant === "duo" ? "text-white/85" : "text-text-secondary",
  );

  return (
    <div className={cn("space-y-xs", variant === "setup" && "username-field username-field--setup")}>
      <label
        htmlFor={id}
        className={cn(
          "block font-label-md text-label-md",
          variant === "setup" ? "username-field__label" : dark ? "text-ea-purple-200" : "text-text-secondary",
        )}
      >
        {uz.username.label}
      </label>
      <div className={wrapperClass}>
        <span className={atClass}>
          {variant === "duo" || variant === "setup" ? (
            <Icon name="at" className={dark ? "text-ea-green-200 text-[20px]" : "text-white text-[20px]"} />
          ) : (
            "@"
          )}
        </span>
        <input
          id={id}
          type="text"
          autoFocus={autoFocus}
          autoComplete="off"
          autoCapitalize="none"
          spellCheck={false}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={uz.username.placeholder.replace("masalan: ", "")}
          className={inputClass}
          aria-describedby={`${id}-status`}
          aria-invalid={tone === "error" || undefined}
        />
        <StatusIcon status={status} variant={variant} />
      </div>
      <StatusLine status={status} variant={variant} id={`${id}-status`} />
    </div>
  );
}

function StatusIcon({ status, variant }: { status: UsernameStatus; variant: "flat" | "duo" | "setup" }) {
  const setupClass = variant === "setup" ? "username-field__status-icon" : "";
  switch (status.kind) {
    case "checking":
      return <Icon name="progress_activity" className={cn("animate-spin text-text-secondary text-[20px]", setupClass)} />;
    case "available":
      return <Icon name="check_circle" filled className={cn("text-success text-[20px]", setupClass)} />;
    case "taken":
    case "invalid":
      return <Icon name="cancel" filled className={cn("text-error text-[20px]", setupClass)} />;
    default:
      return null;
  }
}

function StatusLine({ status, variant, id }: { status: UsernameStatus; variant: "flat" | "duo" | "setup"; id: string }) {
  const { text, tone } = statusMessage(status);
  return (
    <p
      id={id}
      aria-live="polite"
      className={cn(
        "font-caption text-caption min-h-[1.25rem]",
        variant === "setup" && "username-field__status",
        tone === "error" ? "text-error" : tone === "ok" ? "text-success" : "text-text-secondary",
      )}
    >
      {variant === "setup" && status.kind === "available"
        ? <><Icon name="check_circle" className="text-[18px]" /> Bu nom bo‘sh. Sizniki bo‘lsin!</>
        : text || uz.username.hint}
    </p>
  );
}

function statusMessage(status: UsernameStatus): { text: string; tone: "error" | "ok" | "muted" } {
  switch (status.kind) {
    case "checking":
      return { text: uz.username.checking, tone: "muted" };
    case "available":
      return { text: uz.username.available, tone: "ok" };
    case "taken":
      return { text: uz.username.taken, tone: "error" };
    case "invalid":
      return { text: status.message, tone: "error" };
    case "error":
      return { text: uz.username.saveError, tone: "error" };
    default:
      return { text: "", tone: "muted" };
  }
}

function statusTone(status: UsernameStatus): "error" | "ok" | "muted" {
  return statusMessage(status).tone;
}
