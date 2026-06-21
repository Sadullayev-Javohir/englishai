import { act, cleanup, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useVocabularyVoice } from "./useVocabularyVoice";

class FakeAudio {
  static instances: FakeAudio[] = [];
  src = "";
  playbackRate = 1;
  onplaying: (() => void) | null = null;
  onended: (() => void) | null = null;
  onpause: (() => void) | null = null;
  onerror: (() => void) | null = null;
  play = vi.fn(() => Promise.resolve());
  pause = vi.fn(() => this.onpause?.());
  constructor() { FakeAudio.instances.push(this); }
}
class FakeUtterance {
  onstart: (() => void) | null = null;
  onend: (() => void) | null = null;
  onerror: (() => void) | null = null;
  constructor(public text: string) {}
}
const synth = {
  getVoices: vi.fn(() => [{ lang: "en-GB" }]),
  speak: vi.fn(), resume: vi.fn(), cancel: vi.fn(),
  addEventListener: vi.fn(), removeEventListener: vi.fn(),
};
beforeEach(() => {
  vi.useFakeTimers();
  FakeAudio.instances = [];
  vi.stubGlobal("Audio", FakeAudio);
  vi.stubGlobal("SpeechSynthesisUtterance", FakeUtterance);
  vi.stubGlobal("speechSynthesis", synth);
  synth.getVoices.mockReturnValue([{ lang: "en-GB" }]);
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); vi.clearAllMocks(); vi.useRealTimers(); });

describe("vocabulary voice playback lifecycle", () => {
  it("only animates during actual media playback, including clips longer than 2.5 seconds", () => {
    const { result } = renderHook(() => useVocabularyVoice("mother"));
    act(() => result.current.play("AA==", .6));
    const audio = FakeAudio.instances[0];
    expect(result.current.state).toBe("loading");
    expect(audio.playbackRate).toBe(.6);
    act(() => audio.onplaying?.());
    expect(result.current.state).toBe("playing");
    act(() => vi.advanceTimersByTime(8000));
    expect(result.current.state).toBe("playing");
    act(() => audio.onended?.());
    expect(result.current.state).toBe("idle");
  });
  it("stops media and ignores stale events after a new tap or unmount", () => {
    const { result, unmount } = renderHook(() => useVocabularyVoice("mother"));
    act(() => result.current.play("AA=="));
    const old = FakeAudio.instances[0], staleStart = old.onplaying;
    act(() => result.current.play("BB=="));
    expect(old.pause).toHaveBeenCalled();
    act(() => staleStart?.());
    expect(result.current.state).toBe("loading");
    const current = FakeAudio.instances[1];
    act(() => current.onplaying?.());
    act(() => result.current.stop());
    expect(result.current.state).toBe("idle");
    expect(current.onplaying).toBeNull();
    unmount();
    expect(current.onended).toBeNull();
  });
  it("uses utterance start/end events for fallback speech and cancels it before recording", () => {
    const { result } = renderHook(() => useVocabularyVoice("patient"));
    act(() => result.current.play(null));
    act(() => vi.advanceTimersByTime(0));
    const utterance = synth.speak.mock.calls[0][0] as FakeUtterance;
    expect(utterance.text).toBe("patient");
    expect(result.current.state).toBe("loading");
    act(() => utterance.onstart?.());
    expect(result.current.state).toBe("playing");
    act(() => utterance.onend?.());
    expect(result.current.state).toBe("idle");
    act(() => result.current.stop());
    expect(synth.cancel).toHaveBeenCalled();
  });
  it("does not queue delayed speech after leaving the card", () => {
    synth.getVoices.mockReturnValue([]);
    const { result, unmount } = renderHook(() => useVocabularyVoice("mother"));
    act(() => result.current.play(null));
    const delayed = synth.addEventListener.mock.calls[0][1] as () => void;
    unmount();
    act(() => { delayed(); vi.advanceTimersByTime(1000); });
    expect(synth.speak).not.toHaveBeenCalled();
    expect(synth.removeEventListener).toHaveBeenCalledWith("voiceschanged", delayed);
  });
  it("fails honestly if neither media nor a speech engine can play", () => {
    vi.stubGlobal("speechSynthesis", undefined);
    const { result } = renderHook(() => useVocabularyVoice("mother"));
    act(() => result.current.play("invalid"));
    act(() => FakeAudio.instances[0].onerror?.());
    expect(result.current.state).toBe("error");
  });
  it("clears a stuck loading indicator and releases resources", () => {
    const { result } = renderHook(() => useVocabularyVoice("mother"));
    act(() => result.current.play("AA=="));
    act(() => vi.advanceTimersByTime(12_000));
    expect(result.current.state).toBe("error");
    expect(FakeAudio.instances[0].pause).toHaveBeenCalled();
  });
});
