import { useCallback, useEffect } from "react";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { loadNotificationsFeed, useNotificationsFeedSnapshot } from "@/lib/notificationsFeed";
import { useNotificationsRefreshKey } from "@/lib/notificationsRefresh";

export function useNotificationsFeed() {
  const learnerId = getLearnerId();
  const refreshKey = useNotificationsRefreshKey();
  const snapshot = useNotificationsFeedSnapshot();

  const reload = useCallback(() => {
    void loadNotificationsFeed(
      learnerId,
      () => api.vocabulary.notifications(learnerId),
      true,
    ).catch(() => {});
  }, [learnerId]);

  useEffect(() => {
    void loadNotificationsFeed(
      learnerId,
      () => api.vocabulary.notifications(learnerId),
      true,
    ).catch(() => {});
  }, [learnerId, refreshKey]);

  return {
    data: snapshot.learnerId === learnerId ? snapshot.data : null,
    loading: snapshot.learnerId === learnerId ? snapshot.loading : true,
    error: snapshot.learnerId === learnerId ? snapshot.error : null,
    reload,
  };
}
