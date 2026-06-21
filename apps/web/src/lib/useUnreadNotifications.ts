import { useNotificationsFeed } from "@/lib/useNotificationsFeed";

/**
 * Number of unread in-app notifications, for the bell badge. Re-fetches on every route change so
 * the badge clears the moment the learner leaves the Notifications screen after "Hammasini o'qish".
 */
export function useUnreadNotifications(): number {
  const { data } = useNotificationsFeed();
  return data ? data.filter((n) => !n.isRead).length : 0;
}
