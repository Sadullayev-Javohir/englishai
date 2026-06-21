import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { fitIframeToContainer, useYouTubePlayer } from "./useYouTubePlayer";

interface MockPlayer {
  playVideo: ReturnType<typeof vi.fn>;
  pauseVideo: ReturnType<typeof vi.fn>;
  getPlayerState: ReturnType<typeof vi.fn>;
  getIframe: ReturnType<typeof vi.fn>;
  setVolume: ReturnType<typeof vi.fn>;
  unloadModule: ReturnType<typeof vi.fn>;
  getPlaybackRate: ReturnType<typeof vi.fn>;
  getVolume: ReturnType<typeof vi.fn>;
  isMuted: ReturnType<typeof vi.fn>;
  seekTo: ReturnType<typeof vi.fn>;
  destroy: ReturnType<typeof vi.fn>;
}

describe("useYouTubePlayer playback readiness", () => {
  let options: {
    width?: number | string;
    height?: number | string;
    playerVars?: Record<string, string | number>;
    events?: {
      onReady?: (event: { target: MockPlayer }) => void;
      onStateChange?: (event: { target: MockPlayer; data: number }) => void;
    };
  } | undefined;
  let player: MockPlayer;
  let iframe: HTMLIFrameElement;
  let wrapper: HTMLDivElement;

  beforeEach(() => {
    vi.useFakeTimers();
    options = undefined;
    wrapper = document.createElement("div");
    const target = document.createElement("div");
    target.id = "player";
    wrapper.appendChild(target);
    document.body.appendChild(wrapper);
    Object.defineProperties(wrapper, {
      clientWidth: { configurable: true, value: 286 },
      clientHeight: { configurable: true, value: 161 },
    });
    iframe = document.createElement("iframe");
    player = {
      playVideo: vi.fn(),
      pauseVideo: vi.fn(),
      getPlayerState: vi.fn(() => -1),
      getIframe: vi.fn(() => iframe),
      setVolume: vi.fn(),
      unloadModule: vi.fn(),
      getPlaybackRate: vi.fn(() => 1),
      getVolume: vi.fn(() => 80),
      isMuted: vi.fn(() => false),
      seekTo: vi.fn(),
      destroy: vi.fn(),
    };
    window.YT = {
      Player: vi.fn((_target: string | HTMLElement, nextOptions: typeof options) => {
        options = nextOptions;
        return player;
      }) as never,
    };
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
    delete window.YT;
    wrapper.remove();
    vi.restoreAllMocks();
  });

  it("replays the first play request once the iframe player is ready", async () => {
    const { result } = renderHook(() => useYouTubePlayer("abcdefghijk", "player"));
    await act(async () => Promise.resolve());

    act(() => result.current.togglePlay());
    expect(player.playVideo).not.toHaveBeenCalled();

    act(() => options?.events?.onReady?.({ target: player }));
    expect(player.playVideo).toHaveBeenCalledTimes(1);
  });

  it("cancels a queued play when the user toggles again before ready", async () => {
    const { result } = renderHook(() => useYouTubePlayer("abcdefghijk", "player"));
    await act(async () => Promise.resolve());

    act(() => {
      result.current.togglePlay();
      result.current.togglePlay();
    });
    act(() => options?.events?.onReady?.({ target: player }));

    expect(player.playVideo).not.toHaveBeenCalled();
    expect(player.pauseVideo).not.toHaveBeenCalled();
  });

  it("forces the injected YouTube iframe to follow the responsive card width", async () => {
    renderHook(() => useYouTubePlayer("abcdefghijk", "player"));
    await act(async () => Promise.resolve());

    expect(options?.width).toBe("100%");
    expect(options?.height).toBe("100%");

    act(() => options?.events?.onReady?.({ target: player }));

    expect(iframe.width).toBe("286");
    expect(iframe.height).toBe("161");
    expect(iframe.style.position).toBe("absolute");
    expect(iframe.style.inset).toBe("0");
    expect(iframe.style.boxSizing).toBe("border-box");
    expect(iframe.style.width).toBe("100%");
    expect(iframe.style.maxWidth).toBe("100%");
    expect(iframe.style.height).toBe("100%");
    expect(iframe.style.maxHeight).toBe("100%");
  });

  it("does not build a player before the video id is known", async () => {
    // On the /video/:guid/play route the lesson loads asynchronously, so the first render has no
    // id and no mount node. Building a player there left a broken instance behind.
    renderHook(() => useYouTubePlayer("", "player"));
    await act(async () => Promise.resolve());

    expect(window.YT?.Player).not.toHaveBeenCalled();
  });

  it("uses YouTube's native controls, keyboard, fullscreen and untouched caption preferences", async () => {
    renderHook(() => useYouTubePlayer("abcdefghijk", "player"));
    await act(async () => Promise.resolve());
    expect(options?.playerVars).toEqual({
      controls: 1, disablekb: 0, fs: 1, playsinline: 1, origin: window.location.origin,
    });
    act(() => {
      options?.events?.onReady?.({ target: player });
      options?.events?.onStateChange?.({ target: player, data: 1 });
      vi.advanceTimersByTime(2500);
    });
    expect(player.unloadModule).not.toHaveBeenCalled();
    expect(player.setVolume).not.toHaveBeenCalled();
    expect(iframe.allowFullscreen).toBe(true);
    expect(iframe.title).toBe("YouTube video pleyeri");
  });

  it("synchronizes settings changed inside the native player with the learning tools", async () => {
    const { result } = renderHook(() => useYouTubePlayer("abcdefghijk", "player"));
    await act(async () => Promise.resolve());
    act(() => options?.events?.onReady?.({ target: player }));
    player.getPlaybackRate.mockReturnValue(1.5);
    player.getVolume.mockReturnValue(35);
    player.isMuted.mockReturnValue(true);
    act(() => vi.advanceTimersByTime(100));
    expect(result.current.playbackRate).toBe(1.5);
    expect(result.current.volume).toBe(35);
    expect(result.current.muted).toBe(true);
  });

  it("reports a native YouTube end event without waiting for the polling tick", async () => {
    const { result } = renderHook(() => useYouTubePlayer("abcdefghijk", "player"));
    await act(async () => Promise.resolve());
    act(() => options?.events?.onReady?.({ target: player }));

    act(() => options?.events?.onStateChange?.({ target: player, data: 0 }));

    expect(result.current.ended).toBe(true);
    expect(result.current.endedVideoId).toBe("abcdefghijk");
    expect(result.current.isPlaying).toBe(false);
  });

  it("seeks to the chosen caption once the iframe becomes ready", async () => {
    const { result } = renderHook(() => useYouTubePlayer("abcdefghijk", "player"));
    await act(async () => Promise.resolve());
    act(() => result.current.seekTo(18));
    expect(player.seekTo).not.toHaveBeenCalled();
    act(() => options?.events?.onReady?.({ target: player }));
    expect(player.seekTo).toHaveBeenCalledWith(18, true);
  });
});

