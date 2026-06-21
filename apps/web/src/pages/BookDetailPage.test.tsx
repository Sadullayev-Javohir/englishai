import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { BookDetailPage } from "./BookDetailPage";

const { checkAnswer, detail, section, submit, MockApiError } = vi.hoisted(() => ({
  checkAnswer: vi.fn(),
  detail: vi.fn(),
  section: vi.fn(),
  submit: vi.fn(),
  MockApiError: class extends Error {
    constructor(public status: number, public body?: unknown) {
      super(`API ${status}`);
    }
  },
}));

vi.mock("@/api/client", () => ({
  ApiError: MockApiError,
  apiErrorDetails: (error: unknown) => error instanceof MockApiError && typeof error.body === "object"
    ? error.body
    : null,
  api: {
    books: {
      detail,
      section,
      checkAnswer,
      submit,
    },
  },
}));

function CurrentLocation() {
  const location = useLocation();
  return <span data-testid="location">{location.pathname}{location.search}</span>;
}

vi.mock("@/app/session", () => ({
  getLearnerId: () => "learner-1",
}));

vi.mock("@/components/lesson/useLessonSounds", () => ({
  useLessonSounds: () => ({ playAnswer: vi.fn(), playComplete: vi.fn() }),
}));

vi.mock("@/components/game/ExerciseOption", () => ({
  ExerciseOption: ({ label, onSelect, disabled, className = "" }: {
    label: React.ReactNode;
    onSelect?: () => void;
    disabled?: boolean;
    className?: string;
  }) => <button type="button" disabled={disabled} onClick={onSelect} className={`bg-ea-surface ${className}`}>{label}</button>,
}));

vi.mock("@/lib/haptics", () => ({ tapLight: vi.fn() }));

vi.mock("framer-motion", async () => {
  const React = await import("react");
  const motionProps = new Set(["animate", "initial", "exit", "layout", "transition", "variants", "viewport", "whileHover", "whileInView", "whileTap"]);
  const motion = new Proxy({}, {
    get: (_target, tag: string) => React.forwardRef(
      ({ children, ...props }: Record<string, unknown>, ref) => {
        const elementProps = Object.fromEntries(Object.entries(props).filter(([key]) => !motionProps.has(key)));
        return React.createElement(tag, { ...elementProps, ref } as React.Attributes, children as React.ReactNode);
      },
    ),
  });
  return { AnimatePresence: ({ children }: { children: React.ReactNode }) => children, motion };
});

const book = {
  id: "book-1",
  title: "Test Book",
  titleUz: "Test kitob",
  author: "Author",
  synopsis: "Synopsis",
  level: CefrLevel.A1,
  topic: "Topic",
  coverImageUrl: null,
  coverAttribution: null,
  sectionsRead: 0,
  isCompleted: false,
  sections: [
    { id: "section-1", order: 1, title: "First chapter", isRead: true, isLocked: false },
    { id: "section-2", order: 2, title: "Second chapter", isRead: false, isLocked: false },
    { id: "section-3", order: 3, title: "Third chapter", isRead: false, isLocked: true },
  ],
};

afterEach(() => {
  cleanup();
  localStorage.clear();
  detail.mockReset();
  section.mockReset();
  checkAnswer.mockReset();
  submit.mockReset();
  vi.useRealTimers();
});

const readySection = {
  bookId: "book-1",
  sectionId: "section-2",
  order: 2,
  bookTitle: "Test Book",
  title: "Second chapter",
  body: "A long selected section body",
  level: CefrLevel.A1,
  wordCount: 6,
  isReady: true,
  isRead: false,
  questions: [
    { id: "q-1", prompt: "First question?", options: ["Alpha", "Beta"] },
    { id: "q-2", prompt: "Final question?", options: ["Gamma", "Delta"] },
  ],
};

function renderSectionRunner() {
  return render(
    <MemoryRouter initialEntries={["/books/book-1?section=section-2"]}>
      <Routes><Route path="/books/:bookId" element={<BookDetailPage />} /></Routes>
    </MemoryRouter>,
  );
}

