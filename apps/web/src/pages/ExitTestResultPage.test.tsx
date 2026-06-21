import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, TestStage } from "@/api/types";
import type { FinalizeLevelExitTestResult } from "@/api/types";
import { ExitTestResultPage } from "./ExitTestResultPage";

const { setStoredLevel } = vi.hoisted(() => ({ setStoredLevel: vi.fn() }));

vi.mock("@/app/session", () => ({ setStoredLevel }));
vi.mock("@/lesson/Confetti", () => ({ Confetti: () => null }));
vi.mock("framer-motion", async () => {
  const React = await import("react");
  const motion = new Proxy({}, {
    get: (_target, tag: string) => React.forwardRef(
      ({ children, ...props }: Record<string, unknown>, ref) => {
        const domProps = { ...props };
        delete domProps.initial;
        delete domProps.animate;
        delete domProps.transition;
        delete domProps.whileHover;
        delete domProps.whileTap;
        return React.createElement(tag, { ...domProps, ref } as React.Attributes, children as React.ReactNode);
      },
    ),
  });
  return { motion };
});

afterEach(() => {
  cleanup();
  setStoredLevel.mockReset();
});

const passedResult: FinalizeLevelExitTestResult = {
  passed: true,
  advanced: true,
  testedLevel: CefrLevel.A2,
  newLevel: CefrLevel.B1,
  masteredSkillCount: 5,
  requiredMasteredSkills: 4,
  minimumOverallScore: 72,
  minimumStageScore: 58,
  productiveStageFloor: 58,
  failedStages: [],
  result: {
    overallLevel: CefrLevel.B1,
    overallScore: 86,
    stageResults: [
      { stage: TestStage.Vocabulary, level: CefrLevel.B1, score: 88 },
      { stage: TestStage.Grammar, level: CefrLevel.B1, score: 85 },
      { stage: TestStage.Listening, level: CefrLevel.B1, score: 82 },
      { stage: TestStage.Reading, level: CefrLevel.A2, score: 79 },
      { stage: TestStage.Writing, level: CefrLevel.B1, score: 90 },
      { stage: TestStage.Speaking, level: CefrLevel.B1, score: 84 },
    ],
  },
};

describe("ExitTestResultPage direct-open integrity", () => {
  it("shows an honest recovery state and never persists a fake level without router result state", () => {
    render(
      <MemoryRouter initialEntries={["/levels/exit-test/result"]}>
        <Routes>
          <Route path="/levels/exit-test/result" element={<ExitTestResultPage />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByRole("alert")).toBeTruthy();
    expect(screen.getByRole("link", { name: "Darajalar xaritasiga qaytish" })).toBeTruthy();
    expect(setStoredLevel).not.toHaveBeenCalled();
    expect(screen.queryByText("B1")).toBeNull();
  });

  it("renders the passed summary, skill stats, recommendation, and persists the advanced level", async () => {
    render(
      <MemoryRouter initialEntries={[{ pathname: "/levels/exit-test/result", state: { result: passedResult } }]}>
        <Routes>
          <Route path="/levels/exit-test/result" element={<ExitTestResultPage />} />
          <Route path="/levels" element={<h1>Darajalar xaritasi</h1>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByRole("heading", { name: "Test muvaffaqiyatli topshirildi!" })).toBeTruthy();
    expect(screen.getByLabelText("Umumiy natija 86%")).toBeTruthy();
    expect(screen.getByText("Ko'nikma bo'yicha natija")).toBeTruthy();
    expect(screen.getByText("Keyingi darajani boshlang")).toBeTruthy();
    expect(screen.getAllByText(/88%/).length).toBeGreaterThan(0);
    await waitFor(() => expect(setStoredLevel).toHaveBeenCalledWith(CefrLevel.B1));

    fireEvent.click(screen.getAllByRole("button", { name: "Darajalarga qaytish" })[0]);
    expect(screen.getByRole("heading", { name: "Darajalar xaritasi" })).toBeTruthy();
  });

  it("renders a retry CTA for a failed result without changing the stored level", () => {
    const failedResult: FinalizeLevelExitTestResult = {
      ...passedResult,
      passed: false,
      advanced: false,
      newLevel: CefrLevel.A2,
      masteredSkillCount: 2,
      failedStages: [TestStage.Grammar],
      result: { ...passedResult.result, overallLevel: CefrLevel.A2, overallScore: 58 },
    };

    render(
      <MemoryRouter initialEntries={[{ pathname: "/levels/exit-test/result", state: { result: failedResult } }]}>
        <Routes>
          <Route path="/levels/exit-test/result" element={<ExitTestResultPage />} />
          <Route path="/levels/exit-test" element={<h1>Chiqish testi</h1>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByRole("heading", { name: "Hali bir oz mashq kerak" })).toBeTruthy();
    expect(screen.getByText("Qayta urinishga tayyorlaning")).toBeTruthy();
    expect(setStoredLevel).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" }));
    expect(screen.getByRole("heading", { name: "Chiqish testi" })).toBeTruthy();
  });
});
