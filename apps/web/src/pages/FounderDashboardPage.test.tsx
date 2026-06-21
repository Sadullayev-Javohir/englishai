import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { FounderDashboardPage } from "./FounderDashboardPage";

const { metrics, metricsStream, setVariableCostBudget } = vi.hoisted(() => ({
  metrics: vi.fn(),
  metricsStream: vi.fn(() => new Promise(() => undefined)),
  setVariableCostBudget: vi.fn(),
}));

vi.mock("@/api/client", () => ({ api: { admin: { metrics, metricsStream, setVariableCostBudget } } }));
vi.mock("@/components/charts/DashboardCharts", () => ({
  TrendAreaChart: ({ points }: { points: unknown[] }) => <div data-testid="active-chart">{points.length}</div>,
  TrendBarChart: ({ points }: { points: unknown[] }) => <div data-testid="signup-chart">{points.length}</div>,
  RetentionBarChart: () => <div data-testid="retention-chart" />,
  PlanDoughnutChart: () => <div data-testid="plan-chart" />,
  ActivationFunnelChart: ({ counts }: { counts: number[] }) => <div data-testid="activation-chart">{counts.join(",")}</div>,
  ConversionFunnelChart: () => <div data-testid="conversion-chart" />,
  ProductHealthRadarChart: () => <div data-testid="health-radar" />,
  GoalSegmentPieChart: () => <div data-testid="goal-pie" />,
  GoalConversionChart: () => <div data-testid="goal-conversion" />,
  DemographicDoughnutChart: ({ ariaLabel }: { ariaLabel: string }) => <div data-testid={ariaLabel} />,
  DemographicBarChart: ({ ariaLabel }: { ariaLabel: string }) => <div data-testid={ariaLabel} />,
}));

const payload = {
  asOf: "2026-07-26T00:00:00Z",
  overview: { dau: 12, wau: 34, mau: 56, stickinessPct: 21, totalUsers: 100, onboardedUsers: 80, newUsersToday: 4, newUsers7d: 18, premiumUsers: 12, freeUsers: 88, conversionRatePct: 12, mrrUzs: 1200000, arppuUzs: 100000 },
  activeUsersTrend: Array.from({ length: 30 }, (_, index) => ({ day: `2026-07-${String(index + 1).padStart(2, "0")}`, count: index + 1 })),
  signupsTrend: Array.from({ length: 30 }, (_, index) => ({ day: `2026-07-${String(index + 1).padStart(2, "0")}`, count: index + 2 })),
  retention: { d1: { cohortSize: 20, retained: 10, ratePct: 50 }, d7: { cohortSize: 20, retained: 8, ratePct: 40 }, d30: { cohortSize: 20, retained: 4, ratePct: 20 } },
  funnel: { registered: 100, onboarded: 80, active7d: 50, premium: 12 },
  activationFunnel: [
    { code: "registered", count: 100, rateFromRegisteredPct: 100, rateFromPreviousPct: 100 },
    { code: "usernameSetupCompleted", count: 96, rateFromRegisteredPct: 95.9, rateFromPreviousPct: 95.9 },
    { code: "placementCompleted", count: 70, rateFromRegisteredPct: 70, rateFromPreviousPct: 70 },
    { code: "day7Active", count: 70, rateFromRegisteredPct: 70, rateFromPreviousPct: 100 },
  ],
  goalSegments: [{ goal: "Work", users: 40, premiumUsers: 8, conversionRatePct: 20, d7: { cohortSize: 10, retained: 4, ratePct: 40 }, mrrUzs: 800000 }, { goal: "Travel", users: 25, premiumUsers: 2, conversionRatePct: 8, d7: { cohortSize: 10, retained: 3, ratePct: 30 }, mrrUzs: 200000 }],
  planBreakdown: [{ plan: "Monthly", count: 12, mrrUzs: 1200000 }],
  demographics: {
    completed: 80,
    missing: 20,
    completionRatePct: 80,
    gender: [{ label: "Male", count: 42, ratePct: 52.5 }, { label: "Female", count: 38, ratePct: 47.5 }],
    acquisitionSources: [{ label: "Telegram", count: 30, ratePct: 37.5 }],
    ageGroups: [{ label: "18-24", count: 40, ratePct: 50 }],
  },
  variableCosts: {
    day: "2026-07-26",
    dailyBudgetUsedUsd: 3.75,
    dailyBudgetLimitUsd: 25,
    dailyBudgetWarning: false,
    freeTierShed: false,
    pricingConfigured: true,
    categories: [
      { category: "ai", requests: 8, units: 4200, unit: "token", estimatedCostUsd: 0 },
      { category: "speech_to_text", requests: 4, units: 1.25, unit: "audio_hour", estimatedCostUsd: 1.25 },
      { category: "text_to_speech", requests: 7, units: 50000, unit: "character", estimatedCostUsd: 0.75 },
      { category: "pronunciation", requests: 3, units: 1.75, unit: "audio_hour", estimatedCostUsd: 1.75 },
    ],
    topCallers: [{ key: "learner-1", requests: 9, estimatedCostUsd: 2.5 }],
    topEndpoints: [{ key: "/api/speaking/assess", requests: 7, estimatedCostUsd: 2 }],
  },
  canManageVariableCostBudget: true,
};

