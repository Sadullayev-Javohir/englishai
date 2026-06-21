import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { VocabularyProgress } from "./VocabularyChrome";
import { VOCABULARY_LESSON_STEPS } from "./vocabularyLessonSteps";

afterEach(cleanup);
describe("vocabulary lesson progress", () => {
  it.each(VOCABULARY_LESSON_STEPS)("maps $label to exactly one of the five actual lesson stages", ({ id, label }) => {
    const { container } = render(<VocabularyProgress step={id} />);
    const index = VOCABULARY_LESSON_STEPS.findIndex(item => item.id === id);
    const bar = screen.getByRole("progressbar", { name: "Dars bosqichlari" });
    expect(bar.getAttribute("aria-valuenow")).toBe(String(index + 1));
    expect(bar.getAttribute("aria-valuemax")).toBe("5");
    expect(bar.getAttribute("aria-valuetext")).toBe(`${index + 1} / 5 · ${label}`);
    expect(container.querySelectorAll(".vocabulary-progress__segment")).toHaveLength(5);
    expect(container.querySelectorAll('[aria-current="step"]')).toHaveLength(1);
    expect(container.querySelectorAll(".is-done")).toHaveLength(id === "result" ? 5 : index);
    expect(screen.queryByRole("button")).toBeNull();
  });
  it("keeps optional pronunciation inside Flashcards instead of creating a sixth step", () => {
    render(<VocabularyProgress step="flashcards" substep="Talaffuz mashqi" />);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("3 / 5 · Flashcardlar · Talaffuz mashqi");
  });
  it("reverses correctly when returning from test to flashcards", () => {
    const { rerender, container } = render(<VocabularyProgress step="test" hearts={4} />);
    expect(screen.getByLabelText("4 ta jon")).toBeTruthy();
    rerender(<VocabularyProgress step="flashcards" />);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe("3");
    expect(container.querySelectorAll(".is-done")).toHaveLength(2);
    expect(screen.queryByLabelText("4 ta jon")).toBeNull();
  });
});
