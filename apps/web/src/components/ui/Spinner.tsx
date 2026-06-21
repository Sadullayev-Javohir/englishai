import { uz } from "@/content/uz";
import { LoadingSkeleton } from "./LoadingSkeleton";

/** Centered loading state used while a screen fetches its data. */
export function Spinner({
  label = uz.common.loading,
  className,
}: {
  label?: string;
  className?: string;
}) {
  return (
    <LoadingSkeleton variant="inline" label={label} className={className} />
  );
}
