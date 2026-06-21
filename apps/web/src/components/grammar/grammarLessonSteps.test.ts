import { describe, expect, it } from "vitest";
import { GrammarExerciseType as Type, type GrammarExerciseDto } from "@/api/types";
import { grammarLessonProgress } from "./grammarLessonSteps";

const question = (id: string, type: Type): GrammarExerciseDto => ({ id, type, prompt: "Question", options: ["A", "B"] });
const exercises = [question("a", Type.Recognition), question("b", Type.Recognition), question("c", Type.FillInBlank), question("d", Type.Rephrase)];

describe("Grammar section progress", () => {
  it.each(["context", "rule", "examples", "done"] as const)("includes the %s section and all available practice types", stage => {
    const value = grammarLessonProgress(stage, exercises, 0);
    expect(value.steps.map(item => item.label)).toEqual(["Kontekst", "Qoida", "Misollar", "Variantlar", "Yozish", "Gap tuzish", "Yakun"]);
    expect(value.step).toBe(stage);
    expect(value.complete).toBe(stage === "done");
    expect(value.substep).toBeUndefined();
  });
  it.each([
    [0, "recognition", "1 / 2 savol · Test 1 / 4"],
    [1, "recognition", "2 / 2 savol · Test 2 / 4"],
    [2, "typing", "1 / 1 savol · Test 3 / 4"],
    [3, "rephrase", "1 / 1 savol · Test 4 / 4"],
  ] as const)("reports question %s within its section", (index, step, substep) => {
    expect(grammarLessonProgress("quiz", exercises, index)).toMatchObject({ step, substep, complete: false });
  });
  it("does not display nonexistent quiz sections", () => {
    const value = grammarLessonProgress("quiz", [question("only", Type.FillInBlank)], 0);
    expect(value.steps.map(item => item.id)).toEqual(["context", "rule", "examples", "typing", "done"]);
    expect(value.step).toBe("typing");
  });
  it("keeps an empty or pending test within its actual stage", () => {
    const value = grammarLessonProgress("quiz", [], 0);
    expect(value.steps.map(item => item.id)).toEqual(["context", "rule", "examples", "quiz", "done"]);
    expect(value.step).toBe("quiz");
    expect(value.complete).toBe(false);
  });
});
