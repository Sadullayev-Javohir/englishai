import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { VocabularyTopicsPage } from "./VocabularyTopicsPage";

const topicsMock = vi.fn().mockResolvedValue([
  {
    id: "family",
    title: "Family",
    titleUz: "Oila",
    category: "daily_life",
    level: CefrLevel.A1,
    isFilled: true,
    learned: true,
    passedModuleCount: 2,
    requiredModuleCount: 6,
    isMastered: false,
    isLocked: false,
    requiresPro: false,
    modules: [{ module: "Vocabulary", score: 80, passed: true, unlocked: true }],
  },
  {
    id: "travel",
    title: "Travel",
    titleUz: "Sayohat",
    category: "travel",
    level: CefrLevel.A1,
    isFilled: true,
    learned: false,
    passedModuleCount: 0,
    requiredModuleCount: 6,
    isMastered: false,
    isLocked: false,
    requiresPro: false,
    modules: [{ module: "Vocabulary", score: null, passed: false, unlocked: true }],
  },
]);

vi.mock("@/api/client", () => ({
  api: {
    vocabulary: { topics: (...args: unknown[]) => topicsMock(...args) },
  },
}));

vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/components/PaywallProvider", () => ({ usePaywall: () => ({ open: vi.fn() }) }));
vi.mock("@/components/TopicImage", () => ({ TopicImage: ({ title }: { title: string }) => <div>{title} rasmi</div> }));
vi.mock("@/components/ui/ModulePageLoader", () => ({ ModulePageLoader: () => <div>Yuklanmoqda</div> }));

afterEach(() => {
  cleanup();
  topicsMock.mockClear();
});

describe("VocabularyTopicsPage", () => {
  it("renders the Pen catalog, filters, search and saved control", async () => {
    renderPage();

    expect(screen.getByRole("heading", { name: /So‘zlar bilan[\s\S]*dunyoni oching/, level: 1 })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Saqlangan" })).toBeTruthy();
    await screen.findByRole("button", { name: "Family - Oila" });
    expect(screen.getByRole("button", { name: "A1" }).getAttribute("aria-pressed")).toBe("true");

    fireEvent.change(screen.getByRole("searchbox", { name: "Vocabulary mavzularini qidirish" }), { target: { value: "travel" } });

    expect(screen.queryByRole("button", { name: "Family - Oila" })).toBeNull();
    expect(screen.getByRole("button", { name: "Travel - Sayohat" })).toBeTruthy();
  });

  it("opens a selected topic and requests all levels", async () => {
    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: "Family - Oila" }));
    expect(screen.getByTestId("topic-route").textContent).toBe("family");

    cleanup();
    renderPage();
    await screen.findByRole("button", { name: "Family - Oila" });
    fireEvent.click(screen.getByRole("button", { name: "Barchasi" }));
    await waitFor(() => expect(topicsMock).toHaveBeenLastCalledWith("learner-1", undefined, true));
  });
});

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/app/vocabulary/topics"]}>
      <Routes>
        <Route path="/app/vocabulary/topics" element={<VocabularyTopicsPage />} />
        <Route path="/app/vocabulary/topic/:topicId" element={<div data-testid="topic-route">family</div>} />
        <Route path="/home" element={<div>Home</div>} />
      </Routes>
    </MemoryRouter>,
  );
}
