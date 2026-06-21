import { useCallback } from "react";
import { useSearchParams } from "react-router-dom";

const NOTIFICATIONS_PARAM = "notifications";
const OPEN_VALUE = "open";

export function useNotificationsModal() {
  const [searchParams, setSearchParams] = useSearchParams();
  const isOpen = searchParams.get(NOTIFICATIONS_PARAM) === OPEN_VALUE;

  const openNotifications = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current);
      next.set(NOTIFICATIONS_PARAM, OPEN_VALUE);
      return next;
    });
  }, [setSearchParams]);

  const closeNotifications = useCallback(() => {
    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);
        next.delete(NOTIFICATIONS_PARAM);
        return next;
      },
      { replace: true },
    );
  }, [setSearchParams]);

  return { isOpen, openNotifications, closeNotifications };
}
