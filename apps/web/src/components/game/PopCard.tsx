import { DesignCard, type DesignTone } from "@/components/design";

interface PopCardProps extends React.HTMLAttributes<HTMLDivElement> {
  frosted?: boolean;
  dark?: boolean;
  interactive?: boolean;
  as?: "div" | "section" | "article";
}

export type { PopCardProps };

export function PopCard({
  frosted = false,
  dark = false,
  interactive = true,
  as = "div",
  className,
  children,
  ...rest
}: PopCardProps) {
  const tone: DesignTone = dark ? "dark" : frosted ? "performance" : "standard";
  return (
    <DesignCard
      as={as}
      tone={tone}
      interactive={interactive}
      className={className}
      {...rest}
    >
      {children}
    </DesignCard>
  );
}
