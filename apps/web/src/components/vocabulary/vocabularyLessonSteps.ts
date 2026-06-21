/** Required lesson flow. Pronunciation is optional practice inside Flashcards. */
export const VOCABULARY_LESSON_STEPS = [
  { id: "intro", label: "Kirish" },
  { id: "text", label: "Matn" },
  { id: "flashcards", label: "Flashcardlar" },
  { id: "test", label: "Test" },
  { id: "result", label: "Yakun" },
] as const;

export type VocabularyLessonStep = typeof VOCABULARY_LESSON_STEPS[number]["id"];
