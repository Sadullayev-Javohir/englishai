import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AdminReadingPage } from "./AdminReadingPage";

const passage = { id: "11111111-1111-1111-1111-111111111111", title: "City gardens", topic: "Nature", level: "B1", status: "Filled", questionCount: 1, wordCount: 20, vocabularyTopicId: null, createdAt: "2026-01-01T00:00:00Z" };
const mocks = vi.hoisted(() => ({ list: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { admin: { reading: mocks } } }));

beforeEach(() => {
  mocks.list.mockResolvedValue([passage]);
  vi.spyOn(window, "open").mockImplementation(() => null);
});
afterEach(() => cleanup());

describe("AdminReadingPage", () => {
  it("opens editing in a new tab", async () => {
    render(<MemoryRouter><AdminReadingPage /></MemoryRouter>);
    const [editButton] = await screen.findAllByRole("button", { name: /tahrirlash/i });
    fireEvent.click(editButton);
    expect(window.open).toHaveBeenCalledWith(`/admin/reading/${passage.id}/edit`, "_blank");
  });
});
