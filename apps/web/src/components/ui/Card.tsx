import { cn } from "@/lib/cn";
interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  as?: "div" | "section" | "article";
  interactive?: boolean;
  elevated?: boolean;
}

export function Card({
  as: Tag = "div",
  interactive,
  elevated,
  className,
  children,
  ...rest
}: CardProps) {
  return (
    <Tag
      className={cn(
        "ea-card ea-card--pad-md",
        elevated && "ea-card--elevated",
        interactive && "ea-card--interactive",
        className,
      )}
      {...rest}
    >
      {children}
    </Tag>
  );
}
