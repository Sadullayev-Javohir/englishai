import { DesignCard } from "@/components/design";
import { cn } from "@/lib/cn";

export type ProgressAccent =
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

const ACCENT_ICON: Record<ProgressAccent, string> = {
  green: "bg-ea-green-50 text-ea-green-700",
  blue: "bg-ea-blue-50 text-ea-blue-700",
  purple: "bg-ea-purple-50 text-ea-purple-700",
  red: "bg-ea-danger-50 text-ea-danger-700",
  yellow: "bg-ea-yellow-50 text-ea-yellow-800",
  teal: "bg-ea-green-50 text-ea-green-700",
  orange: "bg-ea-orange-50 text-ea-orange-700",
  amber: "bg-ea-yellow-50 text-ea-yellow-800",
  pink: "bg-ea-purple-50 text-ea-purple-700",
  brown: "bg-ea-surface-soft text-ea-text",
};

export function ProgressSurface3D({
  accent,
  children,
  className,
  as = "article",
  interactive = false,
  delay = 0,
  onClick,
}: {
  accent: ProgressAccent;
  children: React.ReactNode;
  className?: string;
  as?: "article" | "section" | "div" | "button";
  interactive?: boolean;
  delay?: number;
  onClick?: () => void;
}) {
  void delay;
  void accent;
  const canInteract = interactive || Boolean(onClick);
  return (
    <DesignCard
      as={as}
      type={as === "button" ? "button" : undefined}
      tone="standard"
      interactive={canInteract}
      onClick={onClick}
      className={cn(
        "min-w-0 text-left",
        canInteract && "cursor-pointer focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-ea-primary/30",
        className,
      )}
    >
      {children}
    </DesignCard>
  );
}

export function ProgressIcon3D({ icon, accent = "green", className }: { icon: React.ReactNode; accent?: ProgressAccent; className?: string }) {
  return (
    <span className={cn("flex h-11 w-11 shrink-0 items-center justify-center rounded-[14px] border border-ea-border md:h-12 md:w-12", ACCENT_ICON[accent], className)}>
      {icon}
    </span>
  );
}

export function ProgressCardDeck({ children, className }: { children: React.ReactNode; className?: string }) {
  return <DesignCard tone="standard" className={className}>{children}</DesignCard>;
}

export function ProgressMiniCard3D({ accent, children, className }: { accent: ProgressAccent; children: React.ReactNode; className?: string }) {
  void accent;
  return <DesignCard tone="standard" padding="sm" className={className}>{children}</DesignCard>;
}
