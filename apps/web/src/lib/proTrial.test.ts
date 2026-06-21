import { describe, expect, it } from "vitest";
import { formatProTrialDate } from "./proTrial";
import { uz } from "@/content/uz";

describe("profile trial expiry", () => {
  it("preserves the UTC expiry date when formatting the profile label", () => {
    const date = formatProTrialDate("2026-08-24T23:59:59Z");
    expect(date).toBe("24.08.2026");
    expect(uz.profile.trialUntil(date)).toBe("24.08.2026gacha bepul Pro");
  });

  it("returns an empty label for an invalid expiry timestamp", () => {
    expect(formatProTrialDate("invalid")).toBe("");
  });
});
