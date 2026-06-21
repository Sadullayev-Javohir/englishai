import { useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { NotificationBell } from "@/components/NotificationBell";
import { useAsync } from "@/lib/useAsync";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAuth } from "@/app/auth";
import { UserAvatar } from "@/components/UserAvatar";
import { formatCompactNumber } from "@/lib/formatCompactNumber";
import { StatPill } from "./StatPill";
import { useHeartsState, HEARTS_MAX } from "@/components/game/HeartsProvider";
import { AppIconButton } from "@/components/design";
import { ParrotLogo } from "@/components/ParrotLogo";

/** Flat Professional Indigo app-level HUD. */

interface NavTarget {
  to: string;
  icon: string;
  label: string;
}

const NAV: NavTarget[] = [
  { to: "/home", icon: "home", label: uz.nav.home },
  { to: "/levels", icon: "map", label: uz.nav.levels },
  { to: "/progress", icon: "trending_up", label: uz.nav.progress },
  { to: "/leaderboard", icon: "trophy", label: uz.nav.leaderboard },
  { to: "/profile", icon: "person", label: uz.nav.profile },
];

export function AppHud() {
  const learnerId = getLearnerId();
  const { user } = useAuth();
  const { data } = useAsync(() => api.gamification.status(learnerId), [learnerId]);
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { data: admin } = useAsync(() => api.admin.access(), []);
  const [menuOpen, setMenuOpen] = useState(false);
  const navigate = useNavigate();

  const streak = data?.currentStreak ?? 0;
  const xp = points?.lifetimeXp ?? 0;
  const gems = points?.spendableCoins ?? 0;
  const { hearts } = useHeartsState();

  const initials = (user?.username ?? user?.email ?? "U").slice(0, 2).toUpperCase();

  const menuItems = [
    ...NAV,
    ...(admin?.isAdmin
      ? [{ to: "/admin", icon: "shield_person", label: uz.nav.admin }]
      : []),
  ];

  return (
    <div data-testid="home-app-hud" className="sticky top-0 z-40 px-3 pt-2 md:px-6">
      <div className="mx-auto flex max-w-[1180px] items-center gap-2 rounded-[18px] border border-[var(--ea-border)] bg-[var(--ea-surface)] px-3 py-2 md:px-4">
        {/* Logo + wordmark */}
        <NavLink to="/home" aria-label="EnglishAI.uz bosh sahifasi" className="flex min-w-0 items-center gap-2 pr-1">
          <ParrotLogo size={44} withWordmark />
        </NavLink>

        {/* Spacer pushes stats to the right */}
        <div className="ml-auto flex items-center gap-1.5 sm:gap-2">
          <span className="hidden sm:inline-flex"><StatPill
            onClick={() => navigate("/home")}
            title={uz.hud.streak}
            icon={<Icon name="local_fire_department" filled className="text-[15px]" />}
            label={String(streak)}
            tone="orange"
          /></span>
          <span className="hidden md:inline-flex"><StatPill
            onClick={() => navigate("/progress")}
            title="XP"
            icon={<Icon name="star" filled className="text-[15px]" />}
            label={formatCompactNumber(xp)}
            tone="yellow"
          /></span>
          <span className="hidden lg:inline-flex"><StatPill
            onClick={() => navigate("/leaderboard")}
            title={uz.hud.gems}
            icon={<Icon name="diamond" filled className="text-[15px]" />}
            label={formatCompactNumber(gems)}
            tone="blue"
          /></span>
          <StatPill
            onClick={() => navigate("/leaderboard")}
            title={uz.hud.league}
            icon={<Icon name="emoji_events" filled className="text-[15px]" />}
            label={uz.league.short}
            tone="purple"
          />
          {hearts < HEARTS_MAX && (
            <StatPill
              onClick={() => navigate("/home")}
              title={uz.hud.heartsLabel}
              icon={<Icon name="heart" filled className="text-[15px]" />}
              label={`${hearts}`}
              tone="red"
            />
          )}

          <NotificationBell className="!bg-[var(--ea-ink)] !text-white !ring-0" />

          {/* Nav menu (desktop) - icon pill that opens a slide-down. Inlined so we
              do not depend on the forbidden duo/HudMenu. */}
          <div className="relative hidden md:block">
            <AppIconButton
              icon="more_horiz"
              label={uz.nav.menu}
              size="sm"
              aria-label={uz.nav.menu}
              aria-expanded={menuOpen}
              onClick={() => setMenuOpen((v) => !v)}
            />
            {menuOpen && (
              <>
                <div
                  className="fixed inset-0 z-40"
                  aria-hidden
                  onClick={() => setMenuOpen(false)}
                />
                <div className="absolute right-0 top-11 z-50 w-52 overflow-hidden rounded-2xl border border-[var(--ea-border)] bg-[var(--ea-surface)] p-1.5">
                  {menuItems.map((item) => {
                    return (
                      <NavLink
                        key={item.to}
                        to={item.to}
                        onClick={() => setMenuOpen(false)}
                        className={({ isActive }) => `flex items-center gap-3 rounded-xl px-3 py-2 font-bold transition-colors ${isActive ? "bg-[var(--ea-primary)] text-[var(--ea-ink)]" : "text-[var(--ea-text)] hover:bg-[var(--ea-surface-soft)]"}`}
                      >
                        <Icon name={item.icon} className="text-[18px]" />
                        {item.label}
                      </NavLink>
                    );
                  })}
                </div>
              </>
            )}
          </div>

          {/* Profile avatar */}
          <NavLink
            to="/profile"
            aria-label={uz.nav.profile}
            className="flex h-10 w-10 items-center justify-center rounded-[12px] border border-[var(--ea-border)] bg-[var(--ea-surface-soft)] text-[13px] font-extrabold text-[var(--ea-text)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--ea-focus)] focus-visible:ring-offset-2"
          >
            <UserAvatar pictureUrl={user?.pictureUrl} name={user?.displayName || initials} className="h-full w-full rounded-[inherit]" fallbackClassName="font-extrabold" />
          </NavLink>
        </div>
      </div>
    </div>
  );
}
