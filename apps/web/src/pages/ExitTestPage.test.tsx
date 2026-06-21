import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, PlacementItemKind, TestStage } from "@/api/types";
import { ExitTestPage } from "./ExitTestPage";
import { releaseKioskLock } from "@/lib/kioskLock";

const { start, finalize, resume, answer, answerWriting, reportIntegrityViolation } = vi.hoisted(() => ({ start: vi.fn(), finalize: vi.fn(), resume: vi.fn(), answer: vi.fn(), answerWriting: vi.fn(), reportIntegrityViolation: vi.fn() }));
vi.mock("@/api/client", () => ({
  api: { levels: { exitTest: { start, finalize } }, placement: { resume, answer, answerWriting, reportIntegrityViolation } },
  apiErrorDetails: () => null,
  ApiError: class ApiError extends Error { constructor(public status: number, message: string) { super(message); } },
}));
vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1", getStoredLevel: () => 2 }));
vi.mock("@/components/ParrotLogo", () => ({ ParrotLogo: () => <div>PARROT</div> }));
vi.mock("framer-motion", async () => {
  const React = await import("react");
  const motionProps = new Set(["animate", "initial", "layout", "transition", "variants", "viewport", "whileHover", "whileInView", "whileTap"]);
  const motion = new Proxy({}, { get: (_target, tag: string) => React.forwardRef(({ children, ...props }: Record<string, unknown>, ref) => {
    const elementProps = Object.fromEntries(Object.entries(props).filter(([key]) => !motionProps.has(key)));
    return React.createElement(tag, { ...elementProps, ref } as React.Attributes, children as React.ReactNode);
  }) });
  return { motion, useReducedMotion: () => true };
});

const item = {
  kind: PlacementItemKind.MultipleChoice,
  id: "q-1",
  stage: TestStage.Grammar,
  difficulty: CefrLevel.A2,
  prompt: "Choose the correct answer",
  options: ["One", "Two"],
  hasAudio: false,
  passageText: null,
  minWords: null,
  maxWords: null,
  stageNumber: 2,
  stageCount: 5,
  itemNumberInStage: 1,
  itemsInStage: 4,
  completedItems: 3,
  totalItems: 12,
};

let fullscreenElement: Element | null = null;
Object.defineProperty(document, "fullscreenEnabled", { configurable: true, value: true });
Object.defineProperty(document, "fullscreenElement", { configurable: true, get: () => fullscreenElement });
Object.defineProperty(document.documentElement, "requestFullscreen", {
  configurable: true,
  value: vi.fn(async () => {
    fullscreenElement = document.documentElement;
    document.dispatchEvent(new Event("fullscreenchange"));
  }),
});
Object.defineProperty(document, "exitFullscreen", {
  configurable: true,
  value: vi.fn(async () => {
    fullscreenElement = null;
    document.dispatchEvent(new Event("fullscreenchange"));
  }),
});

afterEach(() => {
  cleanup();
  // The kiosk lock is module-level state: leaking it would pin html/body for later suites.
  releaseKioskLock();
  sessionStorage.clear();
  fullscreenElement = null;
  start.mockReset();
  finalize.mockReset();
  resume.mockReset();
  answer.mockReset();
  answerWriting.mockReset();
  reportIntegrityViolation.mockReset();
});
function renderPage() {
  render(<MemoryRouter initialEntries={["/levels/exit-test"]}><Routes><Route path="/levels/exit-test" element={<ExitTestPage />} /><Route path="/levels" element={<div>LEVELS_DESTINATION</div>} /><Route path="/levels/exit-test/result" element={<div>RESULT_DESTINATION</div>} /></Routes></MemoryRouter>);
}

