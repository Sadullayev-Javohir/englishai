import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "@/api/client";
import type { VideoFeedItemDto } from "@/api/types";
import type { VideoCategory } from "./videoCategories";

/** Each category has its own request/cursor. Late responses never overwrite a newer category. */
export function useVideoCatalog(category: VideoCategory) {
  // Collection categories have their own playlist-only source. Do not even
  // request the ordinary video-search feed there: the UI must never receive
  // individual clips for Multfilm or Kino.
  const enabled = category.collection === null;
  const [items, setItems] = useState<VideoFeedItemDto[]>([]);
  const [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState(false);
  const generation = useRef(0);
  const busy = useRef(false);

  const fetchPage = useCallback(async (nextCursor: string | null) => {
    if (!enabled || busy.current) return;
    busy.current = true;
    const request = ++generation.current;
    const firstPage = nextCursor === null;
    if (firstPage) setLoading(true);
    else setLoadingMore(true);
    setError(false);
    try {
      const page = await api.video.search(category.query, nextCursor, 12);
      if (request !== generation.current) return;
      setItems(previous => {
        const result = firstPage ? [] : [...previous];
        const seen = new Set(result.map(item => item.youTubeVideoId));
        for (const item of page.items) {
          if (!seen.has(item.youTubeVideoId)) {
            seen.add(item.youTubeVideoId);
            result.push(item);
          }
        }
        return result;
      });
      setCursor(page.nextCursor);
    } catch {
      if (request === generation.current) setError(true);
    } finally {
      if (request === generation.current) {
        busy.current = false;
        setLoading(false);
        setLoadingMore(false);
      }
    }
  }, [category.query, enabled]);

  useEffect(() => {
    setItems([]);
    setCursor(null);
    setLoadingMore(false);
    if (!enabled) {
      generation.current += 1;
      busy.current = false;
      setLoading(false);
      setError(false);
      return;
    }
    void fetchPage(null);
    return () => {
      generation.current += 1;
      busy.current = false;
    };
  }, [enabled, fetchPage]);

  const loadMore = useCallback(() => {
    if (cursor && !error) void fetchPage(cursor);
  }, [cursor, error, fetchPage]);
  const retry = useCallback(() => void fetchPage(cursor), [cursor, fetchPage]);

  return { items, loading, loadingMore, error, hasMore: cursor !== null, loadMore, retry };
}
