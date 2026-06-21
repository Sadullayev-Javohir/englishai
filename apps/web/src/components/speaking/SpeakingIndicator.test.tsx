import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { SpeakingIndicator } from "./SpeakingIndicator";

describe("SpeakingIndicator", () => {
  it("renders seven theme-aware waveform bars without white surfaces", () => {
    const { container } = render(<SpeakingIndicator />);
    const bars = Array.from(container.querySelectorAll<HTMLElement>(".animate-soundbar"));

    expect(bars).toHaveLength(7);
    for (const bar of bars) {
      expect(bar.classList.contains("bg-on-primary-container")).toBe(true);
      expect(bar.classList.contains("bg-white")).toBe(false);
    }
  });
});
