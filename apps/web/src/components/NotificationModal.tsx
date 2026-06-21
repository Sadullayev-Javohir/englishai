import { useState } from "react";
import {
  Bell,
  BookOpen,
  CheckCheck,
  Flame,
  MessagesSquare,
  Target,
  X,
  type LucideIcon,
} from "lucide-react";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { DesignModal } from "@/components/design";
import { Spinner } from "@/components/ui/Spinner";
import { uz } from "@/content/uz";
import { tapLight } from "@/lib/haptics";
import { formatNotificationTime } from "@/lib/labels";
import { useNotificationsModal } from "@/lib/useNotificationsModal";
import { useNotificationsFeed } from "@/lib/useNotificationsFeed";
import { openExternalUrl } from "@/lib/openExternal";
import { resolveNotificationTarget } from "@/lib/notificationTarget";
import type { NotificationDto } from "@/api/types";
import { useNavigate } from "react-router-dom";

type NotificationFilter = "all" | "unread";

function notificationIcon(code: string): LucideIcon {
  if (code.includes("support.message")) return MessagesSquare;
  if (code.includes("streak")) return Flame;
  if (code.includes("daily_goal_done") || code.includes("daily_plan"))
    return Target;
  return BookOpen;
}

function notificationTitle(notification: NotificationDto): string {
  return notification.title?.trim() || notification.message;
}

export function NotificationModal() {
  const { isOpen, closeNotifications } = useNotificationsModal();
  if (!isOpen) return null;
  return <NotificationModalContent onClose={closeNotifications} />;
}

function NotificationModalContent({ onClose }: { onClose: () => void }) {
  const navigate = useNavigate();
  const { data, loading, reload } = useNotificationsFeed();
  const learnerId = getLearnerId();
  const [filter, setFilter] = useState<NotificationFilter>("all");
  const notifications = data ?? [];
  const unreadCount = notifications.filter(
    (notification) => !notification.isRead
  ).length;
  const visible =
    filter === "unread"
      ? notifications.filter((notification) => !notification.isRead)
      : notifications;

  async function dismiss(id: string) {
    await api.vocabulary.dismissNotification(learnerId, id).catch(() => {});
  }

  async function handleCardClick(notification: NotificationDto) {
    tapLight();
    await dismiss(notification.id);
    const resolved = notification.linkUrl
      ? resolveNotificationTarget(notification.linkUrl)
      : null;
    if (resolved?.kind === "internal") {
      onClose();
      navigate(resolved.path);
      return;
    }
    if (resolved?.kind === "external") {
      onClose();
      openExternalUrl(resolved.url);
      return;
    }
    reload();
  }

  async function markAllRead() {
    tapLight();
    await api.vocabulary.markRead(learnerId);
    setFilter("all");
    reload();
  }

  return (
    <DesignModal
      open
      onClose={onClose}
      title={uz.notifications.title}
      closeLabel={uz.notifications.close}
      className="notification-modal"
      panelProps={
        {
          "data-testid": "notification-modal",
        } as React.HTMLAttributes<HTMLElement>
      }
    >
      <div className="notification-modal__content">
        <header className="notification-modal__header">
          <span className="notification-modal__title-icon" aria-hidden="true">
            <Bell size={22} />
          </span>
          <div className="notification-modal__title-copy">
            <h2>Bildirishnomalar</h2>
            <p>
              {unreadCount > 0
                ? `${unreadCount} ta yangi xabaringiz bor`
                : "Yangi xabarlaringiz shu yerda ko‘rinadi"}
            </p>
          </div>
          <button
            type="button"
            className="notification-modal__close"
            aria-label={uz.notifications.close}
            onClick={onClose}
          >
            <X size={20} aria-hidden="true" />
          </button>
        </header>

        <div
          className="notification-modal__filters"
          aria-label="Bildirishnoma filtri"
        >
          <button
            type="button"
            className={filter === "all" ? "is-active" : ""}
            aria-pressed={filter === "all"}
            onClick={() => setFilter("all")}
          >
            Barchasi
          </button>
          <button
            type="button"
            className={filter === "unread" ? "is-active" : ""}
            aria-pressed={filter === "unread"}
            onClick={() => setFilter("unread")}
          >
            O‘qilmagan · {unreadCount}
          </button>
        </div>

        <button
          type="button"
          className="notification-modal__mark-read"
          onClick={markAllRead}
          disabled={unreadCount === 0}
        >
          <CheckCheck size={17} aria-hidden="true" />
          <span>Barchasini o‘qilgan deb belgilash</span>
        </button>

        {loading && !data ? (
          <div
            className="notification-modal__loading"
            role="status"
            aria-label="Yuklanmoqda"
          >
            <Spinner />
          </div>
        ) : visible.length === 0 ? (
          <div className="notification-modal__empty">
            <Bell size={26} aria-hidden="true" />
            <strong>
              {filter === "unread"
                ? "O‘qilmagan xabarlar yo‘q"
                : uz.notifications.empty}
            </strong>
            <p>
              {filter === "unread"
                ? "Barcha bildirishnomalar o‘qilgan."
                : "Yangi xabar kelganda shu yerda ko‘rinadi."}
            </p>
          </div>
        ) : (
          <section
            className="notification-modal__list"
            aria-label="Bildirishnomalar ro‘yxati"
          >
            <p className="notification-modal__section-label">BUGUN</p>
            {visible.map((notification) => {
              const CardIcon = notificationIcon(notification.code);
              const title = notificationTitle(notification);
              return (
                <article
                  key={notification.id}
                  className={`notification-modal__item${
                    notification.isRead ? " is-read" : " is-unread"
                  }`}
                >
                  <button
                    type="button"
                    className="notification-modal__item-button"
                    onClick={() => void handleCardClick(notification)}
                    aria-label={title}
                  >
                    <span
                      className="notification-modal__item-icon"
                      aria-hidden="true"
                    >
                      <CardIcon size={20} />
                    </span>
                    <span className="notification-modal__item-copy">
                      <strong>{title}</strong>
                      {notification.title && (
                        <span>{notification.message}</span>
                      )}
                      <time dateTime={notification.createdAt}>
                        {formatNotificationTime(notification.createdAt)}
                      </time>
                    </span>
                    {!notification.isRead && (
                      <span
                        className="notification-modal__unread-dot"
                        aria-label="O‘qilmagan"
                      />
                    )}
                  </button>
                </article>
              );
            })}
          </section>
        )}
      </div>
    </DesignModal>
  );
}
