import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { writeLessonProgress } from "@/lib/lessonProgress";
import { ListeningTopicPage } from "./ListeningTopicPage";

const { checkAnswer, exercise, submit, track } = vi.hoisted(() => ({
  checkAnswer: vi.fn(),
  exercise: vi.fn(),
  submit: vi.fn(),
  track: vi.fn().mockResolvedValue(undefined),
}));

vi.mock("@/lib/useAsync", () => ({
  useAsync: () => ({ data: exercise(), loading: false, error: null, reload: vi.fn() }),
}));

vi.mock("@/api/client", () => ({
  api: {
    listening: {
      exercise,
      audioUrl: () => "/audio/listening.mp3",
      checkAnswer,
      submit,
    },
    analytics: { track },
    images: { topicUrl: () => "/images/topic.png" },
  },
  ApiError: class ApiError extends Error {},
}));

vi.mock("@/app/session", () => ({
  getLearnerId: () => "learner-1",
}));

vi.mock("@/components/assistantContext", () => ({
  publishAssistantContext: () => undefined,
}));

vi.mock("@/components/lesson/useLessonSounds", () => ({
  useLessonSounds: () => ({
    playAnswer: vi.fn(),
    playComplete: vi.fn(),
  }),
}));

vi.mock("@/components/TopicLessonResult", () => ({
  TopicLessonResult: ({ onRetry, retryLabel }: { onRetry: () => void; retryLabel: string }) => (
    <button type="button" onClick={onRetry}>{retryLabel}</button>
  ),
}));

const topic = {
  topicId: "topic-1",
  title: "B2 Listening",
  topic: "Travel",
  level: CefrLevel.B2,
  wordCount: 0,
  isReady: true,
  transcript: "Listening transcript",
  targetWords: [],
  questions: [
    { id: "question-1", prompt: "Question one", options: ["A", "B"] },
    { id: "question-2", prompt: "Question two", options: ["A", "B"] },
    { id: "question-3", prompt: "Question three", options: ["A", "B"] },
  ],
};

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/listening/topic/topic-1"]}>
      <Routes>
        <Route path="/listening/topic/:topicId" element={<ListeningTopicPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

afterEach(() => {
  vi.clearAllTimers();
  vi.useRealTimers();
  cleanup();
  localStorage.clear();
  exercise.mockReset();
  checkAnswer.mockReset();
  submit.mockReset();
  track.mockClear();
});

async function openAudio() {
  renderPage();
  fireEvent.click(await screen.findByRole("button", { name: /Darsni boshlash/i }));
  await screen.findByRole("button", { name: /Qayta tinglash/i });
  return document.querySelector("audio") as HTMLAudioElement;
}

async function openPractice() {
  const audio = await openAudio();
  fireEvent.canPlay(audio);
  fireEvent.play(audio);
  fireEvent.click(screen.getByRole("button", { name: /Tushunishni tekshirish/i }));
  await screen.findAllByText("Question one");
}

async function chooseAndCheck(optionName = /^A\. A$/) {
  fireEvent.click(screen.getByRole("button", { name: optionName }));
  fireEvent.click(screen.getByRole("button", { name: "Tekshirish" }));
}

describe("ListeningTopicPage retry", () => {
  it("recovers an interrupted quiz as a fresh audio-stage attempt", async () => {
    exercise.mockReturnValue(topic);
    writeLessonProgress("listening-topic.topic-1.stage", "practice");
    writeLessonProgress("listening-topic.topic-1.practice.index", 2);

    renderPage();

    expect(await screen.findByRole("button", { name: /Darsni boshlash/i })).toBeTruthy();
    expect(screen.queryAllByText("Question three")[0] ?? null).toBeNull();
    expect(submit).not.toHaveBeenCalled();
  });

  it("submits all three answers and restarts the retry from question one", async () => {
    exercise.mockReturnValue(topic);
    checkAnswer.mockImplementation((_topicId: string, questionId: string, selectedOptionIndex: number) =>
      Promise.resolve({
        questionId,
        selectedOptionIndex,
        correctOptionIndex: selectedOptionIndex,
        isCorrect: true,
        hint: null,
      }));
    submit.mockResolvedValue({
      topicId: "topic-1",
      totalQuestions: 3,
      correctCount: 3,
      scorePercent: 100,
      passed: true,
      outcomes: [],
      completion: null,
    });
    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: /Darsni boshlash/i }));
    await screen.findByRole("button", { name: /Qayta tinglash/i });
    const audio = document.querySelector("audio");
    expect(audio).not.toBeNull();
    fireEvent.canPlay(audio!);
    fireEvent.play(audio!);
    const startQuiz = screen.getByRole("button", { name: /Tushunishni tekshirish/i });
    await waitFor(() => expect(startQuiz.hasAttribute("disabled")).toBe(false));
    startQuiz.click();

    for (const questionText of ["Question one", "Question two", "Question three"]) {
      expect((await screen.findAllByText(questionText)).length).toBeGreaterThan(0);
      await chooseAndCheck();
      await waitFor(() => expect(screen.getByRole("button", { name: questionText === "Question three" ? "Tekshirish" : "Keyingi savol" })).toBeTruthy());
      fireEvent.click(screen.getByRole("button", { name: questionText === "Question three" ? "Tekshirish" : "Keyingi savol" }));
    }

    await waitFor(() => {
      expect(submit).toHaveBeenCalledWith("topic-1", "learner-1", [
        { questionId: "question-1", selectedOptionIndex: 0 },
        { questionId: "question-2", selectedOptionIndex: 0 },
        { questionId: "question-3", selectedOptionIndex: 0 },
      ]);
    });

    fireEvent.click(await screen.findByRole("button", { name: "Natijani ko'rish" }));
    fireEvent.click(await screen.findByRole("button", { name: "Qayta tinglash va test" }));
    await screen.findByRole("button", { name: /Qayta tinglash/i });
    expect(screen.getByRole("button", { name: "Tinglash" })).toBeTruthy();
  }, 15_000);
});

