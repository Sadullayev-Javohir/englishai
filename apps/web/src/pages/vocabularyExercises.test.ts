import { describe, expect, it } from "vitest";
import type { TopicWordDto } from "@/api/types";
import { buildVocabularyExercises } from "./vocabularyExercises";

const words: TopicWordDto[] = Array.from({ length: 15 }, (_, index) => ({
  word: `word-${index + 1}`,
  translation: `tarjima-${index + 1}`,
  ipa: null,
  exampleSentence: null,
  partOfSpeech: index % 2 === 0 ? "verb" : "noun",
  imageUrl: null,
  imageAttribution: null,
}));

describe("buildVocabularyExercises", () => {
  it("builds exactly one question for each of the 15 topic words", () => {
    const exercises = buildVocabularyExercises(words, () => 0.5);

    expect(exercises).toHaveLength(15);
    expect(exercises.map((exercise) => exercise.word)).toEqual(words.map((word) => word.word));
    expect(new Set(exercises.map((exercise) => exercise.word)).size).toBe(15);
  });

  it("alternates between the multiple-choice and typed exercise types only", () => {
    const exercises = buildVocabularyExercises(words, () => 0.5);

    expect(new Set(exercises.map((exercise) => exercise.kind))).toEqual(
      new Set(["mc", "type"]),
    );
  });
});
