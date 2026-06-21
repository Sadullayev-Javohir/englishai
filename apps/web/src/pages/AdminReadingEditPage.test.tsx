import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AdminReadingEditPage } from "./AdminReadingEditPage";

const passage = {
  id: "11111111-1111-1111-1111-111111111111",
  title: "City gardens",
  topic: "Nature",
  category: "environment",
  level: "B1",
  status: "Filled",
  body: "First paragraph.\n\nSecond paragraph.",
  sections: ["First paragraph.", "Second paragraph."],
  contextGaps: "gap: garden",
  vocabulary: [{ id: "v1", word: "garden", translation: "bog‘", exampleSentence: "A city garden." }],
  questions: [{ id: "q1", prompt: "Where is the garden?", options: ["City", "Village"], correctOptionIndex: 0, hintCode: "place", explanation: "It says city." }],
  vocabularyTopicId: null,
  createdAt: "2026-01-01T00:00:00Z",
};

const mocks = vi.hoisted(() => ({ get: vi.fn(), updateFull: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { admin: { reading: mocks } } }));

beforeEach(() => {
  mocks.get.mockReset().mockResolvedValue(passage);
  mocks.updateFull.mockReset().mockResolvedValue(passage);
  vi.spyOn(window, "close").mockImplementation(() => undefined);
  Object.defineProperty(window, "opener", { configurable: true, value: { location: { reload: vi.fn() } } });
});
afterEach(() => cleanup());

function renderPage() {
  return render(<MemoryRouter initialEntries={[`/admin/reading/${passage.id}/edit`]}><Routes><Route path="/admin/reading/:id/edit" element={<AdminReadingEditPage />} /></Routes></MemoryRouter>);
}

describe("AdminReadingEditPage", () => {
  it("loads full PostgreSQL reading detail by id", async () => {
    renderPage();
    expect(await screen.findByDisplayValue("City gardens")).toBeTruthy();
    expect(screen.getByDisplayValue("First paragraph.")).toBeTruthy();
    expect(screen.getByDisplayValue("garden")).toBeTruthy();
    expect(screen.getByDisplayValue("Where is the garden?")).toBeTruthy();
    expect(mocks.get).toHaveBeenCalledWith(passage.id);
  });

  it("saves the full aggregate and refreshes the opener", async () => {
    renderPage();
    await screen.findByDisplayValue("City gardens");
    fireEvent.change(screen.getByDisplayValue("City gardens"), { target: { value: "Updated gardens" } });
    fireEvent.click(screen.getByRole("button", { name: "Saqlash" }));
    await waitFor(() => expect(mocks.updateFull).toHaveBeenCalledWith(passage.id, expect.objectContaining({ title: "Updated gardens", sections: passage.sections })));
    expect(window.opener?.location.reload).toHaveBeenCalled();
  });
});
