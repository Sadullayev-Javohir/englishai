import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AssessmentIntroPage } from "./AssessmentIntroPage";

const { start, setStoredLevel } = vi.hoisted(() => ({ start: vi.fn(), setStoredLevel: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { learning: { start } } }));
vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1", setStoredLevel }));
vi.mock("framer-motion", async () => {
  const React = await import("react");
  const motionProps = new Set(["animate", "initial", "layout", "transition", "variants", "viewport", "whileHover", "whileInView", "whileTap"]);
  const motion = new Proxy({}, { get: (_target, tag: string) => React.forwardRef(({ children, ...props }: Record<string, unknown>, ref) => {
    const elementProps = Object.fromEntries(Object.entries(props).filter(([key]) => !motionProps.has(key)));
    return React.createElement(tag, { ...elementProps, ref } as React.Attributes, children as React.ReactNode);
  }) });
  return { motion, useReducedMotion: () => true };
});

afterEach(() => { cleanup(); start.mockReset(); setStoredLevel.mockReset(); });
function renderPage() {
  render(<MemoryRouter initialEntries={["/assessment"]}><Routes><Route path="/assessment" element={<AssessmentIntroPage />} /><Route path="/placement" element={<div>PLACEMENT_DESTINATION</div>} /><Route path="/home" element={<div>HOME_DESTINATION</div>} /></Routes></MemoryRouter>);
}

describe("AssessmentIntroPage", () => {
  it("starts the real placement test", () => {
    renderPage(); fireEvent.click(screen.getByRole("button", { name: "Tayyorman, boshlaymiz!" }));
    expect(screen.getByText("PLACEMENT_DESTINATION")).toBeTruthy();
  });
  it("labels the alternative as explicit A1 assignment", () => {
    renderPage(); expect(screen.getByRole("button", { name: "A1 darajadan boshlayman" })).toBeTruthy();
  });
  it("waits for server success before caching A1", async () => {
    let resolveStart: () => void = () => {}; start.mockReturnValue(new Promise<void>((resolve) => { resolveStart = resolve; })); renderPage();
    fireEvent.click(screen.getByRole("button", { name: "A1 darajadan boshlayman" }));
    expect(setStoredLevel).not.toHaveBeenCalled(); expect(screen.queryByText("HOME_DESTINATION")).toBeNull();
    resolveStart(); await waitFor(() => expect(screen.getByText("HOME_DESTINATION")).toBeTruthy()); expect(setStoredLevel).toHaveBeenCalledWith(1);
  });
  it("keeps the learner on screen and announces server failure", async () => {
    start.mockRejectedValue(new Error("offline")); renderPage(); fireEvent.click(screen.getByRole("button", { name: "A1 darajadan boshlayman" }));
    expect(await screen.findByRole("alert")).toBeTruthy(); expect(setStoredLevel).not.toHaveBeenCalled(); expect(screen.queryByText("HOME_DESTINATION")).toBeNull();
  });
});
