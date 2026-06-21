import { describe, expect, it } from "vitest";
import { SupportConversationStatus, SupportSenderKind, type SupportConversationDto, type SupportConversationSummaryDto, type SupportMessageDto } from "./types";
import { appendSupportMessage, applySupportRead, updateSupportSummaryMessage } from "./supportRealtime";

const message = (id: string, senderKind: SupportSenderKind): SupportMessageDto => ({
  id,
  senderId: "sender",
  senderKind,
  text: `message-${id}`,
  createdAt: `2026-08-08T12:00:0${id}Z`,
  isRead: false,
  attachments: [],
});

const conversation: SupportConversationDto = {
  id: "conversation",
  learnerId: "learner",
  learnerName: "Learner",
  learnerEmail: "learner@example.com",
  learnerPictureUrl: null,
  assignedAdminId: "admin",
  assignedAdminName: "Admin",
  status: SupportConversationStatus.Open,
  createdAt: "2026-08-08T12:00:00Z",
  updatedAt: "2026-08-08T12:00:00Z",
  closedAt: null,
  unreadCount: 0,
  messages: [],
};

describe("support realtime state", () => {
  it("appends a message immediately and ignores duplicate events", () => {
    const next = appendSupportMessage(conversation, conversation.id, message("1", SupportSenderKind.Learner));
    expect(next?.messages).toHaveLength(1);
    expect(next?.unreadCount).toBe(1);
    expect(appendSupportMessage(next, conversation.id, message("1", SupportSenderKind.Learner))).toBe(next);
  });

  it("marks the opposite sender messages as read without refetching", () => {
    const current = { ...conversation, messages: [message("1", SupportSenderKind.Learner), message("2", SupportSenderKind.Admin)] };
    const next = applySupportRead(current, conversation.id, true);
    expect(next?.messages[0].isRead).toBe(true);
    expect(next?.messages[1].isRead).toBe(false);
  });

  it("updates and reorders the admin queue from a message event", () => {
    const summary = (id: string, updatedAt: string): SupportConversationSummaryDto => ({
      id,
      learnerId: id,
      learnerName: "Learner",
      learnerEmail: "learner@example.com",
      learnerPictureUrl: null,
      assignedAdminId: "admin",
      assignedAdminName: "Admin",
      status: SupportConversationStatus.Open,
      updatedAt,
      unreadCount: 0,
      lastMessagePreview: "old",
    });
    const next = updateSupportSummaryMessage([
      summary(conversation.id, "2026-08-08T12:00:00Z"),
      summary("other", "2026-08-08T12:00:05Z"),
    ], conversation.id, message("9", SupportSenderKind.Learner));
    expect(next[0].id).toBe(conversation.id);
    expect(next[0].lastMessagePreview).toBe("message-9");
    expect(next[0].unreadCount).toBe(1);
  });
});
