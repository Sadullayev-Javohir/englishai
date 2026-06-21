import { describe, expect, it } from "vitest";
import type { VideoLessonDto } from "@/api/types";
import { sanitizeVideoTranscript, stripTranscriptCues } from "./videoTranscript";

function lessonWithTranscript(transcript: VideoLessonDto["transcript"]): VideoLessonDto {
  return { transcript } as VideoLessonDto;
}

describe("video transcript sanitizing", () => {
  it("removes bracketed non-speech cues regardless of their label or casing", () => {
    expect(stripTranscriptCues("[Music] Hello [SINGING] world [applause]"))
      .toBe("Hello world");
  });

  it("removes cue-only segments and keeps spoken text and words in sync", () => {
    const lesson = lessonWithTranscript([
      {
        startSeconds: 0,
        endSeconds: 2,
        englishText: "[music]",
        uzbekTranslation: null,
        words: [{ text: "[music]", startSeconds: 0, endSeconds: 2 }],
      },
      {
        startSeconds: 2,
        endSeconds: 5,
        englishText: ">> [singing] We are learning",
        uzbekTranslation: null,
        words: [
          { text: ">>", startSeconds: 2, endSeconds: 2.2 },
          { text: "[singing]", startSeconds: 2.2, endSeconds: 2.5 },
          { text: "We", startSeconds: 2.5, endSeconds: 3 },
          { text: "are", startSeconds: 3, endSeconds: 4 },
          { text: "learning", startSeconds: 4, endSeconds: 5 },
        ],
      },
    ]);

    const sanitized = sanitizeVideoTranscript(lesson);

    expect(sanitized.transcript).toHaveLength(1);
    expect(sanitized.transcript[0].englishText).toBe("We are learning");
    expect(sanitized.transcript[0].words.map((word) => word.text)).toEqual(["We", "are", "learning"]);
  });
});
