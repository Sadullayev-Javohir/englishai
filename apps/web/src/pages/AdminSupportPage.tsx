import { useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { api } from "@/api/client";
import { SupportHubClient } from "@/api/supportHub";
import { appendSupportMessage, applySupportRead, updateSupportSummaryMessage, upsertSupportSummary } from "@/api/supportRealtime";
import { SupportConversationStatus, SupportSenderKind, type SupportConversationDto, type SupportConversationSummaryDto } from "@/api/types";
import { useAuth } from "@/app/auth";
import { DesignToast } from "@/components/design";
import { SupportNotificationPermission } from "@/components/SupportNotificationPermission";
import { Icon } from "@/components/ui/Icon";
import { UserAvatar } from "@/components/UserAvatar";
import { incomingSupportNotification, showSystemSupportNotification, type SupportIncomingNotification } from "@/lib/supportNotifications";
import { Composer, MessageList } from "./SupportChatPage";
import "./SupportChat.css";

const FILTERS = [
  { key: "open", label: "Ochiq", icon: "forum" },
  { key: "mine", label: "Meniki", icon: "account_circle" },
  { key: "unassigned", label: "Yangi", icon: "pending" },
  { key: "closed", label: "Yopilgan", icon: "inventory_2" },
];

const formatQueueTime = (value: string) => new Intl.DateTimeFormat("uz-UZ", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
}).format(new Date(value));

