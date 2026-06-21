import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import type { AdminBroadcastDto } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import {
  AppButton,
  AppIconButton,
  DesignCard,
  DesignConfirm,
  DesignInput,
  DesignSelect,
  DesignTextarea,
  DesignToast,
  EmptyState,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  SectionHeader,
  StatCard,
} from "@/components/design";
import { formatDateTime } from "@/lib/labels";
import "./AdminNotificationsPage.css";

const BROADCAST_DESTINATIONS = [
  "/home",
  "/app/vocabulary/topics",
  "/app/vocabulary/review",
  "/app/grammar",
  "/writing",
  "/app/speaking",
  "/listening",
  "/reading",
] as const;

type NoticeTone = "success" | "warning" | "error";
type ComposerNotice = { tone: NoticeTone; message: string } | null;

export function AdminNotificationsPage() {
  const navigate = useNavigate();
  return (
    <main className="ea-admin-notifications">
      <div className="ea-admin-notifications__shell">
        <PageHeader
          eyebrow="Admin studio"
          title={uz.admin.notifTitle}
          description={uz.admin.notifHint}
          actions={<AppButton tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>{uz.admin.back}</AppButton>}
        />

        <NotificationManager />
      </div>
    </main>
  );
}

function NotificationManager() {
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [linkUrl, setLinkUrl] = useState("");
  const [externalUrl, setExternalUrl] = useState("");
  const [sending, setSending] = useState(false);
  const [notice, setNotice] = useState<ComposerNotice>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const { data: recent, loading, error, reload } = useAsync(() => api.admin.broadcasts(), []);

  const broadcasts = recent?.items ?? [];
  const withLinkCount = broadcasts.filter((broadcast) => !!broadcast.linkUrl).length;
  const lastSent = broadcasts[0] ?? null;
  const effectiveLink = externalUrl.trim() || linkUrl || null;
  const externalUrlInvalid =
    externalUrl.trim().length > 0 && !/^https?:\/\/.+/i.test(externalUrl.trim());
  const formInvalid = !title.trim() || !body.trim() || externalUrlInvalid;
  const previewDestination = effectiveLink
    ? uz.notifications.destinations[effectiveLink] ?? effectiveLink
    : uz.admin.broadcastDestNone;

  const requestSend = () => {
    if (formInvalid) {
      setNotice({ tone: "warning", message: uz.admin.broadcastValidation });
      return;
    }
    setNotice(null);
    setConfirmOpen(true);
  };

  const send = async () => {
    setSending(true);
    setNotice(null);
    try {
      await api.admin.sendBroadcast(title.trim(), body.trim(), effectiveLink);
      setTitle("");
      setBody("");
      setLinkUrl("");
      setExternalUrl("");
      setConfirmOpen(false);
      setNotice({ tone: "success", message: uz.admin.broadcastSent });
      reload();
    } catch {
      setConfirmOpen(false);
      setNotice({ tone: "error", message: uz.admin.broadcastError });
    } finally {
      setSending(false);
    }
  };

  return (
    <>
      <section className="ea-admin-notifications__stats" aria-label="Bildirishnomalar statistikasi">
        <StatTile icon="forum" label={uz.admin.statTotalSent} value={broadcasts.length} />
        <StatTile icon="link" label={uz.admin.statWithLink} value={withLinkCount} />
        <StatTile
          icon="schedule_send"
          label={uz.admin.statLastSent}
          value={lastSent ? formatDateTime(lastSent.createdAt) : uz.admin.statLastSentNever}
        />
      </section>

      {notice && <Toast tone={notice.tone} message={notice.message} onClose={() => setNotice(null)} />}

      <section className="ea-admin-notifications__workspace">
        <DesignCard className="ea-admin-notifications__composer-card">
          <SectionHeading
            eyebrow="Broadcast composer"
            title="Yangi xabar yarating"
            description="Xabar mazmuni va auditoriya yo‘nalishini bir joyda boshqaring."
          />

          <div className="ea-admin-notifications__form">
            <FormField label={uz.admin.broadcastTitleLabel} htmlFor="admin-broadcast-title" help={`${title.length}/120`}>
              <DesignInput
                id="admin-broadcast-title"
                type="text"
                value={title}
                maxLength={120}
                onChange={(event) => setTitle(event.target.value)}
                placeholder={uz.admin.broadcastTitlePlaceholder}
              />
            </FormField>

            <FormField label={uz.admin.broadcastBodyLabel} htmlFor="admin-broadcast-body" help={`${body.length}/500`}>
              <DesignTextarea
                id="admin-broadcast-body"
                value={body}
                maxLength={500}
                rows={5}
                onChange={(event) => setBody(event.target.value)}
                placeholder={uz.admin.broadcastBodyPlaceholder}
              />
            </FormField>

            <div className="ea-admin-notifications__audience">
              <div className="ea-admin-notifications__audience-copy">
                <span className="ea-admin-notifications__field-label">Auditoriya</span>
                <strong>Barcha o‘quvchilar</strong>
                <p>Faol learner hisoblariga real vaqtda yuboriladi.</p>
              </div>
              <span className="ea-admin-notifications__audience-badge">
                <Icon name="groups" filled />
                All learners
              </span>
            </div>

            <div className="ea-admin-notifications__form-grid">
              <FormField label={uz.admin.broadcastDestLabel} htmlFor="admin-broadcast-destination">
                <DesignSelect
                  id="admin-broadcast-destination"
                  value={linkUrl}
                  onChange={(event) => setLinkUrl(event.target.value)}
                  disabled={externalUrl.trim().length > 0}
                >
                  <option value="">{uz.admin.broadcastDestNone}</option>
                  {BROADCAST_DESTINATIONS.map((destination) => (
                    <option key={destination} value={destination}>
                      {uz.notifications.destinations[destination] ?? destination}
                    </option>
                  ))}
                </DesignSelect>
              </FormField>

              <FormField
                label={uz.admin.broadcastUrlLabel}
                htmlFor="admin-broadcast-url"
                help={externalUrlInvalid ? undefined : uz.admin.broadcastUrlHint}
                error={externalUrlInvalid ? uz.admin.broadcastUrlInvalid : undefined}
              >
                <DesignInput
                  id="admin-broadcast-url"
                  type="url"
                  inputMode="url"
                  value={externalUrl}
                  maxLength={200}
                  aria-invalid={externalUrlInvalid}
                  onChange={(event) => setExternalUrl(event.target.value)}
                  placeholder={uz.admin.broadcastUrlPlaceholder}
                />
              </FormField>
            </div>

            <div className="ea-admin-notifications__composer-actions">
              <AppButton leadingIcon="send" loading={sending} onClick={requestSend}>{uz.admin.broadcastSend}</AppButton>
              <span>Yuborishdan oldin tasdiqlash oynasi ko‘rsatiladi.</span>
            </div>
          </div>
        </DesignCard>

        <DesignCard as="section" className="ea-admin-notifications__preview-card">
          <SectionHeading
            eyebrow="Live preview"
            title="O‘quvchi ko‘rinishi"
            description="Matn yozganingiz sari preview yangilanadi."
          />
          <div className="ea-admin-notifications__phone">
            <div className="ea-admin-notifications__phone-top">
              <span>EnglishAI</span>
              <Icon name="notifications" filled />
            </div>
            <article className="ea-admin-notifications__preview-notice">
              <span className="ea-admin-notifications__preview-icon">
                <Icon name="campaign" filled />
              </span>
              <div>
                <small>Hozirgina</small>
                <h3>{title.trim() || uz.admin.broadcastTitlePlaceholder}</h3>
                <p>{body.trim() || uz.admin.broadcastBodyPlaceholder}</p>
              </div>
            </article>
            <div className="ea-admin-notifications__preview-destination">
              <span>Yo‘nalish</span>
              <strong>{previewDestination}</strong>
            </div>
          </div>
          <DailyDispatch />
        </DesignCard>
      </section>

      <DesignCard as="section" className="ea-admin-notifications__history">
        <div className="ea-admin-notifications__history-header">
          <SectionHeading
            eyebrow="Delivery history"
            title={uz.admin.broadcastRecent}
            description="Yuborilgan xabarlar va ularning holatini kuzating."
          />
          <AppButton tone="standard" leadingIcon="refresh" onClick={reload} disabled={loading}>Yangilash</AppButton>
        </div>

        {loading && broadcasts.length === 0 ? (
          <LoadingState title="Yuklanmoqda" description="Broadcast tarixi olinmoqda." />
        ) : error ? (
          <ErrorState title="Tarixni yuklab bo‘lmadi" action={<AppButton tone="standard" onClick={reload}>Qayta urinish</AppButton>} />
        ) : broadcasts.length > 0 ? (
          <div className="ea-admin-notifications__history-grid">
            {broadcasts.map((broadcast) => (
              <RecentBroadcastRow key={broadcast.id} broadcast={broadcast} onDeleted={reload} />
            ))}
          </div>
        ) : (
          <EmptyState icon="notifications_off" title={uz.admin.broadcastEmpty} description="Birinchi broadcast yuborilgach, u shu yerda ko‘rinadi." />
        )}
      </DesignCard>

      {confirmOpen && (
        <ConfirmModal
          title={title.trim()}
          body={body.trim()}
          destination={previewDestination}
          sending={sending}
          onCancel={() => setConfirmOpen(false)}
          onConfirm={send}
        />
      )}
    </>
  );
}

