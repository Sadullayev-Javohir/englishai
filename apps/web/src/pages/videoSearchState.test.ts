import { beforeEach, describe, expect, it } from "vitest";
import {
  EMPTY_VIDEO_SEARCH_STATE,
  loadVideoSearchState,
  saveVideoSearchState,
  videoSearchStorageKey,
  type VideoSearchState,
} from "./videoSearchState";

const state: VideoSearchState = {
  query: "travel english",
  results: [
    {
      lessonId: null,
      youTubeVideoId: "abcdefghijk",
      title: "Travel English",
      channel: "Learning Channel",
      durationSeconds: 240,
      topic: "search",
      level: 2,
      hasClosedCaptions: true,
    },
  ],
  isOpen: true,
  selectedVideoId: "abcdefghijk",
  resultsScrollTop: 180,
  pageScrollY: 420,
};

describe("videoSearchState", () => {
  beforeEach(() => window.sessionStorage.clear());

  it("restores the query, all results, selected video and scroll positions", () => {
    saveVideoSearchState("learner-a", state);

    expect(loadVideoSearchState("learner-a")).toEqual(state);
    expect(loadVideoSearchState("learner-b")).toEqual(EMPTY_VIDEO_SEARCH_STATE);
  });

  it("returns a clean state for malformed or legacy payloads", () => {
    window.sessionStorage.setItem(videoSearchStorageKey("learner-a"), "{bad json");
    expect(loadVideoSearchState("learner-a")).toEqual(EMPTY_VIDEO_SEARCH_STATE);

    const legacyItem = { ...state.results[0] } as Partial<(typeof state.results)[number]>;
    delete legacyItem.hasClosedCaptions;
    window.sessionStorage.setItem(
      videoSearchStorageKey("learner-a"),
      JSON.stringify({ ...state, results: [legacyItem] }),
    );
    expect(loadVideoSearchState("learner-a")).toEqual(EMPTY_VIDEO_SEARCH_STATE);
  });
});
