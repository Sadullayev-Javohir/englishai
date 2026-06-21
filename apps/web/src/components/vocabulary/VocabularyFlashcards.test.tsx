import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, type VocabularyTopicDetailDto } from "@/api/types";
import { WordsStage } from "./VocabularyFlashcards";

vi.mock("@/components/lesson/useLessonSounds", () => ({ useLessonSounds: vi.fn() }));
afterEach(cleanup);

function topicWithCards(count: number): VocabularyTopicDetailDto {
  return {
    id: "deck-topic", title: "My Family", titleUz: "Mening oilam", category: "People",
    level: CefrLevel.A1, isReady: true, passage: "", quiz: [],
    words: Array.from({ length: count }, (_, i) => ({
      word: `word-${i + 1}`, translation: `so‘z-${i + 1}`, ipa: null, exampleSentence: "My family is kind.",
      partOfSpeech: "noun", lexicalCategory: "Noun", register: null, usageNote: null,
      imageUrl: `/api/images/vocabulary-topics/deck/word-${i + 1}.png`, imageAttribution: null,
    })),
  };
}

describe("physical vocabulary card deck", () => {
  it("renders all 19 subsequent cards behind card 1, not a decorative shadow", () => {
    const view = render(<MemoryRouter><WordsStage topic={topicWithCards(20)} onPrevWord={vi.fn()} onFinished={vi.fn()} /></MemoryRouter>);
    const layers = view.container.querySelectorAll<HTMLElement>(".vocabulary-card-stack__card");
    expect(layers).toHaveLength(19);
    expect([...layers].map(layer => Number(layer.dataset.stackedIndex))).toEqual(Array.from({ length: 19 }, (_, i) => i + 1));
    expect(view.container.querySelector(".vocabulary-card-stack")?.getAttribute("data-remaining-cards")).toBe("19");
    expect(view.container.querySelector(".vocabulary-flashcard")?.getAttribute("data-has-turned")).toBe("false");
    expect(view.container.querySelector(".vocabulary-card-stack__layers")?.getAttribute("aria-hidden")).toBe("true");
    expect(screen.getAllByRole("button", { name: "Kartani aylantirish" })).toHaveLength(1);
    expect(view.container.querySelector(".vocabulary-card-stack__layers button")).toBeNull();
    const preview = view.container.querySelector(".vocabulary-card-stack__preview");
    expect(preview?.getAttribute("data-preview-word")).toBe("word-2");
    expect(preview?.querySelector("img")?.getAttribute("src")).toBe("/api/images/vocabulary-topics/deck/word-2.png");
    expect(preview?.querySelector("img")?.getAttribute("loading")).toBe("eager");
    expect(view.container.querySelectorAll(".vocabulary-card-stack__layers img")).toHaveLength(1);
    fireEvent.click(screen.getByRole("button", { name: "Kartani aylantirish" }));
    expect(view.container.querySelector(".vocabulary-flashcard")?.classList.contains("is-back")).toBe(true);
    expect(view.container.querySelector(".vocabulary-flashcard")?.getAttribute("data-has-turned")).toBe("true");
    expect(view.container.querySelectorAll(".vocabulary-card-stack__card")).toHaveLength(19);
    fireEvent.click(screen.getByRole("button", { name: "Kartani aylantirish" }));
    expect(view.container.querySelector(".vocabulary-flashcard")?.classList.contains("is-back")).toBe(false);
  });

  it("keeps the deck count aligned with navigation and learning, including returning backwards", () => {
    const view = render(<MemoryRouter><WordsStage topic={topicWithCards(20)} onPrevWord={vi.fn()} onFinished={vi.fn()} /></MemoryRouter>);
    const count = () => view.container.querySelectorAll(".vocabulary-card-stack__card").length;
    fireEvent.click(screen.getByRole("button", { name: "Aylantirish" }));
    fireEvent.click(screen.getByRole("button", { name: "O‘rgandim →" }));
    expect(count()).toBe(18);
    expect(view.container.querySelector(".vocabulary-card-stack__preview")?.getAttribute("data-preview-word")).toBe("word-3");
    expect(view.container.querySelector(".vocabulary-flashcard")?.getAttribute("data-has-turned")).toBe("false");
    fireEvent.click(screen.getByRole("button", { name: "20. so‘z-20" }));
    expect(count()).toBe(0);
    expect(view.container.querySelector(".vocabulary-card-stack__preview")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Oldingi karta" }));
    expect(count()).toBe(1);
    expect(view.container.querySelector(".vocabulary-card-stack__preview")?.getAttribute("data-preview-word")).toBe("word-20");
    fireEvent.click(screen.getByRole("button", { name: "1. so‘z-1 — o‘rganildi" }));
    expect(count()).toBe(19);
  });

  it.each([1, 4, 12])("uses the actual %i-card topic size", count => {
    const view = render(<MemoryRouter><WordsStage topic={topicWithCards(count)} onPrevWord={vi.fn()} onFinished={vi.fn()} /></MemoryRouter>);
    expect(view.container.querySelectorAll(".vocabulary-card-stack__card")).toHaveLength(count - 1);
  });
});
