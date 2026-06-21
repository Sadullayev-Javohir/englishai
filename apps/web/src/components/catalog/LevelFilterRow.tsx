import { cn } from "@/lib/cn";
import { CefrLevel } from "@/api/types";
import { cefrShort } from "@/lib/labels";

const LEVELS: CefrLevel[] = [
  CefrLevel.A1,
  CefrLevel.A2,
  CefrLevel.B1,
  CefrLevel.B2,
  CefrLevel.C1,
  CefrLevel.C2,
];

export type LevelFilter = CefrLevel | "all" | null;

interface LevelFilterRowProps {
  /** Current filter (null = learner's own level, adaptive). */
  value: LevelFilter;
  /** Level label, e.g. "Daraja:". */
  label: string;
  /** "Hammasi" (all levels) label. */
  allLabel: string;
  /** The learner's own level - lights up the matching tab when no level is pinned. */
  learnerLevel?: CefrLevel | null;
  onChange: (next: LevelFilter) => void;
}

/**
 * 3D pill filter row (spec §3): level A1–C2 + "Hammasi". Active pill lifts with a chunky
 * green lip; inactive pills are flat frosted white. Horizontal scroll on phones, wrap from md.
 */
export function LevelFilterRow({
  value,
  label,
  allLabel,
  learnerLevel,
  onChange,
}: LevelFilterRowProps) {
  const isActive = (lvl: LevelFilter) => {
    if (lvl === "all") return value === "all";
    // On the default adaptive view the learner's own level reads as active.
    return value === lvl || (value === null && learnerLevel === lvl);
  };

  return (
    <div className="mb-lg flex items-center gap-xs overflow-x-auto md:flex-wrap [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
      <span className="font-label-md text-label-md text-text-secondary mr-xs shrink-0">{label}</span>
      <FilterPill active={isActive("all")} onClick={() => onChange("all")}>
        {allLabel}
      </FilterPill>
      {LEVELS.map((lvl) => (
        <FilterPill key={lvl} active={isActive(lvl)} onClick={() => onChange(lvl)}>
          {cefrShort(lvl)}
        </FilterPill>
      ))}
    </div>
  );
}

interface FilterPillProps {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
  className?: string;
}

function FilterPill({ active, onClick, children, className }: FilterPillProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={cn(
        "min-h-11 shrink-0 select-none rounded-full px-md py-xs font-duo font-extrabold text-[14px] transition-all",
        active
          ? "bg-ea-green-600 text-white"
          : "bg-ea-surface/90 text-text-secondary border border-border hover:bg-ea-surface ",
        className,
      )}
    >
      {children}
    </button>
  );
}
