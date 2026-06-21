import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api, ApiError } from "@/api/client";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";

type Status =
  | { kind: "idle" }
  | { kind: "queued" }
  | { kind: "ingested"; title: string }
  | { kind: "error" };

// Filled 3D card accents - the full bright Duolingo palette (10 hues). Every card gets its
// own hue so no colour repeats across the screen.
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

/** Shared translucent, dark-tinted input used across the jungle admin forms. */
const inputClass =
  "min-h-11 w-full rounded-xl border border-white/30 bg-white/15 px-md py-sm font-body-md text-body-md text-white placeholder:text-white/60 focus:border-white/70 focus:outline-none disabled:opacity-50";

/**
 * Admin - Video ingestion. Curators add a YouTube video to the catalog; the server runs
 * the quota-limited YouTube/LLM work in the background via Hangfire (PROJECT-SPEC Faza 4,
 * docs/development-guide.md rule 17.3). A 202 (no body) means "queued"; a returned lesson means it was
 * ingested synchronously (Hangfire disabled).
 *
 * Visual language: the same 3D "Jungle Academy" boards used on the learner HomePage and the
 * other admin screens, so the whole product reads as one cohesive design. The global
 * background.mp4 shows through behind a light scrim; every card/button is a chunky 3D shape
 * in its own brand hue - no flat white panels. Real elements paint immediately (no perpetual
 * "Yuklanmoqda" spinner).
 */
export function AdminVideoPage() {
  const navigate = useNavigate();
  const [videoId, setVideoId] = useState("");
  const [topic, setTopic] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [status, setStatus] = useState<Status>({ kind: "idle" });

  const canSubmit = videoId.trim().length > 0 && topic.trim().length > 0 && !submitting;

  async function submit() {
    if (!canSubmit) return;
    setSubmitting(true);
    setStatus({ kind: "idle" });
    try {
      const lesson = await api.video.ingest(videoId.trim(), topic.trim());
      if (lesson) {
        setStatus({ kind: "ingested", title: lesson.title });
      } else {
        setStatus({ kind: "queued" });
      }
      setVideoId("");
      setTopic("");
    } catch (err) {
      if (!(err instanceof ApiError)) throw err;
      setStatus({ kind: "error" });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="relative max-w-[1100px] mx-auto px-1 md:px-6 pb-20 md:pb-16">
      <PageHeader />

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.05, type: "spring", stiffness: 260, damping: 22 }}
        className="mt-xl"
      >
        {/* Hero ingest card - purple board. */}
        <div className={["rounded-[24px] border-2 border-white/25 p-5 md:p-7 text-white", CARD_STYLE.board3.face, CARD_STYLE.board3.lip].join(" ")}>
          <div className="mb-5 flex items-center gap-3">
            <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/25 text-white ring-2 ring-white/40 shadow-sm">
              <Icon name="smart_display" filled className="text-[24px]" />
            </span>
            <div className="min-w-0">
              <h1 className="font-duo font-extrabold text-[20px] md:text-[24px] leading-tight text-white drop-shadow">
                {uz.admin.videoIngestTitle}
              </h1>
              <p className="font-caption text-caption text-white/85">
                {uz.admin.videoIngestSubtitle}
              </p>
            </div>
          </div>

          <div className="space-y-md">
            <label className="block space-y-xs text-left">
              <span className="font-label-md text-label-md text-white">{uz.admin.videoIdLabel}</span>
              <input
                value={videoId}
                onChange={(e) => setVideoId(e.target.value)}
                placeholder={uz.admin.videoIdPlaceholder}
                disabled={submitting}
                className={inputClass}
              />
              <span className="block font-caption text-caption text-white/70">{uz.admin.videoIdHint}</span>
            </label>

            <label className="block space-y-xs text-left">
              <span className="font-label-md text-label-md text-white">{uz.admin.topicLabel}</span>
              <input
                value={topic}
                onChange={(e) => setTopic(e.target.value)}
                placeholder={uz.admin.topicPlaceholder}
                disabled={submitting}
                className={inputClass}
              />
            </label>

            <motion.button
              type="button"
              onClick={submit}
              disabled={!canSubmit}
              className={cn(
                "inline-flex w-full items-center justify-center gap-2 rounded-xl bg-ea-surface px-md py-md font-label-md text-label-md text-ea-primary  transition-transform  disabled:opacity-60",
              )}
            >
              {submitting ? (
                <span className="inline-flex items-center gap-2">
                  <Icon name="pending" className="animate-spin text-[18px]" />
                  {uz.admin.loading}
                </span>
              ) : (
                <span className="inline-flex items-center gap-2">
                  <Icon name="add" filled className="text-[18px]" />
                  {uz.admin.submit}
                </span>
              )}
            </motion.button>
          </div>
        </div>

        {/* Status banners - each its own hue so nothing reads as a flat white panel. */}
        {status.kind === "queued" && (
          <Banner tone="board6" icon="schedule" text={uz.admin.queued} className="mt-md" />
        )}
        {status.kind === "ingested" && (
          <Banner tone="board1" icon="check_circle" text={`${uz.admin.ingested} - ${status.title}`} className="mt-md" />
        )}
        {status.kind === "error" && (
          <Banner tone="board4" icon="error" text={uz.admin.error} className="mt-md" />
        )}
      </motion.div>

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.1, type: "spring", stiffness: 260, damping: 22 }}
        className="mt-md"
      >
        <button
          type="button"
          onClick={() => navigate("/video")}
          className={["group flex w-full items-center justify-center gap-2 rounded-[20px] border-2 border-white/25 bg-ea-primary px-md py-md font-label-md text-label-md text-ea-on-primary  transition-all ", CARD_STYLE.board2.lip].join(" ")}
        >
          <Icon name="arrow_forward" className="text-[20px] transition-transform group-hover:translate-x-1" />
          {uz.admin.openVideo}
        </button>
      </motion.div>
    </div>
  );
}

/** EnglishAI logo + back link, transparent over the looping background video. */
function PageHeader() {
  return (
    <motion.header
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ type: "spring", stiffness: 220, damping: 24 }}
      className="relative flex items-start gap-3 pt-1 md:items-center md:gap-4 md:pt-2"
    >
      <Link
        to="/admin"
        aria-label={uz.admin.title}
        className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-white/20 text-white ring-2 ring-white/40  transition-transform "
      >
        <Icon name="arrow_back" filled className="text-[22px]" />
      </Link>
      <div className="min-w-0">
        <h1 className="font-duo font-extrabold text-[22px] md:text-[26px] leading-tight text-white drop-shadow">
          {uz.admin.videoIngestTitle}
        </h1>
        <p className="font-caption text-caption text-white/80 truncate">
          {uz.admin.videoIngestSubtitle}
        </p>
      </div>
    </motion.header>
  );
}

function Banner({ tone, icon, text, className }: { tone: CardAccent; icon: string; text: string; className?: string }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ type: "spring", stiffness: 260, damping: 22 }}
      className={cn(
        "flex items-start gap-sm rounded-[20px] border-2 border-white/25 p-md text-white",
        CARD_STYLE[tone].face,
        CARD_STYLE[tone].lip,
        className,
      )}
    >
      <Icon name={icon} filled className="text-[22px] shrink-0" />
      <span className="font-body-md text-body-md">{text}</span>
    </motion.div>
  );
}
