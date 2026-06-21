import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, LevelExitState } from "@/api/types";
import type { LevelMapDto, VocabularyTopicSummaryDto } from "@/api/types";

Object.defineProperty(window, "matchMedia", {
  configurable: true,
  value: vi.fn().mockReturnValue({
    matches: false,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  }),
});

const { levelsMap, adminAccess, openPaywall } = vi.hoisted(() => ({
  levelsMap: vi.fn(),
  adminAccess: vi.fn().mockResolvedValue({ role: 0, isAdmin: false, canManageAdmins: false }),
  openPaywall: vi.fn(),
}));

vi.mock("@/api/client", () => ({
  api: {
    levels: { map: levelsMap },
    admin: { access: adminAccess },
  },
}));
vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/components/PaywallProvider", () => ({ usePaywall: () => ({ open: openPaywall }) }));

const { LevelMapPage } = await import("./LevelMapPage");

afterEach(() => {
  cleanup();
  levelsMap.mockReset();
  openPaywall.mockReset();
});

function topic(
  id: string,
  category: string,
  overrides: Partial<VocabularyTopicSummaryDto> = {},
): VocabularyTopicSummaryDto {
  return {
    id,
    title: `Title ${id}`,
    titleUz: `Sarlavha ${id}`,
    category,
    level: CefrLevel.A1,
    isFilled: true,
    learned: false,
    isStarted: false,
    passedModuleCount: 0,
    requiredModuleCount: 6,
    isMastered: false,
    isLocked: false,
    requiresPro: false,
    modules: [
      { module: "Vocabulary", score: 0, passed: false, unlocked: true, achievedAt: null },
      { module: "Grammar", score: 0, passed: false, unlocked: true, achievedAt: null },
      { module: "Reading", score: 0, passed: false, unlocked: true, achievedAt: null },
      { module: "Writing", score: 0, passed: false, unlocked: true, achievedAt: null },
      { module: "Speaking", score: 0, passed: false, unlocked: true, achievedAt: null },
      { module: "Listening", score: 0, passed: false, unlocked: true, achievedAt: null },
    ],
    ...overrides,
  };
}

function levelMap(overrides: Partial<LevelMapDto> = {}): LevelMapDto {
  const topics = [
    topic("f1", "family_people", { isMastered: true, passedModuleCount: 6 }),
    topic("f2", "family_people", { isMastered: true, passedModuleCount: 6 }),
    topic("h1", "home_routine"),
    topic("h2", "home_routine", { isLocked: true }),
    topic("d1", "food_drink", { isLocked: true }),
    topic("d2", "food_drink", { isLocked: true }),
  ];
  return {
    level: CefrLevel.A1,
    currentLevel: CefrLevel.A1,
    isCurrentLevel: true,
    isLevelUnlocked: true,
    hasFullAccess: false,
    canDo: [],
    topics,
    topicsTotal: topics.length,
    topicsLearned: 2,
    topicsMastered: 2,
    activeTopicId: "h1",
    recommendedNextModule: "Vocabulary",
    skillScores: [],
    readiness: null,
    exitState: LevelExitState.Current,
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/levels"]}>
      <LevelMapPage />
    </MemoryRouter>,
  );
}

function rowIds(container: HTMLElement): string[] {
  return [...container.querySelectorAll("[data-roadmap-topic]")].map(
    (row) => row.getAttribute("data-roadmap-topic") ?? "",
  );
}

