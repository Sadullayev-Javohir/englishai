import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { LeaderboardEntryDto } from "@/api/types";
import { PageContainer } from "@/components/ui/ResponsiveViewport";
import { Podium } from "./Podium";
import { RankRow } from "./RankRow";

const longName = "Muhammadali Abdugʻaniyev Karimov";

afterEach(cleanup);

function createEntry(overrides: Partial<LeaderboardEntryDto> = {}): LeaderboardEntryDto {
  return {
    rank: 4,
    learnerId: "learner-4",
    displayName: longName,
    pictureUrl: null,
    score: 1250,
    isPremium: false,
    isCurrentUser: false,
    ...overrides,
  };
}

describe("leaderboard names", () => {
  it("shows a full rank-row name without truncation utilities", () => {
    render(<RankRow entry={createEntry()} onOpen={() => {}} />);

    const name = screen.getByText(longName);
    expect(name.textContent).toBe(longName);
    expect(name.className).not.toContain("truncate");
    expect(name.className).toContain("leaderboard-page__rank-name");
  });

  it("shows the current user's full name together with the Siz badge", () => {
    render(<RankRow entry={createEntry({ isCurrentUser: true })} onOpen={() => {}} />);

    expect(screen.getByText(longName)).toBeTruthy();
    expect(screen.getByText("Siz")).toBeTruthy();
  });

  it("shows full podium names and keeps the current-user badge", () => {
    render(
      <Podium
        top={[
          createEntry({ rank: 1, learnerId: "learner-1", isCurrentUser: true }),
          createEntry({ rank: 2, learnerId: "learner-2", displayName: "Dilnoza Abdurahmonova" }),
          createEntry({ rank: 3, learnerId: "learner-3", displayName: "Javohir Mirzayev" }),
        ]}
      />,
    );

    const name = screen.getByText(longName);
    expect(name.textContent).toBe(longName);
    expect(name.className).not.toContain("truncate");
    expect(name.closest("article")?.className).toContain("leaderboard-page__podium-card");
    expect(screen.getByText("Siz")).toBeTruthy();
  });

  it("opens a podium learner when the card is clicked", () => {
    const onOpen = vi.fn();
    const entry = createEntry({ rank: 1, learnerId: "learner-1" });
    render(<Podium top={[entry]} onOpen={onOpen} />);

    fireEvent.click(screen.getByRole("button"));
    expect(onOpen).toHaveBeenCalledWith(entry);
  });

  it("opens a ranked learner when the row is clicked", () => {
    const onOpen = vi.fn();
    const entry = createEntry();
    render(<RankRow entry={entry} onOpen={onOpen} />);

    fireEvent.click(screen.getByRole("button"));
    expect(onOpen).toHaveBeenCalledWith(entry);
  });

  it("uses a single-column mobile podium and restores three columns on wider screens", () => {
    render(
      <Podium
        top={[
          createEntry({ rank: 1, learnerId: "learner-1" }),
          createEntry({ rank: 2, learnerId: "learner-2", displayName: "Dilnoza Abdurahmonova" }),
          createEntry({ rank: 3, learnerId: "learner-3", displayName: "Javohir Mirzayev" }),
        ]}
      />,
    );

    const podium = screen.getByLabelText("Top uchlik");
    expect(podium.className).toContain("leaderboard-page__podium");

    const firstPlace = screen.getByText("1-o‘rin").closest("article");
    expect(firstPlace?.className).toContain("leaderboard-page__podium-card--winner");
  });

  it("keeps rank-row XP in a compact mobile column", () => {
    render(<RankRow entry={createEntry()} onOpen={() => {}} />);

    const xp = screen.getByText("1250 XP");
    expect(xp.className).not.toContain("col-span-2");
    expect(xp.className).toContain("leaderboard-page__rank-score");
  });

  it("keeps long leaderboard content shrinkable instead of forcing horizontal overflow", () => {
    render(<RankRow entry={createEntry({ isPremium: true })} onOpen={() => {}} />);

    const row = screen.getByRole("button");
    expect(row.className).toContain("leaderboard-page__rank-row");

    const xp = screen.getByText("1250 XP");
    expect(xp.className).toContain("leaderboard-page__rank-score");
  });

  it("keeps the shared page boundary shrinkable inside the app shell", () => {
    render(<PageContainer width="wide" padded={false}>Leaderboard</PageContainer>);

    expect(screen.getByText("Leaderboard").className).toContain("min-w-0");
  });
});
