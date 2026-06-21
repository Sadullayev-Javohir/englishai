import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/api/client";
import { uz } from "@/content/uz";
import { useVideoExplainConversation } from "./useVideoExplainConversation";

const { explainStream } = vi.hoisted(() => ({ explainStream: vi.fn() }));

vi.mock("@/api/client", () => ({
  ApiError: class ApiError extends Error {
    constructor(public status: number, message: string, public body?: unknown) {
      super(message);
    }
  },
  api: { video: { explainStream } },
}));

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((done) => { resolve = done; });
  return { promise, resolve };
}

describe("useVideoExplainConversation", () => {
  beforeEach(() => {
    localStorage.clear();
    explainStream.mockReset();
    explainStream.mockImplementation(async (_id, _focus, _question, _history, onChunk) => {
      onChunk("Javob");
      return { replyUz: "Javob" };
    });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("keeps separate drafts and replies for each selected sentence", async () => {
    const { result, rerender } = renderHook(
      ({ sentence }) => useVideoExplainConversation("lesson-1", () => `[${sentence}] Line`, sentence),
      { initialProps: { sentence: "6" } },
    );
    await act(async () => { await result.current.send("First sentence question"); });
    act(() => result.current.setInput("First sentence draft"));
    rerender({ sentence: "14" });
    expect(result.current.messages).toEqual([]);
    expect(result.current.input).toBe("");
    act(() => result.current.setInput("Second sentence draft"));
    rerender({ sentence: "6" });
    expect(result.current.messages.map(message => message.text)).toEqual(["First sentence question", "Javob"]);
    expect(result.current.input).toBe("First sentence draft");
    rerender({ sentence: "14" });
    expect(result.current.messages).toEqual([]);
    expect(result.current.input).toBe("Second sentence draft");
  });

  it("ignores old streaming chunks after switching away and back to a sentence", async () => {
    const response = deferred<{ replyUz: string }>();
    let stream!: (chunk: string) => void;
    explainStream.mockImplementation((_id, _focus, _question, _history, onChunk) => {
      stream = onChunk;
      return response.promise;
    });
    const { result, rerender } = renderHook(
      ({ sentence }) => useVideoExplainConversation("lesson-1", () => "Line", sentence),
      { initialProps: { sentence: "6" } },
    );
    let sending!: Promise<void>;
    act(() => { sending = result.current.send("Question"); });
    rerender({ sentence: "14" });
    act(() => stream("Wrong sentence answer"));
    expect(result.current.messages).toEqual([]);
    rerender({ sentence: "6" });
    await act(async () => {
      stream("Stale response");
      response.resolve({ replyUz: "Stale response" });
      await sending;
    });
    expect(result.current.messages.every(message => message.role === "user")).toBe(true);
    expect(result.current.sending).toBe(false);
  });

  it("restores messages and draft for one day", async () => {
    const now = new Date("2026-08-08T09:00:00Z");
    vi.setSystemTime(now);
    localStorage.setItem("englishai:video-explain:lesson-1", JSON.stringify({
      expiresAt: now.getTime() + 60_000,
      messages: [{ role: "user", text: "Oldingi savol" }, { role: "assistant", text: "Oldingi javob" }],
      input: "Saqlangan draft",
    }));

    const { result } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Line"));
    await act(async () => undefined);

    expect(result.current.messages).toEqual([
      { role: "user", text: "Oldingi savol" },
      { role: "assistant", text: "Oldingi javob" },
    ]);
    expect(result.current.input).toBe("Saqlangan draft");
  });

  it("removes conversation after one day", async () => {
    const now = new Date("2026-08-08T09:00:00Z");
    vi.setSystemTime(now);
    localStorage.setItem("englishai:video-explain:lesson-1", JSON.stringify({
      expiresAt: now.getTime() - 1,
      messages: [{ role: "user", text: "Eskirgan savol" }],
      input: "Eskirgan draft",
    }));

    const { result } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Line"));
    await act(async () => undefined);

    expect(result.current.messages).toEqual([]);
    expect(result.current.input).toBe("");
    expect(localStorage.getItem("englishai:video-explain:lesson-1")).toBeNull();
  });

  it("stores one user/reply stream and calls the AI once", async () => {
    vi.useFakeTimers();
    explainStream.mockImplementation(async (_id, _focus, _question, _history, onChunk) => {
      onChunk("Bir xil javob");
      return { replyUz: "Bir xil javob" };
    });
    const { result } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Current line"));

    act(() => result.current.setInput("Savol"));
    let sending!: Promise<void>;
    act(() => { sending = result.current.send(); });
    await act(async () => { await vi.runAllTimersAsync(); await sending; });

    expect(explainStream).toHaveBeenCalledTimes(1);
    expect(explainStream.mock.calls[0]?.slice(0, 4)).toEqual(["lesson-1", "Current line", "Savol", []]);
    expect(result.current.messages).toEqual([
      { role: "user", text: "Savol" },
      { role: "assistant", text: "Bir xil javob", streaming: false },
    ]);
    vi.useRealTimers();
  });

  it("auto-sends an explicit transcript question without waiting for draft state", async () => {
    vi.useFakeTimers();
    const { result } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Current transcript"));

    let sending!: Promise<void>;
    act(() => { sending = result.current.send('"Current transcript" gapini tushuntirib bering.'); });
    await act(async () => { await vi.runAllTimersAsync(); await sending; });

    expect(explainStream).toHaveBeenCalledTimes(1);
    expect(explainStream.mock.calls[0]?.slice(0, 4)).toEqual([
      "lesson-1",
      "Current transcript",
      '"Current transcript" gapini tushuntirib bering.',
      [],
    ]);
    expect(result.current.input).toBe("");
    expect(result.current.messages[0]?.text).toContain("Current transcript");
    vi.useRealTimers();
  });

  it("blocks a second send while the first request is in flight", async () => {
    const request = deferred<{ replyUz: string }>();
    explainStream.mockReturnValue(request.promise);
    const { result } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Line"));

    act(() => result.current.setInput("Savol"));
    let first!: Promise<void>;
    act(() => {
      first = result.current.send("Birinchi savol");
      void result.current.send("Ikkinchi savol");
    });

    expect(explainStream).toHaveBeenCalledTimes(1);
    expect(result.current.sending).toBe(true);
    expect(result.current.messages).toEqual([
      { role: "user", text: "Birinchi savol" },
    ]);

    await act(async () => {
      request.resolve({ replyUz: "Javob" });
      await first;
    });
    expect(result.current.sending).toBe(false);
    expect(result.current.messages.at(-1)).toEqual({ role: "assistant", text: "Javob", streaming: false });
  });

  it("keeps the shared draft when a presentation closes and reopens", () => {
    const { result, unmount } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Line"));
    act(() => {
      result.current.setInput("Saqlangan draft");
      result.current.focusInput();
    });
    expect(result.current.input).toBe("Saqlangan draft");
    expect(result.current.focusToken).toBe(1);
    unmount();
  });

  it("resets conversation when the lesson changes", async () => {
    vi.useFakeTimers();
    const { result, rerender } = renderHook(
      ({ lessonId }) => useVideoExplainConversation(lessonId, () => "Line"),
      { initialProps: { lessonId: "lesson-1" } },
    );

    act(() => result.current.setInput("Savol"));
    let sending!: Promise<void>;
    act(() => { sending = result.current.send(); });
    await act(async () => { await vi.runAllTimersAsync(); await sending; });
    expect(result.current.messages).toHaveLength(2);

    rerender({ lessonId: "lesson-2" });
    await act(async () => { await vi.runAllTimersAsync(); });
    expect(result.current.messages).toEqual([]);
    expect(result.current.input).toBe("");
    vi.useRealTimers();
  });

  it("publishes an honest shared unavailable turn", async () => {
    explainStream.mockResolvedValue({ replyUz: null });
    const { result } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Line"));
    act(() => result.current.setInput("Savol"));
    await act(async () => { await result.current.send(); });
    expect(result.current.messages.at(-1)).toMatchObject({ role: "assistant", failed: true });
  });

  it.each([
    [0, uz.videoChat.networkError],
    [401, uz.videoChat.unauthorized],
    [429, uz.videoChat.rateLimited],
    [422, uz.videoChat.invalidQuestion],
    [503, uz.videoChat.unavailable],
  ])("shows the correct failure message for status %s", async (status, message) => {
    explainStream.mockRejectedValue(new ApiError(status, "Failed"));
    const { result } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Line"));

    await act(async () => { await result.current.send("Savol"); });

    expect(result.current.messages.at(-1)).toEqual({ role: "assistant", text: message, failed: true });
  });

  it("does not resend a local failed fallback as assistant history", async () => {
    vi.useFakeTimers();
    explainStream.mockResolvedValueOnce({ replyUz: null }).mockImplementationOnce(async (_id, _focus, _question, _history, onChunk) => {
      onChunk("Javob");
      return { replyUz: "Javob" };
    });
    const { result } = renderHook(() => useVideoExplainConversation("lesson-1", () => "Line"));

    act(() => result.current.setInput("Birinchi"));
    await act(async () => { await result.current.send(); });
    act(() => result.current.setInput("Ikkinchi"));
    let sending!: Promise<void>;
    act(() => { sending = result.current.send(); });
    await act(async () => { await vi.runAllTimersAsync(); await sending; });

    expect(explainStream.mock.calls.at(-1)?.slice(0, 4)).toEqual([
      "lesson-1",
      "Line",
      "Ikkinchi",
      [{ role: "user", text: "Birinchi" }],
    ]);
    vi.useRealTimers();
  });
});
