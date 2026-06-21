import { CefrLevel, PlacementItemKind, TestStage, type PlacementItemDto } from "@/api/types";

// DEV gallery only. Production questions, ordering and progress come from the API.
const base: PlacementItemDto = {
  kind: PlacementItemKind.MultipleChoice, stage: TestStage.Grammar, difficulty: CefrLevel.B1,
  stageNumber: 2, stageCount: 6, itemNumberInStage: 5, itemsInStage: 12,
  minWords: null, maxWords: null, id: "preview-grammar",
  prompt: "Choose the\ncorrect sentence.",
  options: ["She don’t like coffee.", "She doesn’t likes coffee.", "She doesn’t like coffee.", "She not like coffee."],
  passageText: null, hasAudio: false, completedItems: 4, totalItems: 12,
};

export const PLACEMENT_PREVIEWS: Record<string, PlacementItemDto> = {
  grammar: base,
  vocabulary: {
    ...base, id: "preview-vocabulary", stage: TestStage.Vocabulary, stageNumber: 1,
    itemNumberInStage: 3, itemsInStage: 6, completedItems: 2, totalItems: 23,
    prompt: "What does\n“confident” mean?",
    options: ["Sure of yourself", "Very tired", "Always in a hurry", "Afraid to try"],
  },
  listening: {
    ...base, id: "preview-listening", stage: TestStage.Listening, stageNumber: 3,
    itemNumberInStage: 2, itemsInStage: 5, completedItems: 13, totalItems: 23, hasAudio: true,
    prompt: "Where will they meet?",
    options: ["At the library", "At the train station", "At the café", "At the park"],
  },
  reading: {
    ...base, id: "preview-reading", stage: TestStage.Reading, stageNumber: 4,
    itemNumberInStage: 1, itemsInStage: 4, completedItems: 17, totalItems: 23,
    passageText: "When Maya moved to a new city, she hardly knew anyone. One Saturday, she noticed a small community garden near her apartment and decided to help.\n\nAt first, she only watered the plants. Soon, she was sharing gardening tips with her neighbours and joining their weekend lunches. The garden became more than a place to grow vegetables — it helped her feel at home.",
    prompt: "How did the garden\nhelp Maya?",
    options: ["She found a new job", "She connected with her neighbours", "She moved to a bigger apartment", "She started selling vegetables"],
  },
  writing: {
    ...base, id: "preview-writing", kind: PlacementItemKind.Writing, stage: TestStage.Writing,
    stageNumber: 5, itemNumberInStage: 1, itemsInStage: 1, completedItems: 21, totalItems: 23,
    prompt: "Describe a place\nyou enjoy visiting.",
    options: null, minWords: 40, maxWords: 80,
  },
  speaking: {
    ...base, id: "preview-speaking", kind: PlacementItemKind.Speaking, stage: TestStage.Speaking,
    stageNumber: 6, itemNumberInStage: 1, itemsInStage: 1, completedItems: 22, totalItems: 23,
    prompt: "Tell us about\nyour favourite hobby.",
    options: null,
  },
};
