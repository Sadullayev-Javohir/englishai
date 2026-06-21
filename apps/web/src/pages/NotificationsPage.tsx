import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { Icon } from "@/components/ui/Icon";
import { Button } from "@/components/ui/Button";
import { Spinner } from "@/components/ui/Spinner";
import { cn } from "@/lib/cn";
import { formatNotificationTime } from "@/lib/labels";
import { useNotificationsRefreshKey } from "@/lib/notificationsRefresh";
import { openExternalUrl } from "@/lib/openExternal";
import { resolveNotificationTarget } from "@/lib/notificationTarget";
import { tapLight } from "@/lib/haptics";

// Filled 3D card accents - the full bright Duolingo palette (10 hues). Every
// notification card gets its own hue so the feed reads as a varied, cohesive set.
type CardAccent =
  | "board1" | "board2" | "board3" | "board4" | "board5"
  | "board6" | "board7" | "board8" | "board9" | "board10";
const CARD_STYLE: Record<CardAccent, { face: string; lip: string }> = {
  board1: { face: "bg-ea-primary", lip: "" },
  board2: { face: "bg-ea-primary", lip: "" },
  board3: { face: "bg-ea-primary", lip: "" },
  board4: { face: "bg-ea-primary", lip: "" },
  board5: { face: "bg-ea-primary", lip: "" },
  board6: { face: "bg-ea-primary", lip: "" },
  board7: { face: "bg-ea-primary", lip: "" },
  board8: { face: "bg-ea-primary", lip: "" },
  board9: { face: "bg-ea-primary", lip: "" },
  board10: { face: "bg-ea-primary", lip: "" },
};
// Ten-tone cycle - one unique hue per notification card (no repeats until 11th).
const ACCENT_CYCLE: CardAccent[] = [
  "board1",
  "board2",
  "board3",
  "board4",
  "board5",
  "board6",
  "board7",
  "board8",
  "board9",
  "board10",
];
const accentFor = (i: number): CardAccent => ACCENT_CYCLE[i % ACCENT_CYCLE.length];

// Pick an icon from the notification code (vetted server codes, e.g. notify.review_*,
// notify.practice_*). Each notification gets its own modern Lucide icon so the feed
// reads as varied and on-brand.
function iconFor(code: string): string {
  if (code.includes("winback")) return "waving_hand";
  if (code.includes("review")) return "menu_book";
  if (code.includes("streak")) return "local_fire_department";
  if (code.includes("subscription") || code.includes("premium")) return "workspace_premium";
  if (code.includes("daily_goal_done")) return "task_alt";
  if (code.includes("daily_plan")) return "checklist";
  if (code.includes("practice_vocabulary")) return "menu_book";
  if (code.includes("practice_grammar")) return "rule";
  if (code.includes("practice_writing")) return "edit_note";
  if (code.includes("practice_speaking")) return "record_voice_over";
  if (code.includes("practice_listening")) return "headphones";
  if (code.includes("practice_reading")) return "auto_stories";
  if (code.includes("admin_broadcast")) return "campaign";
  return "notifications";
}

// The section a notification opens, shown as a chip so the learner knows where it leads. Falls back
// to a generic "open" label for any destination not in the vetted map.
function destinationLabel(linkUrl: string | null): string | null {
  if (!linkUrl) return null;
  return uz.notifications.destinations[linkUrl] ?? uz.notifications.open;
}

