import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ReviewStage, VocabularySource } from "@/api/types";
import { MySavedWordsPage } from "./MySavedWordsPage";

const list = vi.fn();
const topic = vi.fn();

vi.mock("@/api/client", () => ({
  api: { vocabulary: {
    list: (...args: unknown[]) => list(...args),
    topic: (...args: unknown[]) => topic(...args),
  }, images: {
    topicUrl: (topicId: string, slot: number) => `/images/topics/${topicId}/${slot}`,
  } },
}));
vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/lib/useWordVoice", () => ({ useWordVoice: () => vi.fn() }));

const words = [
  { id: "word-1", learnerId: "learner-1", word: "journey", translation: "sayohat", exampleSentence: "The journey was long.", source: VocabularySource.Manual, stage: ReviewStage.Day3, failCount: 0, nextReviewAt: "2020-01-01T00:00:00Z", sourceTopicId: "topic-1", partOfSpeech: "noun" },
  { id: "word-2", learnerId: "learner-1", word: "calm", translation: "xotirjam", exampleSentence: null, source: VocabularySource.Manual, stage: ReviewStage.Mastered, failCount: 0, nextReviewAt: null, sourceTopicId: null, partOfSpeech: "adjective" },
];

const topicWords = Array.from({ length: 24 }, (_, index) => ({
  id: `topic-word-${index}`,
  learnerId: "learner-1",
  word: index === 0 ? "journey" : `word-${index}`,
  translation: index === 0 ? "sayohat" : `tarjima-${index}`,
  exampleSentence: null,
  source: VocabularySource.Manual,
  stage: index < 8 ? ReviewStage.Mastered : ReviewStage.Day3,
  failCount: 0,
  nextReviewAt: index < 8 ? null : "2099-01-01T00:00:00Z",
  sourceTopicId: "topic-1",
  partOfSpeech: "noun",
}));

afterEach(() => {
  cleanup();
  list.mockReset();
  topic.mockReset();
});

describe("MySavedWordsPage", () => {
  it("renders scoped catalog controls, filters and word details", async () => {
    list.mockResolvedValue([...topicWords, words[1]]);
    topic.mockResolvedValue({ id: "topic-1", title: "Travel", titleUz: "Sayohat", level: 1, category: "travel", words: Array.from({ length: 24 }, (_, index) => ({ word: index === 0 ? "journey" : `word-${index}`, translation: `tarjima-${index}` })) });
    renderPage();

    const topicToggle = await screen.findByRole("button", { name: /Sayohat/ });
    expect(topicToggle.getAttribute("aria-expanded")).toBe("false");
    expect(screen.queryByText("journey")).toBeNull();
    fireEvent.click(topicToggle);
    expect(topicToggle.getAttribute("aria-expanded")).toBe("true");
    expect(screen.getByText("journey")).toBeTruthy();
    expect(screen.getByText(/Lug‘atga saqlangan/)).toBeTruthy();
    expect(screen.getByText("24/24")).toBeTruthy();
    expect(within(screen.getByTestId("saved-topic-grid-topic-1")).getAllByRole("button", { name: /tafsilotlarini ochish/ })).toHaveLength(24);
    fireEvent.click(topicToggle);
    expect(topicToggle.getAttribute("aria-expanded")).toBe("false");
    expect(screen.queryByText("journey")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "O‘zlashtirilgan" }));
    expect(screen.getByText("calm")).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: "calm tafsilotlarini ochish" }));
    expect(screen.getByRole("dialog", { name: "calm" })).toBeTruthy();
    expect(screen.getAllByText("Sifat").length).toBeGreaterThan(0);
    expect(screen.queryByRole("button", { name: "Lug‘atdan o‘chirish" })).toBeNull();
  });

  it("renders the 3/7/21 milestone ladder per word stage", async () => {
    const base = { learnerId: "learner-1", exampleSentence: null, source: VocabularySource.Manual, failCount: 0, partOfSpeech: "noun", sourceTopicId: "topic-1" };
    list.mockResolvedValue([
      { ...base, id: "w-day7", word: "advance", translation: "ilgarilamoq", stage: ReviewStage.Day7, nextReviewAt: "2099-01-01T00:00:00Z" },
      { ...base, id: "w-mastered", word: "settle", translation: "joylashmoq", stage: ReviewStage.Mastered, nextReviewAt: null },
    ]);
    topic.mockResolvedValue({ id: "topic-1", title: "Travel", titleUz: "Sayohat", level: 1, category: "travel", words: [{ word: "advance", translation: "ilgarilamoq" }, { word: "settle", translation: "joylashmoq" }] });
    renderPage();

    // Aggregate ladder on the topic card ticks only the shared minimum (Day7 => one tick).
    expect((await screen.findAllByRole("list", { name: "3 bosqichdan 1 tasi bajarilgan" })).length).toBeGreaterThan(0);

    fireEvent.click(await screen.findByRole("button", { name: /Sayohat/ }));
    // Mastered word shows all three cleared; its ladder is unique on the page.
    expect(screen.getAllByRole("list", { name: "3 bosqichdan 3 tasi bajarilgan" }).length).toBeGreaterThan(0);
  });

  it("keeps the saved count independent from the mastered filter", async () => {
    list.mockResolvedValue(topicWords);
    topic.mockResolvedValue({ id: "topic-1", title: "Travel", titleUz: "Sayohat", level: 1, category: "travel", words: Array.from({ length: 24 }, (_, index) => ({ word: index === 0 ? "journey" : `word-${index}`, translation: `tarjima-${index}` })) });
    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: /Sayohat/ }));
    expect(screen.getByText("24/24")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "O‘zlashtirilgan" }));
    const filteredTopicToggle = await screen.findByRole("button", { name: /Sayohat/ });
    if (filteredTopicToggle.getAttribute("aria-expanded") === "false") fireEvent.click(filteredTopicToggle);
    expect(screen.getByText("24/24")).toBeTruthy();
    expect(screen.getAllByRole("button", { name: /tafsilotlarini ochish/ })).toHaveLength(24);
  });
});

function renderPage() {
  return render(<MemoryRouter initialEntries={["/app/vocabulary/saved"]}><MySavedWordsPage /></MemoryRouter>);
}
