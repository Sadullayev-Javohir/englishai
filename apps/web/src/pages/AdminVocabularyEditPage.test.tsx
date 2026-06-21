import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AdminVocabularyEditPage } from "./AdminVocabularyEditPage";

const topic = { id: "11111111-1111-1111-1111-111111111111", slug: "a1-family", title: "My family", titleUz: "Mening oilam", category: "daily_life", grammarFocusCode: "present-simple", sequence: 1, level: "A1", status: "Filled", wordCount: 1, passage: "This is my family.", words: [{ word: "family", translation: "oila", exampleSentence: "This is my family.", partOfSpeech: "Noun", imageUrl: null, imageSource: null, imageAttribution: null }], createdAt: "2026-01-01T00:00:00Z" };
const mocks = vi.hoisted(() => ({ get: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { admin: { vocabulary: { get: mocks.get, update: vi.fn(), create: vi.fn() } } } }));

beforeEach(() => mocks.get.mockResolvedValue(topic));

describe("AdminVocabularyEditPage", () => {
  it("loads the topic contexts on its dedicated route", async () => {
    render(<MemoryRouter initialEntries={[`/admin/app/vocabulary/${topic.id}/edit`]}><Routes><Route path="/admin/app/vocabulary/:id/edit" element={<AdminVocabularyEditPage />} /></Routes></MemoryRouter>);
    expect(await screen.findByDisplayValue("My family")).toBeTruthy();
    expect(screen.getByDisplayValue("family")).toBeTruthy();
    expect(mocks.get).toHaveBeenCalledWith(topic.id);
  });
});
