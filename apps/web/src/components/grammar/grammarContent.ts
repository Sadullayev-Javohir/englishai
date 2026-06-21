import type { GrammarTopicSummaryDto } from "@/api/types";

export function grammarFocusTitle(topic: Pick<GrammarTopicSummaryDto, "grammarFocusCode" | "title">): string {
  const names: Record<string, string> = {
    "present-perfect": "Present Perfect", "first-conditional": "First Conditional",
    "passive-voice": "Passive Voice", "reported-speech": "Reported Speech",
    "be-verb-present": "To Be", "to-be": "To Be", "wh-questions": "Wh- Questions",
  };
  return names[topic.grammarFocusCode] ?? (topic.grammarFocusCode
    ? topic.grammarFocusCode.split("-").map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(" ")
    : topic.title);
}

export const GRAMMAR_PHOTOS: Record<string, string> = {
  "present-perfect": "/assets/grammar/my-mother.jpg",
  "first-conditional": "/assets/grammar/first-conditional.jpg",
  "passive-voice": "/assets/grammar/passive-voice.jpg",
  "reported-speech": "/assets/grammar/reported-speech.jpg",
};
export function grammarPhoto(focusCode: string | undefined, topicTitle: string): string | undefined {
  const familyTopic = ["my mother", "my family", "family members", "oila a'zolari"].includes(topicTitle.toLowerCase());
  return (focusCode ? GRAMMAR_PHOTOS[focusCode] : undefined) ?? (familyTopic ? GRAMMAR_PHOTOS["present-perfect"] : undefined);
}
export function savedGrammarKey(learnerId: string) { return `englishai.grammar.saved.${learnerId}`; }
export function readSavedGrammar(learnerId: string): string[] {
  try {
    const value: unknown = JSON.parse(localStorage.getItem(savedGrammarKey(learnerId)) ?? "[]");
    return Array.isArray(value) ? value.filter((id): id is string => typeof id === "string") : [];
  } catch { return []; }
}
