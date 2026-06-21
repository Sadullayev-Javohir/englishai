import { NavLink } from "react-router-dom";
import { uz } from "@/content/uz";
import { ParrotLogo } from "@/components/ParrotLogo";
import { useAsync } from "@/lib/useAsync";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { StatPill } from "./StatPill";
import { NotificationBell } from "@/components/NotificationBell";
import { useHeartsState, HEARTS_MAX } from "./HeartsProvider";
import { Flame3d, Bolt3d, Trophy3d, Heart3d } from "./Modern3dIcons";
import { Icon } from "@/components/ui/Icon";
import { useEnergy } from "./EnergyProvider";
import { leagueForXp, TIER_META } from "@/components/leaderboard/useLeagueTier";
import { formatCompactNumber } from "@/lib/formatCompactNumber";

/**
 * Global top Nav HUD (Jungle Academy spec §2.2 / §2.3).
 * Frosted pill groups on the jungle: logo+wordmark left, streak/XP/gems/league/hearts
 * (when < full). The mobile bottom tab bar is unchanged (kept in AppShell).
 * Canonical game-layer copy (migrated from duo/*; renamed NavHud to avoid clashing
 * with the in-lesson LessonHud).
 */

export function NavHud() {
  const learnerId = getLearnerId();
  const { data } = useAsync(() => api.gamification.status(learnerId), [learnerId]);
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { hearts } = useHeartsState();
  const { energy, loading: energyLoading, openEnergyModal } = useEnergy();

  const streak = data?.currentStreak ?? 0;
  const xp = points?.lifetimeXp ?? 0;
  const gems = points?.spendableCoins ?? 0;
  const league = TIER_META[leagueForXp(xp)].label;

  return (
    <div data-testid="desktop-nav-hud" className="sticky top-0 z-40 hidden px-page-gutter pt-3 xl:block">
      <div className="flex w-full items-center gap-2 rounded-[24px] bg-ea-green-600 px-6 py-2  ring-1 ring-white/25">
        {/* Brand logo - parrot mark + wordmark on a white rounded tile */}
        <NavLink to="/home" aria-label="EnglishAI" className="flex shrink-0 items-center transition-transform  ">
          <ParrotLogo size={44} withWordmark wordmarkClassName="text-[18px] sm:text-[20px]" />
        </NavLink>

        <div className="ml-auto flex items-center gap-1.5 sm:gap-2">
          <StatPill to="/home" title={uz.hud.streak} icon={<Flame3d className="text-[22px] !text-ea-orange-200" />} label={String(streak)} tone="orange" className="!bg-ea-green-900 !text-white " />
          <StatPill to="/progress" title={uz.hud.xp} icon={<Icon name="star" filled className="text-[20px] text-ea-brand-yellow" />} label={formatCompactNumber(xp)} tone="yellow" className="!bg-ea-green-900 !text-white " />
          <StatPill onClick={() => openEnergyModal()} title={uz.hud.energy} icon={<Bolt3d className="text-[22px]" />} label={energyLoading ? "…" : energy ? `${energy.current}/${energy.maximum}` : "-"} tone="yellow" className="!bg-ea-yellow-700 !text-white " />
          <StatPill to="/leaderboard#discounts" title={uz.hud.coins} icon={<Icon name="paid" filled className="text-[20px] text-ea-orange-500" />} label={formatCompactNumber(gems)} tone="orange" className="!bg-ea-text !text-white " />
          <StatPill to="/leaderboard" title={uz.hud.league} icon={<Trophy3d className="text-[22px] !text-ea-purple-200" />} label={league} tone="purple" className="!bg-ea-purple-800 !text-white " />
          {hearts < HEARTS_MAX && (
            <StatPill to="/home" title={uz.hud.heartsLabel} icon={<Heart3d className="text-[22px] !text-ea-danger-200" />} label={`${hearts}`} tone="red" className="!bg-ea-green-900/55 !text-white " />
          )}
          <NotificationBell className="!bg-ea-green-900/55 !ring-white/30" />
        </div>
      </div>
    </div>
  );
}
