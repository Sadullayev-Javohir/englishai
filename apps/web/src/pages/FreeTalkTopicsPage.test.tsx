import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useParams } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { FreeTalkTopicsPage } from "./FreeTalkTopicsPage";

const topics = [
  {
    code: "family",
    englishTitle: "Family",
    level: CefrLevel.A1,
    imageId: "11111111-1111-1111-1111-111111111111",
  },
  {
    code: "daily-routine",
    englishTitle: "Daily routine",
    level: CefrLevel.A1,
    imageId: "22222222-2222-2222-2222-222222222222",
  },
];

type AsyncState = {
  data: typeof topics | null;
  loading: boolean;
  error: Error | null;
};

const useAsyncMock = vi.fn<() => AsyncState>(() => ({ data: topics, loading: false, error: null }));

vi.mock("@/lib/useAsync", () => ({
  useAsync: () => useAsyncMock(),
}));

vi.mock("@/api/client", () => ({
  api: {
    speaking: { freeTalkTopics: vi.fn() },
    images: { topicUrl: (topicId: string) => `/api/images/topics/${topicId}` },
  },
}));

vi.mock("@/app/session", () => ({
  getStoredLevel: () => CefrLevel.A1,
}));

afterEach(() => {
  cleanup();
  useAsyncMock.mockClear();
  useAsyncMock.mockReturnValue({ data: topics, loading: false, error: null });
});

describe("FreeTalkTopicsPage", () => {
  it("renders role-talk aligned navigation, filters, and topic cards", () => {
    renderPage();

    expect(screen.getByRole("button", { name: "Orqaga" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "A1" }).getAttribute("aria-pressed")).toBe("true");
    expect(screen.getByLabelText("Erkin suhbat katalogi holati").textContent).toContain("2 mavzu");
    expect(screen.getByText("A1 mavzulari")).toBeTruthy();
    expect(screen.getAllByText("Nutq")).toHaveLength(2);
    expect(screen.getAllByText("Suhbatni boshlash")).toHaveLength(2);
    expect(screen.getByRole("img", { name: "Family" }).getAttribute("src")).toBe(
      "/api/images/topics/11111111-1111-1111-1111-111111111111",
    );
    expect(screen.getByRole("img", { name: "Daily routine" }).getAttribute("src")).toBe(
      "/api/images/topics/22222222-2222-2222-2222-222222222222",
    );
  });

  it("opens the canonical topic route", () => {
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: /Family/ }));

    expect(screen.getByTestId("topic-route").textContent).toBe("family");
  });

  it("returns to the speaking page", () => {
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: "Orqaga" }));

    expect(screen.getByTestId("speaking-route")).toBeTruthy();
  });

  it("shows the scoped error state", () => {
    useAsyncMock.mockReturnValue({ data: null, loading: false, error: new Error("offline") });
    renderPage();

    expect(screen.getByText("Mavzularni yuklab bo‘lmadi. Qayta urinib ko‘ring.")).toBeTruthy();
  });
});

function TopicRoute() {
  const { topicCode } = useParams();
  return <div data-testid="topic-route">{topicCode}</div>;
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/app/speaking/free-talk"]}>
      <Routes>
        <Route path="/app/speaking/free-talk" element={<FreeTalkTopicsPage />} />
        <Route path="/app/speaking/free-talk/:topicCode" element={<TopicRoute />} />
        <Route path="/app/speaking" element={<div data-testid="speaking-route" />} />
      </Routes>
    </MemoryRouter>,
  );
}
