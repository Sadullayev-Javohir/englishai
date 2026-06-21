import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { SpeakingItem } from "./PlacementTestPage";
import { PLACEMENT_PREVIEWS } from "./onboarding/placementPreviewItems";
import { blobToWav16kMono } from "@/lib/audio";

vi.mock("@/lib/audio", () => ({
  blobToWav16kMono: vi.fn(async () => new Uint8Array([82, 73, 70, 70])),
  bytesToBase64: vi.fn(() => "UklGRg=="),
}));

const recorders: FakeRecorder[] = [];
const stopTrack = vi.fn();
class FakeRecorder {
  state = "inactive";
  mimeType = "audio/webm";
  ondataavailable: ((event: { data: Blob }) => void) | null = null;
  onstop: (() => void) | null = null;
  onerror: (() => void) | null = null;
  constructor() { recorders.push(this); }
  start() { this.state = "recording"; }
  stop() {
    this.state = "inactive";
    this.ondataavailable?.({ data: new Blob(["test audio"]) });
    this.onstop?.();
  }
}
const stream = { getAudioTracks: () => [{ readyState: "live" }], getTracks: () => [{ stop: stopTrack }] };
const fullscreenGuardCalled = vi.fn();
function withoutFullscreenWatch<T>(action: () => Promise<T>): Promise<T> {
  fullscreenGuardCalled();
  return action();
}
const baseProps = { item: PLACEMENT_PREVIEWS.speaking, busy: false, onSubmit: vi.fn(), submitError: null, withoutFullscreenWatch };

beforeEach(() => {
  recorders.length = 0;
  vi.useFakeTimers();
  vi.stubGlobal("MediaRecorder", FakeRecorder);
  vi.stubGlobal("URL", { createObjectURL: vi.fn(() => "blob:recording"), revokeObjectURL: vi.fn() });
  Object.defineProperty(navigator, "mediaDevices", { configurable: true, value: { getUserMedia: vi.fn(async () => stream) } });
  vi.spyOn(HTMLMediaElement.prototype, "pause").mockImplementation(() => {});
  vi.mocked(blobToWav16kMono).mockResolvedValue(new Uint8Array([82, 73, 70, 70]));
});
afterEach(() => { cleanup(); vi.useRealTimers(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

describe("Pen speaking task", () => {
  it("records, previews, submits encoded audio, and preserves it after a retryable failure", async () => {
    const onSubmit = vi.fn();
    const { rerender, unmount } = render(<SpeakingItem {...baseProps} onSubmit={onSubmit} />);
    expect((screen.getByRole("button", { name: "Ovozli javobni yuborish" }) as HTMLButtonElement).disabled).toBe(true);
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Yozishni boshlash" })));
    expect(fullscreenGuardCalled).toHaveBeenCalled();
    act(() => vi.advanceTimersByTime(4000));
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Yozishni to‘xtatish" })));
    expect(stopTrack).toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Yozuvni tinglash" })).toBeTruthy();
    expect((screen.getByRole("button", { name: "Ovozli javobni yuborish" }) as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(screen.getByRole("button", { name: "Ovozli javobni yuborish" }));
    expect(onSubmit).toHaveBeenCalledWith("UklGRg==");
    rerender(<SpeakingItem {...baseProps} onSubmit={onSubmit} submitError="Yozuv saqlandi, qayta yuboring." />);
    expect(screen.getByRole("alert").textContent).toContain("Yozuv saqlandi");
    expect(screen.getByRole("button", { name: "Yozuvni tinglash" })).toBeTruthy();
    unmount();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:recording");
  });
  it("rejects recordings shorter than three seconds", async () => {
    render(<SpeakingItem {...baseProps} />);
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Yozishni boshlash" })));
    act(() => vi.advanceTimersByTime(1000));
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Yozishni to‘xtatish" })));
    expect(screen.getByRole("alert").textContent).toContain("juda qisqa");
    expect((screen.getByRole("button", { name: "Ovozli javobni yuborish" }) as HTMLButtonElement).disabled).toBe(true);
  });
  it("shows permission denial with a retryable record button", async () => {
    vi.mocked(navigator.mediaDevices.getUserMedia).mockRejectedValueOnce(new DOMException("Denied", "NotAllowedError"));
    render(<SpeakingItem {...baseProps} />);
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Yozishni boshlash" })));
    expect(screen.getByRole("alert").textContent).toContain("ruxsat berilmadi");
    expect((screen.getByRole("button", { name: "Yozishni boshlash" }) as HTMLButtonElement).disabled).toBe(false);
  });
  it("stops the microphone without converting an abandoned recording", async () => {
    const { unmount } = render(<SpeakingItem {...baseProps} />);
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Yozishni boshlash" })));
    unmount();
    expect(recorders[0].state).toBe("inactive");
    expect(stopTrack).toHaveBeenCalled();
    expect(blobToWav16kMono).not.toHaveBeenCalled();
  });
  it("prevents duplicate microphone requests and releases a late stream after unmount", async () => {
    let resolveStream!: (value: MediaStream) => void;
    vi.mocked(navigator.mediaDevices.getUserMedia).mockReturnValue(new Promise(resolve => { resolveStream = resolve; }));
    const { unmount } = render(<SpeakingItem {...baseProps} />);
    fireEvent.click(screen.getByRole("button", { name: "Yozishni boshlash" }));
    fireEvent.click(screen.getByRole("button", { name: "Yozishni boshlash" }));
    expect(navigator.mediaDevices.getUserMedia).toHaveBeenCalledOnce();
    unmount();
    await act(async () => resolveStream(stream as unknown as MediaStream));
    expect(stopTrack).toHaveBeenCalled();
  });
});
