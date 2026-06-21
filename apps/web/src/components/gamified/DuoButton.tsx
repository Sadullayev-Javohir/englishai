import { AppButton, type DesignTone } from "@/components/design";

export type DuoColor =
  | "green"
  | "blue"
  | "purple"
  | "red"
  | "yellow"
  | "teal"
  | "orange"
  | "amber"
  | "pink"
  | "brown";

const TONE: Record<DuoColor, DesignTone> = {
  green: "primary",
  blue: "performance",
  purple: "performance",
  red: "danger",
  yellow: "standard",
  teal: "success",
  orange: "standard",
  amber: "standard",
  pink: "performance",
  brown: "dark",
};

interface DuoButtonProps {
  children: React.ReactNode;
  onClick?: () => void;
  color?: DuoColor;
  icon?: string;
  type?: "button" | "submit";
  fullWidth?: boolean;
  disabled?: boolean;
  className?: string;
}

export function DuoButton({
  children,
  onClick,
  color = "green",
  icon,
  type = "button",
  fullWidth,
  disabled,
  className,
}: DuoButtonProps) {
  return (
    <AppButton
      type={type}
      onClick={onClick}
      tone={TONE[color]}
      leadingIcon={icon}
      fullWidth={fullWidth}
      disabled={disabled}
      className={className}
    >
      {children}
    </AppButton>
  );
}
