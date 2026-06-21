import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { CefrLevel, type VocabularyTopicDetailDto } from "@/api/types";
import { VocabularyPronunciationPractice } from "./VocabularyPronunciationPage";
const mock = vi.hoisted(() => ({
  recorded: null as ((blob: Blob) => Promise<void>) | null,
  pronounce: vi.fn(), start: vi.fn().mockResolvedValue(false), wordDetail: vi.fn(), micError: false,
}));
vi.mock("@/api/client", () => ({ api: { vocabulary: { pronounce: mock.pronounce }, speaking: { wordDetail: mock.wordDetail } } }));
vi.mock("@/lib/useMicRecorder", () => ({ useMicRecorder: (callback: (blob: Blob) => Promise<void>) => {
  mock.recorded = callback; return { status: "idle", micError: mock.micError, start: mock.start, stop: vi.fn() };
} }));
vi.mock("@/lib/audio", () => ({ blobToWav16kMono: vi.fn(async () => new Uint8Array([1, 2])), bytesToBase64: vi.fn(() => "AQI="), playWordVoice: vi.fn(() => true) }));
const word = { word: "support", translation: "yordam", ipa: "/səˈpɔːt/", exampleSentence: "My family gives me support.", partOfSpeech: "noun", lexicalCategory: "Noun", register: null, usageNote: null, imageUrl: null, imageAttribution: null };
const topic: VocabularyTopicDetailDto = { id: "test-topic", title: "My Mother", titleUz: "Oila", category: "People", level: CefrLevel.B1, isReady: true, words: [word], passage: "", quiz: [] };
const score = { word: "support", recognized: true, isAuthentic: true, correct: false, overallScore: 82, accuracyScore: 82, errorType: 0, feedbackUz: "Urg‘uni o‘zgartiring.", phonemes: [{ phoneme: "sə", accuracyScore: 95 }, { phoneme: "pɔːt", accuracyScore: 60 }] };
function mount() {
  mock.wordDetail.mockResolvedValue({ word: "support", ipa: "səˈpɔːt", tipUz: "Urg‘u ikkinchi bo‘g‘inda.", audioBase64: null });
  return render(<MemoryRouter><VocabularyPronunciationPractice topic={topic} word={word} index={0} onBack={vi.fn()} /></MemoryRouter>);
}
afterEach(() => { cleanup(); vi.clearAllMocks(); mock.micError = false; mock.recorded = null; });
describe("Vocabulary pronunciation assessment integrity", () => {
  it("shows an authentic response, per-phoneme feedback and sends recorded WAV data", async () => {
    mock.pronounce.mockResolvedValue(score); mount();
    expect(screen.queryByText("82 / 100")).toBeNull();
    await act(async () => { await mock.recorded?.(new Blob(["recording"])); });
    expect(screen.getByText("82 / 100")).toBeTruthy();
    expect(screen.getByText("pɔːt").parentElement?.className).toBe("is-review");
    expect(mock.pronounce).toHaveBeenCalledWith("support", "AQI=", expect.any(AbortSignal));
  });
  it.each([
    [{ isAuthentic: false }, "Ishonchli baho olinmadi. Qayta urinib ko‘ring."],
    [{ recognized: false }, "Ovoz aniqlanmadi. So‘zni qayta ayting."],
  ])("does not display a fabricated score for %j", async (override, message) => {
    mock.pronounce.mockResolvedValue({ ...score, ...override }); mount();
    await act(async () => { await mock.recorded?.(new Blob(["recording"])); });
    expect(screen.queryByText("82 / 100")).toBeNull(); expect(screen.getByText(message)).toBeTruthy();
  });
  it("recovers from a rejected assessment without presenting success", async () => {
    mock.pronounce.mockRejectedValue(new Error("offline")); mount();
    await act(async () => { await mock.recorded?.(new Blob(["recording"])); });
    expect(screen.getByText("Talaffuzni tekshirib bo‘lmadi. Qayta urinib ko‘ring.")).toBeTruthy();
    expect(screen.queryByText("82 / 100")).toBeNull(); expect((screen.getByRole("button", { name: "So‘zni aytish" }) as HTMLButtonElement).disabled).toBe(false);
  });
  it("reports microphone denial and never starts a scoring request", () => {
    mock.micError = true; mount(); fireEvent.click(screen.getByRole("button", { name: "So‘zni aytish" }));
    expect(screen.getByText("Mikrofonga ruxsat berilmadi. Brauzer sozlamalarini tekshiring.")).toBeTruthy(); expect(mock.pronounce).not.toHaveBeenCalled();
  });
  it("does not assess a stale recording after unmount", async () => {
    const view = mount(); const callback = mock.recorded; view.unmount();
    await act(async () => { await callback?.(new Blob(["recording"])); });
    expect(mock.pronounce).not.toHaveBeenCalled();
  });
});
