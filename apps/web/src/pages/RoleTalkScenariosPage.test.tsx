import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { RoleTalkScenariosPage } from "./RoleTalkScenariosPage";

const scenarios = [
  {
    code: "airport",
    englishTitle: "Airport",
    level: CefrLevel.A1,
    imageId: "11111111-1111-1111-1111-111111111111",
  },
  {
    code: "restaurant",
    englishTitle: "Restaurant",
    level: CefrLevel.A1,
    imageId: "22222222-2222-2222-2222-222222222222",
  },
];

type AsyncState = {
  data: typeof scenarios | null;
  loading: boolean;
  error: Error | null;
};

const useAsyncMock = vi.fn<() => AsyncState>(() => ({ data: scenarios, loading: false, error: null }));

vi.mock("@/lib/useAsync", () => ({
  useAsync: () => useAsyncMock(),
}));

vi.mock("@/api/client", () => ({
  api: {
    speaking: { roleplayScenarios: vi.fn() },
    images: { topicUrl: (topicId: string) => `/api/images/topics/${topicId}` },
  },
}));

vi.mock("@/app/session", () => ({
  getStoredLevel: () => CefrLevel.A1,
}));

afterEach(() => {
  cleanup();
  useAsyncMock.mockClear();
  useAsyncMock.mockReturnValue({ data: scenarios, loading: false, error: null });
});

describe("RoleTalkScenariosPage", () => {
  it("renders level filters and complete scenario content", () => {
    renderPage();

    expect(screen.getByRole("heading", { name: "Qaysi vaziyatni mashq qilamiz?" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "A1" }).getAttribute("aria-pressed")).toBe("true");
    expect(screen.getByText("Aeroportda")).toBeTruthy();
    expect(screen.getByText(/Reysga ro'yxatdan o'ting/)).toBeTruthy();
  });

  it("opens the canonical scenario route", () => {
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: /Aeroportda/ }));

    expect(screen.getByTestId("scenario-route").textContent).toBe("airport");
  });

  it("returns to the speaking page", () => {
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: "Orqaga" }));

    expect(screen.getByTestId("speaking-route")).toBeTruthy();
  });

  it("shows the scoped error state", () => {
    useAsyncMock.mockReturnValue({ data: null, loading: false, error: new Error("offline") });
    renderPage();

    expect(screen.getByText(uzError)).toBeTruthy();
  });
});

const uzError = "Senariylarni yuklab bo'lmadi. Qayta urinib ko'ring.";

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/app/speaking/role-talk"]}>
      <Routes>
        <Route path="/app/speaking/role-talk" element={<RoleTalkScenariosPage />} />
        <Route path="/app/speaking/role-talk/:scenarioCode" element={<div data-testid="scenario-route">airport</div>} />
        <Route path="/app/speaking" element={<div data-testid="speaking-route" />} />
      </Routes>
    </MemoryRouter>,
  );
}
