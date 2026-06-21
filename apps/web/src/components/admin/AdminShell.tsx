import { useEffect, useRef, useState, type ReactNode } from "react";
import { Link, useLocation } from "react-router-dom";
import { uz } from "@/content/uz";
import { useAuth } from "@/app/auth";
import { Icon } from "@/components/ui/Icon";
import { ParrotLogo } from "@/components/ParrotLogo";
import { UserAvatar } from "@/components/UserAvatar";
import { BottomSheet } from "@/components/mobile/BottomSheet";
import { api } from "@/api/client";
import { NotificationsHubClient } from "@/api/notificationsHub";
import { NotificationBell } from "@/components/NotificationBell";
import { NotificationModal } from "@/components/NotificationModal";
import { bumpNotifications } from "@/lib/notificationsRefresh";
import { useAsync } from "@/lib/useAsync";
import "./AdminShell.css";
import "./adminTheme.css";

const ADMIN_NAV = [
  { to: "/admin", icon: "dashboard", label: "Boshqaruv" },
  { to: "/admin/metrics", icon: "trending_up", label: uz.admin.hub.metrics },
  { to: "/admin/users", icon: "group", label: uz.admin.hub.users },
  { to: "/admin/support", icon: "support_agent", label: "Support" },
  {
    to: "/admin/vocabulary",
    icon: "menu_book",
    label: uz.admin.hub.vocabulary,
  },
  { to: "/admin/grammar", icon: "rule", label: uz.admin.hub.grammar },
  { to: "/admin/listening", icon: "headphones", label: uz.admin.hub.listening },
  { to: "/admin/reading", icon: "auto_stories", label: uz.admin.hub.reading },
  {
    to: "/admin/notifications",
    icon: "campaign",
    label: uz.admin.hub.notifications,
    superOnly: true,
  },
  {
    to: "/admin/sections",
    icon: "science",
    label: uz.admin.hub.sections,
    superOnly: true,
  },
  {
    to: "/admin/server",
    icon: "monitor_heart",
    label: uz.admin.hub.server,
    superOnly: true,
  },
];

const PRIMARY_MOBILE_NAV_ROUTES = new Set([
  "/admin",
  "/admin/metrics",
  "/admin/users",
  "/admin/vocabulary",
]);

function isAdminNavItemActive(pathname: string, to: string) {
  return to === "/admin" ? pathname === to : pathname.startsWith(to);
}

