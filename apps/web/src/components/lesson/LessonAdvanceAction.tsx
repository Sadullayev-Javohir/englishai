import { DuoButton } from "@/components/game/DuoButton";
import { cn } from "@/lib/cn";

export interface LessonAdvanceActionProps {
  onAdvance: () => void;
  disabled?: boolean;
  loading?: boolean;
  timerActive?: boolean;
  remainingSeconds?: number;
  label?: string;
  className?: string;
}

export function LessonAdvanceAction({
  onAdvance,
  disabled = false,
  loading = false,
  timerActive = false,
  remainingSeconds = 0,
  label = "Keyingi",
  className,
}: LessonAdvanceActionProps) {
  const countdown = Math.max(1, Math.ceil(remainingSeconds));

  return (
    <DuoButton
      color="green"
      size="lg"
      fullWidth
      disabled={disabled}
      loading={loading}
      aria-busy={loading || undefined}
      onClick={onAdvance}
      className={cn("ea-lesson-advance-action", className)}
    >
      <span className="ea-lesson-advance-action__content">
        <span>{label}</span>
        {timerActive && remainingSeconds > 0 && (
          <span className="ea-lesson-advance-action__countdown" aria-hidden="true">
            {countdown}
          </span>
        )}
      </span>
    </DuoButton>
  );
}