function StatTile({
  icon,
  label,
  value,
}: {
  icon: string;
  label: string;
  value: string | number;
}) {
  return <StatCard icon={icon} label={label} value={value} />;
}

function SectionHeading({
  eyebrow,
  title,
  description,
}: {
  eyebrow: string;
  title: string;
  description: string;
}) {
  return <SectionHeader eyebrow={eyebrow} title={title} description={description} />;
}

function RecentBroadcastRow({
  broadcast,
  onDeleted,
}: {
  broadcast: AdminBroadcastDto;
  onDeleted: () => void;
}) {
  const [deleting, setDeleting] = useState(false);
  const [failed, setFailed] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);

  const remove = async () => {
    setDeleting(true);
    setFailed(false);
    try {
      await api.admin.deleteBroadcast(broadcast.id);
      setConfirmOpen(false);
      onDeleted();
    } catch {
      setFailed(true);
      setDeleting(false);
    }
  };

  return (
    <>
      <DesignCard as="article" className="ea-admin-notifications__history-card">
        <div className="ea-admin-notifications__history-card-top">
          <span className="ea-admin-notifications__status"><i /> Yuborildi</span>
          <AppIconButton
            icon={deleting ? "progress_activity" : "delete"}
            label={uz.admin.broadcastDelete}
            tone="danger"
            disabled={deleting}
            onClick={() => setConfirmOpen(true)}
          />
        </div>
        <h3>{broadcast.title}</h3>
        <p>{broadcast.body}</p>
        <div className="ea-admin-notifications__history-meta">
          <span><Icon name="schedule" /> {formatDateTime(broadcast.createdAt)}</span>
          <span><Icon name={broadcast.linkUrl ? "link" : "link_off"} /> {broadcast.linkUrl || "Havolasiz"}</span>
        </div>
        {failed && <small className="ea-admin-notifications__delete-error">{uz.admin.broadcastDeleteError}</small>}
      </DesignCard>
      <DesignConfirm
        open={confirmOpen}
        onClose={() => setConfirmOpen(false)}
        title={uz.admin.broadcastDelete}
        message={uz.admin.broadcastDeleteConfirm}
        confirmLabel={uz.admin.broadcastDelete}
        destructive
        loading={deleting}
        onConfirm={() => void remove()}
      />
    </>
  );
}

