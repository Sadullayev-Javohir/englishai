import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { CefrLevel } from "@/api/types";
import { ChoiceExercise, restartVocabularyTopicLesson, VocabularySlideShell, WordsStage } from "./VocabularyTopicPage";
import { readLessonProgress, writeLessonProgress } from "@/lib/lessonProgress";

vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  vi.useRealTimers();
  window.localStorage.clear();
});

const mcExercise = {
  kind: "mc" as const,
  dir: "en-uz" as const,
  word: "family",
  prompt: "family",
  options: ["oila", "do‘st", "uy", "maktab"],
  answer: "oila",
};

const typeExercise = {
  kind: "type" as const,
  word: "family",
  prompt: "oila",
  answer: "family",
};

describe("Vocabulary exercise slides", () => {
  it("restarts from the introduction without reading or writing persisted flow", () => {
    const progressScope = "vocabulary-topic.topic-1";
    writeLessonProgress(`${progressScope}.stage`, "practice");
    writeLessonProgress(`${progressScope}.words.index`, 4);
    writeLessonProgress(`${progressScope}.practice.index`, 7);
    const setStage = vi.fn();

    restartVocabularyTopicLesson(progressScope, setStage);

    expect(readLessonProgress(`${progressScope}.stage`, "missing")).toBe("practice");
    expect(readLessonProgress(`${progressScope}.words.index`, -1)).toBe(4);
    expect(readLessonProgress(`${progressScope}.practice.index`, -1)).toBe(7);
    expect(setStage).toHaveBeenCalledWith("hub");
  });

  it("uses flow layout for long reading content so the beginning and end stay scrollable", () => {
    render(
      <VocabularySlideShell
        title="Matn"
        step="text"
        mode="flow"
        onBack={vi.fn()}
        footer={<button type="button">Davom etish</button>}
      >
        <p>{"Long passage ".repeat(200)}</p>
      </VocabularySlideShell>,
    );

    expect(screen.getByText(/Long passage/).closest(".vocabulary-slide--flow")).toBeTruthy();
  });

  it("can hide the slide title without removing the lesson header controls", () => {
    render(
      <VocabularySlideShell
        title="family"
        showTitle={false}
        step="intro"
        onBack={vi.fn()}
        footer={<button type="button">Keyingi</button>}
      >
        <p>Flashcard</p>
      </VocabularySlideShell>,
    );

    expect(screen.queryByText("family")).toBeNull();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("1 / 5 · Kirish");
    expect(screen.getByRole("button", { name: "Orqaga" })).toBeTruthy();
  });

  it("flips by card click, exposes word audio and keeps the test locked until learned", async () => {
    const topic = {
      id: "topic-1",
      title: "Family",
      titleUz: "Oila",
      category: "People",
      level: CefrLevel.A1,
      isReady: true,
      passage: "",
      quiz: [],
      words: [{
        word: "family",
        translation: "oila",
        ipa: "/ˈfæməli/",
        exampleSentence: "My family is kind.",
        partOfSpeech: "noun",
        lexicalCategory: "Noun",
        register: null,
        usageNote: null,
        imageUrl: null,
        imageAttribution: null,
      }],
    };

    render(
      <MemoryRouter>
        <WordsStage topic={topic} onPrevWord={vi.fn()} onFinished={vi.fn()} />
      </MemoryRouter>,
    );

    expect(screen.getByRole("complementary", { name: "Sizning to‘plamingiz" })).toBeTruthy();
    expect((screen.getByRole("button", { name: /Test hozircha yopiq/ }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByRole("button", { name: "Talaffuzni mashq qilish" })).toBeTruthy();
    const card = screen.getByRole("button", { name: "Kartani aylantirish" });
    expect(card.getAttribute("data-card-face")).toBe("front");
    fireEvent.click(card);
    await waitFor(() => expect(screen.getByRole("button", { name: "Kartani aylantirish" }).getAttribute("data-card-face")).toBe("back"));
    expect(card.closest('[aria-hidden="true"]')).toBeTruthy();
    expect(card.tabIndex).toBe(-1);
    expect(screen.getByRole("button", { name: "family — tinglash" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "O‘rgandim →" }));
    expect(screen.getByRole("heading", { name: "Testga tayyorsiz!" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Testga o‘tish" })).toBeTruthy();
  });

  it("renders idle selectable options with one neutral visual contract", () => {
    render(<ChoiceExercise exercise={mcExercise} accent={0} onResult={vi.fn()} onNext={vi.fn()} />);

    const options = mcExercise.options.map((label) => screen.getByRole("button", { name: new RegExp(label) }));
    expect(new Set(options.map((option) => option.className))).toHaveLength(1);
    expect(options.every((option) => option.className.includes("vocabulary-quiz-option"))).toBe(true);
    expect(options.every((option) => option.dataset.answerState === "idle")).toBe(true);
    expect(options.map(option => option.querySelector(".vocabulary-quiz-option__key")?.textContent)).toEqual(["A", "B", "C", "D"]);
    expect(options.every((option) => option.querySelector(".vocabulary-quiz-option__state"))).toBe(true);
  });

  it("shows separate correct and wrong answer cards without the legacy purple selection ring", () => {
    render(<ChoiceExercise exercise={mcExercise} accent={0} onResult={vi.fn()} onNext={vi.fn()} />);
    fireEvent.click(screen.getByRole("button", { name: /do‘st/ }));
    const wrong = screen.getByRole("button", { name: /do‘st/ });
    const right = screen.getByRole("button", { name: /oila/ });
    expect(wrong.dataset.answerState).toBe("wrong");
    expect(wrong.getAttribute("aria-pressed")).toBe("true");
    expect(right.dataset.answerState).toBe("correct");
    expect(wrong.querySelector(".lucide-circle-x")).toBeTruthy();
    expect(right.querySelector(".lucide-circle-check")).toBeTruthy();
    expect(wrong.className).not.toContain("ring-");
    expect(screen.getByText("family — oila")).toBeTruthy();
    expect(screen.getByText("Birga eslab qolamiz.")).toBeTruthy();
  });

  it("gives the submitted typed answer a visible success field and a descriptive hint", () => {
    render(<ChoiceExercise exercise={typeExercise} accent={0} onResult={vi.fn()} onNext={vi.fn()} />);
    const input = screen.getByRole("textbox", { name: "Javobingiz" });
    expect(input.getAttribute("aria-describedby")).toBe("vocabulary-answer-hint");
    fireEvent.change(input, { target: { value: "family" } });
    fireEvent.click(screen.getByRole("button", { name: "Tekshirish" }));
    expect(input.closest(".vocabulary-answer-field")?.getAttribute("data-answer-state")).toBe("correct");
    expect(input.closest(".vocabulary-answer-field")?.querySelector("svg")).toBeTruthy();
    expect(screen.getByText("To‘ppa-to‘g‘ri!")).toBeTruthy();
  });

  it("starts no timer before feedback and auto-advances exactly after 3000 ms", () => {
    vi.useFakeTimers();
    const onNext = vi.fn();
    render(<ChoiceExercise exercise={mcExercise} accent={0} onResult={vi.fn()} onNext={onNext} />);

    vi.advanceTimersByTime(5000);
    expect(onNext).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: /oila/i }));
    expect(screen.getByText("To‘ppa-to‘g‘ri!")).toBeTruthy();
    vi.advanceTimersByTime(2999);
    expect(onNext).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1);
    expect(onNext).toHaveBeenCalledTimes(1);
  });

  it("manual Keyingi cancels the pending timer", () => {
    vi.useFakeTimers();
    const onNext = vi.fn();
    render(<ChoiceExercise exercise={mcExercise} accent={0} onResult={vi.fn()} onNext={onNext} />);

    fireEvent.click(screen.getByRole("button", { name: /oila/i }));
    fireEvent.click(screen.getByRole("button", { name: /keyingi/i }));
    expect(onNext).toHaveBeenCalledTimes(1);
    vi.advanceTimersByTime(3000);
    expect(onNext).toHaveBeenCalledTimes(1);
  });

  it("does not complete or advance an incomplete typing exercise", () => {
    vi.useFakeTimers();
    const typeNext = vi.fn();
    const typeResult = vi.fn();
    render(<ChoiceExercise exercise={typeExercise} accent={0} onResult={typeResult} onNext={typeNext} />);

    expect((screen.getByRole("button", { name: /tekshir/i }) as HTMLButtonElement).disabled).toBe(true);
    vi.advanceTimersByTime(3000);
    expect(typeResult).not.toHaveBeenCalled();
    expect(typeNext).not.toHaveBeenCalled();
  });

  it("applies reward or heart result once and advances once during rapid final interaction", () => {
    vi.useFakeTimers();
    const onResult = vi.fn();
    const onNext = vi.fn();
    render(<ChoiceExercise exercise={mcExercise} accent={0} onResult={onResult} onNext={onNext} />);

    const answer = screen.getByRole("button", { name: /oila/i });
    fireEvent.click(answer);
    fireEvent.click(answer);
    expect(onResult).toHaveBeenCalledTimes(1);

    const next = screen.getByRole("button", { name: /keyingi/i });
    fireEvent.click(next);
    fireEvent.click(next);
    vi.advanceTimersByTime(3000);
    expect(onNext).toHaveBeenCalledTimes(1);
  });
});
