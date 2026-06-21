import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OfflineModal } from "./OfflineModal";
import { uz } from "@/content/uz";

function setNavigatorOnline(online: boolean) {
  Object.defineProperty(window.navigator, "onLine", { value: online, configurable: true });
}

/** Drives the browser's connectivity events the way losing Wi-Fi does. */
function emit(event: "online" | "offline") {
  fireEvent(window, new Event(event));
}

beforeEach(() => {
  setNavigatorOnline(true);
  vi.stubGlobal("fetch", vi.fn());
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe("OfflineModal", () => {
  it("stays hidden while the browser reports a connection", () => {
    render(<OfflineModal />);
    expect(screen.queryByTestId("offline-modal")).toBeNull();
  });

  it("opens with the design copy when the connection drops", () => {
    render(<OfflineModal />);
    act(() => {
      setNavigatorOnline(false);
      emit("offline");
    });

    expect(screen.getByTestId("offline-modal")).toBeTruthy();
    expect(screen.getAllByText(uz.offline.title).length).toBeGreaterThan(0);
    expect(screen.getByText(uz.offline.eyebrow)).toBeTruthy();
    expect(screen.getByText(uz.offline.text)).toBeTruthy();
    expect(screen.getByText(uz.offline.hint)).toBeTruthy();
    // The reassurance that the failed tap cost the learner nothing.
    expect(screen.getByText(uz.offline.noticeTitle, { exact: false })).toBeTruthy();
  });

  it("opens immediately when the app boots offline", () => {
    setNavigatorOnline(false);
    render(<OfflineModal />);
    expect(screen.getByTestId("offline-modal")).toBeTruthy();
  });

  it("reports that the network is still unreachable when the retry probe fails", async () => {
    const fetchMock = vi.fn().mockRejectedValue(new TypeError("Failed to fetch"));
    vi.stubGlobal("fetch", fetchMock);

    render(<OfflineModal />);
    act(() => {
      setNavigatorOnline(false);
      emit("offline");
    });

    fireEvent.click(screen.getByRole("button", { name: uz.offline.retry }));
    expect(screen.getByText(uz.offline.checking.title)).toBeTruthy();

    await waitFor(() => expect(screen.getByText(uz.offline.stillOffline.title)).toBeTruthy());
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId("offline-modal")).toBeTruthy();
  });

  it("confirms the reconnection and then closes when the retry probe succeeds", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ status: 200, type: "basic" }));

    render(<OfflineModal />);
    act(() => {
      setNavigatorOnline(false);
      emit("offline");
    });

    fireEvent.click(screen.getByRole("button", { name: uz.offline.retry }));
    await waitFor(() => expect(screen.getByText(uz.offline.reconnected.title)).toBeTruthy());
    await waitFor(() => expect(screen.queryByTestId("offline-modal")).toBeNull(), { timeout: 3000 });
  });

  it("closes as soon as the browser reports the connection is back", async () => {
    render(<OfflineModal />);
    act(() => {
      setNavigatorOnline(false);
      emit("offline");
    });
    expect(screen.getByTestId("offline-modal")).toBeTruthy();

    act(() => {
      setNavigatorOnline(true);
      emit("online");
    });
    await waitFor(() => expect(screen.queryByTestId("offline-modal")).toBeNull());
  });

  it("hides on 'Hozir emas' but returns on the next outage", async () => {
    render(<OfflineModal />);
    act(() => {
      setNavigatorOnline(false);
      emit("offline");
    });

    fireEvent.click(screen.getByRole("button", { name: uz.offline.dismiss }));
    await waitFor(() => expect(screen.queryByTestId("offline-modal")).toBeNull());

    act(() => {
      setNavigatorOnline(true);
      emit("online");
    });
    act(() => {
      setNavigatorOnline(false);
      emit("offline");
    });
    expect(screen.getByTestId("offline-modal")).toBeTruthy();
  });
});
