import { describe, expect, it } from "vitest";
import { captionTokens, spokenWordIndex } from "./videoKaraoke";
const segment = { startSeconds: 1, endSeconds: 4, englishText: "Hello, Tim!", uzbekTranslation: null, words: [
  { text: "Hello,", startSeconds: 1, endSeconds: 2 }, { text: "Tim!", startSeconds: 2.5, endSeconds: 4 },
] };
describe("word-level transcript timing", () => {
  it("uses exact timing, not a future word or stale word during silence", () => {
    expect(spokenWordIndex(segment, 0.99)).toBe(-1);
    expect(spokenWordIndex(segment, 1.2)).toBe(0);
    expect(spokenWordIndex(segment, 2.2)).toBe(-1);
    expect(spokenWordIndex(segment, 2.5)).toBe(1);
    expect(spokenWordIndex(segment, 4)).toBe(-1);
  });
  it("preserves timestamp indexing even when words include punctuation", () => {
    expect(captionTokens(segment)).toEqual([{ text: "Hello,", wordIndex: 0 }, { text: " ", wordIndex: -1 }, { text: "Tim!", wordIndex: 1 }]);
  });
  it("estimates timing only when the source has no word timestamps", () => {
    expect(spokenWordIndex({ ...segment, words: [] }, 3.9)).toBe(1);
    expect(captionTokens({ ...segment, words: [] }).filter(t => t.wordIndex >= 0).map(t => t.text)).toEqual(["Hello", "Tim"]);
  });
});
