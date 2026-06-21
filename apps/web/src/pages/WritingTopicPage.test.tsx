import { act, cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  CefrLevel,
  WritingAssessmentSource,
  WritingDimension,
} from "@/api/types";
import { writeLessonProgress } from "@/lib/lessonProgress";
import { WritingTopicPage } from "./WritingTopicPage";

const { submit, task, track } = vi.hoisted(() => ({
  submit: vi.fn(),
  task: vi.fn(),
  track: vi.fn().mockResolvedValue(undefined),
}));

vi.mock("@/lib/useAsync", () => ({
  useAsync: () => ({ data: task(), loading: false, error: null, reload: vi.fn() }),
}));

vi.mock("@/api/client", () => ({
  api: {
    writing: { task, submit },
    analytics: { track },
    images: { topicUrl: () => "/images/topic.png" },
  },
}));

vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/components/assistantContext", () => ({ publishAssistantContext: () => undefined }));
vi.mock("framer-motion", async () => {
  const React = await import("react");
  return {
    AnimatePresence: ({ children }: { children: React.ReactNode }) => <>{children}</>,
    motion: new Proxy({}, {
      get: (_target, tag: string) => React.forwardRef<HTMLElement, React.HTMLAttributes<HTMLElement>>(
        ({ children, ...props }, ref) => React.createElement(tag, { ...props, ref }, children),
      ),
    }),
  };
});

const writingTask = {
  topicId: "topic-1",
  taskId: "task-1",
  title: "My weekend",
  prompt: "Write about what you did last weekend.",
  level: CefrLevel.A2,
  minWords: 3,
  maxWords: 20,
  isReady: true,
  guidance: ["Use the past tense", "Add one feeling"],
  targetWords: [],
};

