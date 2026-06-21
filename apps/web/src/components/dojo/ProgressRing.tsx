import { ProgressRing as BaseProgressRing } from "@/components/ui/ProgressRing";

interface DojoProgressRingProps {
  /** Score 0-100. */
  value: number;
  size?: number;
  children?: React.ReactNode;
}

/** Score ring for the pronunciation dojo: colors green/yellow/red by score band. */
export function ProgressRing({ value, size = 128, children }: DojoProgressRingProps) {
  const arcClassName =
    value >= 80 ? "text-ea-green-600" : value >= 50 ? "text-ea-orange-600" : "text-ea-danger";

  return (
    <BaseProgressRing
      value={value / 100}
      size={size}
      strokeWidth={10}
      trackClassName="text-surface-container"
      arcClassName={arcClassName}
    >
      {children}
    </BaseProgressRing>
  );
}
