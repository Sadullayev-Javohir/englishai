import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, PlacementItemKind, TestStage, type PlacementItemDto } from "@/api/types";
import { McqContent, PlacementBoard, WritingItem } from "./PlacementTestPage";

const item: PlacementItemDto = {
  id: "question-1", kind: PlacementItemKind.MultipleChoice, stage: TestStage.Grammar,
  difficulty: CefrLevel.B1, prompt: "Choose the correct sentence.",
  options: ["She don't like coffee.", "She doesn't like coffee."],
  hasAudio: false, passageText: null, minWords: null, maxWords: null,
  stageNumber: 2, stageCount: 6, itemNumberInStage: 5, itemsInStage: 12,
  completedItems: 16, totalItems: 72,
};

afterEach(cleanup);

describe("Pen placement board", () => {
  it("displays server stage and stage-local question progress, not overall progress", () => {
    render(<PlacementBoard item={item}>Question</PlacementBoard>);
    expect(screen.getByText("Grammar · 2 / 6")).toBeTruthy();
    expect(screen.getByText("5 / 12")).toBeTruthy();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe("2");
  });
  it("requires a selection before an answer can be submitted", () => {
    const onSelect = vi.fn();
    const onSubmit = vi.fn();
    render(<McqContent item={item} selected={null} busy={false} onSelect={onSelect} onSubmit={onSubmit} />);
    expect((screen.getByRole("button", { name: "Javobni yuborish" }) as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(screen.getByRole("button", { name: "A She don't like coffee." }));
    expect(onSelect).toHaveBeenCalledWith(0);
    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("submits only through the action button without revealing correctness", () => {
    const onSubmit = vi.fn();
    render(<McqContent item={item} selected={0} busy={false} onSelect={vi.fn()} onSubmit={onSubmit} />);
    fireEvent.click(screen.getByRole("button", { name: "Javobni yuborish" }));
    expect(onSubmit).toHaveBeenCalledOnce();
    expect(screen.queryByText("To'g'ri!")).toBeNull();
    expect(screen.queryByText("Noto'g'ri")).toBeNull();
    expect((screen.getByRole("button", { name: "O‘tkazib yuborish" }) as HTMLButtonElement).disabled).toBe(true);
  });
  it("locks all answer controls while a request is in progress", () => {
    const onSelect = vi.fn();
    render(<McqContent item={item} selected={0} busy onSelect={onSelect} onSubmit={vi.fn()} />);
    fireEvent.click(screen.getByRole("button", { name: "B She doesn't like coffee." }));
    expect(onSelect).not.toHaveBeenCalled();
    for (const control of screen.getAllByRole("button")) expect((control as HTMLButtonElement).disabled).toBe(true);
  });
  it.each([TestStage.Vocabulary, TestStage.Grammar, TestStage.Listening, TestStage.Reading, TestStage.Writing, TestStage.Speaking])("uses one board for stage %s", stage => {
    render(<PlacementBoard item={{ ...item, stage, stageNumber: stage }}>Skill content</PlacementBoard>);
    expect(screen.getByText(`${TestStage[stage]} · ${stage} / 6`)).toBeTruthy();
  });
  it("shows the reading passage but never exposes a listening transcript", () => {
    const props = { selected: null, busy: false, onSelect: vi.fn(), onSubmit: vi.fn() };
    const { rerender } = render(<McqContent {...props} item={{ ...item, stage: TestStage.Reading, passageText: "A garden brought people together." }} />);
    expect(screen.getByText("A garden brought people together.")).toBeTruthy();
    rerender(<McqContent {...props} item={{ ...item, stage: TestStage.Listening, hasAudio: true, passageText: "Private listening script" }} />);
    expect(screen.queryByText("Private listening script")).toBeNull();
    expect(screen.getByRole("region", { name: "Tinglash audiosi" })).toBeTruthy();
  });
});

describe("Pen writing task", () => {
  const writing = { ...item, kind: PlacementItemKind.Writing, stage: TestStage.Writing, minWords: 3, maxWords: 6, options: null };
  it("enforces the server's lower and upper limits and trims submitted text", () => {
    const onSubmit = vi.fn();
    render(<WritingItem item={writing} busy={false} onSubmit={onSubmit} />);
    const textarea = screen.getByRole("textbox", { name: "Javobingiz" });
    const button = screen.getByRole("button", { name: "Yuborish va davom etish" }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    fireEvent.change(textarea, { target: { value: "one two" } });
    expect(button.disabled).toBe(true);
    fireEvent.change(textarea, { target: { value: "  one two\nthree  " } });
    expect(button.disabled).toBe(false);
    expect(screen.getByText("3 so'z")).toBeTruthy();
    fireEvent.click(button);
    expect(onSubmit).toHaveBeenCalledWith("one two\nthree");
    fireEvent.change(textarea, { target: { value: "one two three four five six seven" } });
    expect(button.disabled).toBe(true);
    expect(textarea.getAttribute("aria-invalid")).toBe("true");
    expect(screen.getByRole("alert").textContent).toContain("6 so‘zgacha");
  });
  it("does not enable spellcheck or submission while grading", () => {
    render(<WritingItem item={writing} busy onSubmit={vi.fn()} />);
    const textarea = screen.getByRole("textbox") as HTMLTextAreaElement;
    expect(textarea.disabled).toBe(true);
    expect(textarea.getAttribute("spellcheck")).toBe("false");
    expect((screen.getByRole("button") as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByRole("heading", { level: 1 })).toBeTruthy();
  });
});