export function AdminSupportPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { user } = useAuth();
  const [filter, setFilter] = useState("open");
  const [items, setItems] = useState<SupportConversationSummaryDto[]>([]);
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

  const refreshList = useCallback(async (nextFilter = filter) => setItems(await api.admin.supportConversations(nextFilter)), [filter]);
  const openConversation = useCallback(async (id: string) => {
    const next = await api.admin.supportConversation(id);
    await hubRef.current?.join(id, true);
    setConversation(next);
    setItems((current) => upsertSupportSummary(current, { ...next, unreadCount: 0 }));
    setError("");
    await api.admin.markSupportRead(id);
  }, []);

  useEffect(() => { void refreshList(filter); }, [filter, refreshList]);
  useEffect(() => {
    const conversationId = searchParams.get("conversation");
    if (!conversationId) return;
    void openConversation(conversationId).then(() => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current);
        next.delete("conversation");
        return next;
      }, { replace: true });
    }).catch(() => setError("Support suhbati ochilmadi."));
  }, [openConversation, searchParams, setSearchParams]);
  useEffect(() => { conversationIdRef.current = conversation?.id ?? null; }, [conversation?.id]);
  useEffect(() => {
    let active = true;
    let connected = false;
    const hub = new SupportHubClient({
      onMessage: (payload) => {
        if (!active) return;
        const message = "conversationId" in payload ? payload.message : payload;
        const incoming = incomingSupportNotification(message, SupportSenderKind.Admin);
        if (incoming && !notifiedMessageIds.current.has(message.id)) {
          notifiedMessageIds.current.add(message.id);
          setNotification(incoming);
          showSystemSupportNotification(incoming);
        }
        if ("conversationId" in payload) {
          setItems((current) => updateSupportSummaryMessage(current, payload.conversationId, message));
        }
        setConversation((current) => {
          const id = "conversationId" in payload ? payload.conversationId : current?.id ?? "";
          if (current?.id === id && message.senderKind === SupportSenderKind.Learner) void api.admin.markSupportRead(id);
          return appendSupportMessage(current, id, message);
        });
      },
      onConversation: (next) => { if (active) { setItems((current) => upsertSupportSummary(current, next)); setConversation((current) => current?.id === next.id ? next : current); } },
      onTyping: (payload) => { if (active && !payload.admin) setConversation((current) => { if (current?.id === payload.conversationId) setTyping(payload.typing); return current; }); },
      onRead: (payload) => { if (active) setConversation((current) => applySupportRead(current, payload.conversationId, payload.byAdmin)); },
      onReconnecting: () => { if (active) setLive(false); },
      onReconnected: async () => {
        if (active) setLive(true);
        await refreshList();
        const openId = conversationIdRef.current;
        if (openId) await hub.join(openId, true);
      },
      onClosed: () => { if (active) setLive(false); },
    });
    hubRef.current = hub;
    const startTimer = window.setTimeout(() => {
      if (!active) return;
      void hub.start().then(() => { connected = true; if (active) setLive(true); }).catch(() => { if (active) setLive(false); });
    }, 500);
    return () => {
      active = false;
      window.clearTimeout(startTimer);
      window.clearTimeout(typingTimer.current);
      if (connected && !import.meta.env.DEV) void hub.stop();
    };
  }, [openConversation, refreshList]);
  useEffect(() => { endRef.current?.scrollIntoView({ behavior: "smooth" }); }, [conversation?.messages.length, typing]);

  const chooseFiles = (selected: FileList | null) => {
    if (!selected) return;
    const next = [...files, ...Array.from(selected)].slice(0, 4);
    if (next.some((file) => !["image/png", "image/jpeg", "image/webp", "image/gif"].includes(file.type) || file.size > 10 * 1024 * 1024)) { setError("Faqat rasm, har biri 10 MB gacha."); return; }
    setFiles(next);
    setError("");
  };
  const send = async () => {
    if (!conversation || busy || (!text.trim() && files.length === 0)) return;
    setBusy(true);
    setError("");
    try {
      const message = await api.admin.sendSupportMessage(conversation.id, text, files);
      setConversation((current) => appendSupportMessage(current, conversation.id, message));
      setItems((current) => updateSupportSummaryMessage(current, conversation.id, message));
      setText(""); setFiles([]);
    }
    catch { setError("Javob yuborilmadi. Suhbatni avval o‘zingizga biriktiring."); }
    finally { setBusy(false); }
  };
  const onText = (value: string) => {
    setText(value);
    if (!conversation) return;
    void hubRef.current?.typing(conversation.id, true, true);
    window.clearTimeout(typingTimer.current);
    typingTimer.current = window.setTimeout(() => void hubRef.current?.typing(conversation.id, true, false), 1200);
  };
  const assign = async () => { if (conversation) setConversation(await api.admin.assignSupportConversation(conversation.id)); };
  const toggleStatus = async () => {
    const current = conversation;
    if (current) setConversation(await api.admin.setSupportStatus(current.id, current.status === SupportConversationStatus.Open));
  };

  const canReply = Boolean(conversation && conversation.assignedAdminId === user?.id && conversation.status === SupportConversationStatus.Open);
  const unreadTotal = items.reduce((sum, item) => sum + item.unreadCount, 0);
  const activeFilter = FILTERS.find((item) => item.key === filter) ?? FILTERS[0];

  return <section className={`ea-admin-support ${conversation ? "has-selection" : ""}`}>
    <aside className="ea-admin-support__queue">
      <div className="ea-admin-support__queue-head">
        <div className="ea-admin-support__title-row">
          <div>
            <span className="ea-admin-support__eyebrow">Mijozlar markazi</span>
            <h1>Support inbox</h1>
            <p>Foydalanuvchilar bilan real vaqtda muloqot qiling.</p>
          </div>
          <span className="ea-admin-support__live" title={live ? "Real-time aloqa faol" : "Real-time aloqa qayta ulanmoqda"}><i /> {live ? "Jonli" : "Ulanmoqda"}</span>
        </div>
        <div className="ea-admin-support__summary" aria-label="Support navbati holati">
          <span><strong>{items.length}</strong><small>{activeFilter.label}</small></span>
          <span><strong>{unreadTotal}</strong><small>O‘qilmagan</small></span>
        </div>
        <div className="ea-admin-support__filters">{FILTERS.map((item) => <button type="button" key={item.key} className={filter === item.key ? "is-active" : ""} onClick={() => { setFilter(item.key); setConversation(null); }}><Icon name={item.icon} /><span>{item.label}</span></button>)}</div>
      </div>
      <div className="ea-admin-support__list">{items.map((item) => <button type="button" key={item.id} className={`ea-admin-support__item ${conversation?.id === item.id ? "is-active" : ""}`} onClick={() => void openConversation(item.id)}>
        <span className="ea-admin-support__avatar-wrap"><UserAvatar pictureUrl={item.learnerPictureUrl} name={item.learnerName} className="h-12 w-12 shrink-0 rounded-[15px]" />{item.status === SupportConversationStatus.Open && <i />}</span>
        <span className="ea-admin-support__item-copy">
          <span className="ea-admin-support__item-top"><strong>{item.learnerName}</strong><time>{formatQueueTime(item.updatedAt)}</time></span>
          <span className="ea-admin-support__preview">{item.lastMessagePreview || "Yangi suhbat"}</span>
          {item.assignedAdminName && <small><Icon name="support_agent" />{item.assignedAdminName}</small>}
        </span>
        {item.unreadCount > 0 && <b className="ea-support-badge">{item.unreadCount}</b>}
      </button>)}{items.length === 0 && <div className="ea-support-empty"><div><span className="ea-support-empty__icon"><Icon name="inbox" /></span><strong>Suhbat topilmadi</strong><p>Bu filtrda hozircha murojaat yo‘q.</p></div></div>}</div>
    </aside>

    <div className="ea-admin-support__workspace">{conversation ? <div className="ea-support-chat">
      <header className="ea-support-header ea-support-header--admin">
        <div className="ea-support-header__identity">
          <button type="button" className="ea-support-back" onClick={() => setConversation(null)} aria-label="Suhbatlar ro‘yxati"><Icon name="arrow_back" /></button>
          <span className="ea-admin-support__avatar-wrap"><UserAvatar pictureUrl={conversation.learnerPictureUrl} name={conversation.learnerName} className="h-12 w-12 rounded-[15px]" /><i /></span>
          <div className="ea-support-header__copy"><div className="ea-support-header__name-row"><h2>{conversation.learnerName}</h2><span className={`ea-support-status ${conversation.status === SupportConversationStatus.Closed ? "is-closed" : ""}`}><i />{conversation.status === SupportConversationStatus.Open ? "Ochiq" : "Yopilgan"}</span></div><p>{conversation.learnerEmail}</p><small>{conversation.assignedAdminName ? `${conversation.assignedAdminName} javob bermoqda` : "Hali agent biriktirilmagan"}</small></div>
        </div>
        <div className="ea-admin-support__actions">
          {conversation.assignedAdminId !== user?.id && conversation.status === SupportConversationStatus.Open && <button type="button" className="is-primary" onClick={() => void assign()}><Icon name="person_add" /><span>Qabul qilish</span></button>}
          <button type="button" onClick={() => void toggleStatus()}><Icon name={conversation.status === SupportConversationStatus.Open ? "check_circle" : "refresh"} /><span>{conversation.status === SupportConversationStatus.Open ? "Yopish" : "Qayta ochish"}</span></button>
        </div>
      </header>
      <MessageList conversation={conversation} mine={SupportSenderKind.Admin} typing={typing} onImage={setLightbox} endRef={endRef} />
      {canReply ? <Composer text={text} files={files} busy={busy} error={error} inputRef={inputRef} onText={onText} onChoose={chooseFiles} onRemove={(index) => setFiles((list) => list.filter((_, i) => i !== index))} onSend={send} /> : <div className="ea-support-locked"><span><Icon name={conversation.status === SupportConversationStatus.Closed ? "lock" : "support_agent"} /></span><div><strong>{conversation.status === SupportConversationStatus.Closed ? "Suhbat yopilgan" : "Javob berish uchun suhbatni qabul qiling"}</strong><p>{conversation.status === SupportConversationStatus.Closed ? "Kerak bo‘lsa suhbatni qayta ochishingiz mumkin." : "Qabul qilgandan so‘ng xabar yuborish maydoni faollashadi."}</p></div></div>}
    </div> : <div className="ea-support-empty ea-support-empty--workspace"><div><span className="ea-support-empty__icon"><Icon name="support_agent" /></span><span className="ea-admin-support__eyebrow">Support workspace</span><strong>Suhbatni tanlang</strong><p>Navbatdagi murojaat tafsilotlari va xabarlar shu yerda ko‘rinadi.</p></div></div>}</div>
    <SupportNotificationPermission />
    {lightbox && <button type="button" className="ea-support-lightbox" onClick={() => setLightbox(null)}><img src={lightbox} alt="Support rasmi" /></button>}
    <DesignToast open={notification !== null} title={notification?.title ?? ""} message={notification?.message} onClose={() => setNotification(null)} />
  </section>;
}

export default AdminSupportPage;
