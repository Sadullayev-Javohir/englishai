import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { uz } from "@/content/uz";
import { LessonGuidance } from "./LessonStageFrame";

afterEach(() => {
  cleanup();
  localStorage.clear();
});

describe("LessonGuidance", () => {
  it("renders a labelled, actionable instruction list", () => {
    render(
      <LessonGuidance
        title="Bu bosqichda nima qilasiz?"
        items={["Matnni o'qing.", "Ajratilgan so'zni bosing."]}
      />,
    );

    expect(screen.getByRole("complementary", { name: "Bu bosqichda nima qilasiz?" })).toBeTruthy();
    expect(screen.getAllByRole("listitem").map((item) => item.textContent)).toEqual([
      "Matnni o'qing.",
      "Ajratilgan so'zni bosing.",
    ]);
  });

  it("does not render an empty guidance block", () => {
    const { container } = render(<LessonGuidance title="Yordam" items={[]} />);
    expect(container.innerHTML).toBe("");
  });

  it("persists one shared close and reopen preference", () => {
    const { rerender } = render(
      <LessonGuidance title={uz.lessonGuidance.title} items={["Birinchi ko'rsatma"]} />,
    );

    fireEvent.click(screen.getByRole("button", { name: uz.lessonGuidance.close }));
    expect(screen.queryByRole("complementary", { name: uz.lessonGuidance.title })).toBeNull();
    const openButton = screen.getByRole("button", { name: uz.lessonGuidance.open });
    expect(openButton).toBeTruthy();
    expect(openButton.parentElement?.classList.contains("ea-lesson-guidance-toggle-row")).toBe(true);

    rerender(<LessonGuidance title={uz.lessonGuidance.title} items={["Boshqa sahifa ko'rsatmasi"]} />);
    expect(screen.queryByText("Boshqa sahifa ko'rsatmasi")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: uz.lessonGuidance.open }));
    expect(screen.getByText("Boshqa sahifa ko'rsatmasi")).toBeTruthy();
  });
});
