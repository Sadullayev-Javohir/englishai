import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "@/app/auth";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { getRouteMetadata, type NavSection } from "@/app/routeMetadata";
import { NotificationBell } from "@/components/NotificationBell";
import { ParrotLogo } from "@/components/ParrotLogo";
import { UserAvatar } from "@/components/UserAvatar";

interface SideItem {
  to: string;
  icon: string;
  label: string;
  section: NavSection;
}

const ITEMS: SideItem[] = [
  { to: "/home", icon: "home", label: uz.nav.home, section: "home" },
  { to: "/levels", icon: "map", label: uz.nav.levels, section: "levels" },
  { to: "/app/speaking", icon: "forum", label: uz.nav.speaking, section: "speaking" },
  { to: "/progress", icon: "trending_up", label: uz.nav.progress, section: "progress" },
  { to: "/leaderboard", icon: "trophy", label: uz.nav.leaderboard, section: "leaderboard" },
];

const COLLAPSE_KEY = "englishai:learner-sidebar-collapsed";

function initialCollapsed() {
  if (typeof window === "undefined") return false;
  return window.localStorage.getItem(COLLAPSE_KEY) === "true";
}

export function SideNav() {
  const { pathname } = useLocation();
  const { user } = useAuth();
  const [collapsed, setCollapsed] = useState(initialCollapsed);
  const activeSection = getRouteMetadata(pathname).section;
  const { data: adminAccess } = useAsync(() => api.admin.access(), []);
  const accountName = user?.username?.trim() || user?.displayName?.trim() || user?.email?.trim() || uz.nav.profile;
  const accountHandle = user?.username?.trim() ? `@${user.username.trim()}` : accountName;
  const accountInitials = accountName.slice(0, 2).toLocaleUpperCase("uz-UZ");

  useEffect(() => {
    document.documentElement.dataset.learnerSidebar = collapsed ? "collapsed" : "expanded";
    window.localStorage.setItem(COLLAPSE_KEY, String(collapsed));
    return () => { delete document.documentElement.dataset.learnerSidebar; };
  }, [collapsed]);

  const navItem = (item: SideItem) => (
    <NavLink
      key={item.to}
      to={item.to}
      end={item.to === "/home"}
      title={collapsed ? item.label : undefined}
      aria-current={activeSection === item.section ? "page" : undefined}
      className={cn("ea-learner-nav__item", activeSection === item.section && "is-active")}
    >
      <Icon name={item.icon} filled={activeSection === item.section} />
      <span>{item.label}</span>
    </NavLink>
  );

  const sidebar = (
    <aside data-testid="desktop-side-nav" className={cn("ea-learner-sidebar", collapsed && "is-collapsed")}>
      <div className="ea-learner-sidebar__brand">
        <NavLink to="/home" aria-label={uz.brand} className="ea-learner-sidebar__logo">
          <ParrotLogo
            size={42}
            className="ea-learner-sidebar__parrot-logo"
            withWordmark
            wordmarkClassName="ea-learner-sidebar__wordmark ea-sidebar-brand-wordmark"
          />
        </NavLink>
        <button
          type="button"
          className="ea-learner-sidebar__collapse"
          aria-label={collapsed ? "Menyuni kengaytirish" : "Menyuni ixchamlashtirish"}
          aria-pressed={collapsed}
          onClick={(event) => {
            setCollapsed((value) => !value);
            event.currentTarget.blur();
          }}
        >
          <Icon name="panel_left" />
        </button>
      </div>

      <nav className="ea-learner-nav" aria-label="Asosiy navigatsiya">
        {ITEMS.map(navItem)}
      </nav>

      <div className="ea-learner-sidebar__footer">
        {adminAccess?.canManageAdmins && navItem({ to: "/admin", icon: "shield_person", label: uz.nav.admin, section: "admin" })}
        <NotificationBell className="ea-learner-sidebar__notification" iconClassName="text-[21px]" />
        <NavLink
          to="/profile"
          className="ea-learner-profile"
          aria-label={`${uz.nav.sidebar.signedInAs}: ${accountName}`}
          title={collapsed ? accountHandle : undefined}
        >
          <UserAvatar pictureUrl={user?.pictureUrl} name={accountName || accountInitials} />
          <strong>{accountHandle}</strong>
          <Icon name="chevron_right" />
        </NavLink>
      </div>
    </aside>
  );

  return typeof document === "undefined" ? sidebar : createPortal(sidebar, document.body);
}
