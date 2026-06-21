import { CefrLevel } from "@/api/types";
import { DesignCard } from "@/components/design";
import { Icon } from "@/components/ui/Icon";
import { cefrShort } from "@/lib/labels";
import { cn } from "@/lib/cn";

export type RoleBoard =
  | "board1" | "board2" | "board3" | "board4" | "board5"
  | "board6" | "board7" | "board8" | "board9" | "board10";

export const ROLE_BOARD_STYLE: Record<RoleBoard, { face: string; lip: string }> = {
  board1: { face: "", lip: "" },
  board2: { face: "", lip: "" },
  board3: { face: "", lip: "" },
  board4: { face: "", lip: "" },
  board5: { face: "", lip: "" },
  board6: { face: "", lip: "" },
  board7: { face: "", lip: "" },
  board8: { face: "", lip: "" },
  board9: { face: "", lip: "" },
  board10: { face: "", lip: "" },
};

const BOARD_CYCLE: RoleBoard[] = [
  "board1", "board2", "board3", "board4", "board5",
  "board6", "board7", "board8", "board9", "board10",
];
export const roleBoardFor = (index: number): RoleBoard => BOARD_CYCLE[index % BOARD_CYCLE.length];

export interface RoleCard3DProps {
  title: string;
  subtitle?: string;
  level?: CefrLevel;
  board?: RoleBoard;
  icon: string;
  onClick: () => void;
  index?: number;
  className?: string;
  cover?: React.ReactNode;
}

export function RoleCard3D({
  title,
  subtitle,
  level,
  icon,
  onClick,
  className,
  cover,
}: RoleCard3DProps) {
  return (
    <DesignCard
      as="button"
      type="button"
      padding="none"
      interactive
      onClick={onClick}
      className={cn("group flex min-h-0 w-full flex-col items-stretch overflow-hidden text-left", className)}
    >
      {cover && <div className="relative aspect-[16/9] w-full overflow-hidden bg-ea-surface-soft">{cover}</div>}
      <div className="flex items-center gap-3 px-4 pt-4">
        {!cover && (
          <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-ea-purple-50 text-ea-purple-600">
            <Icon name={icon} className="text-[26px]" />
          </span>
        )}
        <h3 className="min-w-0 flex-1 font-duo text-[16px] font-extrabold leading-tight text-ea-text line-clamp-2">{title}</h3>
      </div>
      <div className="flex flex-1 flex-col gap-2 px-4 pb-4 pt-3">
        {subtitle && <p className="font-caption text-caption text-ea-muted line-clamp-2">{subtitle}</p>}
        <div className="mt-auto flex items-center justify-between pt-1">
          {level != null && (
            <span className="inline-flex items-center gap-1 rounded-full bg-ea-purple-50 px-2.5 py-1 font-duo text-caption font-bold text-ea-purple-600">
              <Icon name="star" className="text-[13px]" />
              {cefrShort(level)}
            </span>
          )}
          <Icon name="arrow_forward" className="text-[18px] text-ea-muted" />
        </div>
      </div>
    </DesignCard>
  );
}
