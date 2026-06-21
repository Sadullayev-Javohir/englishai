import { describe, expect, it, vi } from "vitest";
import type { LeaderboardEntryDto } from "@/api/types";

vi.stubEnv("DEV", true);

import { withLeaderboardPreview } from "./previewEntries";

const currentUser: LeaderboardEntryDto = {
  rank: 1,
  learnerId: "current-user",
  displayName: "Javohir",
  pictureUrl: null,
  score: 190,
  isPremium: false,
  isCurrentUser: true,
};

describe("leaderboard preview entries", () => {
  it("fills a sparse development leaderboard to ten ranked users", () => {
    const entries = withLeaderboardPreview([currentUser]);

    expect(entries).toHaveLength(10);
    expect(entries.map((entry) => entry.rank)).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
    expect(entries.some((entry) => entry.learnerId === currentUser.learnerId)).toBe(true);
  });
});
