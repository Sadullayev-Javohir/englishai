import type { TranscriptSegmentDto, TranscriptWordDto, VideoLessonDto } from "@/api/types";

/** YouTube caption speaker-change marker ('>>') with any surrounding whitespace. */
const SPEAKER_MARKER = /\s*>+\s*/g;
/** Non-spoken caption cues such as [music], [singing], [applause], or [laughter]. */
const NON_SPEECH_CUE = /\[[^\]]*\]/g;

export function stripTranscriptCues(text: string): string {
  return text
    .replace(SPEAKER_MARKER, " ")
    .replace(NON_SPEECH_CUE, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function sanitizeWords(words: TranscriptWordDto[]): TranscriptWordDto[] {
  let insideCue = false;

  return words.flatMap((word) => {
    let text = word.text;
    let visible = "";

    for (const character of text) {
      if (character === "[") {
        insideCue = true;
        continue;
      }
      if (character === "]" && insideCue) {
        insideCue = false;
        continue;
      }
      if (!insideCue) visible += character;
    }

    text = stripTranscriptCues(visible);
    return text ? [{ ...word, text }] : [];
  });
}

/**
 * Removes source-only caption markers before they reach the player, full transcript, shadowing,
 * or AI context. Segments containing only a non-spoken cue are removed entirely.
 */
export function sanitizeVideoTranscript(lesson: VideoLessonDto): VideoLessonDto {
  const transcript = lesson.transcript.flatMap((segment): TranscriptSegmentDto[] => {
    const englishText = stripTranscriptCues(segment.englishText);
    if (!englishText) return [];

    return [{
      ...segment,
      englishText,
      words: sanitizeWords(segment.words),
    }];
  });

  return { ...lesson, transcript };
}
