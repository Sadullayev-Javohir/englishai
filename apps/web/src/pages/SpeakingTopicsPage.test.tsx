import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useParams } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { SpeakingTopicsPage } from "./SpeakingTopicsPage";

const topicsMock = vi.fn().mockResolvedValue([
  {
    id: "family",
    title: "My Family",
    titleUz: "Mening oilam",
    category: "people",
    level: CefrLevel.A1,
    isFilled: true,
    learned: false,
    passedModuleCount: 2,
    requiredModuleCount: 6,
    isMastered: false,
    isLocked: false,
    requiresPro: false,
    modules: [{ module: "Speaking", passed: false, unlocked: true }],
  },
]);

vi.mock("@/api/client", () => ({
  api: { vocabulary: { topics: (...args: unknown[]) => topicsMock(...args) } },
}));

vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/components/PaywallProvider", () => ({ usePaywall: () => ({ open: vi.fn() }) }));
vi.mock("@/components/TopicImage", () => ({
  TopicImage: ({ title }: { title: string }) => <div>{title} rasmi</div>,
}));
vi.mock("@/components/ui/ModulePageLoader", () => ({ ModulePageLoader: () => <div>Yuklanmoqda</div> }));

afterEach(() => {
  cleanup();
  topicsMock.mockClear();
});

describe("SpeakingTopicsPage", () => {
  it("renders topic progress and opens the canonical speaking lesson", async () => {
    renderPage();

    expect(await screen.findByText("My Family")).toBeTruthy();
    expect(screen.getByText("33%")).toBeTruthy();
    expect(screen.getByText("2/6")).toBeTruthy();
    expect(screen.getByLabelText("Mavzu progressi 33%").firstElementChild?.getAttribute("style")).toContain("width: 33%");

    fireEvent.click(screen.getByRole("button", { name: /My Family/ }));

    expect((await screen.findByTestId("topic-route")).textContent).toBe("family");
  });

  it("filters by level and returns to the speaking hub", async () => {
    renderPage();
    await screen.findByText("My Family");

    fireEvent.click(screen.getByRole("button", { name: "A1" }));
    await waitFor(() => expect(topicsMock).toHaveBeenLastCalledWith("learner-1", CefrLevel.A1));

    expect(screen.queryByRole("button", { name: "Orqaga" })).toBeNull();
  });
});

function TopicRoute() {
  const { topicId } = useParams();
  return <div data-testid="topic-route">{topicId}</div>;
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/app/speaking/topics"]}>
      <Routes>
        <Route path="/app/speaking/topics" element={<SpeakingTopicsPage />} />
        <Route path="/app/speaking/topic/:topicId" element={<TopicRoute />} />
        <Route path="/app/speaking" element={<div data-testid="speaking-hub" />} />
      </Routes>
    </MemoryRouter>,
  );
}
