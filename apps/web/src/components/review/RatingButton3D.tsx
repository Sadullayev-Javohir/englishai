import { AppButton, type DesignTone } from "@/components/design";

export type RatingGrade = "hard" | "good" | "easy";

interface RatingButton3DProps {
  grade: RatingGrade;
  label: string;
  onRate: (grade: RatingGrade) => void;
  disabled?: boolean;
}

const GRADE_TONE: Record<RatingGrade, DesignTone> = {
  hard: "danger",
  good: "standard",
  easy: "success",
};

export function RatingButton3D({ grade, label, onRate, disabled }: RatingButton3DProps) {
  return (
    <AppButton
      type="button"
      tone={GRADE_TONE[grade]}
      size="lg"
      fullWidth
      disabled={disabled}
      aria-label={label}
      onClick={() => onRate(grade)}
    >
      {label}
    </AppButton>
  );
}
