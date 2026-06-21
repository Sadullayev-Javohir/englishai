import { uz } from "@/content/uz";
import type { CefrLevel, TopicModuleScoreDto } from "@/api/types";
import { SKILL_STEPS, type SkillStep } from "@/lib/skillSteps";
import type { HomeFallbackKind } from "./homeRecommendation";
import { getNativeCapabilities } from "@/api/nativeCapabilities";

export interface HomeAction {
  key: string;
  title: string;
  text: string;
  cta: string;
  icon: string;
  to: string;
  topicId?: string;
  topicTitle?: string;
  skillKey?: SkillStep["key"];
  fallbackKind?: HomeFallbackKind;
  done?: boolean;
  locked?: boolean;
  current?: boolean;
  count?: number;
}

export interface HomeViewModel {
  name: string;
  pictureUrl?: string | null;
  activeTopicTitle?: string | null;
  activeTopicId?: string | null;
  activeTopicLevel?: CefrLevel | null;
  completed: number;
  total: number;
  remaining: number;
  complete: boolean;
  streakAtRisk: boolean;
  currentStreak: number;
  dailyGoalTarget: number;
  todayCompletedTasks: number;
  /** Distinct core skills practiced today, separate from the personal task goal. */
  todayCompletedSkills: number;
  practiceWordCount: number;
  savedCount: number;
  /** Saved words whose 3/7/21 review has come due - drives the home "takrorlash" nudge badge. */
  dueSavedCount: number;
  leaderboardRank?: number | null;
  dueTopicTitle?: string | null;
  dueReviewDay?: 3 | 7 | 21;
  daily: HomeAction[];
  vocabulary: HomeAction[];
  modules: HomeAction[];
  nextAction: HomeAction;
  weakestSkill: HomeAction;
  weeklyMinutes: number;
  weeklyProgress: number[];
  badges: string[];
}

export interface HomeTopicProgress {
  id: string;
  title: string;
  modules: TopicModuleScoreDto[];
}

export interface HomeViewModelInput {
  name: string;
  pictureUrl?: string | null;
  activeTopic?: HomeTopicProgress | null;
  activeTopicLevel?: CefrLevel | null;
  fallbackKind?: HomeFallbackKind | null;
  streakAtRisk?: boolean;
  goalMet?: boolean;
  currentStreak?: number;
  dailyGoalTarget?: number;
  todayCompletedTasks?: number;
  skillsCompletedToday?: string[];
  practiceWordCount?: number;
  savedCount?: number;
  dueSavedCount?: number;
  leaderboardRank?: number | null;
  dueTopicTitle?: string | null;
  dueReviewDay?: 3 | 7 | 21;
  weekSeconds?: number;
  last7Days?: Array<{ day: string; seconds: number }>;
}

const MODULES = [
  ...SKILL_STEPS.map(({ key, icon }) => ({ key, to: fallbackRoute(key), icon })),
  { key: "roleplay", to: "/app/speaking/role-talk", icon: "theater_comedy" },
  { key: "books", to: "/books", icon: "library_books" },
  { key: "video", to: "/video", icon: "play_circle" },
] as const;

function copyFor(key: keyof typeof uz.home.modules) {
  return uz.home.modules[key];
}

function fallbackRoute(key: SkillStep["key"]): string {
  switch (key) {
    case "vocabulary": return "/app/vocabulary/topics";
    case "grammar": return "/app/grammar";
    case "reading": return "/reading";
    case "writing": return "/writing";
    case "speaking": return "/app/speaking";
    case "listening": return "/listening";
  }
}

function topicRoute(step: SkillStep, topicId: string): string {
  switch (step.key) {
    case "vocabulary": return `/app/vocabulary/topic/${topicId}`;
    case "grammar": return `/app/grammar/topic/${topicId}`;
    case "reading": return `/reading/topic/${topicId}`;
    case "writing": return `/writing/task/${topicId}`;
    case "speaking": return `/app/speaking/topic/${encodeURIComponent(topicId)}`;
    case "listening": return `/listening/topic/${topicId}`;
  }
}

