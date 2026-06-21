import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { createRef } from "react";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { Composer, formatSupportMessageTime, MessageList } from "./SupportChatPage";
import { SupportConversationStatus, SupportSenderKind, type SupportConversationDto } from "@/api/types";

describe("Support message time", () => {
  it("formats message time as HH:mm:ss", () => {
    expect(formatSupportMessageTime(new Date(2026, 7, 8, 9, 7, 5))).toBe("09:07:05");
  });

  it("keeps HH:mm:ss for older messages", () => {
    expect(formatSupportMessageTime(new Date(2026, 7, 8, 23, 59, 4))).toBe("23:59:04");
  });
});

describe("Support composer", () => {
  beforeAll(() => { URL.createObjectURL = vi.fn(() => "blob:preview"); });
  afterEach(cleanup);
  it("allows a text-only support message", () => {
    const send = vi.fn();
    render(<Composer text="Muammo bor" files={[]} busy={false} error="" inputRef={createRef<HTMLInputElement>()} onText={vi.fn()} onChoose={vi.fn()} onRemove={vi.fn()} onSend={send} />);
    fireEvent.click(screen.getByRole("button", { name: "Xabar yuborish" }));
    expect(send).toHaveBeenCalledOnce();
  });

  it("allows an image-only support message", () => {
    const send = vi.fn();
    const file = new File(["image"], "screen.png", { type: "image/png" });
    render(<Composer text="" files={[file]} busy={false} error="" inputRef={createRef<HTMLInputElement>()} onText={vi.fn()} onChoose={vi.fn()} onRemove={vi.fn()} onSend={send} />);
    fireEvent.click(screen.getByRole("button", { name: "Xabar yuborish" }));
    expect(send).toHaveBeenCalledOnce();
  });

  it("opens the native image input from the attachment control", () => {
    render(<Composer text="" files={[]} busy={false} error="" inputRef={createRef<HTMLInputElement>()} onText={vi.fn()} onChoose={vi.fn()} onRemove={vi.fn()} onSend={vi.fn()} />);
    const attachment = screen.getByLabelText("Rasm biriktirish");
    expect(attachment.getAttribute("for")).toBe("support-image-input");
    expect(document.getElementById("support-image-input")).toHaveProperty("type", "file");
  });

  it("passes selected images to the composer", () => {
    const choose = vi.fn();
    const file = new File(["image"], "screen.png", { type: "image/png" });
    render(<Composer text="" files={[]} busy={false} error="" inputRef={createRef<HTMLInputElement>()} onText={vi.fn()} onChoose={choose} onRemove={vi.fn()} onSend={vi.fn()} />);
    fireEvent.change(document.getElementById("support-image-input")!, { target: { files: [file] } });
    expect(choose).toHaveBeenCalledOnce();
    expect(choose.mock.calls[0][0][0]).toBe(file);
  });
});

describe("Support delivery status", () => {
  afterEach(cleanup);

  const conversation = (isRead: boolean): SupportConversationDto => ({
    id: "conversation", learnerId: "learner", learnerName: "Learner", learnerEmail: "learner@example.com",
    learnerPictureUrl: null, assignedAdminId: "admin", assignedAdminName: "Admin",
    status: SupportConversationStatus.Open, createdAt: "2026-08-08T09:00:00Z", updatedAt: "2026-08-08T09:00:00Z",
    closedAt: null, unreadCount: 0, messages: [{ id: "message", senderId: "learner",
      senderKind: SupportSenderKind.Learner, text: "Salom", createdAt: "2026-08-08T09:00:00Z",
      isRead, attachments: [] }],
  });

  it("shows sent status before the other side reads", () => {
    render(<MessageList conversation={conversation(false)} mine={SupportSenderKind.Learner} typing={false} onImage={vi.fn()} endRef={createRef<HTMLDivElement>()} />);
    expect(screen.getByLabelText("Yuborildi")).toBeTruthy();
  });

  it("shows read status after the other side reads", () => {
    render(<MessageList conversation={conversation(true)} mine={SupportSenderKind.Learner} typing={false} onImage={vi.fn()} endRef={createRef<HTMLDivElement>()} />);
    expect(screen.getByLabelText("O‘qildi")).toBeTruthy();
  });
});