function DailyDispatch() {
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<number | null>(null);
  const [failed, setFailed] = useState(false);

  const run = async () => {
    setRunning(true);
    setFailed(false);
    setResult(null);
    try {
      const response = await api.admin.dispatchDaily();
      setResult(response.notifiedLearners);
    } catch {
      setFailed(true);
    } finally {
      setRunning(false);
    }
  };

  return (
    <div className="ea-admin-notifications__dispatch">
      <span className="ea-admin-notifications__dispatch-icon"><Icon name="notification_add" filled /></span>
      <div>
        <small>Scheduled action</small>
        <strong>{uz.admin.dispatchDaily}</strong>
        <p>{uz.admin.dispatchDailyHint}</p>
        {result !== null && <em>{uz.admin.dispatchDailyDone(result)}</em>}
        {failed && <em className="is-error">{uz.admin.dispatchDailyError}</em>}
      </div>
      <AppButton tone="standard" leadingIcon="send" loading={running} onClick={() => void run()}>
        Ishga tushirish
      </AppButton>
    </div>
  );
}

function Toast({ tone, message, onClose }: { tone: NoticeTone; message: string; onClose: () => void }) {
  return <DesignToast open title={message} tone={tone === "success" ? "success" : tone === "error" ? "danger" : "standard"} onClose={onClose} closeLabel="Xabarni yopish" />;
}

function ConfirmModal({
  title,
  body,
  destination,
  sending,
  onCancel,
  onConfirm,
}: {
  title: string;
  body: string;
  destination: string;
  sending: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  return (
    <DesignConfirm
      open
      onClose={onCancel}
      title="Xabarni yuborishni tasdiqlaysizmi?"
      description="Final confirmation"
      confirmLabel="Tasdiqlash va yuborish"
      loading={sending}
      closeOnBackdrop={!sending}
      closeOnEscape={!sending}
      onConfirm={onConfirm}
      message={(
        <div className="space-y-3 text-left">
          <div className="rounded-xl border border-ea-border bg-ea-surface-soft p-4">
          <strong>{title}</strong>
            <p className="mt-1 break-words text-sm leading-relaxed text-ea-muted">{body}</p>
            <span className="mt-3 inline-flex items-center gap-1.5 text-xs font-bold text-ea-primary-deep"><Icon name="near_me" /> {destination}</span>
          </div>
          <p className="text-sm leading-relaxed text-ea-orange-600">
            Bu xabar barcha o‘quvchilarga yuboriladi va amalni avtomatik bekor qilib bo‘lmaydi.
          </p>
        </div>
      )}
    />
  );
}
