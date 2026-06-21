import { cn } from "@/lib/cn";

interface ParrotBadge3DProps {
  size?: number;
  className?: string;
  tone?: "green" | "white";
}

export function ParrotBadge3D({ size = 92, className, tone = "green" }: ParrotBadge3DProps) {
  return (
    <div
      className={cn(
        "flex items-center justify-center rounded-full border bg-ea-surface p-2",
        tone === "green" ? "border-ea-primary" : "border-ea-border",
        className,
      )}
      style={{ width: size, height: size }}
    >
      <img
        src="/assets/parrot-mascot.png"
        alt=""
        aria-hidden
        className="h-full w-full rounded-full object-contain"
        decoding="async"
      />
    </div>
  );
}
