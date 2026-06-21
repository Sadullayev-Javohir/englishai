import { useEffect, useRef, useState } from "react";
import { api } from "@/api/client";
import { SupportHubClient } from "@/api/supportHub";
import { appendSupportMessage, applySupportRead } from "@/api/supportRealtime";
import { SupportSenderKind, type SupportConversationDto, type SupportMessageDto } from "@/api/types";
import { DesignToast } from "@/components/design";
import { SupportNotificationPermission } from "@/components/SupportNotificationPermission";
import { Icon } from "@/components/ui/Icon";
import { incomingSupportNotification, showSystemSupportNotification, type SupportIncomingNotification } from "@/lib/supportNotifications";
import "./SupportChat.css";

const MAX_FILES = 4;
const MAX_BYTES = 10 * 1024 * 1024;

const padTimePart = (value: number) => String(value).padStart(2, "0");

export function formatSupportMessageTime(createdAt: string | Date) {
  const created = new Date(createdAt);
  return `${padTimePart(created.getHours())}:${padTimePart(created.getMinutes())}:${padTimePart(created.getSeconds())}`;
}

export function SupportChatPage() {
  const [conversation, setConversation] = useState<SupportConversationDto | null>(null);
  const [text, setText] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [busy, setBusy] = useState(false);
  const [typing, setTyping] = useState(false);
  const [live, setLive] = useState(false);
  const [error, setError] = useState("");
  const [lightbox, setLightbox] = useState<string | null>(null);
  const [notification, setNotification] = useState<SupportIncomingNotification | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const endRef = useRef<HTMLDivElement>(null);
  const hubRef = useRef<SupportHubClient | null>(null);
  const conversationIdRef = useRef<string | null>(null);
  const notifiedMessageIds = useRef(new Set<string>());
  const typingTimer = useRef<number>();

  const refresh = async () => {
    const next = await api.support.conversation();
    conversationIdRef.current = next.id;
    setConversation(next);
    return next;
  };

  const joinAndMarkRead = async (hub: SupportHubClient, next: SupportConversationDto) => {
    await hub.join(next.id, false);
    await api.support.markRead(next.id);
  };

  useEffect(() => {
    let active = true;
    let connected = false;
    let startTimer: number | undefined;
    const hub = new SupportHubClient({
      onMessage: (payload) => {
        if (!active) return;
        const message = "conversationId" in payload ? payload.message : payload;
        const incoming = incomingSupportNotification(message, SupportSenderKind.Learner);
        if (incoming && !notifiedMessageIds.current.has(message.id)) {
          notifiedMessageIds.current.add(message.id);
          setNotification(incoming);
          showSystemSupportNotification(incoming);
        }
        setConversation((current) => appendSupportMessage(
          current,
          "conversationId" in payload ? payload.conversationId : current?.id ?? "",
          message,
        ));
        if (message.senderKind === SupportSenderKind.Admin) {
          const conversationId = "conversationId" in payload ? payload.conversationId : conversationIdRef.current;
          if (conversationId) void api.support.markRead(conversationId);
        }
      },
      onConversation: (next) => { if (active) setConversation(next); },
      onTyping: (payload) => { if (active && payload.admin) setTyping(payload.typing); },
      onRead: (payload) => { if (active) setConversation((current) => applySupportRead(current, payload.conversationId, payload.byAdmin)); },
      onReconnecting: () => { if (active) setLive(false); },
      onReconnected: async () => { if (active) setLive(true); const next = await refresh(); await joinAndMarkRead(hub, next); },
      onClosed: () => { if (active) setLive(false); },
    });
    hubRef.current = hub;
    void refresh().then((next) => {
      if (!active) return;
      startTimer = window.setTimeout(() => {
        if (!active) return;
        void hub.start().then(async () => {
          if (!active) return;
          connected = true;
          await joinAndMarkRead(hub, next);
          if (active) setLive(true);
        }).catch(() => { if (active) setError("Support suhbatini yuklab bo‘lmadi."); });
      }, 500);
    }).catch(() => { if (active) setError("Support suhbatini yuklab bo‘lmadi."); });
    return () => {
      active = false;
      window.clearTimeout(startTimer);
      window.clearTimeout(typingTimer.current);
      if (connected && !import.meta.env.DEV) void hub.stop();
    };
  }, []);

  useEffect(() => { endRef.current?.scrollIntoView({ behavior: "smooth" }); }, [conversation?.messages.length, typing]);

  const chooseFiles = (selected: FileList | null) => {
    if (!selected) return;
    const next = [...files, ...Array.from(selected)].slice(0, MAX_FILES);
    const invalid = next.find((file) => !["image/png", "image/jpeg", "image/webp", "image/gif"].includes(file.type) || file.size > MAX_BYTES);
    if (invalid) { setError("Faqat PNG, JPEG, WebP yoki GIF, har biri 10 MB gacha."); return; }
    setError(""); setFiles(next);
  };

  const send = async () => {
    if ((!text.trim() && files.length === 0) || busy) return;
    setBusy(true); setError("");
    try {
      const message = await api.support.send(text, files);
      setConversation((current) => current ? appendSupportMessage(current, current.id, message) : current);
      setText(""); setFiles([]);
    }
    catch { setError("Xabar yuborilmadi. Qayta urinib ko‘ring."); }
    finally { setBusy(false); }
  };

  const onText = (value: string) => {
    setText(value);
    if (!conversation) return;
    void hubRef.current?.typing(conversation.id, false, true);
    window.clearTimeout(typingTimer.current);
    typingTimer.current = window.setTimeout(() => void hubRef.current?.typing(conversation.id, false, false), 1200);
  };

  return <div className="ea-support-page"><SupportNotificationPermission /><div className="ea-support-shell"><div className="ea-support-chat" data-live={live}>
    <MessageList conversation={conversation} mine={SupportSenderKind.Learner} typing={typing} onImage={setLightbox} endRef={endRef} />
    <Composer text={text} files={files} busy={busy} error={error} inputRef={inputRef} onText={onText} onChoose={chooseFiles} onRemove={(index) => setFiles((items) => items.filter((_, i) => i !== index))} onSend={send} />
  </div></div>{lightbox && <button type="button" className="ea-support-lightbox" onClick={() => setLightbox(null)}><img src={lightbox} alt="Support rasmi" /></button>}<DesignToast open={notification !== null} title={notification?.title ?? ""} message={notification?.message} onClose={() => setNotification(null)} /></div>;
}

