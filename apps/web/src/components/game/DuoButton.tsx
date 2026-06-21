import type { ButtonHTMLAttributes } from "react";
import { AppButton, type DesignSize, type DesignTone } from "@/components/design";

export type DuoColor = "green" | "blue" | "red" | "yellow" | "purple" | "orange";

export interface DuoButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  color?: DuoColor;
  size?: "sm" | "md" | "lg";
  fullWidth?: boolean;
  icon?: string;
  iconTrailing?: boolean;
  loading?: boolean;
  children: React.ReactNode;
}

const TONE: Record<DuoColor, DesignTone> = {
  green: "primary",
  blue: "performance",
  red: "danger",
  yellow: "standard",
  purple: "performance",
  orange: "standard",
};

export function DuoButton({
  color = "green",
  size = "md",
  fullWidth,
  icon,
  iconTrailing,
  loading,
  children,
  ...rest
}: DuoButtonProps) {
  return (
    <AppButton
      tone={TONE[color]}
      size={size as DesignSize}
      fullWidth={fullWidth}
      loading={loading}
      leadingIcon={iconTrailing ? undefined : icon}
      trailingIcon={iconTrailing ? icon : undefined}
      {...rest}
    >
      {children}
    </AppButton>
  );
}
