import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, WritingAssessmentSource, WritingDimension, type WritingTaskDto } from "@/api/types";
import { AssessmentLoadingOverlay, WritingTaskPage } from "./WritingTaskPage";

const { MockApiError, reloadMock, submitMock, useAsyncMock } = vi.hoisted(() => {
  class HoistedApiError extends Error {
    constructor(public status: number, message = `HTTP ${status}`, public body?: unknown) {
      super(message);
    }
  }

  const reload = vi.fn();
  return {
    MockApiError: HoistedApiError,
    reloadMock: reload,
    submitMock: vi.fn(),
    useAsyncMock: vi.fn(() => ({ data: null as WritingTaskDto | null, loading: false, error: null as unknown, reload })),
  };
});

vi.mock("@/lib/useAsync", () => ({
  useAsync: () => useAsyncMock(),
}));

vi.mock("@/api/client", () => ({
  api: { writing: { task: vi.fn(), submit: submitMock } },
  ApiError: MockApiError,
}));

vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));

afterEach(() => {
  cleanup();
  localStorage.clear();
  reloadMock.mockClear();
  submitMock.mockReset();
  useAsyncMock.mockClear();
  useAsyncMock.mockReturnValue({ data: null, loading: false, error: null, reload: reloadMock });
});

async function openEditor() {
  fireEvent.click(await screen.findByRole("button", { name: "Yozishni boshlash" }));
  return screen.findByLabelText("Sizning javobingiz");
}

describe("Writing Pen stage layout", () => {
  it("keeps review and final stages inside the viewport-owned runner body", async () => {
    useAsyncMock.mockReturnValue({
      data: {
        topicId: "travel",
        taskId: "task-1",
        title: "Travel",
        prompt: "Write about a trip.",
        level: CefrLevel.A2,
        minWords: 1,
        maxWords: 100,
        isReady: true,
        guidance: [],
        targetWords: [],
      },
      loading: false,
      error: null,
      reload: reloadMock,
    });
    submitMock.mockResolvedValue({
      topicId: "travel",
      taskId: "task-1",
      dimensionScores: [
        { dimension: WritingDimension.TaskAchievement, score: 4 },
        { dimension: WritingDimension.Coherence, score: 4 },
        { dimension: WritingDimension.LexicalResource, score: 4 },
        { dimension: WritingDimension.GrammaticalAccuracy, score: 4 },
      ],
      issues: [],
      overallBand: 4,
      overallPercent: 80,
      estimatedLevel: CefrLevel.A2,
      assessmentSource: WritingAssessmentSource.Hermes,
      completion: null,
    });

    render(
      <MemoryRouter initialEntries={["/writing/task/travel"]}>
        <Routes>
          <Route path="/writing/task/:topicId" element={<WritingTaskPage />} />
        </Routes>
      </MemoryRouter>,
    );

    const editor = await openEditor();
    fireEvent.change(editor, { target: { value: "My trip was wonderful." } });
    fireEvent.click(screen.getByRole("button", { name: /AI bilan tekshirish/i }));

    const result = await screen.findByText("80 / 100");
    const resultBody = result.closest(".writing-task-stage__body");
    const resultStage = result.closest(".writing-task-stage");
    const resultPage = result.closest(".writing-task-page");
    expect(resultBody?.className).toContain("writing-task-stage__body");
    expect(resultStage?.className).toContain("writing-task-stage--review");
    expect(resultPage?.className).toBe("writing-task-page");

    fireEvent.click(screen.getByRole("button", { name: /Natijani yakunlash/i }));
    expect(await screen.findByText("Fikringiz yetib bordi!")).toBeTruthy();
    expect(screen.getByText("My trip was wonderful.").closest(".writing-task-stage")?.className)
      .toContain("writing-task-stage--result");
  });
});

