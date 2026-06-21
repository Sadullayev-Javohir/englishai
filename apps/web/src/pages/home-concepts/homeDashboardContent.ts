import { BookA, BookOpen, Headphones, Mic, PencilLine, Puzzle } from "lucide-react";
import type { VideoFeedItemDto } from "@/api/types";
import type { SkillStepKey } from "@/lib/skillSteps";

/** Screen 61's visual order. Completion and locking still come from the level map. */
export const HOME_SKILLS = [
  { key: "vocabulary", label: "Vocabulary", icon: BookA, to: "/app/vocabulary/topics", tile: "bg-ea-primary-soft", done: "bg-ea-primary border-ea-primary-deep" },
  { key: "grammar", label: "Grammar", icon: Puzzle, to: "/app/grammar", tile: "bg-ea-orange-50", done: "bg-[var(--home-grammar)] border-[var(--home-grammar-border)]" },
  { key: "reading", label: "Reading", icon: BookOpen, to: "/reading", tile: "bg-ea-green-50", done: "bg-[var(--home-green)] border-[var(--home-green)]" },
  { key: "writing", label: "Writing", icon: PencilLine, to: "/writing", tile: "bg-ea-orange-50", done: "bg-[var(--home-grammar)] border-[var(--home-grammar-border)]" },
  { key: "speaking", label: "Speaking", icon: Mic, to: "/app/speaking", tile: "bg-ea-green-50", done: "bg-[var(--home-green)] border-[var(--home-green)]" },
  { key: "listening", label: "Listening", icon: Headphones, to: "/listening", tile: "bg-ea-primary-soft", done: "bg-ea-primary border-ea-primary-deep" },
] satisfies Array<{ key: SkillStepKey; label: string; icon: typeof BookA; to: string; tile: string; done: string }>;

/** The two curated videos already present in the catalog and the approved Pen frame. */
export const HOME_VIDEOS: Array<VideoFeedItemDto & { thumbnail: string }> = [
  {
    lessonId: null,
    youTubeVideoId: "I_tRSrPru94",
    title: "Introduce yourself",
    channel: "BBC Learning English",
    durationSeconds: 157,
    topic: "everyday",
    level: 2,
    hasClosedCaptions: true,
    thumbnail: "/assets/play/home-videos/I_tRSrPru94.jpg",
  },
  {
    lessonId: null,
    youTubeVideoId: "bq6GBbh3uhU",
    title: "Daily routines",
    channel: "BBC Learning English",
    durationSeconds: 297,
    topic: "everyday",
    level: 2,
    hasClosedCaptions: true,
    thumbnail: "/assets/play/home-videos/bq6GBbh3uhU.jpg",
  },
];

export const HOME_WORD = { word: "grateful", translation: "minnatdor" };

export const homeFocus = "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-ea-primary";
export const homePrimaryButton = `inline-flex items-center justify-center gap-[9px] rounded-[14px] border-b-[3px] border-ea-primary-deep bg-ea-primary px-[18px] text-[13px] font-extrabold leading-[1.5] text-ea-on-primary min-[1200px]:border-b-0 hover:bg-ea-primary-deep disabled:cursor-not-allowed disabled:opacity-50 ${homeFocus}`;
export const homeEyebrow = "text-[10px] font-extrabold leading-[1.45] text-ea-primary";
