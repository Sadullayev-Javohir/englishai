import { useEffect } from "react";

export function SupportNotificationPermission() {
  useEffect(() => {
    if (!("Notification" in window) || window.Notification.permission !== "default") return;

    void window.Notification.requestPermission();
  }, []);

  return null;
}
