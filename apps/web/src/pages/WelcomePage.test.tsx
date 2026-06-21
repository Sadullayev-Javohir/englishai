import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { WelcomePage } from "./WelcomePage";

const { start, setStoredLevel } = vi.hoisted(() => ({
  start: vi.fn(),
  setStoredLevel: vi.fn(),
}));

vi.mock("@/api/client", () => ({ api: { learning: { start } } }));
vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1", setStoredLevel }));
vi.mock("framer-motion", async () => {
  const React = await import("react");
  const motionProps = new Set(["animate", "initial", "layout", "transition", "variants", "viewport", "whileHover", "whileInView", "whileTap"]);
  const motion = new Proxy({}, {
    get: (_target, tag: string) => React.forwardRef(
      ({ children, ...props }: Record<string, unknown>, ref) => {
        const elementProps = Object.fromEntries(Object.entries(props).filter(([key]) => !motionProps.has(key)));
        return React.createElement(tag, { ...elementProps, ref } as React.Attributes, children as React.ReactNode);
      },
    ),
  });
  return { motion, useReducedMotion: () => true };
});

afterEach(() => {
  cleanup();
  start.mockReset();
  setStoredLevel.mockReset();
});

function renderPage() {
  render(
    <MemoryRouter initialEntries={["/welcome"]}>
      <Routes>
        <Route path="/welcome" element={<WelcomePage />} />
        <Route path="/home" element={<div>HOME_DESTINATION</div>} />
        <Route path="/assessment" element={<div>ASSESSMENT_DESTINATION</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("WelcomePage onboarding choice", () => {
  it("renders the Pen starting-point hierarchy and original artwork", () => {
    renderPage();
    expect(screen.getByTestId("welcome-viewport")).toBeTruthy();
    expect(screen.getByRole("heading", { level: 1, name: "Qayerdan boshlaymiz?" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Har kimning yo‘li o‘zgacha." })).toBeTruthy();
    expect(screen.getByRole("img", { name: /So‘zlardan yangi dunyoga/ })).toBeTruthy();
    expect(screen.getByLabelText("CEFR darajalari").children).toHaveLength(6);
  });

  it("shows two explicit starting paths", () => {
    renderPage();
    expect(screen.getByRole("button", { name: "Darajamni aniqlash" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "A1 darajadan boshlash" })).toBeTruthy();
  });

  it("keeps both starting paths visible without an extra onboarding step", () => {
    renderPage();
    expect(screen.getByText("Noldan boshlayman")).toBeTruthy();
    expect(screen.getByText("O‘z darajamdan")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Keyingisi" })).toBeNull();
  });

  it("navigates to assessment from the primary path", () => {
    renderPage();
    fireEvent.click(screen.getByRole("button", { name: "Darajamni aniqlash" }));
    expect(screen.getByText("ASSESSMENT_DESTINATION")).toBeTruthy();
  });

  it("waits for the server before storing A1 and navigating home", async () => {
    let resolveStart: () => void = () => {};
    start.mockReturnValue(new Promise<void>((resolve) => { resolveStart = resolve; }));
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: "A1 darajadan boshlash" }));
    expect(setStoredLevel).not.toHaveBeenCalled();
    expect(screen.queryByText("HOME_DESTINATION")).toBeNull();

    resolveStart();
    await waitFor(() => expect(screen.getByText("HOME_DESTINATION")).toBeTruthy());
    expect(setStoredLevel).toHaveBeenCalledWith(1);
  });

  it("shows an error and stays on welcome when A1 setup fails", async () => {
    start.mockRejectedValue(new Error("offline"));
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: "A1 darajadan boshlash" }));
    expect(await screen.findByRole("alert")).toBeTruthy();
    expect(setStoredLevel).not.toHaveBeenCalled();
    expect(screen.queryByText("HOME_DESTINATION")).toBeNull();
  });
});