export function AdminShell({
  variant = "content",
  children,
}: {
  variant?: "hub" | "content";
  children: ReactNode;
}) {
  const location = useLocation();
  const isHub = variant === "hub";
  const [moreNavOpen, setMoreNavOpen] = useState(false);
  useEffect(() => {
    const client = new NotificationsHubClient({
      onBroadcast: () => bumpNotifications(),
      onReconnected: () => bumpNotifications(),
    });
    void client.start().catch(() => {});
    return () => { if (!import.meta.env.DEV) void client.stop(); };
  }, []);
  const { data: adminAccess } = useAsync(() => api.admin.access(), []);
  const visibleNav = ADMIN_NAV.filter(
    (item) => !item.superOnly || adminAccess?.canManageAdmins
  );
  const primaryMobileNav = visibleNav.filter((item) =>
    PRIMARY_MOBILE_NAV_ROUTES.has(item.to)
  );
  const secondaryMobileNav = visibleNav.filter(
    (item) => !PRIMARY_MOBILE_NAV_ROUTES.has(item.to)
  );
  const moreNavActive = secondaryMobileNav.some((item) =>
    isAdminNavItemActive(location.pathname, item.to)
  );

  return (
    <div className={`ea-admin-shell ea-admin-unified-scope ea-admin-shell--${variant}`} data-design-system="englishai-play">
      {isHub && (
        <aside
          className="ea-admin-shell__sidebar"
          aria-label="Admin navigatsiyasi"
        >
          <Link
            to="/home"
            className="ea-admin-shell__brand"
            aria-label={uz.admin.backToApp}
          >
            <ParrotLogo
              size={54}
              imgSize={50}
              rounded="14px"
              withWordmark
            />
          </Link>

          <nav className="ea-admin-shell__nav">
            {visibleNav.map((item) => {
              const active = isAdminNavItemActive(location.pathname, item.to);
              return (
                <Link
                  key={item.to}
                  to={item.to}
                  className={
                    active
                      ? "ea-admin-shell__nav-link is-active"
                      : "ea-admin-shell__nav-link"
                  }
                  aria-current={active ? "page" : undefined}
                >
                  <Icon name={item.icon} filled={active} />
                  <span>{item.label}</span>
                </Link>
              );
            })}
          </nav>

          <Link to="/home" className="ea-admin-shell__back">
            <Icon name="arrow_back" />
            <span>{uz.admin.backToApp}</span>
          </Link>
        </aside>
      )}

      <div
        className={
          isHub
            ? "ea-admin-shell__workspace"
            : "ea-admin-shell__workspace ea-admin-shell__workspace--content"
        }
      >
        {isHub && <AdminTopBar />}
        <main className="ea-admin-shell__main">{children}</main>
        {isHub && (
          <nav
            className="ea-admin-shell__mobile-nav"
            aria-label="Mobil admin navigatsiyasi"
          >
            {primaryMobileNav.map((item) => {
              const active = isAdminNavItemActive(location.pathname, item.to);
              return (
                <Link
                  key={item.to}
                  to={item.to}
                  className={active ? "is-active" : undefined}
                  aria-current={active ? "page" : undefined}
                >
                  <Icon name={item.icon} filled={active} />
                  <span>{item.label}</span>
                </Link>
              );
            })}
            <button
              type="button"
              className={moreNavActive ? "is-active" : undefined}
              aria-haspopup="dialog"
              aria-expanded={moreNavOpen}
              onClick={() => setMoreNavOpen(true)}
            >
              <Icon name="more_horiz" filled={moreNavActive} />
              <span>Boshqa</span>
            </button>
          </nav>
        )}
      </div>
      {isHub && (
        <BottomSheet
          open={moreNavOpen}
          onClose={() => setMoreNavOpen(false)}
          title="Boshqa admin bo‘limlari"
          className="ea-admin-shell__more-sheet"
        >
          <nav
            className="ea-admin-shell__more-nav"
            aria-label="Qo‘shimcha admin navigatsiyasi"
          >
            {secondaryMobileNav.map((item) => {
              const active = isAdminNavItemActive(location.pathname, item.to);
              return (
                <Link
                  key={item.to}
                  to={item.to}
                  className={active ? "is-active" : undefined}
                  aria-current={active ? "page" : undefined}
                  onClick={() => setMoreNavOpen(false)}
                >
                  <i aria-hidden="true">
                    <Icon name={item.icon} filled={active} />
                  </i>
                  <span>{item.label}</span>
                  <Icon name="chevron_right" />
                </Link>
              );
            })}
          </nav>
        </BottomSheet>
      )}
      <NotificationModal />
    </div>
  );
}

function AdminTopBar() {
  const { user, signOut } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const menuButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!menuOpen) return;
    const onDown = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node))
        setMenuOpen(false);
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setMenuOpen(false);
        menuButtonRef.current?.focus();
      }
    };
    const focusMenu = window.requestAnimationFrame(() => {
      menuRef.current?.querySelector<HTMLElement>("[role='menuitem']")?.focus();
    });
    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      window.cancelAnimationFrame(focusMenu);
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [menuOpen]);

  const onSignOut = async () => {
    setMenuOpen(false);
    await signOut();
  };

  return (
    <header className="ea-admin-shell__topbar">
      <div className="ea-admin-shell__topbar-copy">
        <span>Operator paneli</span>
        <strong>{uz.admin.title}</strong>
      </div>
      <div className="ea-admin-shell__profile" ref={menuRef}>
        <NotificationBell />

        <button
          ref={menuButtonRef}
          type="button"
          onClick={() => setMenuOpen((open) => !open)}
          aria-label={uz.nav.profile}
          aria-haspopup="menu"
          aria-expanded={menuOpen}
        >
          <UserAvatar pictureUrl={user?.pictureUrl} name={user?.displayName || user?.email} className="ea-admin-shell__avatar" />
          <span className="ea-admin-shell__profile-copy">
            <strong>{user?.displayName || "Administrator"}</strong>
            <small>{user?.email || "Admin hisob"}</small>
          </span>
          <Icon name="expand_more" />
        </button>

        {menuOpen && (
          <div role="menu" className="ea-admin-shell__menu">
            <Link to="/home" role="menuitem" onClick={() => setMenuOpen(false)}>
              <Icon name="arrow_back" />
              {uz.admin.backToApp}
            </Link>
            <button type="button" role="menuitem" onClick={onSignOut}>
              <Icon name="logout" />
              {uz.logout}
            </button>
          </div>
        )}
      </div>
    </header>
  );
}
