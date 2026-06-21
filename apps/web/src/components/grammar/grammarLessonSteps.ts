import { GrammarExerciseType, type GrammarExerciseDto } from "@/api/types";

export type GrammarStage = "context" | "rule" | "examples" | "quiz" | "done";

const CONTENT_STEPS = [
  { id: "context", label: "Kontekst" },
  { id: "rule", label: "Qoida" },
  { id: "examples", label: "Misollar" },
] as const;
const PRACTICE_STEPS = [
  { id: "recognition", label: "Variantlar", type: GrammarExerciseType.Recognition },
  { id: "typing", label: "Yozish", type: GrammarExerciseType.FillInBlank },
  { id: "rephrase", label: "Gap tuzish", type: GrammarExerciseType.Rephrase },
] as const;

/** Only sections present in the real lesson count; correction is part of its
 * question and result is reached only after the server has saved the attempt. */
export function grammarLessonProgress(stage: GrammarStage, exercises: readonly GrammarExerciseDto[], index: number) {
  const practice = PRACTICE_STEPS.filter(step => exercises.some(exercise => exercise.type === step.type));
  const steps = [...CONTENT_STEPS, ...(practice.length ? practice : [{ id: "quiz", label: "Test" }]), { id: "done", label: "Yakun" }];
  const current = exercises[index];
  const step = stage === "quiz" ? practice.find(item => item.type === current?.type)?.id ?? "quiz" : stage;
  const sectionQuestions = current ? exercises.filter(exercise => exercise.type === current.type) : [];
  const position = sectionQuestions.findIndex(exercise => exercise.id === current?.id) + 1;
  return {
    steps, step, complete: stage === "done",
    substep: stage === "quiz" && current ? `${position} / ${sectionQuestions.length} savol · Test ${index + 1} / ${exercises.length}` : undefined,
  };
}
