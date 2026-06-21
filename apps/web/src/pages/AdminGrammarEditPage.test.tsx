import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { vi, describe, it, expect, beforeEach } from "vitest";
import { AdminGrammarEditPage } from "./AdminGrammarEditPage";

const mocks = vi.hoisted(() => ({ get: vi.fn(), updateFull: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { admin: { grammar: mocks } } }));

const detail = {
  id: "11111111-1111-4111-8111-111111111111", title: "Articles", category: "Articles", level: "A1", status: "Filled", exerciseCount: 1,
  vocabularyTopicId: null, grammarFocusCode: null, contextIntro: "Context", explanation: "Rule", createdAt: "2026-01-01T00:00:00Z",
  curatedTitleUz: "Wh savollar", curatedSummaryUz: "Qisqa izoh", curatedFormulas: ["Wh + do + subject"], curatedRules: [{ headingUz: "Tuzilishi", bodyUz: "Wh so‘z bilan boshlanadi." }],
  examples: [{ english: "I have a book.", uzbek: "Menda kitob bor." }], commonMistakes: [{ text: "Artiklni tushirib qoldirish" }],
  exercises: [{ type: "Recognition", prompt: "Choose", options: ["a", "an"], correctOptionIndex: 0, hintCode: null, explanation: "Because" }],
  applicationTasks: [{ targetSkill: "Speaking", prompt: "Use the rule" }],
};

function renderPage() {
  return render(<MemoryRouter initialEntries={[`/admin/app/grammar/${detail.id}/edit`]}><Routes><Route path="/admin/app/grammar/:id/edit" element={<AdminGrammarEditPage />} /></Routes></MemoryRouter>);
}

describe("AdminGrammarEditPage", () => {
  beforeEach(() => { vi.clearAllMocks(); mocks.get.mockResolvedValue(detail); vi.spyOn(window, "confirm").mockReturnValue(true); });
  it("loads the full grammar lesson by id", async () => {
    renderPage();
    expect((await screen.findAllByDisplayValue("Articles")).length).toBeGreaterThan(0);
    expect(mocks.get).toHaveBeenCalledWith(detail.id);
    expect(screen.getByDisplayValue("I have a book.")).toBeTruthy();
    expect(screen.getByDisplayValue("Because")).toBeTruthy();
  });
  it("adds, reorders and saves full content", async () => {
    mocks.updateFull.mockResolvedValue(detail);
    renderPage();
    await screen.findAllByDisplayValue("Articles");
    fireEvent.click(screen.getAllByRole("button", { name: "Mashq qo‘shish" }).at(-1)!);
    const promptFields = screen.getAllByLabelText("Savol / prompt");
    fireEvent.change(promptFields.at(-1)!, { target: { value: "Second question" } });
    const blankOptions = screen.getAllByPlaceholderText(/-variant/).slice(-2);
    fireEvent.change(blankOptions[0], { target: { value: "one" } });
    fireEvent.change(blankOptions[1], { target: { value: "two" } });
    const form = screen.getAllByRole("button", { name: "PostgreSQL'ga saqlash" }).at(-1)!.closest("form")!;
    fireEvent.submit(form);
    expect(screen.queryByRole("alert")).toBeNull();
    const saveButtons = await screen.findAllByRole("button", { name: "PostgreSQL'ga saqlash" });
    fireEvent.click(saveButtons.at(-1)!);
    await waitFor(() => expect(mocks.updateFull).toHaveBeenCalled());
    expect(mocks.updateFull.mock.calls[0][1].exercises).toHaveLength(2);
  });
});
