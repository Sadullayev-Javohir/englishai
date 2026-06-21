import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import { tapLight } from "@/lib/haptics";
import { useNotificationsModal } from "@/lib/useNotificationsModal";
import { useUnreadNotifications } from "@/lib/useUnreadNotifications";
import { Bell } from "lucide-react";

interface NotificationBellProps {
  variant?: "default" | "home";
  className?: string;
  iconClassName?: string;
  badgeClassName?: string;
}

export function NotificationBell({
  variant = "default",
  className = "",
  iconClassName = "text-[22px]",
  badgeClassName = "-top-1.5 -right-1.5 min-w-[18px] h-[18px] leading-[18px] ring-2 ring-ea-green-600",
}: NotificationBellProps) {
  const unread = useUnreadNotifications();
  const hasUnread = unread > 0;
  const { openNotifications } = useNotificationsModal();
  if (variant === "home") {
    return <button type="button" onClick={() => { tapLight(); openNotifications(); }} aria-label={uz.nav.notifications} data-home-notifications data-has-unread={hasUnread} className={`relative flex min-h-[49px] items-center gap-[6px] rounded-[14px] bg-ea-primary-soft px-[10px] py-[9px] text-ea-primary min-[1200px]:h-11 min-[1200px]:min-h-0 min-[1200px]:w-11 min-[1200px]:justify-center min-[1200px]:p-0 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-ea-primary ${className}`}>
      <Bell size={20} aria-hidden />
      {hasUnread && <span className="rounded-[20px] bg-ea-primary px-[11px] py-[7px] text-[11px] font-extrabold leading-[1.5] text-ea-on-primary min-[1200px]:absolute min-[1200px]:left-[22px] min-[1200px]:top-0 min-[1200px]:grid min-[1200px]:h-[31px] min-[1200px]:min-w-[29px] min-[1200px]:place-items-center min-[1200px]:rounded-[10px] min-[1200px]:px-[11px] min-[1200px]:py-0 min-[1200px]:text-[10px] min-[1200px]:leading-none">{unread > 99 ? "99+" : unread}</span>}
    </button>;
  }

  return (
    <button
      type="button"
      onClick={() => {
        tapLight();
        openNotifications();
      }}
      aria-label={uz.nav.notifications}
      data-has-unread={hasUnread}
      className={`relative flex w-10 h-10 items-center justify-center rounded-2xl ring-2 transition-all duration-150 ${hasUnread ? "!bg-error !text-white !ring-error" : "!bg-[var(--ea-surface)] !text-[var(--ea-text)] !ring-[var(--ea-border)]"} ${className}`}
    >
      <Icon name="notifications" filled className={iconClassName} />
      {hasUnread && (
        <span className={`absolute rounded-full bg-error px-1 text-center font-duo text-[10px] font-extrabold text-white ${badgeClassName}`}>
          {unread > 9 ? "9+" : unread}
        </span>
      )}
    </button>
  );
}
