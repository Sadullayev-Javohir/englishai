import { cn } from "@/lib/cn";
import { Icon } from "./Icon";

type Variant = "accent" | "primary" | "secondary" | "ghost" | "outline";
type Size = "sm" | "md" | "lg";

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  fullWidth?: boolean;
  loading?: boolean;
  icon?: string;
  /** Render the icon after the label instead of before (e.g. a trailing arrow). */
  iconTrailing?: boolean;
}

// Compatibility API for older callers. Visual behavior is owned by the same
// canonical `.ea-button` contract as `AppButton`.
const VARIANTS: Record<Variant, string> = {
  accent: "ea-button--primary",
  primary: "ea-button--primary",
  secondary: "ea-button--standard",
  outline: "ea-button--outline",
  ghost: "ea-button--ghost",
};

const SIZES: Record<Size, string> = {
  sm: "ea-button--sm",
  md: "ea-button--md",
  lg: "ea-button--lg",
};

const ICON_SIZE: Record<Size, string> = {
  sm: "text-[calc(var(--responsive-icon-size)-1px)]",
  md: "text-[var(--responsive-icon-size)]",
  lg: "text-[calc(var(--responsive-icon-size)+2px)]",
};

export function Button({
  variant = "primary",
  size = "md",
  fullWidth,
  loading,
  icon,
  iconTrailing,
  className,
  children,
  disabled,
  ...rest
}: ButtonProps) {
  const glyph = loading ? (
    <Icon name="progress_activity" className={cn("animate-spin", ICON_SIZE[size])} />
  ) : (
    icon && <Icon name={icon} className={ICON_SIZE[size]} />
  );

  return (
    <button
      className={cn(
        "ea-button",
        SIZES[size],
        VARIANTS[variant],
        fullWidth && "ea-button--full",
        className,
      )}
      disabled={disabled || loading}
      {...rest}
    >
      {!iconTrailing && glyph}
      {children}
      {iconTrailing && !loading && glyph}
    </button>
  );
}
