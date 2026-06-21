import { DesignCard, type DesignTone } from "@/components/design";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";

export type ExerciseState = "idle" | "correct" | "wrong" | "dim";
export type ExerciseAccent = "blue" | "purple" | "orange" | "teal";

export interface ExerciseOptionProps {
  label: React.ReactNode;
  state?: ExerciseState;
  accent?: ExerciseAccent;
  selected?: boolean;
  disabled?: boolean;
  onSelect?: () => void;
  feedbackIcon?: "check" | "close";
  className?: string;
}

const STATE_TONE: Record<ExerciseState, DesignTone> = {
  idle: "standard",
  correct: "success",
  wrong: "danger",
  dim: "standard",
};

export function ExerciseOption({
  label,
  state = "idle",
  selected,
  disabled,
  onSelect,
  feedbackIcon,
  className,
}: ExerciseOptionProps) {
  return (
    <DesignCard
      as="button"
      type="button"
      tone={STATE_TONE[state]}
      padding="sm"
      interactive={!disabled}
      disabled={disabled}
      onClick={onSelect}
      className={cn(
        "group flex w-full items-center gap-3 text-left focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ea-primary/25",
        state === "dim" && "opacity-55",
        selected && "ring-2 ring-ea-primary ring-offset-2",
        className,
      )}
    >
      <span className="flex-1">{label}</span>
      {feedbackIcon && <Icon name={feedbackIcon} filled className="text-[22px]" />}
    </DesignCard>
  );
}