describe("BookDetailPage section cards", () => {
  it("opens the clicked section even when saved stage is intro", async () => {
    detail.mockResolvedValue(book);
    section.mockImplementation((_bookId: string, sectionId: string) => Promise.resolve({
      bookId: "book-1",
      sectionId,
      order: 2,
      bookTitle: "Test Book",
      title: "Second chapter",
      body: "Selected section body",
      level: CefrLevel.A1,
      wordCount: 3,
      isReady: true,
      isRead: false,
      questions: [],
    }));

    render(
      <MemoryRouter initialEntries={["/books/book-1"]}>
        <Routes>
          <Route path="/books/:bookId" element={<BookDetailPage />} />
        </Routes>
      </MemoryRouter>,
    );

    fireEvent.click(await screen.findByRole("button", { name: /Second chapter/ }));

    await waitFor(() => expect(section).toHaveBeenCalledWith("book-1", "section-2", "learner-1"));
    expect(await screen.findByText("Selected section body")).toBeTruthy();
  });

  it("shows later sections as locked instead of opening an error state", async () => {
    detail.mockResolvedValue(book);

    render(
      <MemoryRouter initialEntries={["/books/book-1"]}>
        <Routes>
          <Route path="/books/:bookId" element={<BookDetailPage />} />
        </Routes>
      </MemoryRouter>,
    );

    const lockedSection = await screen.findByRole("button", { name: /Third chapter/ });
    expect(lockedSection.hasAttribute("disabled")).toBe(true);
    expect(screen.getByText("Avval oldingi bo'limni tugating")).toBeTruthy();
    fireEvent.click(lockedSection);
    expect(section).not.toHaveBeenCalled();
  });

  it("does not load a locked section supplied through the URL", async () => {
    detail.mockResolvedValue(book);

    render(
      <MemoryRouter initialEntries={["/books/book-1?section=section-3"]}>
        <CurrentLocation />
        <Routes>
          <Route path="/books/:bookId" element={<BookDetailPage />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText("First chapter")).toBeTruthy();
    await waitFor(() => {
      expect(screen.getByTestId("location").textContent).toBe("/books/book-1?section=section-2");
    });
    expect(section).not.toHaveBeenCalledWith("book-1", "section-3", "learner-1");
  });

  it("explains when section generation exceeds the request timeout", async () => {
    detail.mockResolvedValue(book);
    section.mockRejectedValue(new MockApiError(408));
    render(
      <MemoryRouter initialEntries={["/books/book-1?section=section-1"]}>
        <Routes>
          <Route path="/books/:bookId" element={<BookDetailPage />} />
        </Routes>
      </MemoryRouter>,
    );

    fireEvent.click(await screen.findByRole("button", { name: /First chapter/ }));
    expect(await screen.findByText(/odatdagidan uzoqroq davom etdi/)).toBeTruthy();
    expect(screen.getByRole("button", { name: "Qayta urinish" })).toBeTruthy();
  });
});

describe("BookDetailPage continuous runner", () => {
  it("keeps reading explicit and renders semantic header, main and footer regions", async () => {
    detail.mockResolvedValue(book);
    section.mockResolvedValue(readySection);
    renderSectionRunner();
    expect(await screen.findByText("A long selected section body")).toBeTruthy();
    expect(screen.getByRole("banner")).toBeTruthy();
    expect(screen.getByRole("main")).toBeTruthy();
    expect(screen.getByRole("contentinfo")).toBeTruthy();
    expect(screen.queryByText("First question?")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Tushunganingizni tekshiring" }));
    expect(await screen.findByRole("heading", { name: "First question?" })).toBeTruthy();
  });

  it("uses neutral idle options and does not expose next while check is pending", async () => {
    detail.mockResolvedValue(book);
    section.mockResolvedValue(readySection);
    checkAnswer.mockImplementation(() => new Promise(() => {}));
    renderSectionRunner();
    fireEvent.click(await screen.findByRole("button", { name: "Tushunganingizni tekshiring" }));

    const alpha = await screen.findByRole("button", { name: /Alpha/ });
    const beta = screen.getByRole("button", { name: /Beta/ });
    expect(alpha.className).toContain("bg-ea-surface");
    expect(beta.className).toContain("bg-ea-surface");
    fireEvent.click(alpha);
    expect(screen.queryByRole("button", { name: /Keyingi/ })).toBeNull();
  });

  it("manual next cancels the timer without a second advance", async () => {
    detail.mockResolvedValue(book);
    section.mockResolvedValue(readySection);
    checkAnswer.mockResolvedValue({ questionId: "q-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, explanation: null });
    renderSectionRunner();
    fireEvent.click(await screen.findByRole("button", { name: "Tushunganingizni tekshiring" }));
    await act(async () => { screen.getByRole("button", { name: /Alpha/ }).click(); });
    await waitFor(() => expect(checkAnswer).toHaveBeenCalledTimes(1));
    const next = await screen.findByRole("button", { name: /Keyingi/ });

    fireEvent.click(next);
    expect(screen.getByRole("heading", { name: "Final question?" })).toBeTruthy();
    await new Promise((resolve) => setTimeout(resolve, 3100));
    expect(screen.getByRole("heading", { name: "Final question?" })).toBeTruthy();
    expect(submit).not.toHaveBeenCalled();
  });

  it("auto-advances exactly after feedback has been available for 3000 ms", async () => {
    detail.mockResolvedValue(book);
    section.mockResolvedValue(readySection);
    checkAnswer.mockResolvedValue({ questionId: "q-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, explanation: null });
    renderSectionRunner();
    fireEvent.click(await screen.findByRole("button", { name: "Tushunganingizni tekshiring" }));
    await act(async () => { screen.getByRole("button", { name: /Alpha/ }).click(); });
    expect(await screen.findByRole("button", { name: /Keyingi/ })).toBeTruthy();

    await new Promise((resolve) => setTimeout(resolve, 3100));
    expect(screen.getByRole("heading", { name: "Final question?" })).toBeTruthy();
  }, 7000);

  it("submits the final quiz once under rapid manual clicks and result stays explicit", async () => {
    detail.mockResolvedValue(book);
    section.mockResolvedValue(readySection);
    checkAnswer
      .mockResolvedValueOnce({ questionId: "q-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, explanation: null })
      .mockResolvedValueOnce({ questionId: "q-2", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, explanation: null });
    submit.mockResolvedValue({
      bookId: "book-1", sectionId: "section-2", totalQuestions: 2, correctCount: 2,
      requiredCorrect: 2, scorePercent: 100, passed: true, bookCompleted: false,
      sectionsRead: 2, sectionCount: 3, outcomes: [],
    });
    renderSectionRunner();
    fireEvent.click(await screen.findByRole("button", { name: "Tushunganingizni tekshiring" }));
    await act(async () => { screen.getByRole("button", { name: /Alpha/ }).click(); });
    await waitFor(() => expect(checkAnswer).toHaveBeenCalledTimes(1));
    fireEvent.click(await screen.findByRole("button", { name: /Keyingi/ }));
    await act(async () => { screen.getByRole("button", { name: /Gamma/ }).click(); });
    await waitFor(() => expect(checkAnswer).toHaveBeenCalledTimes(2));
    const finish = await screen.findByRole("button", { name: /Tekshirish/ });
    fireEvent.click(finish);
    fireEvent.click(finish);

    expect(await screen.findByText("2/2 to'g'ri")).toBeTruthy();
    expect(submit).toHaveBeenCalledTimes(1);
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(screen.getByText("2/2 to'g'ri")).toBeTruthy();
  });

  it("keeps the passed result visible instead of restarting the section", async () => {
    detail.mockResolvedValue(book);
    section.mockResolvedValue(readySection);
    checkAnswer
      .mockResolvedValueOnce({ questionId: "q-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, explanation: null })
      .mockResolvedValueOnce({ questionId: "q-2", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, explanation: null });
    submit.mockResolvedValue({
      bookId: "book-1", sectionId: "section-2", totalQuestions: 2, correctCount: 2,
      requiredCorrect: 2, scorePercent: 100, passed: true, bookCompleted: false,
      sectionsRead: 2, sectionCount: 3, outcomes: [],
    });

    renderSectionRunner();
    fireEvent.click(await screen.findByRole("button", { name: "Tushunganingizni tekshiring" }));
    await act(async () => { screen.getByRole("button", { name: /Alpha/ }).click(); });
    fireEvent.click(await screen.findByRole("button", { name: /Keyingi/ }));
    await act(async () => { screen.getByRole("button", { name: /Gamma/ }).click(); });
    fireEvent.click(await screen.findByRole("button", { name: /Tekshirish/ }));

    expect(await screen.findByText("2/2 to'g'ri")).toBeTruthy();
    expect(detail).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("button", { name: "Tushunganingizni tekshiring" })).toBeNull();
  });

  it("keeps answers and exposes a correlation id when final submit fails, then retries", async () => {
    detail.mockResolvedValue(book);
    section.mockResolvedValue(readySection);
    checkAnswer
      .mockResolvedValueOnce({ questionId: "q-1", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, explanation: null })
      .mockResolvedValueOnce({ questionId: "q-2", selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, explanation: null });
    submit
      .mockRejectedValueOnce(new MockApiError(500, { code: "unexpected_error", correlationId: "book-500" }))
      .mockResolvedValueOnce({
        bookId: "book-1", sectionId: "section-2", totalQuestions: 2, correctCount: 2,
        requiredCorrect: 2, scorePercent: 100, passed: true, bookCompleted: false,
        sectionsRead: 2, sectionCount: 3, outcomes: [],
      });
    renderSectionRunner();
    fireEvent.click(await screen.findByRole("button", { name: "Tushunganingizni tekshiring" }));
    await act(async () => { screen.getByRole("button", { name: /Alpha/ }).click(); });
    fireEvent.click(await screen.findByRole("button", { name: /Keyingi/ }));
    await act(async () => { screen.getByRole("button", { name: /Gamma/ }).click(); });
    fireEvent.click(await screen.findByRole("button", { name: /Tekshirish/ }));

    expect(await screen.findByText(/Javoblaringiz saqlandi/)).toBeTruthy();
    expect(screen.getByText("Yordam kodi: book-500")).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Final question?" })).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: "Natijani qayta yuborish" }));

    expect(await screen.findByText("2/2 to'g'ri")).toBeTruthy();
    expect(submit).toHaveBeenCalledTimes(2);
    expect(submit.mock.calls[1]).toEqual(submit.mock.calls[0]);
  });
});
