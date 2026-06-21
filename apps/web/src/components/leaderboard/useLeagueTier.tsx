import type { LeaderboardEntryDto } from "@/api/types";
import { uz } from "@/content/uz";
import { AppButton } from "@/components/design";

export type LeagueTier = "bronze" | "silver" | "gold" | "platinum" | "diamond";

export interface LeagueMeta {
  label: string;
  icon: string;
  minimumXp: number;
}

export const LEAGUE_ORDER: LeagueTier[] = ["bronze", "silver", "gold", "platinum", "diamond"];

export const TIER_META: Record<LeagueTier, LeagueMeta> = {
  bronze: { label: uz.league.bronze, icon: "workspace_premium", minimumXp: 0 },
  silver: { label: uz.league.silver, icon: "military_tech", minimumXp: 500 },
  gold: { label: uz.league.gold, icon: "emoji_events", minimumXp: 1_500 },
  platinum: { label: uz.league.platinum, icon: "stars", minimumXp: 3_000 },
  diamond: { label: uz.league.diamond, icon: "diamond", minimumXp: 6_000 },
};

export interface LeagueProgress {
  tier: LeagueTier;
  current: LeagueMeta;
  next: LeagueMeta | null;
  xp: number;
  xpIntoTier: number;
  xpForTier: number;
  xpToNext: number;
  progressPercent: number;
}

export function leagueForXp(lifetimeXp: number | undefined): LeagueTier {
  const xp = Math.max(0, lifetimeXp ?? 0);
  return [...LEAGUE_ORDER].reverse().find((tier) => xp >= TIER_META[tier].minimumXp) ?? "bronze";
}

export function leagueProgressForXp(lifetimeXp: number | undefined): LeagueProgress {
  const xp = Math.max(0, lifetimeXp ?? 0);
  const tier = leagueForXp(xp);
  const currentIndex = LEAGUE_ORDER.indexOf(tier);
  const current = TIER_META[tier];
  const nextTier = LEAGUE_ORDER[currentIndex + 1];
  const next = nextTier ? TIER_META[nextTier] : null;
  const xpIntoTier = xp - current.minimumXp;
  const xpForTier = next ? next.minimumXp - current.minimumXp : 0;
  const xpToNext = next ? Math.max(0, next.minimumXp - xp) : 0;
  const progressPercent = next && xpForTier > 0 ? Math.min(100, Math.round(xpIntoTier / xpForTier * 100)) : 100;

  return { tier, current, next, xp, xpIntoTier, xpForTier, xpToNext, progressPercent };
}

export function LevelTab({ active, label, onClick }: { active: boolean; label: string; onClick: () => void }) {
  return (
    <AppButton
      type="button"
      onClick={onClick}
      tone={active ? "primary" : "standard"}
      size="sm"
      className="min-w-[64px] shrink-0 snap-start whitespace-nowrap"
      aria-pressed={active}
    >
      {label}
    </AppButton>
  );
}

export type { LeaderboardEntryDto };