describe("LevelMapPage screen 62 path", () => {
  it("renders the Pen heading, compact level pills, and current-level progress card", async () => {
    levelsMap.mockResolvedValue(levelMap());
    renderPage();

    await screen.findByRole("heading", { name: "Har kuni bir qadam." });
    expect(screen.getByText("Mavzuni 6 ta ko‘nikma bilan to‘liq o‘zlashtiring.")).toBeTruthy();
    expect(screen.getByText("A1 — Boshlang'ich")).toBeTruthy();
    expect(screen.getByRole("progressbar", { name: "Daraja rivojlanishi" }).getAttribute("aria-valuenow")).toBe("33");
    expect(screen.getByTestId("level-selector-grid").querySelectorAll("button")).toHaveLength(6);
  });

  it("opens only the active chapter in the focused content column and names guide chapters in Uzbek", async () => {
    levelsMap.mockResolvedValue(levelMap());
    const { container } = renderPage();

    await waitFor(() => expect(rowIds(container)).toEqual(["h1", "h2"]));
    expect(screen.getByRole("heading", { name: /02 · Uy va kun tartibi/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: /Oila va odamlar/ })).toBeTruthy();
    expect(screen.queryByText("Home routine")).toBeNull();
  });

  it("moves the selected guide chapter into the focused content column", async () => {
    levelsMap.mockResolvedValue(levelMap());
    const { container } = renderPage();

    await screen.findByRole("button", { name: /Oila va odamlar/ });
    fireEvent.click(screen.getByRole("button", { name: /Oila va odamlar/ }));

    await waitFor(() => expect(rowIds(container)).toEqual(["f1", "f2"]));
    expect(screen.getByRole("heading", { name: /01 · Oila va odamlar/ })).toBeTruthy();
  });

  it("keeps sequentially locked topics disabled and the active topic expanded by default", async () => {
    levelsMap.mockResolvedValue(levelMap());
    const { container } = renderPage();

    await waitFor(() => expect(rowIds(container)).toEqual(["h1", "h2"]));
    expect(container.querySelector<HTMLButtonElement>('[data-roadmap-topic="h2"] .lvmap-card')?.disabled).toBe(true);
    expect(container.querySelector('[data-roadmap-topic="h1"] .lvmap-card--active')).not.toBeNull();
    expect(container.querySelector('[data-roadmap-topic="h1"] .lvmap-panel')).not.toBeNull();
    expect(screen.getByRole("button", { name: /Vocabularyni boshlash/ })).toBeTruthy();

    const activeTopic = container.querySelector<HTMLButtonElement>(
      '[data-roadmap-topic="h1"] .lvmap-card',
    )!;
    fireEvent.click(activeTopic);
    await waitFor(() =>
      expect(container.querySelector('[data-roadmap-topic="h1"] .lvmap-panel')).toBeNull(),
    );

    fireEvent.click(activeTopic);
    await waitFor(() =>
      expect(container.querySelector('[data-roadmap-topic="h1"] .lvmap-panel')).not.toBeNull(),
    );
  });

  it("opens the paywall from a Pro-gated topic instead of navigating", async () => {
    const map = levelMap();
    map.topics[2] = topic("h1", "home_routine", { requiresPro: true });
    levelsMap.mockResolvedValue(map);
    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: /Vocabularyni boshlash/ }));
    expect(openPaywall).toHaveBeenCalledTimes(1);
  });

  it("keeps every remaining chapter reachable with an explicit more-chapters control", async () => {
    const map = levelMap({
      topics: [
        topic("a", "family_people", { isMastered: true, passedModuleCount: 6 }),
        topic("b", "home_routine"),
        topic("c", "food_drink"),
        topic("d", "school"),
        topic("e", "animals_pets"),
        topic("f", "body_health"),
        topic("g", "clothes_weather"),
      ],
      topicsTotal: 7,
      topicsMastered: 1,
    });
    levelsMap.mockResolvedValue(map);
    renderPage();

    const more = await screen.findByRole("button", { name: "Yana 2 bo‘lim" });
    fireEvent.click(more);
    expect(await screen.findByRole("button", { name: /Kiyim va ob-havo/ })).toBeTruthy();
  });

  it("shows the empty-state template when the level has no topics", async () => {
    levelsMap.mockResolvedValue(levelMap({ topics: [], topicsTotal: 0, topicsMastered: 0 }));
    renderPage();

    await screen.findByText("Bu daraja uchun mavzular topilmadi.");
  });
});
