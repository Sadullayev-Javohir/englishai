import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "@/api/client";
import { MiniTestType, ReviewStage } from "@/api/types";
import { VocabularyReviewPage } from "./VocabularyReviewPage";

vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/components/ui/ModulePageLoader", () => ({
  ModulePageLoader: () => <div>Yuklanmoqda</div>,
}));

describe("VocabularyReviewPage", () => {
  beforeEach(() => {
    cleanup();
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it("keeps the empty review state visible when opened directly", async () => {
    vi.spyOn(api.vocabulary, "due").mockResolvedValue([]);
    const completed = vi.fn();
    window.addEventListener("review:completed", completed);

    render(
      <MemoryRouter initialEntries={["/app/vocabulary/review"]}>
        <VocabularyReviewPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText("Barcha takrorlashlar bajarildi!")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Orqaga" })).toBeTruthy();
    expect(completed).not.toHaveBeenCalled();
    window.removeEventListener("review:completed", completed);
  });

  it("explains completed and total due word counts", async () => {
    vi.spyOn(api.vocabulary, "due").mockResolvedValue([
      {
        id: "word-1",
        word: "reduce",
        translation: "kamaytirmoq",
        exampleSentence: null,
        stage: ReviewStage.Day3,
        miniTestType: MiniTestType.SpokenUsage,
        sourceTopicId: "topic-1",
        partOfSpeech: "verb",
        options: null,
      },
      {
        id: "word-2",
        word: "supply",
        translation: "ta'minot",
        exampleSentence: null,
        stage: ReviewStage.Day3,
        miniTestType: MiniTestType.SpokenUsage,
        sourceTopicId: "topic-1",
        partOfSpeech: "noun",
        options: null,
      },
    ]);

    render(
      <MemoryRouter initialEntries={["/app/vocabulary/review"]}>
        <VocabularyReviewPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText("Bajarildi: 0 / 2")).toBeTruthy();
    expect(screen.getByText("Bugungi navbat: 2 ta")).toBeTruthy();
    expect(screen.getByText("Bu sahifa nima uchun kerak?")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Keyinroq" })).toBeTruthy();
    expect(screen.getByText("Tezroq yana chiqadi")).toBeTruthy();
  });

  it("lets the learner close and reopen the purpose guidance", async () => {
    vi.spyOn(api.vocabulary, "due").mockResolvedValue([{
      id: "word-1", word: "reduce", translation: "kamaytirmoq", exampleSentence: null,
      stage: ReviewStage.Day3, miniTestType: MiniTestType.SpokenUsage, sourceTopicId: null,
      partOfSpeech: "verb", options: null,
    }]);

    render(<MemoryRouter><VocabularyReviewPage /></MemoryRouter>);

    fireEvent.click(await screen.findByRole("button", { name: "Ko'rsatmalarni yopish" }));
    expect(screen.queryByText("Bu sahifa nima uchun kerak?")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Ko'rsatmalarni ochish" }));
    expect(screen.getByText("Bu sahifa nima uchun kerak?")).toBeTruthy();
  });

  it("waits for Keyingi savol after a checked test answer", async () => {
    vi.spyOn(api.vocabulary, "due").mockResolvedValue([
      { id: "word-1", word: "reduce", translation: "kamaytirmoq", exampleSentence: null, stage: ReviewStage.Day3, miniTestType: MiniTestType.ClozeChoice, sourceTopicId: null, partOfSpeech: "verb", options: ["reduce", "supply"] },
      { id: "word-2", word: "supply", translation: "ta'minot", exampleSentence: null, stage: ReviewStage.Day3, miniTestType: MiniTestType.ClozeChoice, sourceTopicId: null, partOfSpeech: "noun", options: ["reduce", "supply"] },
    ]);
    vi.spyOn(api.vocabulary, "review").mockResolvedValue({
      id: "word-1",
      stage: ReviewStage.Day7,
      mastered: false,
      failCount: 0,
      nextReviewAt: null,
      passed: true,
      reasonCode: null,
    });

    render(<MemoryRouter><VocabularyReviewPage /></MemoryRouter>);
    fireEvent.click(await screen.findByRole("button", { name: "reduce" }));

    expect(await screen.findByRole("button", { name: "Keyingi savol" })).toBeTruthy();
    expect(screen.getByText("1 / 2")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Keyingi savol" }));
    await waitFor(() => expect(screen.getByText("2 / 2")).toBeTruthy());
  });
});
