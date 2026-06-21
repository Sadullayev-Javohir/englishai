import { useState } from "react";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { Icon } from "@/components/ui/Icon";
import { Spinner } from "@/components/ui/Spinner";

/** Builds the public share URL that carries the referral code as `?ref=`. */
function shareUrlFor(code: string): string {
  const origin =
    typeof window !== "undefined" && window.location?.origin
      ? window.location.origin
      : "https://englishai.uz";
  return `${origin}/?ref=${encodeURIComponent(code)}`;
}

/**
 * Profile "Invite friends" section. Shows the learner's referral code + share link (Telegram /
 * copy) and the bonus earned so far. Every qualified friend unlocks +2 topics and a
 * small Speaking/Writing credit bundle for both sides, capped to keep the programme cheap.
 */
export function ReferralSection() {
  const { data, loading, error } = useAsync(() => api.referral.status(), []);
  const [copied, setCopied] = useState(false);

  if (loading) {
    return (
      <section className="mt-lg md:mt-xl">
        <div className="flex justify-center rounded-[22px] border border-ea-border bg-ea-surface p-lg text-ea-primary shadow-ea-card">
          <Spinner className="text-ea-primary [&_.material-symbols-rounded]:text-ea-primary" />
        </div>
      </section>
    );
  }

  if (error || !data) {
    return (
      <section className="mt-lg md:mt-xl">
        <div className="rounded-[22px] border border-ea-danger-200 bg-ea-danger-50 p-lg text-ea-danger-700">
          <p className="font-body-md text-body-md text-ea-danger-700">{uz.referral.error}</p>
        </div>
      </section>
    );
  }

  const url = shareUrlFor(data.code);
  const telegramHref = `https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(
    uz.referral.shareMessage(data.code, ""),
  )}`;
  const capReached = data.qualifiedCount >= data.rewardCap;

  const copyCode = async () => {
    try {
      // Copy exactly what the learner sees in the "Sizning kodingiz" box - the code itself,
      // not the share URL (the Telegram button already shares the full link).
      await navigator.clipboard.writeText(data.code);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      // Clipboard blocked (insecure context / permissions) - the code is still visible to copy.
    }
  };

  return (
    <section className="mt-lg md:mt-xl">
      <div className="flex items-center gap-md mb-xs">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-[12px] border border-ea-border bg-ea-primary-soft text-ea-primary">
          <Icon name="group_add" filled className="text-[21px]" />
        </span>
        <h3 className="font-headline-md text-headline-md text-ea-text">{uz.referral.title}</h3>
      </div>
      <p className="mb-md font-body-md text-body-md text-ea-muted md:mb-lg">{uz.referral.subtitle}</p>

      <div className="relative space-y-lg overflow-hidden rounded-[18px] border border-ea-border bg-ea-surface-soft p-lg text-ea-text">
        {/* Code + copy */}
        <div className="relative z-10">
          <p className="mb-xs font-label-md text-label-md text-ea-muted">{uz.referral.yourCode}</p>
          <div className="flex items-center gap-sm">
            <code className="flex-1 rounded-xl border border-ea-border bg-ea-surface px-md py-sm text-center font-headline-md text-headline-md tracking-[0.2em] text-ea-text">
              {data.code}
            </code>
            <button
              type="button"
              onClick={copyCode}
              className="inline-flex shrink-0 items-center gap-xs rounded-xl border border-ea-primary bg-ea-primary px-md py-sm font-label-md text-label-md text-ea-on-primary transition-colors hover:bg-ea-primary-deep"
            >
              <Icon name={copied ? "check" : "content_copy"} className="text-[18px]" />
              {copied ? uz.referral.copied : uz.referral.copy}
            </button>
          </div>
        </div>

        {/* Share buttons - natural width, not stretched to the full row. */}
        <div className="relative z-10 flex">
          <a
            href={telegramHref}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-xs rounded-xl border border-ea-border bg-ea-surface px-md py-sm font-label-md text-label-md text-ea-primary transition-colors hover:border-ea-primary"
          >
            <Icon name="send" filled className="text-[18px]" />
            {uz.referral.shareTelegram}
          </a>
        </div>

        {/* Progress + bonus */}
        <div className="relative z-10 space-y-sm border-t border-ea-border pt-md">
          <div className="flex items-center justify-between font-label-md text-label-md text-ea-text">
            <span>{uz.referral.invited(data.invitedCount)}</span>
            <span>{uz.referral.progress(data.qualifiedCount, data.rewardCap)}</span>
          </div>

          <div className="flex flex-wrap gap-xs">
            <BonusChip icon="menu_book" label={uz.referral.bonusTopics(data.bonusTopicsUnlocked)} />
            <BonusChip icon="mic" label={uz.referral.bonusSpeaking(data.remainingSpeakingCredits)} />
            <BonusChip icon="edit_note" label={uz.referral.bonusWriting(data.remainingWritingCredits)} />
          </div>

          <p className="font-caption text-caption text-ea-muted">
            {capReached
              ? uz.referral.capReached
              : uz.referral.perReferral(
                  data.topicsPerReferral,
                  data.speakingCreditsPerReferral,
                  data.writingCreditsPerReferral,
                )}
          </p>
        </div>
      </div>
    </section>
  );
}

function BonusChip({ icon, label }: { icon: string; label: string }) {
  return (
    <span className="inline-flex items-center gap-xs rounded-full border border-ea-border bg-ea-surface px-md py-xs font-label-md text-label-md text-ea-muted-deep">
      <Icon name={icon} filled className="text-[16px]" />
      {label}
    </span>
  );
}
