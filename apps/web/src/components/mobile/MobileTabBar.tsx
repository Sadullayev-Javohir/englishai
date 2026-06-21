import { NavLink, useLocation } from "react-router-dom";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import { tapLight } from "@/lib/haptics";
import { getRouteMetadata, type NavSection } from "@/app/routeMetadata";

interface Tab {
  to: string;
  icon: string;
  label: string;
  section: NavSection;
}

const TABS: Tab[] = [
  { to: "/home", icon: "home", label: uz.nav.home, section: "home" },
  { to: "/levels", icon: "map", label: uz.nav.levels, section: "levels" },
  { to: "/progress", icon: "trending_up", label: uz.nav.progress, section: "progress" },
  { to: "/leaderboard", icon: "trophy", label: uz.nav.leaderboard, section: "leaderboard" },
  { to: "/profile", icon: "person", label: uz.nav.profile, section: "profile" },
];

export function MobileTabBar() {
  const { pathname } = useLocation();
  const activeSection = getRouteMetadata(pathname).section;

  return (
    <nav data-testid="compact-tab-bar" className="ea-bottom-nav" aria-label="Mobil navigatsiya">
      <div data-testid="compact-tab-surface" className="ea-bottom-nav__surface">
        {TABS.map((tab) => {
          const active = activeSection === tab.section;
          return (
            <NavLink
              key={tab.to}
              to={tab.to}
              end={tab.to === "/home"}
              onClick={() => tapLight()}
              aria-current={active ? "page" : undefined}
              className={cn("ea-bottom-nav__item", active && "is-active")}
            >
              <Icon name={tab.icon} filled={active} />
              <span>{tab.label}</span>
            </NavLink>
          );
        })}
      </div>
    </nav>
  );
}
