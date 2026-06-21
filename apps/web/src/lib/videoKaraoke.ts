import type { TranscriptSegmentDto } from "@/api/types";
import { activeWordIndex } from "./useYouTubePlayer";

export function spokenWordIndex(segment: TranscriptSegmentDto, seconds: number): number {
  if (seconds < segment.startSeconds || seconds >= segment.endSeconds) return -1;
  if (segment.words.length) {
    return segment.words.findIndex(word => word.startSeconds <= seconds && seconds < word.endSeconds);
  }
  // Caption providers without word timestamps can only be estimated, never called exact.
  return activeWordIndex(segment, seconds);
}

export function captionTokens(segment: TranscriptSegmentDto): Array<{ text: string; wordIndex: number }> {
  if (segment.words.length) {
    return segment.words.flatMap((word, index) => [
      { text: word.text, wordIndex: index },
      ...(index < segment.words.length - 1 ? [{ text: " ", wordIndex: -1 }] : []),
    ]);
  }
  let wordIndex = -1;
  return segment.englishText.split(/([A-Za-z][A-Za-z'-]*)/g).map((text, index) => ({
    text, wordIndex: index % 2 ? ++wordIndex : -1,
  }));
}