describe("ListeningTopicPage unified slides", () => {
  it("keeps audio controls in a dedicated fixed action region for long transcripts", async () => {
    exercise.mockReturnValue({ ...topic, transcript: "Long listening transcript ".repeat(120) });
    const audio = await openAudio();
    fireEvent.canPlay(audio);

    const controls = screen.getByTestId("listening-audio-controls");
    expect(controls.contains(screen.getByRole("button", { name: "Tinglash" }))).toBe(true);
    expect(controls.contains(screen.getByRole("button", { name: /Qayta tinglash/i }))).toBe(true);
    expect(controls.parentElement?.querySelector(".listening-topic__audio-content")).not.toBeNull();
    expect(controls.closest(".listening-topic__audio-card")?.classList.contains("listening-topic__audio-card")).toBe(true);
    expect(screen.getByRole("progressbar", { name: "Dars bosqichlari" }).getAttribute("aria-valuetext")).toContain("1 / 3 · Audio");
  });

  it("reserves feedback and footer geometry before an answer is selected", async () => {
    exercise.mockReturnValue(topic);
    await openPractice();

    expect(screen.getByRole("progressbar", { name: "Dars bosqichlari" }).getAttribute("aria-valuetext")).toContain("2 / 3 · Mashq · Savol 1 / 3");
    expect(document.querySelector(".listening-topic__options-grid")).not.toBeNull();
    const firstAnswer = screen.getByRole("button", { name: /^A\. A$/ });
    expect(firstAnswer.className).toContain("lesson-quiz-option");
    expect(firstAnswer.className).toContain("listening-topic__option");
    expect(firstAnswer.className).not.toContain("ea-card");
    expect(firstAnswer.querySelector(".listening-topic__option__key")?.textContent).toBe("A");
    expect(screen.getByTestId("listening-footer-slot")).toBeTruthy();
  });

  it("does not start answer auto-advance when audio is played or replayed", async () => {
    exercise.mockReturnValue(topic);
    const audio = await openAudio();
    Object.defineProperty(audio, "play", { configurable: true, value: vi.fn().mockResolvedValue(undefined) });

    fireEvent.canPlay(audio);
    fireEvent.play(audio);
    fireEvent.click(screen.getByRole("button", { name: /Qayta tinglash/i }));

    expect(screen.getByText("Listening transcript").closest("details")?.open).toBe(false);
    expect(screen.queryAllByText("Question one")[0] ?? null).toBeNull();
    expect(screen.queryByTestId("answer-auto-advance")).toBeNull();
  });

  it("keeps next and the timer unavailable while authoritative check is pending", async () => {
    exercise.mockReturnValue(topic);
    checkAnswer.mockReturnValue(new Promise(() => undefined));
    await openPractice();
    vi.useFakeTimers();

    await chooseAndCheck();

    expect(screen.getAllByRole("status").length).toBeGreaterThan(0);
    expect(screen.queryByRole("button", { name: /Keyingi savol/i })).toBeNull();
    expect(screen.queryByTestId("answer-auto-advance")).toBeNull();
    act(() => { vi.advanceTimersByTime(3000); });
    expect(screen.getAllByText("Question one")[0]).toBeTruthy();
  });

  it("starts 3000 ms auto-advance only after feedback succeeds", async () => {
    exercise.mockReturnValue(topic);
    checkAnswer.mockResolvedValue({
      questionId: "question-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, hint: null,
    });
    await openPractice();
    vi.useFakeTimers();

    await chooseAndCheck();
    await act(async () => { await Promise.resolve(); });
    expect(screen.getByText("To'g'ri")).toBeTruthy();
    expect(screen.getByTestId("answer-auto-advance")).toBeTruthy();
    act(() => { vi.advanceTimersByTime(2999); });
    expect(screen.getAllByText("Question one")[0]).toBeTruthy();
    act(() => { vi.advanceTimersByTime(1); });
    expect(screen.getAllByText("Question two").length).toBeGreaterThan(0);
  });

  it("keeps manual next and timer race single-shot", async () => {
    exercise.mockReturnValue(topic);
    checkAnswer.mockResolvedValue({
      questionId: "question-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, hint: null,
    });
    await openPractice();
    vi.useFakeTimers();

    await chooseAndCheck();
    await act(async () => { await Promise.resolve(); });
    fireEvent.click(screen.getByRole("button", { name: /Keyingi savol/i }));
    act(() => { vi.advanceTimersByTime(3000); });

    expect(screen.getAllByText("Question two").length).toBeGreaterThan(0);
    expect(screen.queryAllByText("Question three")[0] ?? null).toBeNull();
  });

  it("removes one heart for a wrong answer and submits the final answer once", async () => {
    exercise.mockReturnValue({ ...topic, questions: [topic.questions[0]] });
    checkAnswer.mockResolvedValue({
      questionId: "question-1", selectedOptionIndex: 0, correctOptionIndex: 1, isCorrect: false, hint: null,
    });
    submit.mockResolvedValue({
      topicId: "topic-1", totalQuestions: 1, correctCount: 0, scorePercent: 0, passed: false, outcomes: [], completion: null,
    });
    await openPractice();
    vi.useFakeTimers();

    await chooseAndCheck();
    await act(async () => { await Promise.resolve(); });
    expect(screen.getByText("Noto'g'ri")).toBeTruthy();
    expect(screen.getByLabelText("4 / 5 yurak")).toBeTruthy();
    const finish = screen.getByRole("button", { name: /Tekshirish/i });
    fireEvent.click(finish);
    fireEvent.click(finish);
    act(() => { vi.advanceTimersByTime(3000); });

    expect(submit).toHaveBeenCalledTimes(1);
  });

  it("shows the transcript after submission before opening the result", async () => {
    exercise.mockReturnValue({ ...topic, questions: [topic.questions[0]] });
    checkAnswer.mockResolvedValue({
      questionId: "question-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, hint: null,
    });
    submit.mockResolvedValue({
      topicId: "topic-1", totalQuestions: 1, correctCount: 1, scorePercent: 100, passed: true, outcomes: [], completion: null,
    });
    await openPractice();

    await chooseAndCheck();
    await waitFor(() => expect(screen.getByRole("button", { name: "Tekshirish" })).toBeTruthy());
    fireEvent.click(screen.getByRole("button", { name: "Tekshirish" }));

    expect(await screen.findByText("Listening transcript")).toBeTruthy();
    expect(screen.getByRole("progressbar", { name: "Dars bosqichlari" }).getAttribute("aria-valuetext")).toContain("3 / 3 · Yakun");
    expect(screen.getByRole("button", { name: "Natijani ko'rish" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Natijani ko'rish" }));
    expect(screen.getByRole("progressbar", { name: "Dars bosqichlari" }).getAttribute("aria-valuetext")).toContain("3 / 3 · Yakun");
  });

  it("does not create a timer when answer checking fails and allows retry", async () => {
    exercise.mockReturnValue(topic);
    checkAnswer.mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce({
      questionId: "question-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, hint: null,
    });
    await openPractice();
    vi.useFakeTimers();

    await chooseAndCheck();
    await act(async () => { await Promise.resolve(); });
    expect(screen.getByRole("alert")).toBeTruthy();
    expect(screen.queryByTestId("answer-auto-advance")).toBeNull();
    act(() => { vi.advanceTimersByTime(3000); });
    expect(screen.getAllByText("Question one")[0]).toBeTruthy();

    await chooseAndCheck();
    await act(async () => { await Promise.resolve(); });
    expect(screen.getByTestId("answer-auto-advance")).toBeTruthy();
  });
});
