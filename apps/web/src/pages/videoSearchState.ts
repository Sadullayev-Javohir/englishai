import type { VideoFeedItemDto } from "@/api/types";

export interface VideoSearchState {
  query: string;
  results: VideoFeedItemDto[];
  isOpen: boolean;
  selectedVideoId: string | null;
  resultsScrollTop: number;
  pageScrollY: number;
}

export const EMPTY_VIDEO_SEARCH_STATE: VideoSearchState = {
  query: "",
  results: [],
  isOpen: false,
  selectedVideoId: null,
  resultsScrollTop: 0,
  pageScrollY: 0,
};

const STORAGE_PREFIX = "englishai:video-search:";

export function videoSearchStorageKey(learnerId: string): string {
  return `${STORAGE_PREFIX}${learnerId}`;
}

export function loadVideoSearchState(learnerId: string): VideoSearchState {
  try {
    const raw = window.sessionStorage.getItem(videoSearchStorageKey(learnerId));
    if (!raw) return EMPTY_VIDEO_SEARCH_STATE;

    const value = JSON.parse(raw) as Partial<VideoSearchState>;
    if (
      typeof value.query !== "string"
      || !Array.isArray(value.results)
      || typeof value.isOpen !== "boolean"
      || !(typeof value.selectedVideoId === "string" || value.selectedVideoId === null)
      || typeof value.resultsScrollTop !== "number"
      || typeof value.pageScrollY !== "number"
      || !value.results.every(isVideoFeedItem)
    ) {
      return EMPTY_VIDEO_SEARCH_STATE;
    }

    return {
      query: value.query,
      results: value.results,
      isOpen: value.isOpen,
      selectedVideoId: value.selectedVideoId,
      resultsScrollTop: Math.max(0, value.resultsScrollTop),
      pageScrollY: Math.max(0, value.pageScrollY),
    };
  } catch {
    return EMPTY_VIDEO_SEARCH_STATE;
  }
}

export function saveVideoSearchState(learnerId: string, state: VideoSearchState): void {
  window.sessionStorage.setItem(videoSearchStorageKey(learnerId), JSON.stringify(state));
}

function isVideoFeedItem(value: unknown): value is VideoFeedItemDto {
  if (typeof value !== "object" || value === null) return false;
  const item = value as Partial<VideoFeedItemDto>;
  return (
    (typeof item.lessonId === "string" || item.lessonId === null)
    && typeof item.youTubeVideoId === "string"
    && typeof item.title === "string"
    && typeof item.channel === "string"
    && (item.channelAvatarUrl == null || typeof item.channelAvatarUrl === "string")
    && typeof item.durationSeconds === "number"
    && typeof item.topic === "string"
    && typeof item.level === "number"
    && typeof item.hasClosedCaptions === "boolean"
  );
}