export default SupportChatPage;

export function MessageList({ conversation, mine, typing, onImage, endRef }: { conversation: SupportConversationDto | null; mine: SupportSenderKind; typing: boolean; onImage: (url: string) => void; endRef: React.RefObject<HTMLDivElement> }) {
  return <div className="ea-support-messages">{!conversation?.messages.length && <div className="ea-support-empty"><div><Icon name="forum" /><strong>Support bilan suhbatni boshlang</strong><span>Matn yozing yoki muammo screenshotini yuboring.</span></div></div>}{conversation?.messages.map((message) => <MessageBubble key={message.id} message={message} mine={message.senderKind === mine} onImage={onImage} />)}{typing && <div className="ea-support-typing">Javob yozilmoqda...</div>}<div ref={endRef} /></div>;
}

function MessageBubble({ message, mine, onImage }: { message: SupportMessageDto; mine: boolean; onImage: (url: string) => void }) {
  const hasImages = message.attachments.length > 0;
  const hasText = Boolean(message.text);
  const deliveryLabel = message.isRead ? "O‘qildi" : "Yuborildi";
  return <article className={`ea-support-message ${mine ? "is-mine" : "is-theirs"}`}><div className={`ea-support-message-card ${hasImages && hasText ? "has-image-and-text" : ""}`}>{hasImages && <div className="ea-support-images">{message.attachments.map((image) => <button type="button" key={image.id} onClick={() => onImage(image.url)}><img src={image.url} alt={image.fileName} /></button>)}</div>}{hasText && <div className="ea-support-bubble"><p>{message.text}</p></div>}</div><div className="ea-support-meta"><span>{formatSupportMessageTime(message.createdAt)}</span>{mine && <span className={`ea-support-delivery ${message.isRead ? "is-read" : ""}`} title={deliveryLabel} aria-label={deliveryLabel}><Icon name={message.isRead ? "done_all" : "done"} /><span>{deliveryLabel}</span></span>}</div></article>;
}

export function Composer({ text, files, busy, error, inputRef, onText, onChoose, onRemove, onSend }: { text: string; files: File[]; busy: boolean; error: string; inputRef: React.RefObject<HTMLInputElement>; onText: (value: string) => void; onChoose: (files: FileList | null) => void; onRemove: (index: number) => void; onSend: () => void }) {
  const attachmentDisabled = files.length >= MAX_FILES || busy;
  return <footer className="ea-support-composer">{files.length > 0 && <div className="ea-support-previews">{files.map((file, index) => <div className="ea-support-preview" key={`${file.name}-${index}`}><img src={URL.createObjectURL(file)} alt={file.name} /><button type="button" className="ea-support-preview__remove" onClick={() => onRemove(index)} aria-label={`${file.name} rasmini olib tashlash`}><Icon name="close" /></button></div>)}</div>}<div className="ea-support-compose-row"><input id="support-image-input" ref={inputRef} type="file" multiple accept="image/png,image/jpeg,image/webp,image/gif" className="sr-only" disabled={attachmentDisabled} onChange={(event) => { onChoose(event.target.files); event.target.value = ""; }} /><label htmlFor="support-image-input" className={`ea-support-attach ${attachmentDisabled ? "is-disabled" : ""}`} aria-label="Rasm biriktirish" aria-disabled={attachmentDisabled}><Icon name="add_photo_alternate" /></label><textarea value={text} disabled={busy} maxLength={8000} rows={1} placeholder="Xabaringizni yozing..." onChange={(event) => onText(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); onSend(); } }} /><button type="button" className="ea-support-send" disabled={busy || (!text.trim() && files.length === 0)} onClick={onSend} aria-label="Xabar yuborish"><Icon name={busy ? "hourglass_top" : "send"} filled /></button></div>{error && <p className="ea-support-error">{error}</p>}</footer>;
}
