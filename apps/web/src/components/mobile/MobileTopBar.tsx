import { Link, useLocation } from "react-router-dom";
import { uz } from "@/content/uz";
import { NotificationBell } from "@/components/NotificationBell";
import { ParrotLogo } from "@/components/ParrotLogo";
import { getRouteMetadata } from "@/app/routeMetadata";
import { useAuth } from "@/app/auth";
import { UserAvatar } from "@/components/UserAvatar";

export function MobileTopBar() {
  const { user } = useAuth();
  const { pathname } = useLocation();
  const metadata = getRouteMetadata(pathname);

  return (
    <div data-testid="compact-top-bar" className="ea-compact-topbar">
      <Link to="/home" aria-label={uz.brand} className="ea-compact-topbar__brand">
        <ParrotLogo
          size={38}
          withWordmark
          className="ea-compact-topbar__logo"
          wordmarkClassName="ea-compact-topbar__wordmark"
        />
      </Link>
      <div className="ea-compact-topbar__route" aria-live="polite">
        <span>{metadata.title}</span>
      </div>
      <div className="ea-compact-topbar__actions">
        <NotificationBell className="ea-compact-topbar__notification" iconClassName="text-[20px]" badgeClassName="-top-1 -right-1 min-w-[17px] h-[17px] leading-[17px] ring-2 ring-white" />
        <Link to="/profile" aria-label={uz.nav.profile} className="ea-compact-topbar__profile">
          <UserAvatar pictureUrl={user?.pictureUrl} name={user?.displayName || user?.email} className="h-full w-full rounded-[inherit]" fallbackClassName="font-black" />
        </Link>
      </div>
    </div>
  );
}
