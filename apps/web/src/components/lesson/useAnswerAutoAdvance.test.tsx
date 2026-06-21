import { act, renderHook } from "@testing-library/react";
import { StrictMode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useAnswerAutoAdvance } from "./useAnswerAutoAdvance";

describe("useAnswerAutoAdvance", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.clearAllTimers();
    vi.useRealTimers();
  });

  it("advances once at 3000 ms, not at 2999 ms", () => {
    const onAdvance = vi.fn();
    const { result } = renderHook(
      () => useAnswerAutoAdvance({ enabled: true, onAdvance, resetKey: "question-1" }),
      { wrapper: StrictMode },
    );

    expect(result.current.remainingSeconds).toBe(3);
    act(() => { vi.advanceTimersByTime(2999); });
    expect(onAdvance).not.toHaveBeenCalled();
    expect(result.current.remainingSeconds).toBe(1);

    act(() => { vi.advanceTimersByTime(1); });
    expect(onAdvance).toHaveBeenCalledTimes(1);
    expect(result.current.active).toBe(false);
  });

  it("manual advance cancels the timer and stays single-shot under rapid clicks", () => {
    const onAdvance = vi.fn();
    const { result } = renderHook(() => useAnswerAutoAdvance({ enabled: true, onAdvance }));

    act(() => {
      result.current.advance();
      result.current.advance();
      vi.advanceTimersByTime(3000);
    });

    expect(onAdvance).toHaveBeenCalledTimes(1);
  });

  it("keeps the timer/manual boundary race single-shot", () => {
    const onAdvance = vi.fn();
    const { result } = renderHook(() => useAnswerAutoAdvance({ enabled: true, onAdvance }));

    act(() => {
      vi.advanceTimersByTime(3000);
      result.current.advance();
    });

    expect(onAdvance).toHaveBeenCalledTimes(1);
  });

  it("does not schedule while disabled and cancels when disabled", () => {
    const onAdvance = vi.fn();
    const { result, rerender } = renderHook(
      ({ enabled }) => useAnswerAutoAdvance({ enabled, onAdvance }),
      { initialProps: { enabled: false } },
    );

    expect(result.current.active).toBe(false);
    act(() => {
      result.current.advance();
      vi.advanceTimersByTime(3000);
    });
    expect(onAdvance).not.toHaveBeenCalled();

    rerender({ enabled: true });
    act(() => { vi.advanceTimersByTime(1000); });
    rerender({ enabled: false });
    act(() => { vi.advanceTimersByTime(3000); });
    expect(onAdvance).not.toHaveBeenCalled();
  });

  it("restarts safely when the reset key changes", () => {
    const onAdvance = vi.fn();
    const { rerender } = renderHook(
      ({ resetKey }) => useAnswerAutoAdvance({ enabled: true, onAdvance, resetKey }),
      { initialProps: { resetKey: "question-1" } },
    );

    act(() => { vi.advanceTimersByTime(2000); });
    rerender({ resetKey: "question-2" });
    act(() => { vi.advanceTimersByTime(2999); });
    expect(onAdvance).not.toHaveBeenCalled();
    act(() => { vi.advanceTimersByTime(1); });
    expect(onAdvance).toHaveBeenCalledTimes(1);
  });

  it("cleans up on unmount", () => {
    const onAdvance = vi.fn();
    const { unmount } = renderHook(() => useAnswerAutoAdvance({ enabled: true, onAdvance }));

    unmount();
    act(() => { vi.advanceTimersByTime(3000); });
    expect(onAdvance).not.toHaveBeenCalled();
  });
});
