import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MiniTestType, ReviewStage, VocabularySource } from "@/api/types";
import { SavedVocabularyTopicPracticePage } from "./SavedVocabularyTopicPracticePage";

const list = vi.fn();
const topic = vi.fn();
const due = vi.fn();
const review = vi.fn();

vi.mock("@/api/client", () => ({
  api: {
    vocabulary: {
      list: (...args: unknown[]) => list(...args),
      topic: (...args: unknown[]) => topic(...args),
      due: (...args: unknown[]) => due(...args),
      review: (...args: unknown[]) => review(...args),
    },
    images: { topicUrl: (id: string, slot: number) => `/images/${id}/${slot}` },
  },
}));
vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/lib/useWordVoice", () => ({ useWordVoice: () => vi.fn() }));

const savedWord = (id: string, word: string) => ({
  id, learnerId: "learner-1", word, translation: `${word}-uz`, exampleSentence: null,
  source: VocabularySource.Manual, stage: ReviewStage.Day3, failCount: 0,
  nextReviewAt: "2020-01-01T00:00:00Z", sourceTopicId: "topic-1", partOfSpeech: "noun",
});

const topicDetail = { id: "topic-1", title: "Travel", titleUz: "Sayohat", level: 1, category: "travel", words: [], quiz: [] };

const passResult = { id: "go", stage: ReviewStage.Day7, mastered: false, failCount: 0, nextReviewAt: null, passed: true, reasonCode: null };

afterEach(() => {
  cleanup();
  list.mockReset();
  topic.mockReset();
  due.mockReset();
  review.mockReset();
});

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/app/vocabulary/saved/topic-1/practice"]}>
      <Routes>
        <Route path="/app/vocabulary/saved/:topicId/practice" element={<SavedVocabularyTopicPracticePage />} />
        <Route path="/app/vocabulary/saved" element={<div>saved-page</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("SavedVocabularyTopicPracticePage", () => {
  it("submits a cloze answer to the SRS review endpoint and advances the ladder", async () => {
    list.mockResolvedValue([savedWord("go", "go"), savedWord("stop", "stop")]);
    topic.mockResolvedValue(topicDetail);
    due.mockResolvedValue([
      { id: "go", word: "go", translation: "bormoq", exampleSentence: null, stage: ReviewStage.Day3, miniTestType: MiniTestType.ClozeChoice, sourceTopicId: "topic-1", partOfSpeech: "verb", options: ["go", "stop"] },
    ]);
    review.mockResolvedValue(passResult);
    renderPage();

    // Review mode is chosen because a word is due (cloze prompt is shown).
    expect(await screen.findByText(/Mos inglizcha/)).toBeTruthy();

    fireEvent.click(await screen.findByRole("button", { name: "go" }));
    expect(review).toHaveBeenCalledWith("go", { submittedAnswer: "go" });

    // Feedback appears (finish button), then finishing persists the single-item session.
    fireEvent.click(await screen.findByRole("button", { name: /Yakunlash/ }));
    expect(await screen.findByText("Takrorlash yakunlandi!")).toBeTruthy();
  });

  it("self-rates spoken recall via selfRatedPassed", async () => {
    list.mockResolvedValue([savedWord("go", "go")]);
    topic.mockResolvedValue(topicDetail);
    due.mockResolvedValue([
      { id: "go", word: "go", translation: "bormoq", exampleSentence: null, stage: ReviewStage.Day21, miniTestType: MiniTestType.SpokenUsage, sourceTopicId: "topic-1", partOfSpeech: "verb", options: null },
    ]);
    review.mockResolvedValue(passResult);
    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: /Ha, esladim/ }));
    expect(review).toHaveBeenCalledWith("go", { selfRatedPassed: true });
  });

  it("falls back to free practice when nothing is due", async () => {
    list.mockResolvedValue([savedWord("go", "go"), savedWord("stop", "stop")]);
    topic.mockResolvedValue(topicDetail);
    due.mockResolvedValue([]);
    renderPage();

    expect(await screen.findByText(/Erkin mashq/)).toBeTruthy();
    expect(review).not.toHaveBeenCalled();
  });
});
