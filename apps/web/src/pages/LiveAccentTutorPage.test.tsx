import { cleanup, render, screen, waitFor, act } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LiveAccentTutorPage } from "./LiveAccentTutorPage";
import type { VoiceLiveCallbacks } from "@/api/voiceLiveSession";

// Capture the callbacks the page wires so tests can drive server events, and record lifecycle.
let lastCallbacks: VoiceLiveCallbacks | null = null;
const startSpy = vi.fn().mockResolvedValue(undefined);
const stopSpy = vi.fn();

vi.mock("@/api/voiceLiveSession", () => ({
  VoiceLiveSession: class {
    constructor(_tutorId: string, callbacks: VoiceLiveCallbacks) {
      lastCallbacks = callbacks;
    }
    start = startSpy;
    stop = stopSpy;
    setMuted = vi.fn();
  },
}));

vi.mock("@/lib/microphoneCapture", () => ({ isMicrophonePermissionDenied: () => false }));

function renderPage(tutorId = "american") {
  return render(
    <MemoryRouter initialEntries={[`/app/speaking/live-tutor/${tutorId}`]}>
      <Routes>
        <Route path="/app/speaking/live-tutor/:tutorId" element={<LiveAccentTutorPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

afterEach(() => {
  cleanup();
  lastCallbacks = null;
  startSpy.mockClear();
  stopSpy.mockClear();
});

describe("LiveAccentTutorPage (Voice Live)", () => {
  it("starts a realtime session on entry", async () => {
    renderPage();
    await waitFor(() => expect(startSpy).toHaveBeenCalledTimes(1));
    expect(screen.getByTestId("tutor-identity-badge").textContent).toBe("US");
  });

  it("renders finalized learner and tutor transcripts in the feed", async () => {
    renderPage();
    await waitFor(() => expect(lastCallbacks).not.toBeNull());

    act(() => {
      lastCallbacks!.onStatus?.("listening");
      lastCallbacks!.onUserTranscript?.("I went to the park.", true);
      lastCallbacks!.onTutorTranscript?.("Nice! What did you do there?", true);
    });

    expect(await screen.findByText("I went to the park.")).toBeTruthy();
    expect(await screen.findByText("Nice! What did you do there?")).toBeTruthy();
  });

  it("does not add a feed message for a non-final (partial) transcript", async () => {
    renderPage();
    await waitFor(() => expect(lastCallbacks).not.toBeNull());

    act(() => lastCallbacks!.onUserTranscript?.("I went to", false));

    expect(screen.queryByText("I went to", { selector: ".live-message p" })).toBeNull();
  });

  it("shows a retry card on a fatal connection error", async () => {
    renderPage();
    await waitFor(() => expect(lastCallbacks).not.toBeNull());

    act(() => lastCallbacks!.onError?.("Voice connection closed.", true));

    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain("Voice connection closed.");
    expect(screen.getByRole("button", { name: "Qayta urinish" })).toBeTruthy();
  });

  it("stops the session on unmount", async () => {
    const { unmount } = renderPage();
    await waitFor(() => expect(startSpy).toHaveBeenCalled());
    unmount();
    expect(stopSpy).toHaveBeenCalled();
  });
});
