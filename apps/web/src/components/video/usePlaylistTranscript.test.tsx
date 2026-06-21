import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "@/api/client";
import { CefrLevel, IngestionStatus, TranscriptStatus, type VideoLessonDto } from "@/api/types";
import { usePlaylistTranscript } from "./usePlaylistTranscript";

vi.mock("@/api/client", () => ({ api: { video: { lesson: vi.fn() } } }));
const pending: VideoLessonDto = {
  id: "lesson-1", youTubeVideoId: "abcdefghijk", title: "Video", channel: "Channel", durationSeconds: 60,
  topic: "test", level: CefrLevel.A2, status: IngestionStatus.Leveled,
  transcriptStatus: TranscriptStatus.Pending, transcript: [], glossary: [], questions: [],
};
const available: VideoLessonDto = { ...pending, transcriptStatus: TranscriptStatus.Available, transcript: [
  { startSeconds: 1, endSeconds: 3, englishText: "Hello world.", uzbekTranslation: null, words: [] },
] };
const tick = async (ms = 0) => { await act(async () => { await vi.advanceTimersByTimeAsync(ms); }); };

beforeEach(() => { vi.useFakeTimers(); vi.mocked(api.video.lesson).mockReset(); });
afterEach(() => { vi.useRealTimers(); });

describe("playlist transcript refresh", () => {
  it("renders captions from open immediately, even when the follow-up read fails", async () => {
    vi.mocked(api.video.lesson).mockRejectedValue(new Error("offline"));
    const { result } = renderHook(() => usePlaylistTranscript(available));
    expect(result.current.lesson?.transcript[0].englishText).toBe("Hello world.");
    await tick();
    expect(result.current.state).toBe("ready");
    await tick(100_000);
    expect(api.video.lesson).toHaveBeenCalledTimes(1);
    expect(result.current.state).toBe("ready");
  });

  it("polls pending through partial to full captions and stops", async () => {
    vi.mocked(api.video.lesson)
      .mockResolvedValueOnce(pending)
      .mockResolvedValueOnce({ ...available, transcriptStatus: TranscriptStatus.Partial })
      .mockResolvedValue(available);
    const { result } = renderHook(() => usePlaylistTranscript(pending));
    await tick();
    expect(result.current.lesson?.transcript).toHaveLength(0);
    await tick(2500);
    expect(result.current.lesson?.transcript).toHaveLength(1);
    await tick(2500);
    expect(result.current.state).toBe("ready");
    await tick(100_000);
    expect(api.video.lesson).toHaveBeenCalledTimes(3);
  });

  it("updates same-count partial captions instead of leaving old text stuck", async () => {
    const partial = { ...available, transcriptStatus: TranscriptStatus.Partial };
    vi.mocked(api.video.lesson).mockResolvedValue({ ...partial, transcript: [{ ...partial.transcript[0], uzbekTranslation: "Salom." }] });
    const { result } = renderHook(() => usePlaylistTranscript(partial));
    await tick();
    expect(result.current.lesson?.transcript[0].uzbekTranslation).toBe("Salom.");
  });

  it("retries a failed first read instead of leaving a spinner with no polling", async () => {
    vi.mocked(api.video.lesson).mockRejectedValueOnce(new Error("offline")).mockResolvedValue(available);
    const { result } = renderHook(() => usePlaylistTranscript(pending));
    await tick();
    expect(result.current.state).toBe("delayed");
    await tick(2500);
    expect(result.current.state).toBe("ready");
  });

  it("turns repeated read failures into a retryable error and retries on request", async () => {
    vi.mocked(api.video.lesson).mockRejectedValue(new Error("offline"));
    const { result } = renderHook(() => usePlaylistTranscript(pending));
    await tick(5000);
    expect(result.current.state).toBe("error");
    await tick(100_000);
    expect(api.video.lesson).toHaveBeenCalledTimes(3);
    vi.mocked(api.video.lesson).mockResolvedValue(available);
    act(() => result.current.retry());
    await tick();
    expect(result.current.state).toBe("ready");
  });

  it("explains a slow upstream and stops endless pending after the deadline", async () => {
    vi.mocked(api.video.lesson).mockResolvedValue(pending);
    const { result } = renderHook(() => usePlaylistTranscript(pending));
    await tick(30_000);
    expect(result.current.state).toBe("delayed");
    await tick(60_000);
    expect(result.current.state).toBe("error");
    const calls = vi.mocked(api.video.lesson).mock.calls.length;
    await tick(100_000);
    expect(api.video.lesson).toHaveBeenCalledTimes(calls);
  });

  it("never overlaps a slow request and ignores its result after timeout", async () => {
    let resolve!: (value: VideoLessonDto) => void;
    vi.mocked(api.video.lesson).mockImplementation(() => new Promise(done => { resolve = done; }));
    const { result } = renderHook(() => usePlaylistTranscript(pending));
    await tick(95_000);
    expect(api.video.lesson).toHaveBeenCalledTimes(1);
    expect(result.current.state).toBe("error");
    await act(async () => { resolve(available); });
    expect(result.current.lesson?.transcript).toHaveLength(0);
  });

  it("ignores an old episode response after navigation", async () => {
    let resolve!: (value: VideoLessonDto) => void;
    vi.mocked(api.video.lesson).mockImplementationOnce(() => new Promise(done => { resolve = done; })).mockResolvedValue({ ...pending, id: "lesson-2", youTubeVideoId: "newVideo001" });
    const { result, rerender } = renderHook(({ opened }) => usePlaylistTranscript(opened), { initialProps: { opened: pending } });
    rerender({ opened: { ...pending, id: "lesson-2", youTubeVideoId: "newVideo001" } });
    await act(async () => { resolve(available); });
    expect(result.current.lesson?.id).toBe("lesson-2");
    expect(result.current.lesson?.transcript).toHaveLength(0);
  });

  it("does not erase real captions when a stale pending read arrives", async () => {
    vi.mocked(api.video.lesson).mockResolvedValue(pending);
    const { result } = renderHook(() => usePlaylistTranscript(available));
    await tick();
    expect(result.current.lesson?.transcript).toHaveLength(1);
    expect(result.current.state).toBe("ready");
  });

  it("stops without a fake pending spinner for cue-only completed captions", async () => {
    vi.mocked(api.video.lesson).mockResolvedValue({ ...available, transcript: [{ ...available.transcript[0], englishText: "[Music]" }] });
    const { result } = renderHook(() => usePlaylistTranscript(pending));
    await tick();
    expect(result.current.state).toBe("unavailable");
    expect(result.current.lesson?.transcript).toHaveLength(0);
  });

  it("cleans up polling on unmount", async () => {
    vi.mocked(api.video.lesson).mockResolvedValue(pending);
    const { unmount } = renderHook(() => usePlaylistTranscript(pending));
    await tick();
    unmount();
    await tick(100_000);
    expect(api.video.lesson).toHaveBeenCalledTimes(1);
  });
});
