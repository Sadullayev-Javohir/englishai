import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import type { WritingTaskSummaryDto } from "@/api/types";
import { WritingCatalogPage } from "./WritingCatalogPage";

const catalogTopics: WritingTaskSummaryDto[] = [
  { topicId: "family", title: "My Family", titleUz: "Mening oilam", category: "People", level: CefrLevel.A1 },
  { topicId: "travel", title: "My Travel", titleUz: "Mening sayohatim", category: "Travel", level: CefrLevel.A1 },
];

const catalogMock = vi.fn().mockResolvedValue(catalogTopics);

const mapMock = vi.fn();

type AsyncState = {
  data: WritingTaskSummaryDto[] | null;
  loading: boolean;
  error: Error | null;
  reload: () => void;
};

const reloadMock = vi.fn();
const useAsyncMock = vi.fn<() => AsyncState>(() => ({
  data: catalogTopics,
  loading: false,
  error: null,
  reload: reloadMock,
}));

vi.mock("@/lib/useAsync", () => ({
  useAsync: () => useAsyncMock(),
}));

vi.mock("@/api/client", () => ({
  api: {
    writing: { catalog: (...args: unknown[]) => catalogMock(...args) },
    levels: { map: (...args: unknown[]) => mapMock(...args) },
  },
}));

vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/lib/skillTopicGates", () => ({
  useSkillTopicGates: () => ({
    ready: true,
    gateOf: (topicId: string) => topicId === "travel"
      ? {
          isLocked: false,
          requiresPro: false,
          passed: true,
          isMastered: true,
          passedModuleCount: 6,
          requiredModuleCount: 6,
        }
      : {
          isLocked: false,
          requiresPro: false,
          passed: false,
          isMastered: false,
          passedModuleCount: 2,
          requiredModuleCount: 6,
        },
  }),
}));
vi.mock("@/components/TopicImage", () => ({ TopicImage: ({ title }: { title: string }) => <div>{title} rasmi</div> }));
vi.mock("@/components/ui/ModulePageLoader", () => ({ ModulePageLoader: () => <div>Yuklanmoqda</div> }));

afterEach(() => {
  cleanup();
  catalogMock.mockClear();
  mapMock.mockClear();
  reloadMock.mockClear();
  useAsyncMock.mockClear();
  useAsyncMock.mockReturnValue({
    data: catalogTopics,
    loading: false,
    error: null,
    reload: reloadMock,
  });
});

describe("WritingCatalogPage", () => {
  it("shows real topic completion progress on writing cards", async () => {
    render(
      <MemoryRouter initialEntries={["/writing"]}>
        <WritingCatalogPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText("33%")).toBeTruthy();
    expect(screen.getByText("2/6")).toBeTruthy();
    expect(screen.getByLabelText("Mavzu progressi 33%").firstElementChild?.getAttribute("style")).toContain("width: 33%");
    expect(screen.getByText("100%")).toBeTruthy();
    expect(screen.getByText("6/6")).toBeTruthy();
  });

  it("does not render clickable fake topics when the catalog request fails", () => {
    useAsyncMock.mockReturnValue({ data: null, loading: false, error: new Error("offline"), reload: reloadMock });

    render(
      <MemoryRouter initialEntries={["/writing"]}>
        <WritingCatalogPage />
      </MemoryRouter>,
    );

    expect(screen.getByText("Yozish mavzularini yuklab bo'lmadi. Qayta urinib ko'ring.")).toBeTruthy();
    expect(screen.queryByText("My Family")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" }));
    expect(reloadMock).toHaveBeenCalledOnce();
  });
});
