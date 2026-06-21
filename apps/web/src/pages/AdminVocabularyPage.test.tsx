import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AdminVocabularyPage } from "./AdminVocabularyPage";

const topic = {
  id: "11111111-1111-1111-1111-111111111111",
  slug: "a1-family",
  title: "My family",
  titleUz: "Mening oilam",
  category: "daily_life",
  grammarFocusCode: "present-simple",
  sequence: 1,
  level: "A1",
  status: "Filled",
  wordCount: 1,
  passage: "This is my family.",
  words: [{ word: "family", translation: "oila", exampleSentence: "This is my family.", partOfSpeech: "Noun", imageUrl: null, imageSource: null, imageAttribution: null }],
  createdAt: "2026-01-01T00:00:00Z",
};

const mocks = vi.hoisted(() => ({ list: vi.fn(), get: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { admin: { vocabulary: { list: mocks.list, get: mocks.get, create: vi.fn(), update: vi.fn(), remove: vi.fn() } } } }));

beforeEach(() => {
  mocks.list.mockResolvedValue([topic]);
  mocks.get.mockResolvedValue(topic);
  Object.defineProperty(window, "requestAnimationFrame", { configurable: true, value: (callback: FrameRequestCallback) => { callback(0); return 1; } });
  Object.defineProperty(Element.prototype, "scrollIntoView", { configurable: true, value: vi.fn() });
  vi.spyOn(window, "open").mockImplementation(() => null);
});

describe("AdminVocabularyPage", () => {
  it("opens the dedicated editor route in a new tab", async () => {
    render(<MemoryRouter><AdminVocabularyPage /></MemoryRouter>);
    const [editButton] = await screen.findAllByRole("button", { name: /tahrirlash/i });
    fireEvent.click(editButton);
    expect(window.open).toHaveBeenCalledWith(`/admin/vocabulary/${topic.id}/edit`, "_blank", "noopener,noreferrer");
  });
});
