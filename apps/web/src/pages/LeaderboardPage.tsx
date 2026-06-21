import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { LeaderboardEntryDto } from "@/api/types";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { Icon } from "@/components/ui/Icon";
import { UserAvatar } from "@/components/UserAvatar";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { leagueForXp, TIER_META } from "@/components/leaderboard/useLeagueTier";
import { withLeaderboardPreview } from "@/components/leaderboard/previewEntries";
import { useAsync } from "@/lib/useAsync";
import "./LeaderboardPage.css";

type Period = "week" | "month" | "all";

const PERIODS: Array<{ value: Period; label: string }> = [
  { value: "week", label: "Haftalik" },
  { value: "month", label: "Oylik" },
  { value: "all", label: "Umumiy" },
];

const PODIUM_ORDER = [2, 1, 3] as const;
const PODIUM_META = {
  1: { icon: "workspace_premium", tone: "winner", place: "#1" },
  2: { icon: "military_tech", tone: "runner-up", place: "#2" },
  3: { icon: "workspace_premium", tone: "third", place: "#3" },
} as const;

function readableRankChange() {
  return "—";
}

function LeaderboardState({
  icon,
  title,
  copy,
  onRetry,
}: {
  icon: string;
  title: string;
  copy: string;
  onRetry?: () => void;
}) {
  return (
    <section className="leaderboard-pen__state" role={onRetry ? "alert" : "status"}>
      <Icon name={icon} />
      <h1>{title}</h1>
      <p>{copy}</p>
      {onRetry && (
        <button type="button" onClick={onRetry}>
          <Icon name="refresh" />Qayta urinish
        </button>
      )}
    </section>
  );
}

function podiumName(displayName: string) {
  return displayName.trim().split(/\s+/)[0] || displayName;
}

function PodiumCard({ entry }: { entry: LeaderboardEntryDto }) {
  const meta = PODIUM_META[entry.rank as 1 | 2 | 3];
  if (!meta) return null;

  return (
    <article className={`leaderboard-pen__podium-card leaderboard-pen__podium-card--${meta.tone}`}>
      <Icon name={meta.icon} filled className="leaderboard-pen__podium-medal" />
      <UserAvatar
        pictureUrl={entry.pictureUrl}
        name={entry.displayName}
        alt={entry.displayName}
        className="leaderboard-pen__podium-avatar"
        fallbackClassName="font-duo font-extrabold"
      />
      <strong>{podiumName(entry.displayName)}</strong>
      <span>{entry.score.toLocaleString("uz-UZ")} XP</span>
      <b>{meta.place}</b>
    </article>
  );
}

function RankRow({ entry, onOpen }: { entry: LeaderboardEntryDto; onOpen: (entry: LeaderboardEntryDto) => void }) {
  const movement = readableRankChange();
  return (
    <button
      type="button"
      className={`leaderboard-pen__rank-row${entry.isCurrentUser ? " is-current" : ""}`}
      onClick={() => onOpen(entry)}
    >
      <span className="leaderboard-pen__rank-number">{entry.rank}</span>
      <UserAvatar
        pictureUrl={entry.pictureUrl}
        name={entry.displayName}
        alt={entry.displayName}
        className="leaderboard-pen__rank-avatar"
        fallbackClassName="font-caption font-extrabold"
      />
      <span className="leaderboard-pen__rank-name">{entry.isCurrentUser ? `${entry.displayName} (siz)` : entry.displayName}</span>
      <span className="leaderboard-pen__rank-score">{entry.score.toLocaleString("uz-UZ")} XP</span>
      <span className={`leaderboard-pen__rank-change${movement.startsWith("↓") ? " is-down" : ""}`}>{movement}</span>
    </button>
  );
}