describe("ExitTestPage", () => {
  it("shows the exit-test intro and level transition", () => {
    renderPage();
    expect(screen.getByRole("heading", { name: "Bu darajani yakunlash vaqti!" })).toBeTruthy();
    expect(screen.getByText("A2")).toBeTruthy();
    expect(screen.getByText("B1")).toBeTruthy();
  });

  it("starts the test and submits a selected answer", async () => {
    start.mockResolvedValue({ sessionId: "session-1", firstItem: item });
    answer.mockResolvedValue({ isTestCompleted: true, nextItem: null });
    finalize.mockResolvedValue({ passed: true });
    renderPage();
    fireEvent.click(screen.getByRole("button", { name: /Chiqish testini boshlash/ }));
    expect(await screen.findByRole("heading", { name: "Choose the correct answer" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /Two/ }));
    fireEvent.click(screen.getByRole("button", { name: "Javobni tekshirish" }));
    await waitFor(() => expect(answer).toHaveBeenCalledWith("session-1", "q-1", 1));
    await waitFor(() => expect(finalize).toHaveBeenCalledWith("session-1"));
  });

  it("still starts the test on a browser without the Fullscreen API", async () => {
    // iOS Safari / WKWebView: no fullscreenEnabled and no requestFullscreen at all. The test has
    // to fall back to the kiosk lock instead of refusing to run (it used to refuse).
    Object.defineProperty(document, "fullscreenEnabled", { configurable: true, value: false });
    const root = document.documentElement as unknown as Record<string, unknown>;
    const request = root.requestFullscreen;
    delete root.requestFullscreen;
    start.mockResolvedValue({ sessionId: "session-1", firstItem: item });
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: /Chiqish testini boshlash/ }));

    expect(await screen.findByRole("heading", { name: "Choose the correct answer" })).toBeTruthy();
    expect(document.body.classList.contains("secure-assessment-kiosk")).toBe(true);
    Object.defineProperty(document, "fullscreenEnabled", { configurable: true, value: true });
    Object.defineProperty(root, "requestFullscreen", { configurable: true, value: request });
  });

  it("shows a scoped error state when start fails", async () => {
    start.mockRejectedValue(new Error("offline"));
    renderPage();
    fireEvent.click(screen.getByRole("button", { name: /Chiqish testini boshlash/ }));
    expect(await screen.findByRole("alert")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Qayta urinish" })).toBeTruthy();
  });

  it("retries finalize without restarting the completed test", async () => {
    start.mockResolvedValue({ sessionId: "session-1", firstItem: item });
    answer.mockResolvedValue({ isTestCompleted: true, nextItem: null });
    finalize.mockRejectedValueOnce(new Error("temporary")).mockResolvedValueOnce({ passed: true });
    renderPage();
    fireEvent.click(screen.getByRole("button", { name: /Chiqish testini boshlash/ }));
    expect(await screen.findByRole("heading", { name: "Choose the correct answer" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /Two/ }));
    fireEvent.click(screen.getByRole("button", { name: "Javobni tekshirish" }));
    expect(await screen.findByRole("alert")).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" }));

    await waitFor(() => expect(finalize).toHaveBeenCalledTimes(2));
    expect(start).toHaveBeenCalledTimes(1);
  });

  it("requires a user gesture before restoring an active exit test", async () => {
    sessionStorage.setItem("englishai.exit-test.session", "session-1");
    resume.mockResolvedValue({ sessionId: "session-1", isCompleted: false, currentItem: item });

    renderPage();

    const continueButton = screen.getByRole("button", { name: "Testni davom ettirish" });
    expect(continueButton).toBeTruthy();
    expect(resume).not.toHaveBeenCalled();
    fireEvent.click(continueButton);
    expect(await screen.findByRole("heading", { name: "Choose the correct answer" })).toBeTruthy();
    expect(start).not.toHaveBeenCalled();
    expect(resume).toHaveBeenCalledWith("session-1");
  });

  it("recovers from a lost answer response without restarting the test", async () => {
    start.mockResolvedValue({ sessionId: "session-1", firstItem: item });
    answer.mockRejectedValue(new Error("response lost"));
    resume.mockResolvedValue({
      sessionId: "session-1",
      isCompleted: false,
      currentItem: { ...item, id: "q-2", prompt: "Recovered next question", completedItems: 4 },
    });
    renderPage();
    fireEvent.click(screen.getByRole("button", { name: /Chiqish testini boshlash/ }));
    expect(await screen.findByRole("heading", { name: "Choose the correct answer" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /Two/ }));
    fireEvent.click(screen.getByRole("button", { name: "Javobni tekshirish" }));

    expect(await screen.findByRole("heading", { name: "Recovered next question" })).toBeTruthy();
    expect(start).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("enforces the writing task word limits before submission", async () => {
    start.mockResolvedValue({
      sessionId: "session-1",
      firstItem: {
        ...item,
        kind: PlacementItemKind.Writing,
        id: "writing-1",
        prompt: "Write about your day",
        options: null,
        minWords: 3,
        maxWords: 5,
      },
    });
    renderPage();
    fireEvent.click(screen.getByRole("button", { name: /Chiqish testini boshlash/ }));
    const textbox = await screen.findByRole("textbox");
    const submit = screen.getByRole("button", { name: "Yuborish va davom etish" });

    fireEvent.change(textbox, { target: { value: "Two words" } });
    expect((submit as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByRole("alert").textContent).toContain("3–5");

    fireEvent.change(textbox, { target: { value: "Exactly three words" } });
    expect((submit as HTMLButtonElement).disabled).toBe(false);
  });
});