/** Screen 10 - Notifications, reskinned into the 3D Jungle Academy language. */
export function NotificationsPage() {
  const navigate = useNavigate();
  const learnerId = getLearnerId();
  // The realtime refresh key changes whenever a super-admin broadcast arrives over SignalR, so the
  // feed reloads the moment one is sent - no page refresh needed.
  const refreshKey = useNotificationsRefreshKey();
  const { data: page, loading, reload } = useAsync(
    () => api.vocabulary.notifications(learnerId),
    [learnerId, refreshKey],
  );
  const data = page?.items;

  // Dismissing a tapped notification: record it server-side so it drops out of the feed for good,
  // then either navigate to its internal destination or (for an informational card) refresh so it
  // disappears in place. Best-effort - a failed dismiss never blocks navigation.
  async function dismiss(id: string) {
    await api.vocabulary.dismissNotification(learnerId, id).catch(() => {});
  }

  // Opens a notification's destination. A link to our own site (a relative route, or an absolute
  // URL a super-admin copied straight from the address bar like https://englishai.uz/video/…) is
  // routed *inside* the app; only a genuinely foreign URL opens externally (new tab on web,
  // system in-app browser on the native APK).
  function openTarget(target: string) {
    const resolved = resolveNotificationTarget(target);
    if (!resolved) return;
    if (resolved.kind === "internal") {
      navigate(resolved.path);
    } else {
      openExternalUrl(resolved.url);
    }
  }

  // A tap on a notification marks it read (it leaves the feed), then opens its destination if it
  // has an internal one; informational cards just drop out after the refresh.
  async function handleCardClick(id: string, target: string | null) {
    tapLight();
    await dismiss(id);
    const resolved = target ? resolveNotificationTarget(target) : null;
    if (resolved?.kind === "internal") {
      navigate(resolved.path);
      return;
    }
    reload();
  }

  // "Hammasini o'qish" dismisses the feed: read notifications drop out of view entirely (the
  // durable server watermark keeps them dismissed), so only genuinely new ones ever show here.
  const visible = data?.filter((n) => !n.isRead) ?? [];

  async function markAllRead() {
    tapLight();
    await api.vocabulary.markRead(learnerId);
    reload();
  }

  return (
    <div className="relative z-[1] mx-auto min-h-[100svh] max-w-[640px] overflow-x-hidden px-4 pb-[calc(7rem+env(safe-area-inset-bottom))] md:px-6 md:pb-16">
      {/* ── Header: back button + title ── */}
      <motion.header
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 220, damping: 24 }}
        className="sticky top-0 z-20 -mx-4 md:-mx-6 px-4 md:px-6 pt-4 pb-3 mb-md
                   flex items-center justify-between gap-3"
      >
        <div className="flex items-center gap-3 min-w-0">
          <button
            type="button"
            onClick={() => {
              tapLight();
              navigate(-1);
            }}
            aria-label={uz.notifications.back}
            className="text-white/80 hover:text-white transition-colors shrink-0
                       w-11 h-11 rounded-2xl bg-white/15 ring-1 ring-white/25
                       flex items-center justify-center  "
          >
            <Icon name="arrow_back" />
          </button>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <span
            aria-hidden
            className="hidden h-11 w-11 rounded-2xl bg-white/15 ring-1 ring-white/25 text-white min-[390px]:flex items-center justify-center"
          >
            <Icon name="notifications" filled className="text-[20px]" />
          </span>
          {visible.length > 0 && (
            <Button
              variant="ghost"
              onClick={markAllRead}
              className="min-h-11 !whitespace-normal !px-3 !py-2 !text-right !text-white/90 hover:!bg-white/15 hover:!text-white"
            >
              {uz.notifications.markAllRead}
            </Button>
          )}
        </div>
      </motion.header>

      {/* Page title */}
      <motion.h1
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.04, type: "spring", stiffness: 260, damping: 22 }}
        className="font-duo font-extrabold text-[28px] md:text-[34px] leading-tight text-white drop-shadow-[0_2px_6px_rgba(0,0,0,0.45)] mb-md"
      >
        {uz.notifications.title}
      </motion.h1>

      {/* Spinner only on the very first load; a background refetch (realtime poll / tab focus)
          keeps the current feed on screen so it never flickers to a spinner. */}
      {loading && !data ? (
        <div className="flex justify-center py-xl">
          <Spinner />
        </div>
      ) : visible.length === 0 ? (
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          className={[
            "rounded-[20px] border-2 border-white/25 px-4 py-xl text-center text-white sm:rounded-[22px] md:rounded-[24px]",
            CARD_STYLE.board6.face,
            CARD_STYLE.board6.lip,
          ].join(" ")}
        >
          <Icon name="notifications_off" filled className="text-[44px] mb-sm" />
          <p className="font-duo font-bold text-body-lg">{uz.notifications.empty}</p>
        </motion.div>
      ) : (
        <div className="space-y-md">
          {visible.map((n, idx) => {
            // Each notification deep-links to a section; informational ones have no destination.
            const target = n.linkUrl;
            // "External" here means a genuinely foreign site (opens in a browser). A link to our
            // own site - even an absolute https://englishai.uz/… one - resolves to "internal" so it
            // opens on card tap inside the app instead of via the browser button.
            const external = target ? resolveNotificationTarget(target)?.kind === "external" : false;
            // Every card is tappable: a tap always dismisses the notification, and additionally
            // opens its internal destination when it has one. External broadcast links still open
            // only via the explicit button below, so a stray tap never hijacks the router.
            const destination = destinationLabel(target);
            const ac = accentFor(idx);
            const iconName = iconFor(n.code);
            return (
              <motion.div
                key={n.id}
                initial={{ opacity: 0, y: 18 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.06 + idx * 0.04, type: "spring", stiffness: 260, damping: 22 }}
                onClick={() => handleCardClick(n.id, external ? null : target)}
                className={cn(
                  "relative flex items-start gap-3 rounded-[20px] border-2 border-white/25 p-4 text-white cursor-pointer transition-all sm:gap-md sm:rounded-[22px] sm:p-lg md:rounded-[24px]",
                  CARD_STYLE[ac].face,
                  CARD_STYLE[ac].lip,
                )}
              >
                {/* Icon medallion - white glossy 3D chip */}
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-white/25 text-white ring-2 ring-white/40 shadow-sm sm:h-12 sm:w-12 sm:rounded-2xl">
                  <Icon name={iconName} filled className="text-[24px]" />
                </div>
                <div className="flex-1 min-w-0">
                  {/* Broadcasts carry a bold headline; reminders have only a message. */}
                  {n.title && (
                    <p className="font-duo font-extrabold text-label-lg text-white mb-xs drop-shadow-[0_1px_2px_rgba(0,0,0,0.3)]">
                      {n.title}
                    </p>
                  )}
                  <p className="font-body-md text-body-md text-white/95">{n.message}</p>
                  <div className="flex items-center flex-wrap gap-x-sm gap-y-xs mt-sm">
                    <span className="font-caption text-caption text-white/80">
                      {formatNotificationTime(n.createdAt)}
                    </span>
                    {/* Tells the learner which section an internal reminder opens. */}
                    {destination && !external && (
                      <span className="inline-flex items-center gap-xs font-caption text-caption text-white bg-white/20 rounded-full px-2.5 py-0.5">
                        <Icon name="open_in_new" className="text-[14px]" />
                        {destination}
                      </span>
                    )}
                  </div>
                  {/* An attached external link (broadcast URL) renders as a tappable 3D button. */}
                  {target && external && (
                    <Button
                      variant="ghost"
                      icon="open_in_new"
                      className="mt-sm !bg-white/20 !text-white hover:!bg-white/30 active:!bg-white/25 !py-xs"
                      onClick={(e) => {
                        e.stopPropagation();
                        void dismiss(n.id).then(() => {
                          openTarget(target);
                          reload();
                        });
                      }}
                    >
                      {uz.notifications.openLink}
                    </Button>
                  )}
                </div>
                {target && !external && (
                  <Icon
                    name="chevron_right"
                    className="text-white/90 text-[22px] shrink-0 self-center transition-transform group-hover:translate-x-0.5"
                  />
                )}
              </motion.div>
            );
          })}
        </div>
      )}
    </div>
  );
}
