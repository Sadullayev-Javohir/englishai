import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ListeningAudioPlayer } from "./ListeningAudioPlayer";

vi.mock("@/api/client", () => ({ api: { listening: { audioUrl: () => "/fixture.wav" } } }));
afterEach(() => { cleanup(); vi.restoreAllMocks(); });

function player() {
  const onListened = vi.fn();
  const onPlaybackError = vi.fn();
  const view = render(<ListeningAudioPlayer topicId="topic" title="Library" onListened={onListened} onPlaybackError={onPlaybackError} />);
  const audio = view.container.querySelector("audio")!;
  const play = vi.spyOn(audio, "play").mockResolvedValue(undefined);
  const pause = vi.spyOn(audio, "pause").mockImplementation(() => undefined);
  return { ...view, audio, play, pause, onListened, onPlaybackError };
}

describe("ListeningAudioPlayer", () => {
  it("unlocks the lesson only after actual playback, never metadata or seeking", () => {
    const { audio, onListened } = player();
    expect(screen.getByRole("button", { name: "Tinglash" }).hasAttribute("disabled")).toBe(true);
    Object.defineProperty(audio, "duration", { value: 42, configurable: true });
    fireEvent.loadedMetadata(audio);
    fireEvent.canPlay(audio);
    fireEvent.change(screen.getByRole("slider"), { target: { value: "10" } });
    expect(audio.currentTime).toBe(10);
    expect(onListened).not.toHaveBeenCalled();
    fireEvent.play(audio);
    expect(onListened).toHaveBeenCalledOnce();
  });

  it("shows real duration and elapsed time and supports speed changes", () => {
    const { audio } = player();
    Object.defineProperty(audio, "duration", { value: 42, configurable: true });
    fireEvent.loadedMetadata(audio);
    audio.currentTime = 8;
    fireEvent.timeUpdate(audio);
    expect(screen.getByText("0:08 / 0:42")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Tinglash tezligi 1x" }));
    expect(audio.playbackRate).toBe(0.75);
    fireEvent.click(screen.getByRole("button", { name: "Tinglash tezligi 0.75x" }));
    expect(audio.playbackRate).toBe(1.25);
  });

  it("replays from zero and releases playback when the stage unmounts", async () => {
    const { audio, play, pause, unmount } = player();
    fireEvent.canPlay(audio);
    audio.currentTime = 15;
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Qayta tinglash" })); });
    expect(audio.currentTime).toBe(0);
    expect(play).toHaveBeenCalledOnce();
    unmount();
    expect(pause).toHaveBeenCalledOnce();
  });

  it("reports errors and retains an actionable replay retry", async () => {
    const { audio, play, onPlaybackError } = player();
    const load = vi.spyOn(audio, "load").mockImplementation(() => undefined);
    fireEvent.error(audio);
    expect(onPlaybackError).toHaveBeenCalledOnce();
    expect(screen.getByRole("alert")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Tinglash" }).hasAttribute("disabled")).toBe(true);
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Qayta tinglash" })); });
    expect(load).toHaveBeenCalledOnce();
    expect(play).toHaveBeenCalledOnce();
  });

  it("does not present failed play requests as successful listening", async () => {
    const { audio, play, onListened, onPlaybackError } = player();
    fireEvent.canPlay(audio);
    play.mockRejectedValueOnce(new Error("playback unavailable"));
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Tinglash" })); });
    expect(onListened).not.toHaveBeenCalled();
    expect(onPlaybackError).toHaveBeenCalledOnce();
    expect(screen.getByRole("alert")).toBeTruthy();
  });
});
