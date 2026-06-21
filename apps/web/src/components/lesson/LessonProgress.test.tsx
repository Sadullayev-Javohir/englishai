import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LessonProgress } from "./LessonProgress";

afterEach(cleanup);
const steps = [{ id: "context", label: "Kontekst" }, { id: "test", label: "Test" }, { id: "done", label: "Yakun" }];
describe("Shared lesson progress", () => {
  it("shows one current section with completed sections behind it", () => {
    const { container, rerender } = render(<LessonProgress steps={steps} step="test" substep="2 / 4 savol" />);
    const bar = screen.getByRole("progressbar", { name: "Dars bosqichlari" });
    expect(bar.getAttribute("aria-valuetext")).toBe("2 / 3 · Test · 2 / 4 savol");
    expect(container.querySelectorAll('[aria-current="step"]')).toHaveLength(1);
    expect(container.querySelectorAll(".is-done")).toHaveLength(1);
    rerender(<LessonProgress steps={steps} step="done" complete />);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("3 / 3 · Yakun");
    expect(container.querySelectorAll(".is-done")).toHaveLength(3);
    rerender(<LessonProgress steps={steps} step="context" />);
    expect(container.querySelectorAll(".is-done")).toHaveLength(0);
  });
  it("keeps a disabled back control safe while grading and preserves the stop action", () => {
    const back = vi.fn();
    render(<LessonProgress steps={steps} step="test" onBack={back} backDisabled action={<button>To‘xtatish</button>} hearts={4} />);
    fireEvent.click(screen.getByRole("button", { name: "Orqaga" }));
    expect(back).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "To‘xtatish" })).toBeTruthy();
    expect(screen.getByLabelText("4 ta jon")).toBeTruthy();
  });
});
