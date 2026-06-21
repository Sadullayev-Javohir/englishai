import { describe, expect, it } from "vitest";
import { SupportSenderKind, type SupportMessageDto } from "@/api/types";
import { incomingSupportNotification } from "./supportNotifications";

const message = (senderKind: SupportSenderKind, text: string | null): SupportMessageDto => ({
  id: "message-1",
  senderId: "sender-1",
  senderKind,
  text,
  createdAt: "2026-08-10T10:00:00Z",
  isRead: false,
  attachments: [],
});

describe("incomingSupportNotification", () => {
  it("notifies an admin when a learner sends a message", () => {
    expect(incomingSupportNotification(message(SupportSenderKind.Learner, "Yordam kerak"), SupportSenderKind.Admin)).toEqual({
      title: "Yangi support xabari",
      message: "Yordam kerak",
    });
  });

  it("notifies a learner when an admin replies", () => {
    expect(incomingSupportNotification(message(SupportSenderKind.Admin, "Tekshirib berdik"), SupportSenderKind.Learner)).toEqual({
      title: "Support javob berdi",
      message: "Tekshirib berdik",
    });
  });

  it("does not notify the sender for their own message", () => {
    expect(incomingSupportNotification(message(SupportSenderKind.Learner, "Salom"), SupportSenderKind.Learner)).toBeNull();
    expect(incomingSupportNotification(message(SupportSenderKind.Admin, "Salom"), SupportSenderKind.Admin)).toBeNull();
  });
});
