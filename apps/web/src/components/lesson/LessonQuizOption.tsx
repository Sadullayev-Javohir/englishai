import { CheckCircle2, Circle, XCircle } from "lucide-react";
import "./LessonQuizOption.css";

export type LessonAnswerState = "idle" | "correct" | "wrong" | "dim";
export interface LessonQuizOptionProps {
  index: number;
  label: string;
  state: LessonAnswerState;
  selected: boolean;
  disabled: boolean;
  onSelect: () => void;
  classPrefix?: string;
}

/** One answer-card implementation for vocabulary and grammar. */
export function LessonQuizOption({ index, label, state, selected, disabled, onSelect, classPrefix = "lesson-quiz-option" }: LessonQuizOptionProps) {
  const letter = String.fromCharCode(65 + index);
  const StateIcon = state === "correct" ? CheckCircle2 : state === "wrong" ? XCircle : Circle;
  const names = (suffix = "") => `lesson-quiz-option${suffix}${classPrefix === "lesson-quiz-option" ? "" : ` ${classPrefix}${suffix}`}`;
  return (
    <button type="button" className={names()} data-answer-state={state} aria-pressed={selected}
      aria-label={`${letter}. ${label}${state === "correct" ? " — to‘g‘ri javob" : state === "wrong" ? " — xato javob" : ""}`}
      disabled={disabled} onClick={onSelect}>
      <span className={names("__key")} aria-hidden="true">{letter}</span>
      <span className={names("__label")}>{label}</span>
      <StateIcon className={names("__state")} size={20} aria-hidden="true" />
    </button>
  );
}
