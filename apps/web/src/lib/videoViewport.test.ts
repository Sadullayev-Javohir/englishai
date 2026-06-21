import { describe, expect, it } from "vitest";
import { clampVideoViewportSize } from "./videoViewport";

const LAYOUT = { width: 390, height: 844 };

describe("clampVideoViewportSize", () => {
  it("keeps the layout size when nothing diverges", () => {
    expect(clampVideoViewportSize({ width: 390, height: 844 }, LAYOUT)).toEqual(LAYOUT);
  });

  it("clamps a pinch-zoomed visual viewport back to the layout width", () => {
    // Zoomed 2x: the visual viewport reports half the layout width.
    expect(clampVideoViewportSize({ width: 195, height: 422 }, LAYOUT)).toEqual({ width: 195, height: 422 });
  });

  it("never reports more than the layout viewport", () => {
    // Zoomed out / transient values above the layout size used to widen the whole shell.
    expect(clampVideoViewportSize({ width: 640, height: 1200 }, LAYOUT)).toEqual(LAYOUT);
  });

  it("tracks the shorter height while the URL bar is expanded", () => {
    expect(clampVideoViewportSize({ width: 390, height: 731 }, LAYOUT)).toEqual({ width: 390, height: 731 });
  });

  it("falls back to the layout viewport when visualViewport is unavailable", () => {
    expect(clampVideoViewportSize(null, LAYOUT)).toEqual(LAYOUT);
  });

  it("never returns a zero or negative size", () => {
    expect(clampVideoViewportSize({ width: 0, height: 0 }, { width: 0, height: 0 })).toEqual({ width: 1, height: 1 });
    expect(clampVideoViewportSize({ width: -5, height: -5 }, LAYOUT)).toEqual({ width: 1, height: 1 });
  });
});