export function buildHomeViewModel(input: HomeViewModelInput): HomeViewModel {
  const capabilities = getNativeCapabilities();
  const moduleByName = new Map(input.activeTopic?.modules.map((module) => [module.module, module]) ?? []);
  const currentKey = SKILL_STEPS.find((step) => {
    const module = moduleByName.get(step.module);
    return module?.unlocked && !module.passed;
  })?.key;
  const daily = SKILL_STEPS.map((step) => {
    const copy = copyFor(step.key);
    const module = moduleByName.get(step.module);
    const hasTopic = Boolean(input.activeTopic);
    const done = Boolean(module?.passed);
    const locked = hasTopic ? !module?.unlocked : false;
    const current = hasTopic ? step.key === currentKey : step.key === SKILL_STEPS[0].key;
    return {
      key: step.module,
      title: copy.title,
      text: input.activeTopic ? `${input.activeTopic.title} · ${copy.text}` : copy.text,
      cta: copy.cta,
      icon: step.icon,
      topicId: input.activeTopic?.id,
      topicTitle: input.activeTopic?.title,
      skillKey: step.key,
      to: input.activeTopic
        ? topicRoute(step, input.activeTopic.id)
        : fallbackRoute(step.key),
      done,
      locked,
      current,
    };
  });
  const completed = daily.filter((skill) => skill.done).length;
  const total = daily.length;
  const remaining = Math.max(0, total - completed);
  const practiceWordCount = input.practiceWordCount ?? 0;
  const savedCount = input.savedCount ?? 0;
  const dueSavedCount = input.dueSavedCount ?? 0;
  const vocabulary: HomeAction[] = [
    {
      key: "saved",
      title: uz.home.savedWordsCard.title,
      text: uz.home.savedWordsCard.text,
      cta: uz.home.openLabel,
      icon: "bookmarks",
      to: "/app/vocabulary/saved",
      count: savedCount,
    },
    {
      key: "review",
      title: uz.home.reviewCard.title,
      text: uz.home.reviewCard.text,
      cta: uz.home.openLabel,
      icon: "record_voice_over",
      to: "/app/speaking/practice-words",
      count: practiceWordCount,
    },
  ];
  const modules = MODULES.filter((module) => capabilities.video || module.key !== "video").map((module) => {
    const copy = copyFor(module.key);
    return { key: module.key, title: copy.title, text: copy.text, cta: copy.cta, icon: module.icon, to: module.to };
  });
  const nextTopicSkill = daily.find((skill) => skill.current && !skill.done && !skill.locked)
    ?? daily.find((skill) => !skill.done && !skill.locked)
    ?? daily[0];
  const fallbackKind = input.fallbackKind === "video" && !capabilities.video ? "books" : input.fallbackKind;
  const fallbackModule = fallbackKind
    ? modules.find((module) => module.key === fallbackKind)
    : undefined;
  const nextAction = fallbackModule
    ? { ...fallbackModule, fallbackKind: fallbackKind ?? undefined }
    : nextTopicSkill;
  const weakestSkill = daily.find((skill) => !skill.done && !skill.locked && skill.key !== nextAction.key) ?? nextTopicSkill;
  const weeklyMinutes = Math.round(Math.max(0, input.weekSeconds ?? 0) / 60);
  const weeklyProgress = normalizeLast7Days(input.last7Days);

  return {
    name: input.name,
    pictureUrl: input.pictureUrl ?? null,
    activeTopicTitle: input.activeTopic?.title ?? null,
    activeTopicId: input.activeTopic?.id ?? null,
    activeTopicLevel: input.activeTopicLevel ?? null,
    completed,
    total,
    remaining,
    complete: completed >= total,
    streakAtRisk: Boolean(input.streakAtRisk) && !input.goalMet,
    currentStreak: Math.max(0, input.currentStreak ?? 0),
    dailyGoalTarget: Math.max(0, input.dailyGoalTarget ?? 0),
    todayCompletedTasks: Math.max(0, input.todayCompletedTasks ?? 0),
    todayCompletedSkills: SKILL_STEPS.filter(step => input.skillsCompletedToday?.includes(step.module)).length,
    practiceWordCount,
    savedCount,
    dueSavedCount,
    leaderboardRank: input.leaderboardRank ?? null,
    dueTopicTitle: input.dueTopicTitle ?? null,
    dueReviewDay: input.dueReviewDay,
    daily,
    vocabulary,
    modules,
    nextAction,
    weakestSkill,
    weeklyMinutes,
    weeklyProgress,
    badges: [uz.homeConceptLab.badges.consistency, uz.homeConceptLab.badges.firstStep, uz.homeConceptLab.badges.weekly],
  };
}

function normalizeLast7Days(days?: Array<{ day: string; seconds: number }>): number[] {
  const values = (days ?? []).slice(-7).map((day) => Math.round(Math.max(0, day.seconds) / 60));
  return [...Array(Math.max(0, 7 - values.length)).fill(0), ...values];
}
