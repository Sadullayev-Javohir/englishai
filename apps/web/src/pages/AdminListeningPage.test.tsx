import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "@/api/client";
import { AdminListeningPage } from "./AdminListeningPage";

vi.mock("@/api/client", () => ({ api: { admin: { listening: { list: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn() } } } }));

const exercise = { id: "11111111-1111-1111-1111-111111111111", title: "Airport", topic: "travel", level: "A2", status: "Filled", questionCount: 2, wordCount: 80, vocabularyTopicId: null, createdAt: "2026-01-01T00:00:00Z" };

describe("AdminListeningPage", () => {
  beforeEach(() => { vi.mocked(api.admin.listening.list).mockResolvedValue([exercise]); vi.spyOn(window, "open").mockImplementation(() => null); });
  afterEach(cleanup);
  it("opens the independent editor in a new tab", async () => {
    render(<MemoryRouter><AdminListeningPage /></MemoryRouter>);
    await screen.findAllByText("Airport");
    fireEvent.click(screen.getAllByRole("button", { name: /Tahrirlash/ })[0]);
    expect(window.open).toHaveBeenCalledWith(`/admin/listening/${exercise.id}/edit`, "_blank", "noopener,noreferrer");
  });
  it("filters by level", async () => {
    render(<MemoryRouter><AdminListeningPage /></MemoryRouter>);
    await screen.findAllByText("Airport");
    fireEvent.change(screen.getByRole("searchbox"), { target: { value: "C2" } });
    await waitFor(() => expect(screen.queryAllByText("Airport")).toHaveLength(0));
  });
});
