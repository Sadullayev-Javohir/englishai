import type { LeaderboardEntryDto } from "@/api/types";
import { cn } from "@/lib/cn";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { UserAvatar } from "@/components/UserAvatar";

export function RankRow({ entry, onOpen }: { entry: LeaderboardEntryDto; onOpen: (entry: LeaderboardEntryDto) => void }) {
  const isMe = entry.isCurrentUser;

  return (
    <button
      type="button"
      onClick={() => onOpen(entry)}
      className={cn("leaderboard-page__rank-row", isMe && "leaderboard-page__rank-row--me")}
    >
      <span className="leaderboard-page__rank-number">{entry.rank}</span>
      <UserAvatar
        pictureUrl={entry.pictureUrl}
        name={entry.displayName}
        alt={entry.displayName}
        className="leaderboard-page__rank-avatar"
        fallbackClassName="font-duo font-extrabold"
      />
      <span className="leaderboard-page__rank-identity">
        <span className="leaderboard-page__rank-name">{entry.displayName}</span>
        <span className="leaderboard-page__rank-meta">
          {isMe && <span>{uz.leaderboard.you}</span>}
          {entry.isPremium && <span><Icon name="workspace_premium" filled />PRO</span>}
        </span>
      </span>
      <span className="leaderboard-page__rank-score">
        <Icon name="bolt" filled />
        {entry.score} XP
      </span>
      <Icon name="chevron_right" className="leaderboard-page__rank-chevron" />
    </button>
  );
}
