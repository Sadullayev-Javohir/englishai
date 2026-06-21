import { LessonQuizOption, type LessonQuizOptionProps } from "@/components/lesson/LessonQuizOption";
import "./VocabularyQuizOption.css";

/** Keeps the vocabulary API and selectors while sharing the exact answer UI. */
export function VocabularyQuizOption(props: Omit<LessonQuizOptionProps, "classPrefix">) {
  return <LessonQuizOption {...props} classPrefix="vocabulary-quiz-option" />;
}