export function LeaderboardPage() {
  const navigate = useNavigate();
  const learnerId = getLearnerId();
  const [period, setPeriod] = useState<Period>("week");
  const request = useAsync(() => api.gamification.leaderboard(learnerId), [learnerId]);
  const points = useAsync(() => api.gamification.points(learnerId), [learnerId]);

  const entries = useMemo(
    () => request.data ? withLeaderboardPreview(request.data.top) : [],
    [request.data],
  );
  const podium = useMemo(
    () => PODIUM_ORDER.map((rank) => entries.find((entry) => entry.rank === rank)).filter((entry): entry is LeaderboardEntryDto => Boolean(entry)),
    [entries],
  );
  const leaderRows = useMemo(() => {
    const rows = entries.filter((entry) => entry.rank > 3).slice(0, 6);
    const currentUser = request.data?.currentUserEntry ?? entries.find((entry) => entry.isCurrentUser) ?? null;
    if (currentUser && !rows.some((entry) => entry.learnerId === currentUser.learnerId)) {
      rows[rows.length - 1] = currentUser;
    }
    return rows;
  }, [entries, request.data?.currentUserEntry]);
  const currentUser = request.data?.currentUserEntry ?? entries.find((entry) => entry.isCurrentUser) ?? null;
  const nextRank = currentUser ? entries.find((entry) => entry.rank === currentUser.rank - 1) : null;
  const xpToNextRank = currentUser && nextRank ? Math.max(0, nextRank.score - currentUser.score) : 0;
  const league = TIER_META[leagueForXp(points.data?.lifetimeXp)].label;

  const openLearner = (entry: LeaderboardEntryDto) => {
    navigate(`/leaderboard/${encodeURIComponent(entry.learnerId)}`, { state: { learner: entry } });
  };

  return (
    <div className="leaderboard-pen" data-testid="leaderboard-page">
      <div className="leaderboard-pen__mobile-actions">
        <button type="button" onClick={() => navigate("/app/vocabulary/saved")}>
          <Icon name="bookmark_border" />Saqlangan
        </button>
      </div>

      <header className="leaderboard-pen__heading">
        <span>REYTING</span>
        <h1>Birga o‘samiz.</h1>
        <p>Har bir mashq bilan yuqoriga ko‘tariling.</p>
      </header>

      <nav className="leaderboard-pen__periods" aria-label="Reyting davri">
        {PERIODS.map((option) => (
          <button
            key={option.value}
            type="button"
            aria-pressed={period === option.value}
            onClick={() => setPeriod(option.value)}
          >
            {option.label}
          </button>
        ))}
      </nav>

      {request.loading ? (
        <ModulePageLoader icon="emoji_events" accent="purple" embedded />
      ) : request.error ? (
        <LeaderboardState icon="cloud_off" title="Reytingni yuklab bo‘lmadi" copy="Ulanishni tekshirib, yana urinib ko‘ring." onRetry={request.reload} />
      ) : entries.length === 0 ? (
        <LeaderboardState icon="emoji_events" title="Hali reyting ma’lumoti yo‘q" copy="Birinchi mashqni yakunlab, haftalik reytingga qo‘shiling." />
      ) : (
        <>
          <section className="leaderboard-pen__league" aria-labelledby="leaderboard-league-title">
            <div className="leaderboard-pen__league-title">
              <span className="leaderboard-pen__league-icon"><Icon name="workspace_premium" filled /></span>
              <div>
                <small>LIGA</small>
                <h2 id="leaderboard-league-title">{league} liga</h2>
              </div>
              <span className="leaderboard-pen__countdown">3 kun qoldi</span>
            </div>
            <p>Eng yaxshi 10 ishtirokchi keyingi ligaga o‘tadi.</p>
          </section>

          <section className="leaderboard-pen__podium" aria-label="Haftalik top uchlik">
            {podium.map((entry) => <PodiumCard key={entry.learnerId} entry={entry} />)}
          </section>

          <section className="leaderboard-pen__leaders" aria-labelledby="leaderboard-leaders-title">
            <h2 id="leaderboard-leaders-title">Bu haftaning yetakchilari</h2>
            <div className="leaderboard-pen__rank-list">
              {leaderRows.map((entry) => <RankRow key={entry.learnerId} entry={entry} onOpen={openLearner} />)}
            </div>
          </section>

          {currentUser && (
            <section className="leaderboard-pen__summary" aria-labelledby="leaderboard-summary-title">
              <div className="leaderboard-pen__summary-title">
                <Icon name="trending_up" />
                <h2 id="leaderboard-summary-title">{currentUser.rank}-o‘rin · Haftalik reyting</h2>
              </div>
              <p>
                {nextRank && xpToNextRank > 0
                  ? `Keyingi o‘ringacha ${xpToNextRank.toLocaleString("uz-UZ")} XP. Yangi darsni boshlang!`
                  : "Haftalik o‘sish ritmini saqlang. Yangi darsni boshlang!"}
              </p>
              <button type="button" onClick={() => navigate("/home")}>
                <Icon name="arrow_forward" />XP yig‘ishni boshlash
              </button>
            </section>
          )}
        </>
      )}
    </div>
  );
}
