import { act, cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { VideoFeedDto, VideoFeedItemDto } from "@/api/types";
import { useVideoCatalog } from "./useVideoCatalog";
import { VIDEO_CATEGORIES, type VideoCategory } from "./videoCategories";

const mocks = vi.hoisted(() => ({ feed: vi.fn(), search: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { video: mocks } }));
const item: VideoFeedItemDto = {
  lessonId: null, youTubeVideoId: "abcdefghijk", title: "Listening lesson",
  channel: "Channel", channelAvatarUrl: "https://yt3.ggpht.com/avatar", durationSeconds: 120,
  level: 2, topic: "listening", hasClosedCaptions: true,
};
const firstLearningCategory: VideoCategory = {
  id: "all", label: "First", query: "first English learning query", collection: null,
};
const secondLearningCategory: VideoCategory = {
  id: "all", label: "Second", query: "second English learning query", collection: null,
};
const thirdLearningCategory: VideoCategory = {
  id: "all", label: "Third", query: "third English learning query", collection: null,
};
beforeEach(() => {
  mocks.feed.mockReset().mockResolvedValue({ items: [item], nextCursor: null });
  mocks.search.mockReset().mockResolvedValue({ items: [], nextCursor: null });
});
afterEach(cleanup);

describe("useVideoCatalog", () => {
  it("ignores a late response from the previously selected category", async () => {
    let resolveOld!: (page: VideoFeedDto) => void;
    mocks.search.mockImplementation((query: string) => query === firstLearningCategory.query
      ? new Promise<VideoFeedDto>(resolve => { resolveOld = resolve; })
      : Promise.resolve({ items: [{ ...item, title: "Grammar lesson" }], nextCursor: null }));
    const { result, rerender } = renderHook(
      ({ category }: { category: VideoCategory }) => useVideoCatalog(category),
      { initialProps: { category: firstLearningCategory } },
    );
    rerender({ category: secondLearningCategory });
    await waitFor(() => expect(result.current.items[0]?.title).toBe("Grammar lesson"));
    await act(async () => resolveOld({ items: [{ ...item, title: "Stale speaking lesson" }], nextCursor: "old" }));
    expect(result.current.items[0].title).toBe("Grammar lesson");
    expect(result.current.hasMore).toBe(false);
    expect(result.current.loading).toBe(false);
  });

  it("uses the selected category cursor, deduplicates pages and resets on category change", async () => {
    mocks.search
      .mockResolvedValueOnce({ items: [item], nextCursor: "grammar-next" })
      .mockResolvedValueOnce({ items: [item, { ...item, youTubeVideoId: "lmnopqrstuv" }], nextCursor: null })
      .mockResolvedValueOnce({ items: [{ ...item, title: "Pronunciation lesson" }], nextCursor: null });
    const { result, rerender } = renderHook(
      ({ category }: { category: VideoCategory }) => useVideoCatalog(category),
      { initialProps: { category: firstLearningCategory } },
    );
    await waitFor(() => expect(result.current.loading).toBe(false));
    act(() => { result.current.loadMore(); result.current.loadMore(); });
    await waitFor(() => expect(result.current.items).toHaveLength(2));
    expect(mocks.search).toHaveBeenNthCalledWith(2, firstLearningCategory.query, "grammar-next", 12);
    rerender({ category: secondLearningCategory });
    await waitFor(() => expect(result.current.items[0]?.title).toBe("Pronunciation lesson"));
    expect(mocks.search).toHaveBeenNthCalledWith(3, secondLearningCategory.query, null, 12);
    expect(result.current.items).toHaveLength(1);
  });

  it("shows a retryable error instead of unrelated cached videos", async () => {
    mocks.search.mockRejectedValueOnce(new Error("Offline"))
      .mockResolvedValueOnce({ items: [item], nextCursor: null });
    const { result } = renderHook(() => useVideoCatalog(thirdLearningCategory));
    await waitFor(() => expect(result.current.error).toBe(true));
    expect(result.current.loading).toBe(false);
    expect(result.current.items).toEqual([]);
    act(() => result.current.retry());
    await waitFor(() => expect(result.current.items).toHaveLength(1));
    expect(result.current.error).toBe(false);
  });

  it.each(VIDEO_CATEGORIES.filter((category) => category.collection !== null))(
    "does not fetch individual videos for the %s collection category",
    async (category) => {
      const { result } = renderHook(() => useVideoCatalog(category));
      await waitFor(() => expect(result.current.loading).toBe(false));
      expect(mocks.search).not.toHaveBeenCalled();
      expect(result.current.items).toEqual([]);
    },
  );
});
