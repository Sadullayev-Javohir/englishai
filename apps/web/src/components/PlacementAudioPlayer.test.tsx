import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AssessmentAudioPlayer, formatAudioTime, PlacementAudioPlayer } from "./PlacementAudioPlayer";

beforeEach(() => {
  vi.spyOn(HTMLMediaElement.prototype, "pause").mockImplementation(function (this: HTMLMediaElement) {
    if (this.paused) return;
    Object.defineProperty(this, "paused", { configurable: true, value: true });
    fireEvent.pause(this);
  });
  vi.spyOn(HTMLMediaElement.prototype, "play").mockImplementation(async function (this: HTMLMediaElement) {
    Object.defineProperty(this, "paused", { configurable: true, value: false });
    fireEvent.playing(this);
  });
  vi.spyOn(HTMLMediaElement.prototype, "load").mockImplementation(() => {});
});
afterEach(() => { cleanup(); vi.restoreAllMocks(); });

describe("shared placement audio player", () => {
  it("pauses and resumes at the same position; only restarts an ended clip", async () => {
    const { container } = render(<AssessmentAudioPlayer src="/test.wav" />);
    const audio = container.querySelector("audio")!;
    Object.defineProperty(audio, "duration", { configurable: true, value: 20 });
    fireEvent.loadedMetadata(audio);
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Tinglash" })));
    audio.currentTime = 7;
    fireEvent.timeUpdate(audio);
    fireEvent.click(screen.getByRole("button", { name: "To'xtatib turish" }));
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Tinglashni davom ettirish" })));
    expect(audio.currentTime).toBe(7);
    expect(screen.getByRole("progressbar").getAttribute("value")).toBe("7");
    Object.defineProperty(audio, "paused", { configurable: true, value: true });
    Object.defineProperty(audio, "ended", { configurable: true, value: true });
    fireEvent.ended(audio);
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Qayta tinglash" })));
    expect(audio.currentTime).toBe(0);
  });
  it("handles play rejection and provides an explicit retry", async () => {
    vi.mocked(HTMLMediaElement.prototype.play).mockRejectedValueOnce(new Error("network"));
    render(<AssessmentAudioPlayer src="/test.wav" />);
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Tinglash" })));
    expect(screen.getByRole("alert").textContent).toContain("yuklab bo'lmadi");
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" })));
    expect(HTMLMediaElement.prototype.load).toHaveBeenCalledOnce();
    expect(screen.getByRole("button", { name: "To'xtatib turish" })).toBeTruthy();
  });
  it("resets the transport and pauses the old clip when a question changes", () => {
    const { container, rerender, unmount } = render(<PlacementAudioPlayer questionId="first" />);
    const first = container.querySelector("audio")!;
    first.currentTime = 4;
    fireEvent.timeUpdate(first);
    rerender(<PlacementAudioPlayer questionId="second" />);
    expect(container.querySelector("audio")!.src).toContain("/audio/second");
    expect(screen.getByRole("progressbar").getAttribute("value")).toBe("0");
    expect(HTMLMediaElement.prototype.pause).toHaveBeenCalled();
    unmount();
  });
  it("blocks playback during submission and does not fabricate a duration", () => {
    render(<AssessmentAudioPlayer src="/test.wav" recording disabled />);
    expect((screen.getByRole("button", { name: "Yozuvni tinglash" }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByText("—:—")).toBeTruthy();
  });
  it("uses the recorder's measured duration when WebM metadata is infinite", () => {
    const { container } = render(<AssessmentAudioPlayer src="/recording.webm" recording durationHint={4} />);
    Object.defineProperty(container.querySelector("audio"), "duration", { configurable: true, value: Infinity });
    fireEvent.loadedMetadata(container.querySelector("audio")!);
    expect(screen.getByRole("progressbar").getAttribute("max")).toBe("4");
    expect(screen.getByText("0:04")).toBeTruthy();
  });
  it.each([[NaN, "0:00"], [Infinity, "0:00"], [-1, "0:00"], [65, "1:05"]])("formats %s safely", (value, expected) => {
    expect(formatAudioTime(value as number)).toBe(expected);
  });
});
