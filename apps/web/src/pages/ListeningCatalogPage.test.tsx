import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { ListeningCatalogPage } from "./ListeningCatalogPage";

const catalogMock = vi.fn().mockResolvedValue([
  {
    topicId: "daily-routine",
    title: "Daily Routine",
    titleUz: "Kun tartibi",
    category: "Listening",
    level: CefrLevel.A1,
  },
]);

vi.mock("@/api/client", () => ({
  api: {
    listening: { catalog: (...args: unknown[]) => catalogMock(...args) },
    images: { topicUrl: (topicId: string) => `/api/images/topics/${topicId}` },
  },
}));

vi.mock("@/app/auth", () => ({
  useAuth: () => ({ user: { id: "learner-1" }, status: "authenticated" }),
}));

vi.mock("@/lib/skillTopicGates", () => ({
  useSkillTopicGates: () => ({
    ready: true,
    gateOf: () => ({
      isLocked: false,
      requiresPro: false,
      passed: false,
      isMastered: false,
      passedModuleCount: 2,
      requiredModuleCount: 6,
    }),
  }),
}));

afterEach(() => {
  cleanup();
  catalogMock.mockClear();
});

describe("ListeningCatalogPage", () => {
  it("renders Pen screen 36 with search, level filters and real progress", async () => {
    renderPage();

    expect(screen.getByRole("heading", { name: "Quloq soling. Ma’noni tuting.", level: 1 })).toBeTruthy();
    expect(screen.getByRole("searchbox", { name: "Listening mavzusini qidirish" })).toBeTruthy();
    await waitFor(() => expect(screen.getByText("Daily Routine")).toBeTruthy());
    expect(screen.getByRole("button", { name: "A1" }).getAttribute("aria-pressed")).toBe("true");
    expect(screen.getByText("33% · 2/6")).toBeTruthy();
  });

  it("filters topics by title or translation without losing the level selection", async () => {
    renderPage();
    await screen.findByText("Daily Routine");
    fireEvent.change(screen.getByRole("searchbox"), { target: { value: "not a topic" } });
    expect(screen.queryByRole("button", { name: /Daily Routine/ })).toBeNull();
    expect(screen.getByText("Audio mavzular topilmadi")).toBeTruthy();
    fireEvent.change(screen.getByRole("searchbox"), { target: { value: "kun tartibi" } });
    expect(screen.getByRole("button", { name: /Daily Routine/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: "A1" }).getAttribute("aria-pressed")).toBe("true");
  });

  it("filters the started and completed states using the authoritative gates", async () => {
    renderPage();
    await screen.findByText("Daily Routine");
    fireEvent.click(screen.getByRole("button", { name: "Boshlangan" }));
    expect(screen.getByRole("button", { name: /Daily Routine/ })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Tugatilgan" }));
    expect(screen.queryByRole("button", { name: /Daily Routine/ })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Barchasi" }));
    expect(screen.getByRole("button", { name: /Daily Routine/ })).toBeTruthy();
  });

  it("opens the selected listening topic", async () => {
    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: /Daily Routine/ }));

    expect(screen.getByTestId("topic-route").textContent).toBe("daily-routine");
  });

  it("requests all levels when Hammasi is selected", async () => {
    renderPage();
    await waitFor(() => expect(catalogMock).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Hammasi" }));

    await waitFor(() => expect(catalogMock).toHaveBeenLastCalledWith("learner-1", undefined, true));
  });
});

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/listening"]}>
      <Routes>
        <Route path="/listening" element={<ListeningCatalogPage />} />
        <Route path="/listening/topic/:topicId" element={<div data-testid="topic-route">daily-routine</div>} />
      </Routes>
    </MemoryRouter>,
  );
}
