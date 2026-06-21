import type { LeaderboardEntryDto } from "@/api/types";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { UserAvatar } from "@/components/UserAvatar";
import { DesignCard } from "@/components/design";

const placement = {
  1: { icon: "emoji_events", label: "1-o‘rin", className: "leaderboard-page__podium-card--gold" },
  2: { icon: "military_tech", label: "2-o‘rin", className: "leaderboard-page__podium-card--silver" },
  3: { icon: "workspace_premium", label: "3-o‘rin", className: "leaderboard-page__podium-card--bronze" },
} as const;

export function Podium({ top, onOpen }: { top: LeaderboardEntryDto[]; onOpen?: (entry: LeaderboardEntryDto) => void }) {
  const sorted = [...top].sort((left, right) => left.rank - right.rank);

  return (
    <section className="leaderboard-page__podium" aria-label="Top uchlik">
      {sorted.map((entry) => {
        const meta = placement[entry.rank as keyof typeof placement] ?? placement[3];

        return (
          <DesignCard
            as="article"
            key={entry.learnerId}
            padding="none"
            className={`leaderboard-page__podium-card ${meta.className} ${entry.rank === 1 ? "leaderboard-page__podium-card--winner" : ""}`}
          >
            <button type="button" className="leaderboard-page__podium-action" onClick={() => onOpen?.(entry)}>
              <div className="leaderboard-page__podium-place">
                <Icon name={meta.icon} filled />
                <span>{meta.label}</span>
              </div>
              <UserAvatar
                pictureUrl={entry.pictureUrl}
                name={entry.displayName}
                alt={entry.displayName}
                className="leaderboard-page__podium-avatar"
                fallbackClassName="font-duo font-extrabold"
              />
              <div className="leaderboard-page__podium-identity">
                <strong>{entry.displayName}</strong>
                <div>
                  {entry.isCurrentUser && <span className="leaderboard-page__podium-you">{uz.leaderboard.you}</span>}
                  {entry.isPremium && <span className="leaderboard-page__podium-pro"><Icon name="workspace_premium" filled />PRO</span>}
                </div>
              </div>
              <span className="leaderboard-page__podium-score">
                <Icon name="bolt" filled />
                {entry.score.toLocaleString("uz-UZ")} XP
              </span>
            </button>
          </DesignCard>
        );
      })}
    </section>
  );
}
