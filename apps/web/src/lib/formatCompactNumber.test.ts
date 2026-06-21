import { describe, expect, it } from "vitest";
import { formatCompactNumber } from "./formatCompactNumber";

describe("formatCompactNumber", () => {
  it.each([
    [400, "400"],
    [999, "999"],
    [1000, "1k"],
    [1200, "1.2k"],
    [4000, "4k"],
    [10000, "10k"],
  ])("formats %i as %s", (value, expected) => {
    expect(formatCompactNumber(value)).toBe(expected);
  });
});