describe("Writing viewport scroll policy", () => {
  it("keeps short intro and editor stages inside the page runner", async () => {
    localStorage.clear();
    useAsyncMock.mockReturnValue({
      data: {
        topicId: "travel",
        taskId: "task-1",
        title: "Travel",
        prompt: "Write a detailed report about a trip.",
        level: CefrLevel.A2,
        minWords: 1,
        maxWords: 100,
        isReady: true,
        guidance: ["Describe where you went.", "Explain what happened.", "Add a conclusion."],
        targetWords: [],
      },
      loading: false,
      error: null,
      reload: reloadMock,
    });

    renderTaskPage("travel");

    const introPrompt = screen.getByText("Write a detailed report about a trip.");
    expect(introPrompt.closest(".writing-task-stage")?.className).toContain("writing-task-stage--intro");
    expect(introPrompt.closest(".writing-task-stage")?.querySelector(".writing-task-stage__body")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Yozishni boshlash" }));
    const editor = await screen.findByLabelText("Sizning javobingiz");
    expect(editor.closest(".writing-task-stage")?.className).toContain("writing-task-stage--editor");
  });
});

describe("Writing editor actions", () => {
  it("renders Pen progress for each stage and returns to the introduction", async () => {
    useAsyncMock.mockReturnValue({
      data: {
        topicId: "travel",
        taskId: "task-1",
        title: "Travel",
        prompt: "Write about a trip.",
        level: CefrLevel.A2,
        minWords: 1,
        maxWords: 100,
        isReady: true,
        guidance: [],
        targetWords: [],
      },
      loading: false,
      error: null,
      reload: reloadMock,
    });
    renderTaskPage("travel");

    expect(screen.getByText("1 / 4")).toBeTruthy();
    expect(document.querySelector(".vocabulary-progress")).toBeTruthy();
    expect(screen.getAllByText("Topshiriq")).toHaveLength(2);
    expect(screen.getByText("Matn yozish")).toBeTruthy();
    expect(screen.getByText("AI izohi")).toBeTruthy();
    expect(screen.getByText("Yakuniy natija")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Yozishni boshlash" }));
    expect(await screen.findByText("2 / 4")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Oldingi bosqich" }));
    expect(await screen.findByText("1 / 4")).toBeTruthy();
  });
});

describe("Writing AI failure", () => {
  it("keeps the text and exposes a retryable AI error instead of a local result", async () => {
    useAsyncMock.mockReturnValue({
      data: {
        topicId: "travel",
        taskId: "task-1",
        title: "Travel",
        prompt: "Write about a trip.",
        level: CefrLevel.A2,
        minWords: 1,
        maxWords: 100,
        isReady: true,
        guidance: [],
        targetWords: [],
      },
      loading: false,
      error: null,
      reload: reloadMock,
    });
    submitMock.mockRejectedValue(new MockApiError(504, "", {
      code: "timeout",
      correlationId: "writing-123",
    }));
    renderTaskPage("travel");
    const editor = await openEditor() as HTMLTextAreaElement;
    fireEvent.change(editor, { target: { value: "My saved answer." } });
    fireEvent.click(screen.getByRole("button", { name: /AI bilan tekshirish/i }));

    expect(await screen.findByText(/AI belgilangan vaqtda javob bermadi/)).toBeTruthy();
    expect(screen.getByText(/writing-123/)).toBeTruthy();
    expect(editor.value).toBe("My saved answer.");
    expect(screen.queryByText(/lokal tekshiruv/)).toBeNull();
  });
});

describe("Writing assessment loading state", () => {
  it("announces results inside the reserved feedback slot", () => {
    render(<AssessmentLoadingOverlay />);

    const status = screen.getByRole("status");
    expect(status.className).toContain("writing-task-assessment");
    expect(status.textContent).toContain("Natijalar chiqmoqda...");
  });

  it("uses the compact assessment state design", () => {
    render(<AssessmentLoadingOverlay />);

    const status = screen.getByRole("status");
    expect(status.querySelector(".animate-spin")).toBeTruthy();
  });
});

describe("Writing task load recovery", () => {
  it("returns to the writing catalog when the topic no longer exists", () => {
    useAsyncMock.mockReturnValue({
      data: null,
      loading: false,
      error: new MockApiError(404),
      reload: reloadMock,
    });

    renderTaskPage();

    expect(screen.getByText("Bu yozish mavzusi endi mavjud emas.")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Yozish mavzulari" }));
    expect(screen.getByTestId("writing-catalog-route")).toBeTruthy();
  });

  it("offers retry for a transient task load error", () => {
    useAsyncMock.mockReturnValue({
      data: null,
      loading: false,
      error: new MockApiError(500),
      reload: reloadMock,
    });

    renderTaskPage();

    expect(screen.getByText("Topshiriqni hozir yuklab bo'lmadi. Qayta urinib ko'ring.")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" }));
    expect(useAsyncMock).toHaveBeenCalledTimes(2);
    expect(screen.getByTestId("writing-task-route")).toBeTruthy();
  });
});

function renderTaskPage(topicId = "missing-topic") {
  return render(
    <MemoryRouter initialEntries={[`/writing/task/${topicId}`]}>
      <Routes>
        <Route path="/writing/task/:topicId" element={<div data-testid="writing-task-route"><WritingTaskPage /></div>} />
        <Route path="/writing" element={<div data-testid="writing-catalog-route" />} />
      </Routes>
    </MemoryRouter>,
  );
}
