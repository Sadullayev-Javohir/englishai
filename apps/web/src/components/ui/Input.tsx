import { forwardRef } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./Icon";

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  /** Optional leading icon name (Material-Symbols name, per the Icon component). */
  icon?: string;
  invalid?: boolean;
}

/**
 * Compatibility wrapper over the canonical design-system form control.
 */
export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { icon, invalid, className, ...rest },
  ref,
) {
  return (
    <div className="relative w-full">
      {icon && (
        <Icon
          name={icon}
          className="pointer-events-none absolute left-md top-1/2 z-10 -translate-y-1/2 text-[var(--responsive-icon-size)] text-text-muted"
        />
      )}
      <input
        ref={ref}
        className={cn(
          "ea-form-control",
          invalid && "ea-form-control--invalid",
          icon && "pl-[44px]",
          className,
        )}
        aria-invalid={invalid || undefined}
        {...rest}
      />
    </div>
  );
});
