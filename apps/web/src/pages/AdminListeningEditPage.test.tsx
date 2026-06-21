import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "@/api/client";
import { AdminListeningEditPage } from "./AdminListeningEditPage";

vi.mock("@/api/client", () => ({ api: { admin: { listening: { get: vi.fn(), updateFull: vi.fn() } } } }));
const detail = { id: "11111111-1111-1111-1111-111111111111", title: "Airport", topic: "travel", level: "A2", status: "Filled", questionCount: 1, wordCount: 4, vocabularyTopicId: null, createdAt: "2026-01-01T00:00:00Z", transcript: "Welcome to the airport.", audio: { streamUrl: "/audio", hasCachedAudio: false, sizeBytes: null, generatedAt: null, contentType: "audio/mpeg" }, segments: [], questions: [{ id: "22222222-2222-2222-2222-222222222222", prompt: "Where?", options: ["Airport", "School"], correctOptionIndex: 0, hintCode: null, explanation: "The speaker says airport." }], vocabularyContext: [] };

describe("AdminListeningEditPage", () => {
  beforeEach(() => { vi.mocked(api.admin.listening.get).mockResolvedValue(detail); vi.mocked(api.admin.listening.updateFull).mockResolvedValue(detail); vi.spyOn(window, "confirm").mockReturnValue(true); vi.spyOn(window, "close").mockImplementation(() => undefined); });
  afterEach(cleanup);
  it("loads full PostgreSQL detail and supports adding questions", async () => {
    render(<MemoryRouter initialEntries={[`/admin/listening/${detail.id}/edit`]}><Routes><Route path="/admin/listening/:id/edit" element={<AdminListeningEditPage />} /></Routes></MemoryRouter>);
    expect(await screen.findByDisplayValue("Welcome to the airport.")).toBeTruthy();
    fireEvent.click(screen.getAllByRole("button", { name: "Qo‘shish" })[1]);
    expect(screen.getByText("Savol 2")).toBeTruthy();
  });
});
