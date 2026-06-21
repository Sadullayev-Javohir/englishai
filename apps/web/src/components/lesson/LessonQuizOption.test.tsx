import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LessonQuizOption } from "./LessonQuizOption";

afterEach(cleanup);
describe("Shared answer card", () => {
  it("keeps selection distinct from server correctness", () => {
    const select = vi.fn();
    const { container } = render(<LessonQuizOption index={0} label="has" state="idle" selected disabled={false} onSelect={select} />);
    const button = screen.getByRole("button", { name: "A. has" });
    expect(button.getAttribute("aria-pressed")).toBe("true");
    expect(button.getAttribute("data-answer-state")).toBe("idle");
    expect(container.querySelector(".lesson-quiz-option__key")?.textContent).toBe("A");
    fireEvent.click(button);
    expect(select).toHaveBeenCalledTimes(1);
  });
  it.each(["correct", "wrong", "dim"] as const)("retains accessible %s feedback and disables graded answers", state => {
    const select = vi.fn();
    render(<LessonQuizOption index={1} label="have" state={state} selected disabled onSelect={select} />);
    const button = screen.getByRole("button");
    expect(button.getAttribute("data-answer-state")).toBe(state);
    fireEvent.click(button);
    expect(select).not.toHaveBeenCalled();
    if (state !== "dim") expect(button.getAttribute("aria-label")).toContain(state === "correct" ? "to‘g‘ri javob" : "xato javob");
  });
});
