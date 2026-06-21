import { useEffect, useState } from "react";
import { AppButton, DesignModal } from "@/components/design";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import { useOnline, type ConnectionProbe } from "@/lib/useOnline";
import "./OfflineModal.css";

// Global "no internet" dialog - design: englishai.pen, screens
// "81 - WEB - /home - Internet aloqasi yo'q" and the MOBILE twin.
//
// Built on DesignModal so it inherits the focus trap, Escape handling, portal and
// body-scroll lock (DesignGuard.test.ts requires every dialog to go through it). The
// canonical header only renders a title + close button, which does not match this
// design, so it is kept for the accessible name only and hidden visually; the whole
// visible card lives in the body where the pen structure can be reproduced exactly.

/** The retry-feedback strips from the pen "Internet / ..." component family. */
const PROBE_STRIPS: Record<
  Exclude<ConnectionProbe, "idle">,
  { tone: string; icon: string; spin?: boolean; title: string; text: string }
> = {
  checking: {
    tone: "checking",
    icon: "progress_activity",
    spin: true,
    title: uz.offline.checking.title,
    text: uz.offline.checking.text,
  },
  offline: {
    tone: "still-offline",
    icon: "wifi_off",
    title: uz.offline.stillOffline.title,
    text: uz.offline.stillOffline.text,
  },
  online: {
    tone: "reconnected",
    icon: "wifi",
    title: uz.offline.reconnected.title,
    text: uz.offline.reconnected.text,
  },
};

export function OfflineModal() {
  const { offline, probe, retry } = useOnline();
  const [dismissed, setDismissed] = useState(false);

  // "Hozir emas" hides the dialog for this outage only - a later disconnect shows it again.
  useEffect(() => {
    if (!offline) setDismissed(false);
  }, [offline]);

  const strip = probe === "idle" ? null : PROBE_STRIPS[probe];

  return (
    <DesignModal
      open={offline && !dismissed}
      onClose={() => setDismissed(true)}
      title={uz.offline.title}
      closeLabel={uz.offline.close}
      showClose={false}
      className="offline-modal"
      panelProps={{ "data-testid": "offline-modal" } as React.HTMLAttributes<HTMLElement>}
    >
      <div className="offline-dialog">
        <div className="offline-dialog__bar">
          <p className="offline-dialog__eyebrow">{uz.offline.eyebrow}</p>
          <button
            type="button"
            className="offline-dialog__close"
            aria-label={uz.offline.close}
            onClick={() => setDismissed(true)}
          >
            <Icon name="close" className="text-[15px]" />
          </button>
        </div>

        <span className="offline-dialog__tile" aria-hidden="true">
          <Icon name="wifi_off" className="text-[32px]" />
        </span>

        <div className="offline-dialog__message">
          {/* The accessible name already comes from the hidden canonical <h2>. */}
          <p className="offline-dialog__title" aria-hidden="true">{uz.offline.title}</p>
          <p className="offline-dialog__text">{uz.offline.text}</p>
        </div>

        <div className="offline-dialog__notice">
          <Icon name="bolt" className="text-[19px]" />
          <p>
            {uz.offline.noticeTitle}
            <br />
            {uz.offline.noticeText}
          </p>
        </div>

        {strip && (
          <div
            className={`offline-dialog__strip offline-dialog__strip--${strip.tone}`}
            role="status"
            aria-live="polite"
          >
            <Icon
              name={strip.icon}
              className={strip.spin ? "text-[22px] offline-dialog__spinner" : "text-[22px]"}
            />
            <div>
              <p className="offline-dialog__strip-title">{strip.title}</p>
              <p className="offline-dialog__strip-text">{strip.text}</p>
            </div>
          </div>
        )}

        <div className="offline-dialog__actions">
          <AppButton
            fullWidth
            leadingIcon="refresh"
            className="offline-dialog__retry"
            loading={probe === "checking"}
            onClick={retry}
          >
            {uz.offline.retry}
          </AppButton>
          <button type="button" className="offline-dialog__dismiss" onClick={() => setDismissed(true)}>
            {uz.offline.dismiss}
          </button>
        </div>

        <p className="offline-dialog__hint">{uz.offline.hint}</p>
      </div>
    </DesignModal>
  );
}