const assessment = {
  topicId: "topic-1",
  taskId: "task-1",
  dimensionScores: [
    { dimension: WritingDimension.TaskAchievement, score: 4 },
    { dimension: WritingDimension.Coherence, score: 3.5 },
    { dimension: WritingDimension.LexicalResource, score: 3 },
    { dimension: WritingDimension.GrammaticalAccuracy, score: 3.5 },
  ],
  issues: [],
  overallBand: 3.5,
  overallPercent: 70,
  estimatedLevel: CefrLevel.A2,
  assessmentSource: WritingAssessmentSource.Hermes,
  completion: null,
};

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/writing/topic/topic-1"]}>
      <Routes>
        <Route path="/writing/topic/:topicId" element={<WritingTopicPage />} />
        <Route path="/writing" element={<div>Writing catalog</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

async function openTask() {
  renderPage();
  fireEvent.click(await screen.findByRole("button", { name: "Darsni boshlash" }));
  fireEvent.click(await screen.findByRole("button", { name: "Yozishni boshlash" }));
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((res) => { resolve = res; });
  return { promise, resolve };
}

function getRegions() {
  const header = document.querySelector("[data-lesson-stage-header]") as HTMLElement;
  const main = document.querySelector("[data-lesson-stage-body]") as HTMLElement;
  const footer = document.querySelector("[data-lesson-stage-footer]") as HTMLElement;
  expect(header).toBeTruthy();
  expect(main).toBeTruthy();
  expect(footer).toBeTruthy();
  return { header, main, footer };
}

beforeEach(() => {
  Object.defineProperty(Element.prototype, "scrollIntoView", { configurable: true, value: vi.fn() });
  task.mockReturnValue(writingTask);
  submit.mockResolvedValue(assessment);
});

afterEach(() => {
  cleanup();
  localStorage.clear();
  vi.useRealTimers();
  task.mockReset();
  submit.mockReset();
  track.mockClear();
});

describe("WritingTopicPage unified lesson slides", () => {
  it("renders topic and prompt stages with semantic regions and one stage action", async () => {
    renderPage();
    let regions = getRegions();
    expect(regions.header.textContent).toContain("My weekend");
    expect(regions.header.textContent).toContain("1 / 5");
    expect(regions.main.textContent).toContain("My weekend");
    expect(within(regions.footer).getAllByRole("button")).toHaveLength(1);
    expect(within(regions.footer).getByRole("button", { name: "Darsni boshlash" })).toBeTruthy();

    fireEvent.click(within(regions.footer).getByRole("button", { name: "Darsni boshlash" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "Yozishni boshlash" })).toBeTruthy());
    regions = getRegions();
    expect(regions.main.textContent).toContain("Write about what you did last weekend.");
    expect(within(regions.footer).getAllByRole("button")).toHaveLength(1);
    expect(within(regions.footer).getByRole("button", { name: "Yozishni boshlash" })).toBeTruthy();
  });

  it("keeps the editor in main and blocks empty or over-limit submissions", async () => {
    await openTask();
    const { main, footer } = getRegions();
    const editor = within(main).getByRole("textbox");
    const action = within(footer).getByRole("button", { name: "Baholashga yuborish" });

    expect(action.hasAttribute("disabled")).toBe(true);
    fireEvent.click(action);
    expect(submit).not.toHaveBeenCalled();

    fireEvent.change(editor, { target: { value: Array.from({ length: 21 }, (_, i) => `word${i}`).join(" ") } });
    expect(action.hasAttribute("disabled")).toBe(true);
    fireEvent.click(action);
    expect(submit).not.toHaveBeenCalled();
  });

  it("blocks duplicate submit while assessment is loading", async () => {
    const pending = deferred<typeof assessment>();
    submit.mockReturnValue(pending.promise);
    await openTask();
    const editor = screen.getByRole("textbox");
    fireEvent.change(editor, { target: { value: "I visited my family yesterday." } });
    const action = screen.getByRole("button", { name: "Baholashga yuborish" });

    fireEvent.click(action);
    fireEvent.click(action);
    expect(submit).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("button", { name: "Baholanmoqda..." }).hasAttribute("disabled")).toBe(true);

    await act(async () => {
      pending.resolve(assessment);
      await pending.promise;
    });
  });

  it("does not auto-advance the assessment and uses manual Keyingi", async () => {
    await openTask();
    fireEvent.change(screen.getByRole("textbox"), { target: { value: "I visited my family yesterday." } });
    fireEvent.click(screen.getByRole("button", { name: "Baholashga yuborish" }));
    await act(async () => { await Promise.resolve(); });
    await waitFor(() => expect(screen.getByText("Umumiy ball: 70%")).toBeTruthy());

    const { header, main, footer } = getRegions();
    expect(header.textContent).toMatch(/My weekend|Write about what you did last weekend\./);
    expect(header.textContent).toContain("4 / 5");
    expect(main.textContent).toContain("70%");
    expect(main.textContent).not.toContain("lokal tekshiruv");
    expect(within(footer).getByRole("button", { name: "Davom etish" })).toBeTruthy();

    vi.useFakeTimers();
    act(() => { vi.advanceTimersByTime(10_000); });
    expect(screen.getByText("Umumiy ball: 70%")).toBeTruthy();
    fireEvent.click(within(footer).getByRole("button", { name: "Davom etish" }));
    expect(screen.getByRole("heading", { name: "Tabriklaymiz! Bu mavzu to'liq egallandi." })).toBeTruthy();
  });

  it("ignores a stale completed stage and starts from the hub", () => {
    vi.useFakeTimers();
    writeLessonProgress("writing-topic.topic-1.stage", "done");
    renderPage();
    const { footer } = getRegions();
    expect(within(footer).getAllByRole("button")).toHaveLength(1);
    expect(within(footer).getByRole("button", { name: "Darsni boshlash" })).toBeTruthy();

    act(() => { vi.advanceTimersByTime(10_000); });
    expect(screen.queryByText("Writing catalog")).toBeNull();
  });
});
