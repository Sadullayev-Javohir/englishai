import type { TopicWordDto } from "@/api/types";

export type VocabularyExercise =
  | { kind: "mc"; dir: "en-uz" | "uz-en"; word: string; prompt: string; options: string[]; answer: string }
  | { kind: "type"; word: string; prompt: string; answer: string };

function shuffle<T>(values: T[], random: () => number): T[] {
  const result = [...values];
  for (let index = result.length - 1; index > 0; index -= 1) {
    const swapIndex = Math.floor(random() * (index + 1));
    [result[index], result[swapIndex]] = [result[swapIndex], result[index]];
  }
  return result;
}

export function buildVocabularyExercises(
  words: TopicWordDto[],
  random: () => number = Math.random,
): VocabularyExercise[] {
  const validWords = words.filter((word) => word.word.trim() && word.translation.trim());
  const translations = validWords.map((word) => word.translation);

  // Only two exercise shapes: a multiple-choice "Test" (pick the translation) and a "So'zni yozing"
  // typed answer. They alternate so every other word is typed, which keeps recall active without
  // the match-pairs drill (dropped as redundant with the multiple-choice test).
  return validWords.map((word, index) => {
    const distractors = shuffle(
      translations.filter((translation) => translation !== word.translation),
      random,
    ).slice(0, 3);

    if (index % 2 === 1) {
      return {
        kind: "type",
        word: word.word,
        prompt: word.translation,
        answer: word.word,
      };
    }

    return {
      kind: "mc",
      dir: "en-uz",
      word: word.word,
      prompt: word.word,
      options: shuffle([word.translation, ...distractors], random),
      answer: word.translation,
    };
  });
}
