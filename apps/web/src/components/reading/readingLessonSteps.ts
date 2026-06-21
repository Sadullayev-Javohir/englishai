export const READING_LESSON_STEPS = [
  { id: "catalog", label: "Mavzular" },
  { id: "text", label: "Matn" },
  { id: "practice", label: "Test" },
  { id: "result", label: "Yakun" },
] as const;

export type ReadingLessonStep = typeof READING_LESSON_STEPS[number]["id"];
