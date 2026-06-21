import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LessonAdvanceAction } from "./LessonAdvanceAction";
import { LessonStageFrame } from "./LessonStageFrame";

describe("LessonAdvanceAction", () => {
  afterEach(cleanup);

  it("renders an accessible Keyingi action with a visual-only countdown", () => {
    render(<LessonAdvanceAction onAdvance={vi.fn()} timerActive remainingSeconds={3} />);

    const button = screen.getByRole("button", { name: "Keyingi" });
    expect(button).toBeTruthy();
    expect(button.getAttribute("aria-describedby")).toBeNull();
    expect(screen.getByText("3").getAttribute("aria-hidden")).toBe("true");
    expect(document.querySelector("[aria-live]")).toBeNull();
  });

  it("calls advance once per click and exposes disabled state", () => {
    const onAdvance = vi.fn();
    const { rerender } = render(<LessonAdvanceAction onAdvance={onAdvance} />);

    fireEvent.click(screen.getByRole("button", { name: "Keyingi" }));
    expect(onAdvance).toHaveBeenCalledTimes(1);

    rerender(<LessonAdvanceAction onAdvance={onAdvance} disabled />);
    const disabledButton = screen.getByRole("button", { name: "Keyingi" });
    expect(disabledButton.hasAttribute("disabled")).toBe(true);
    fireEvent.click(disabledButton);
    expect(onAdvance).toHaveBeenCalledTimes(1);
  });

  it("exposes loading semantics and blocks activation", () => {
    const onAdvance = vi.fn();
    render(<LessonAdvanceAction onAdvance={onAdvance} loading />);

    const button = screen.getByRole("button", { name: "Keyingi" });
    expect(button.getAttribute("aria-busy")).toBe("true");
    expect(button.hasAttribute("disabled")).toBe(true);
    fireEvent.click(button);
    expect(onAdvance).not.toHaveBeenCalled();
  });

  it("keeps the lesson shell semantic without empty header or footer rows", () => {
    const { rerender } = render(<LessonStageFrame>Lesson body</LessonStageFrame>);

    expect(screen.getByRole("main").textContent).toBe("Lesson body");
    expect(document.querySelector("[data-lesson-stage-header]")).toBeNull();
    expect(document.querySelector("[data-lesson-stage-footer]")).toBeNull();

    rerender(<LessonStageFrame header="Lesson header" footer="Lesson footer">Lesson body</LessonStageFrame>);
    expect(screen.getByRole("banner").textContent).toBe("Lesson header");
    expect(screen.getByRole("contentinfo").textContent).toBe("Lesson footer");
    expect(document.querySelector("[data-lesson-stage-body]")?.className).toContain("ea-lesson-stage__body");
  });
});