/** Minimal stand-in for the YouTube IFrame API player object. */
function playerWith(iframe: HTMLIFrameElement) {
  return { getIframe: () => iframe } as unknown as Parameters<typeof fitIframeToContainer>[0];
}

function rectOf(width: number, height: number) {
  return () => ({ width, height, top: 0, left: 0, right: width, bottom: height, x: 0, y: 0, toJSON: () => ({}) });
}

function measuredContainer(size: { clientWidth?: number; clientHeight?: number; rect?: [number, number] }) {
  const container = document.createElement("div");
  Object.defineProperty(container, "clientWidth", { configurable: true, value: size.clientWidth ?? 0 });
  Object.defineProperty(container, "clientHeight", { configurable: true, value: size.clientHeight ?? 0 });
  if (size.rect) container.getBoundingClientRect = rectOf(...size.rect);
  return container;
}

describe("fitIframeToContainer", () => {
  afterEach(() => { document.body.innerHTML = ""; });

  it("falls back to the bounding rect when clientWidth is zero", () => {
    const iframe = document.createElement("iframe");
    fitIframeToContainer(playerWith(iframe), measuredContainer({ rect: [360, 202] }));

    expect(iframe.width).toBe("360");
    expect(iframe.height).toBe("202");
  });

  it("uses the iframe's own rect when the container cannot be measured", () => {
    const iframe = document.createElement("iframe");
    iframe.getBoundingClientRect = rectOf(412, 232);

    fitIframeToContainer(playerWith(iframe), measuredContainer({}));

    expect(iframe.width).toBe("412");
    expect(iframe.height).toBe("232");
  });

  it("never writes a percentage - that is what re-arms the 640px WebView surface", () => {
    const iframe = document.createElement("iframe");
    Object.defineProperty(document.documentElement, "clientWidth", { configurable: true, value: 390 });

    fitIframeToContainer(playerWith(iframe), null);

    expect(iframe.width).toBe("390");
    // Nothing measurable for the height: a 16:9 estimate, still not a percentage.
    expect(iframe.height).toBe("219");
    expect(iframe.width).not.toContain("%");
    expect(iframe.height).not.toContain("%");
  });

  it("is a no-op when the player has no iframe yet", () => {
    const player = { getIframe: () => undefined } as unknown as Parameters<typeof fitIframeToContainer>[0];
    expect(() => fitIframeToContainer(player, null)).not.toThrow();
  });
});
