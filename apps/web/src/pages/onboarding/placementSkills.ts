import { BookA, BookOpen, Headphones, Mic, PencilLine, Puzzle } from "lucide-react";
import { TestStage } from "@/api/types";

export const PLACEMENT_SKILLS = [
  { stage: TestStage.Vocabulary, label: "Vocabulary", Icon: BookA },
  { stage: TestStage.Grammar, label: "Grammar", Icon: Puzzle },
  { stage: TestStage.Listening, label: "Listening", Icon: Headphones },
  { stage: TestStage.Reading, label: "Reading", Icon: BookOpen },
  { stage: TestStage.Writing, label: "Writing", Icon: PencilLine },
  { stage: TestStage.Speaking, label: "Speaking", Icon: Mic },
] as const;
