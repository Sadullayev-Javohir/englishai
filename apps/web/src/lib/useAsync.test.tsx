import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/api/client";
import { useAsync } from "./useAsync";

describe("useAsync retries", () => {
  beforeEach(() => vi.useFakeTimers());

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it("clears a pending retry when unmounted", async () => {
    const loader = vi.fn().mockRejectedValue(new TypeError("Failed to fetch"));
    const clearTimeoutSpy = vi.spyOn(window, "clearTimeout");
    const { unmount } = renderHook(() => useAsync(loader));

    await act(async () => Promise.resolve());
    expect(loader).toHaveBeenCalledTimes(1);

    unmount();
    expect(clearTimeoutSpy).toHaveBeenCalled();

    await act(async () => vi.runAllTimersAsync());
    expect(loader).toHaveBeenCalledTimes(1);
  });

  it("does not retry request timeouts", async () => {
    const timeoutError = new ApiError(408, "timeout");
    const loader = vi.fn().mockRejectedValue(timeoutError);
    const { result } = renderHook(() => useAsync(loader));

    await act(async () => Promise.resolve());
    expect(result.current.loading).toBe(false);
    await act(async () => vi.runAllTimersAsync());

    expect(loader).toHaveBeenCalledTimes(1);
    expect(result.current.error).toBe(timeoutError);
  });

  it("retries a persistent server failure only once", async () => {
    const serverError = new ApiError(503, "unavailable");
    const loader = vi.fn().mockRejectedValue(serverError);
    const { result } = renderHook(() => useAsync(loader));

    await act(async () => Promise.resolve());
    expect(loader).toHaveBeenCalledTimes(1);
    await act(async () => vi.runAllTimersAsync());

    expect(loader).toHaveBeenCalledTimes(2);
    expect(result.current.loading).toBe(false);
    expect(result.current.error).toBe(serverError);
  });
});
