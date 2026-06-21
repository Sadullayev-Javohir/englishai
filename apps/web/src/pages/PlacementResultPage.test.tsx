import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, TestStage } from "@/api/types";
import { PlacementResultPage } from "./PlacementResultPage";

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
        return React.createElement(tag, { ...domProps, ref } as React.Attributes, children as React.ReactNode);
      },
    ),
  });
  return { motion };
});

afterEach(() => {
  cleanup();
  sessionStorage.clear();
  setStoredLevel.mockReset();
});

const result = {
  overallLevel: CefrLevel.B1,
  overallScore: 78,
  stageResults: [
    { stage: TestStage.Vocabulary, level: CefrLevel.B1, score: 82 },
    { stage: TestStage.Grammar, level: CefrLevel.B1, score: 76 },
    { stage: TestStage.Listening, level: CefrLevel.A2, score: 69 },
    { stage: TestStage.Reading, level: CefrLevel.B1, score: 84 },
    { stage: TestStage.Writing, level: CefrLevel.B1, score: 77 },
    { stage: TestStage.Speaking, level: CefrLevel.B1, score: 80 },
  ],
};

function renderResultPage() {
  return render(
    <MemoryRouter initialEntries={[{ pathname: "/placement/result", state: { result } }]}> 
      <Routes>
        <Route path="/placement/result" element={<PlacementResultPage />} />
        <Route path="/home" element={<div>HOME_DESTINATION</div>} />
        <Route path="/assessment" element={<div>ASSESSMENT_DESTINATION</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("PlacementResultPage", () => {
  it("renders the level, statistics, skills and recommendation", () => {
    renderResultPage();

    expect(screen.getByRole("heading", { name: "Ajoyib natija!" })).toBeTruthy();
    expect(screen.getByLabelText("Aniqlangan daraja: B1")).toBeTruthy();
    expect(screen.getAllByText("Umumiy natija: 78%").length).toBeGreaterThan(0);
    expect(screen.getByRole("heading", { name: "SIZNING KO‘NIKMALARINGIZ" })).toBeTruthy();
    expect(screen.getByText("Listening’ni birga kuchaytiramiz.")).toBeTruthy();
    expect(screen.getAllByRole("article")).toHaveLength(6);
    expect(setStoredLevel).toHaveBeenCalledWith(CefrLevel.B1);
  });

  it("keeps the dashboard CTA navigation behavior", () => {
    renderResultPage();
    fireEvent.click(screen.getByRole("button", { name: "Birinchi darsimga!" }));
    expect(screen.getByText("HOME_DESTINATION")).toBeTruthy();
  });

  it("redirects a direct open without a result", () => {
    render(
      <MemoryRouter initialEntries={["/placement/result"]}>
        <Routes>
          <Route path="/placement/result" element={<PlacementResultPage />} />
          <Route path="/assessment" element={<div>ASSESSMENT_DESTINATION</div>} />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByText("ASSESSMENT_DESTINATION")).toBeTruthy();
    expect(setStoredLevel).not.toHaveBeenCalled();
  });
});
