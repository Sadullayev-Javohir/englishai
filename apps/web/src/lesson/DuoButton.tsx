import type { ReactNode } from "react";
import { AppButton, type DesignTone } from "@/components/design";

type DuoColor = "green" | "blue" | "red" | "yellow" | "purple" | "orange" | "white";

const TONE: Record<DuoColor, DesignTone> = {
  green: "primary",
  blue: "performance",
  red: "danger",
  yellow: "standard",
  purple: "performance",
  orange: "standard",
  white: "standard",
};

interface DuoButtonProps {
  children: ReactNode;
  onClick?: () => void;
  color?: DuoColor;
  fullWidth?: boolean;
  disabled?: boolean;
  icon?: string;
  iconTrailing?: boolean;
  size?: "md" | "lg";
  className?: string;
  type?: "button" | "submit";
}

export function DuoButton({
  children,
  onClick,
  color = "green",
  fullWidth,
  disabled,
  icon,
  iconTrailing,
  size = "lg",
  className,
  type = "button",
}: DuoButtonProps) {
  return (
    <AppButton
      type={type}
      tone={TONE[color]}
      size={size}
      fullWidth={fullWidth}
      disabled={disabled}
      onClick={onClick}
      leadingIcon={iconTrailing ? undefined : icon}
      trailingIcon={iconTrailing ? icon : undefined}
      className={className}
    >
      {children}
    </AppButton>
  );
}
