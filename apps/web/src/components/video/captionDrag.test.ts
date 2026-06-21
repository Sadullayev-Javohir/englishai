import { describe, expect, it } from "vitest";
import { clampCaptionOffset } from "./captionDrag";

const frame = { left: 0, top: 0, right: 800, bottom: 450 };
const caption = { left: 300, top: 350, right: 500, bottom: 400 };

describe("clampCaptionOffset", () => {
  it("keeps a desired offset when the caption remains inside the frame", () => {
    expect(clampCaptionOffset({ x: 40, y: -80 }, { x: 0, y: 0 }, frame, caption))
      .toEqual({ x: 40, y: -80 });
  });

  it("clamps every caption edge to the video frame", () => {
    expect(clampCaptionOffset({ x: -500, y: -500 }, { x: 0, y: 0 }, frame, caption))
      .toEqual({ x: -300, y: -350 });
    expect(clampCaptionOffset({ x: 500, y: 500 }, { x: 0, y: 0 }, frame, caption))
      .toEqual({ x: 300, y: 50 });
  });

  it("re-clamps from an existing translated position", () => {
    const translatedCaption = { left: 550, top: 380, right: 750, bottom: 430 };
    expect(clampCaptionOffset({ x: 400, y: 100 }, { x: 250, y: 30 }, frame, translatedCaption))
      .toEqual({ x: 300, y: 50 });
  });
});
