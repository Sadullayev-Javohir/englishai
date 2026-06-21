import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { VocabularyPronunciationPage } from "./VocabularyPronunciationPage";

vi.mock("@/api/client", () => ({
  api: {
    speaking: {
      wordDetail: vi.fn(async () => ({
        word: "family",
        spokenForm: null,
        ipa: "ˈfæməli",
        phonemes: [],
        tipUz: "Lablarni bo'sh tuting.",
        visemes: [{ visemeId: 1, offsetMs: 0 }],
        visemeAnimation: null,
        audioBase64: "audio",
      })),
    },
    vocabulary: {
      topic: vi.fn(async () => ({
        id: "topic-1",
        isReady: true,
        words: [{
          word: "family",
          translation: "oila",
          ipa: "/ˈfæməli/",
          exampleSentence: "My family is kind.",
        }],
      })),
      pronounce: vi.fn(),
    },
  },
  ApiError: class ApiError extends Error {},
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("VocabularyPronunciationPage", () => {
  it("renders the Pen vocabulary recorder without inventing a pronunciation score", async () => {
    render(
      <MemoryRouter initialEntries={["/app/vocabulary/topic/topic-1/pronunciation/0"]}>
        <Routes>
          <Route
            path="/app/vocabulary/topic/:topicId/pronunciation/:wordIndex"
            element={<VocabularyPronunciationPage />}
          />
        </Routes>
      </MemoryRouter>,
    );

    await waitFor(() => expect(screen.getByText("family")).toBeTruthy());
    expect(screen.getByRole("button", { name: "Namunani tinglash" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "So‘zni aytish" })).toBeTruthy();
    expect(screen.queryByText("82 / 100")).toBeNull();
  });
});
