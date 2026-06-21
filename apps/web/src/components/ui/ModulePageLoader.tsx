import { uz } from "@/content/uz";
import { LoadingSkeleton } from "./LoadingSkeleton";

export type ModuleLoaderAccent =
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

export function ModulePageLoader({
  label = uz.common.loading,
  embedded = false,
  className,
}: {
  icon: string;
  accent?: ModuleLoaderAccent;
  label?: string;
  embedded?: boolean;
  className?: string;
}) {
  return (
    <LoadingSkeleton variant={embedded ? "catalog" : "detail"} embedded={embedded} dataModuleLoader={embedded ? "embedded" : "page"} label={label} className={className} />
  );
}