beforeEach(() => {
  metrics.mockResolvedValue(payload);
  setVariableCostBudget.mockResolvedValue({ ...payload.variableCosts, dailyBudgetLimitUsd: 50 });
});
afterEach(() => { cleanup(); metrics.mockReset(); metricsStream.mockClear(); setVariableCostBudget.mockReset(); });

describe("FounderDashboardPage", () => {
  it("renders API metrics, filters chart range and goal table", async () => {
    render(<MemoryRouter><FounderDashboardPage /></MemoryRouter>);
    expect(await screen.findByRole("heading", { name: "O'sish paneli" })).toBeTruthy();
    expect(screen.getByTestId("active-chart").textContent).toBe("30");
    fireEvent.click(screen.getByRole("button", { name: "7 kun" }));
    await waitFor(() => expect(metrics).toHaveBeenLastCalledWith(expect.any(String), expect.any(String)));
    expect(screen.getByTestId("active-chart").textContent).toBe("7");
    fireEvent.change(screen.getByLabelText("Tugash"), { target: { value: "2026-07-20" } });
    await screen.findByLabelText("Boshlanish");
    fireEvent.change(screen.getByLabelText("Boshlanish"), { target: { value: "2026-07-01" } });
    await waitFor(() => expect(metrics).toHaveBeenLastCalledWith("2026-07-01", "2026-07-20"));
    fireEvent.click(screen.getByRole("button", { name: "90 kun" }));
    await waitFor(() => expect(metrics).toHaveBeenLastCalledWith(expect.any(String), expect.any(String)));
    fireEvent.change(screen.getByRole("combobox"), { target: { value: "Work" } });
    expect(screen.getAllByText("Ish / karyera").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Ish / karyera").length).toBe(2);
    expect(screen.getAllByText("Sayohat").length).toBe(1);
    expect(screen.getByTestId("activation-chart").textContent).toBe("100,96,70,70");
    expect(screen.getByTestId("health-radar")).toBeTruthy();
    expect(screen.getByTestId("goal-pie")).toBeTruthy();
    expect(screen.getByTestId("goal-conversion")).toBeTruthy();
    expect(screen.getByText("-4.1%")).toBeTruthy();
    expect(screen.queryByText("-4.099999999999994%")).toBeNull();
    expect(screen.getByText("0%")).toBeTruthy();
    expect(screen.queryByText("-0%")).toBeNull();
    expect(screen.getByRole("heading", { name: "AI va Speech xarajatlari" })).toBeTruthy();
    expect(screen.getAllByText("$3.75").length).toBeGreaterThan(0);
    expect(screen.getByText("/api/speaking/assess")).toBeTruthy();
    expect(screen.getByText("learner-1")).toBeTruthy();
  }, 10_000);

  it("warns when provider pricing is incomplete", async () => {
    metrics.mockResolvedValueOnce({ ...payload, variableCosts: { ...payload.variableCosts, pricingConfigured: false } });
    render(<MemoryRouter><FounderDashboardPage /></MemoryRouter>);
    expect(await screen.findByText("Narxlar to‘liq sozlanmagan")).toBeTruthy();
  });

  it("lets the super-admin change the daily variable-cost budget", async () => {
    render(<MemoryRouter><FounderDashboardPage /></MemoryRouter>);
    const input = await screen.findByLabelText("Kunlik AI budgeti");
    fireEvent.change(input, { target: { value: "50" } });
    fireEvent.click(screen.getByRole("button", { name: "Limitni saqlash" }));

    await waitFor(() => expect(setVariableCostBudget).toHaveBeenCalledWith(50));
    expect(await screen.findByText("Yangi limit saqlandi.")).toBeTruthy();
    expect(screen.getAllByText("$50.00").length).toBeGreaterThan(0);
  });

  it("hides the budget editor from ordinary admins", async () => {
    metrics.mockResolvedValueOnce({ ...payload, canManageVariableCostBudget: false });
    render(<MemoryRouter><FounderDashboardPage /></MemoryRouter>);
    expect(await screen.findByRole("heading", { name: "AI va Speech xarajatlari" })).toBeTruthy();
    expect(screen.queryByLabelText("Kunlik AI budgeti")).toBeNull();
  });

  it("opens a metrics SSE stream for the active range", async () => {
    render(<MemoryRouter><FounderDashboardPage /></MemoryRouter>);
    await waitFor(() => expect(metricsStream).toHaveBeenCalledTimes(1));
    expect(metricsStream).toHaveBeenCalledWith(expect.any(String), expect.any(String), expect.any(Function), expect.any(AbortSignal));
  });
});
