import { afterEach, describe, expect, it, vi } from "vitest";
import {
  exitFullscreenIfActive,
  fullscreenElement,
  fullscreenSupported,
  onFullscreenChange,
  requestFullscreenOn,
} from "./fullscreen";

type Mutable = Record<string, unknown>;

/** Installs a fullscreen API surface and returns a restore function. */
function stubFullscreen(options: {
  enabled?: boolean;
  webkitEnabled?: boolean;
  request?: unknown;
  webkitRequest?: unknown;
  exit?: unknown;
  webkitExit?: unknown;
}) {
  const doc = document as unknown as Mutable;
  const root = document.documentElement as unknown as Mutable;
  const keys: Array<[Mutable, string]> = [
    [doc, "fullscreenEnabled"],
    [doc, "webkitFullscreenEnabled"],
    [doc, "exitFullscreen"],
    [doc, "webkitExitFullscreen"],
    [root, "requestFullscreen"],
    [root, "webkitRequestFullscreen"],
  ];
  for (const [target, key] of keys) delete target[key];

  Object.defineProperty(doc, "fullscreenEnabled", { configurable: true, value: options.enabled ?? false });
  if (options.webkitEnabled !== undefined) {
    Object.defineProperty(doc, "webkitFullscreenEnabled", { configurable: true, value: options.webkitEnabled });
  }
  if (options.request) Object.defineProperty(root, "requestFullscreen", { configurable: true, value: options.request });
  if (options.webkitRequest) {
    Object.defineProperty(root, "webkitRequestFullscreen", { configurable: true, value: options.webkitRequest });
  }
  if (options.exit) Object.defineProperty(doc, "exitFullscreen", { configurable: true, value: options.exit });
  if (options.webkitExit) {
    Object.defineProperty(doc, "webkitExitFullscreen", { configurable: true, value: options.webkitExit });
  }
}

let active: Element | null = null;
Object.defineProperty(document, "fullscreenElement", { configurable: true, get: () => active });
Object.defineProperty(document, "webkitFullscreenElement", { configurable: true, get: () => null });

afterEach(() => {
  active = null;
  stubFullscreen({});
});

describe("fullscreen", () => {
  it("reports support only when the document is enabled and the element is requestable", () => {
    stubFullscreen({});
    expect(fullscreenSupported()).toBe(false);

    stubFullscreen({ enabled: true });
    expect(fullscreenSupported()).toBe(false);

    stubFullscreen({ enabled: true, request: vi.fn() });
    expect(fullscreenSupported()).toBe(true);
  });

  it("treats a webkit-only browser as supported", () => {
    stubFullscreen({ webkitEnabled: true, webkitRequest: vi.fn() });
    expect(fullscreenSupported()).toBe(true);
  });

  it("prefers the unprefixed request and reports the real outcome", async () => {
    const request = vi.fn(async () => { active = document.documentElement; });
    const webkitRequest = vi.fn();
    stubFullscreen({ enabled: true, request, webkitRequest });

    await expect(requestFullscreenOn(document.documentElement)).resolves.toBe(true);
    expect(request).toHaveBeenCalledTimes(1);
    expect(webkitRequest).not.toHaveBeenCalled();
    expect(fullscreenElement()).toBe(document.documentElement);
  });

  it("retries without navigationUI before falling back to the webkit request", async () => {
    const request = vi.fn(async (options?: FullscreenOptions) => {
      if (options) throw new TypeError("unsupported option");
    });
    const webkitRequest = vi.fn(async () => { active = document.documentElement; });
    stubFullscreen({ enabled: true, request, webkitRequest });

    await expect(requestFullscreenOn(document.documentElement)).resolves.toBe(true);
    expect(request).toHaveBeenCalledTimes(2);
    expect(webkitRequest).toHaveBeenCalledTimes(1);
  });

  it("resolves false instead of throwing when no fullscreen API exists", async () => {
    stubFullscreen({});
    await expect(requestFullscreenOn(document.documentElement)).resolves.toBe(false);
  });

  it("exits through the prefixed method when the standard one is absent", async () => {
    const webkitExit = vi.fn(async () => { active = null; });
    stubFullscreen({ webkitEnabled: true, webkitRequest: vi.fn(), webkitExit });
    active = document.documentElement;

    await exitFullscreenIfActive();
    expect(webkitExit).toHaveBeenCalledTimes(1);
  });

  it("does not call exit when nothing is fullscreen", async () => {
    const exit = vi.fn();
    stubFullscreen({ enabled: true, request: vi.fn(), exit });

    await exitFullscreenIfActive();
    expect(exit).not.toHaveBeenCalled();
  });

  it("subscribes to both change events and unsubscribes cleanly", () => {
    const listener = vi.fn();
    const unsubscribe = onFullscreenChange(listener);

    document.dispatchEvent(new Event("fullscreenchange"));
    document.dispatchEvent(new Event("webkitfullscreenchange"));
    expect(listener).toHaveBeenCalledTimes(2);

    unsubscribe();
    document.dispatchEvent(new Event("fullscreenchange"));
    document.dispatchEvent(new Event("webkitfullscreenchange"));
    expect(listener).toHaveBeenCalledTimes(2);
  });
});
