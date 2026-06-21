import { SupportSenderKind, type SupportMessageDto } from "@/api/types";

export interface SupportIncomingNotification {
  title: string;
  message: string;
}

export function incomingSupportNotification(
  message: SupportMessageDto,
  recipient: SupportSenderKind,
): SupportIncomingNotification | null {
  if (message.senderKind === recipient) return null;

  return recipient === SupportSenderKind.Admin
    ? {
        title: "Yangi support xabari",
        message: message.text?.trim() || "Foydalanuvchi rasm yubordi.",
      }
    : {
        title: "Support javob berdi",
        message: message.text?.trim() || "Support xodimi rasm yubordi.",
      };
}

export function showSystemSupportNotification(notification: SupportIncomingNotification) {
  if (typeof window === "undefined" || !("Notification" in window)) return;
  if (window.Notification.permission !== "granted") return;
  if (document.visibilityState === "visible") return;

  new window.Notification(notification.title, {
    body: notification.message,
    icon: "/icons/icon-192.webp",
    tag: "englishai-support-message",
  });
}
