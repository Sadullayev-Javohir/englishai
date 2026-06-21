import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { CefrLevel, ReviewStage, type DueReviewDto, type VideoSummaryDto, type VocabularyItemDto } from "@/api/types";
import { HomeEntryModal, buildHomeEntryPrompts, selectHomeEntryPrompt } from "./HomeEntryModal";

vi.mock("@/api/client", () => ({ api: { video: { open: vi.fn() } } }));

const savedWords: VocabularyItemDto[] = [
  { id: "word-1", learnerId: "learner", word: "grateful", translation: "minnatdor", exampleSentence: null, source: 0, stage: ReviewStage.Day3, failCount: 0, nextReviewAt: null, sourceTopicId: "topic-1", partOfSpeech: null },
  { id: "word-2", learnerId: "learner", word: "kind", translation: "mehribon", exampleSentence: null, source: 0, stage: ReviewStage.Day3, failCount: 0, nextReviewAt: null, sourceTopicId: "topic-1", partOfSpeech: null },
  { id: "word-3", learnerId: "learner", word: "calm", translation: "xotirjam", exampleSentence: null, source: 0, stage: ReviewStage.Day3, failCount: 0, nextReviewAt: null, sourceTopicId: "topic-1", partOfSpeech: null },
];
const dueWords: DueReviewDto[] = [{ id: "word-1", word: "grateful", translation: "minnatdor", exampleSentence: null, stage: ReviewStage.Day3, miniTestType: 0, sourceTopicId: "topic-1", partOfSpeech: null, options: null }];
const videos: VideoSummaryDto[] = [{ id: "video-1", youTubeVideoId: "I_tRSrPru94", title: "Introduce yourself", channel: "BBC Learning English", durationSeconds: 157, topic: "everyday", level: CefrLevel.A2 }];

function prompts() {
  return buildHomeEntryPrompts({
    savedWords,
    dueWords,
    currentStreak: 7,
    activeTopic: { id: "topic-1", title: "My Mother", level: CefrLevel.A2 },
    videos,
  });
}

afterEach(cleanup);

describe("home entry prompts", () => {
  it("only builds prompts from valid learner and catalog data", () => {
    const result = prompts();
    expect(result.map(prompt => prompt.kind)).toEqual(["mini-quiz", "due-review", "streak", "video"]);
    expect(result[0]).toMatchObject({ word: "grateful", correctTranslation: "minnatdor", topicTitle: "My Mother" });
    expect(result[1]).toMatchObject({ dueCount: 1, dueReviewDay: 3, topicTitle: "My Mother" });
    expect(result[3]).toMatchObject({ video: { youTubeVideoId: "I_tRSrPru94", title: "Introduce yourself" } });
  });

  it("does not fabricate a mini quiz from duplicate or missing translations", () => {
    const result = buildHomeEntryPrompts({
      savedWords: savedWords.map(word => ({ ...word, translation: "bir xil" })),
      dueWords: [],
      currentStreak: 0,
      activeTopic: null,
      videos: [],
    });
    expect(result).toEqual([]);
  });

  it("selects a bounded random candidate", () => {
    const result = prompts();
    expect(selectHomeEntryPrompt(result, () => 0)?.kind).toBe("mini-quiz");
    expect(selectHomeEntryPrompt(result, () => .999)?.kind).toBe("video");
  });

  it("renders the Pen mini quiz and checks the backend-provided correct translation", () => {
    const onClose = vi.fn();
    render(<MemoryRouter><HomeEntryModal prompt={prompts()[0]} open onClose={onClose} onSuppressToday={vi.fn()} /></MemoryRouter>);
    expect(screen.getByRole("dialog", { name: "Kunlik mini-mashq" })).toBeTruthy();
    expect(screen.getByText("grateful")).toBeTruthy();
    fireEvent.click(screen.getByRole("radio", { name: /minnatdor/i }));
    fireEvent.click(screen.getByRole("button", { name: "Tekshirish" }));
    expect(screen.getByRole("status").textContent).toContain("To‘g‘ri javob");
    fireEvent.click(screen.getByRole("button", { name: "Davom etish" }));
    expect(onClose).toHaveBeenCalledOnce();
  });

  it("does not use an entry prompt again after the learner suppresses it for today", () => {
    const onSuppressToday = vi.fn();
    render(<MemoryRouter><HomeEntryModal prompt={prompts()[2]} open onClose={vi.fn()} onSuppressToday={onSuppressToday} /></MemoryRouter>);
    fireEvent.click(screen.getByRole("button", { name: "Bugun boshqa ko‘rsatma" }));
    expect(onSuppressToday).toHaveBeenCalledOnce();
  });
});
