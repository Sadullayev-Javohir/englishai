import { uz } from "@/content/uz";
import { CefrLevel, ErrorCategory, ReviewStage, SkillType } from "@/api/types";

const CEFR_KEY: Record<CefrLevel, keyof typeof uz.cefrNames> = {
  [CefrLevel.A1]: "A1",
  [CefrLevel.A2]: "A2",
  [CefrLevel.B1]: "B1",
  [CefrLevel.B2]: "B2",
  [CefrLevel.C1]: "C1",
  [CefrLevel.C2]: "C2",
};

export const cefrShort = (level: CefrLevel): string => CEFR_KEY[level];
export const cefrLong = (level: CefrLevel): string => uz.cefrNames[CEFR_KEY[level]];

const SKILL_KEY: Record<SkillType, keyof typeof uz.skills> = {
  [SkillType.Speaking]: "Speaking",
  [SkillType.Listening]: "Listening",
  [SkillType.Reading]: "Reading",
  [SkillType.Writing]: "Writing",
  [SkillType.Grammar]: "Grammar",
  [SkillType.Vocabulary]: "Vocabulary",
};

export const skillLabel = (skill: SkillType): string => uz.skills[SKILL_KEY[skill]];

const ERROR_KEY: Record<ErrorCategory, keyof typeof uz.errorCategories> = {
  [ErrorCategory.Articles]: "Articles",
  [ErrorCategory.VerbTense]: "VerbTense",
  [ErrorCategory.Prepositions]: "Prepositions",
  [ErrorCategory.GerundInfinitive]: "GerundInfinitive",
  [ErrorCategory.Modals]: "Modals",
  [ErrorCategory.SubjectVerbAgreement]: "SubjectVerbAgreement",
  [ErrorCategory.WordOrder]: "WordOrder",
  [ErrorCategory.Pronunciation]: "Pronunciation",
  [ErrorCategory.Vocabulary]: "Vocabulary",
  [ErrorCategory.Spelling]: "Spelling",
  [ErrorCategory.Other]: "Other",
};

export const errorCategoryLabel = (category: ErrorCategory): string =>
  uz.errorCategories[ERROR_KEY[category]];

export const reviewStageLabel = (stage: ReviewStage): string => {
  switch (stage) {
    case ReviewStage.Day3:
      return uz.vocabulary.stageDay3;
    case ReviewStage.Day7:
      return uz.vocabulary.stageDay7;
    case ReviewStage.Day21:
      return uz.vocabulary.stageDay21;
    case ReviewStage.Mastered:
      return uz.vocabulary.stageMastered;
  }
};

// Every learner-facing date uses one Uzbek-standard numeric format: dd.mm.yyyy
// (e.g. "29.06.2026"). Centralised so every screen reads the same way.
export const formatDate = (iso: string): string => {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  const day = d.getDate().toString().padStart(2, "0");
  const month = (d.getMonth() + 1).toString().padStart(2, "0");
  return `${day}.${month}.${d.getFullYear()}`;
};

// Same numeric date as formatDate plus the wall-clock time (24h), e.g. "29.06.2026 14:05".
// Used where "when did this arrive?" matters, such as the notifications feed.
export const formatDateTime = (iso: string): string => {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  const day = d.getDate().toString().padStart(2, "0");
  const month = (d.getMonth() + 1).toString().padStart(2, "0");
  const hours = d.getHours().toString().padStart(2, "0");
  const minutes = d.getMinutes().toString().padStart(2, "0");
  return `${day}.${month}.${d.getFullYear()} ${hours}:${minutes}`;
};

// Compact "when did this notification arrive?" label: just the wall-clock time (24h, e.g. "13:46")
// for something from today, or the standard full date once a day or more has passed.
export const formatNotificationTime = (iso: string): string => {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  const now = new Date();
  const sameDay =
    d.getFullYear() === now.getFullYear() &&
    d.getMonth() === now.getMonth() &&
    d.getDate() === now.getDate();
  if (sameDay) {
    const hours = d.getHours().toString().padStart(2, "0");
    const minutes = d.getMinutes().toString().padStart(2, "0");
    return `${hours}:${minutes}`;
  }
  return formatDate(iso);
};

export const formatDuration = (seconds: number): string => {
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return `${m}:${s.toString().padStart(2, "0")}`;
};

// Human study time in vetted Uzbek (rule 11): "2 soat 15 daqiqa" / "45 daqiqa" / "30 soniya".
export const formatStudyTime = (seconds: number): string => {
  const t = uz.progress.stats;
  if (seconds <= 0) return t.zero;
  if (seconds < 60) return `${seconds} ${t.second}`;

  const totalMinutes = Math.floor(seconds / 60);
  const h = Math.floor(totalMinutes / 60);
  const m = totalMinutes % 60;
  if (h === 0) return `${m} ${t.minute}`;
  if (m === 0) return `${h} ${t.hour}`;
  return `${h} ${t.hour} ${m} ${t.minute}`;
};

// Short "Iyn 2026" label for a 12-month bucket.
export const monthLabel = (year: number, month: number): string =>
  `${uz.progress.stats.months[month - 1]} ${year}`;

export const optionLetter = (index: number): string => String.fromCharCode(65 + index);

// The short Uzbek-facing name of a grammar focus code (e.g. "past-simple" → "Past Simple (oddiy
// o'tgan zamon)"). Falls back to a humanised version of the code if no vetted label exists yet, so a
// new spine code is still readable rather than blank.
export const grammarFocusLabel = (code: string): string =>
  uz.grammar.focusNames[code] ??
  code
    .split("-")
    .map((w) => (w ? w[0].toUpperCase() + w.slice(1) : w))
    .join(" ");
