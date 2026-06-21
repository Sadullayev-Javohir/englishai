import { SupportSenderKind, type SupportConversationDto, type SupportConversationSummaryDto, type SupportMessageDto } from "./types";

export function appendSupportMessage(
  conversation: SupportConversationDto | null,
  conversationId: string,
  message: SupportMessageDto,
): SupportConversationDto | null {
  if (!conversation || conversation.id !== conversationId) return conversation;
  if (conversation.messages.some((item) => item.id === message.id)) return conversation;
  return {
    ...conversation,
    updatedAt: message.createdAt,
    unreadCount: conversation.unreadCount + (message.senderKind === SupportSenderKind.Learner ? 1 : 0),
    messages: [...conversation.messages, message],
  };
}

export function applySupportRead(
  conversation: SupportConversationDto | null,
  conversationId: string,
  byAdmin: boolean,
): SupportConversationDto | null {
  if (!conversation || conversation.id !== conversationId) return conversation;
  const readSender = byAdmin ? SupportSenderKind.Learner : SupportSenderKind.Admin;
  return {
    ...conversation,
    unreadCount: byAdmin ? conversation.unreadCount : 0,
    messages: conversation.messages.map((message) =>
      message.senderKind === readSender ? { ...message, isRead: true } : message,
    ),
  };
}

export function upsertSupportSummary(
  items: SupportConversationSummaryDto[],
  conversation: SupportConversationDto,
): SupportConversationSummaryDto[] {
  const current = items.find((item) => item.id === conversation.id);
  const lastMessage = conversation.messages.at(-1);
  const next: SupportConversationSummaryDto = {
    id: conversation.id,
    learnerId: conversation.learnerId,
    learnerName: conversation.learnerName,
    learnerEmail: conversation.learnerEmail,
    learnerPictureUrl: conversation.learnerPictureUrl,
    assignedAdminId: conversation.assignedAdminId,
    assignedAdminName: conversation.assignedAdminName,
    status: conversation.status,
    updatedAt: conversation.updatedAt,
    unreadCount: conversation.unreadCount,
    lastMessagePreview: lastMessage?.text
      ?? (lastMessage?.attachments.length ? "Rasm yuborildi" : current?.lastMessagePreview ?? "Yangi suhbat"),
  };
  return [next, ...items.filter((item) => item.id !== conversation.id)];
}

export function updateSupportSummaryMessage(
  items: SupportConversationSummaryDto[],
  conversationId: string,
  message: SupportMessageDto,
): SupportConversationSummaryDto[] {
  return items
    .map((item) => item.id === conversationId ? {
      ...item,
      updatedAt: message.createdAt,
      unreadCount: item.unreadCount + (message.senderKind === SupportSenderKind.Learner ? 1 : 0),
      lastMessagePreview: message.text ?? (message.attachments.length ? "Rasm yuborildi" : item.lastMessagePreview),
    } : item)
    .sort((left, right) => Date.parse(right.updatedAt) - Date.parse(left.updatedAt));
}
