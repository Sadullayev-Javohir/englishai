import { useEffect, useMemo, useState } from "react";
import type { EnergyAction, EnergyDto } from "@/api/types";
import { DesignModal } from "@/components/design";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import "./energyModal.css";

export interface PendingEnergyAction {
  action: EnergyAction;
  /** Already includes the activity wording when a caller needs a custom label. */
  label?: string;
  resume?: () => void | Promise<void>;
}

interface EnergyModalProps {
  open: boolean;
  energy: EnergyDto | null;
  pending?: PendingEnergyAction;
  onClose: () => void;
  onPrimary: () => void;
}

function remaining(instant: string | null, now: number): string {
  if (!instant) return "00:00:00";
  const seconds = Math.max(0, Math.ceil((new Date(instant).getTime() - now) / 1000));
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const remainder = seconds % 60;
  return [hours, minutes, remainder].map((part) => String(part).padStart(2, "0")).join(":");
}

export function EnergyModal({ open, energy, pending, onClose, onPrimary }: EnergyModalProps) {
  const [now, setNow] = useState(() => Date.now());
  const current = energy?.current ?? 0;
  const maximum = energy?.maximum ?? 5;
  const full = current >= maximum;

  useEffect(() => {
    if (!open || full) return;
    setNow(Date.now());
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [full, open]);

  const copy = useMemo(() => {
    if (current <= 0) {
      return { title: uz.energy.emptyTitle, text: uz.energy.emptyText };
    }
    if (full) {
      return { title: uz.energy.fullTitle, text: uz.energy.fullText };
    }
    return { title: uz.energy.restoredTitle(current), text: uz.energy.restoredText(current) };
  }, [current, full]);

  const primary = pending?.label
    ?? (pending?.action === "speaking" ? uz.energy.resumeSpeaking : current === 0 ? uz.energy.alternative : uz.energy.resumeVideo);
  const secondary = current === 0 ? uz.energy.gotIt : uz.energy.close;

  return (
    <DesignModal
      open={open}
      onClose={onClose}
      title={uz.energy.eyebrow}
      closeLabel={uz.common.close}
      showClose={false}
      className="energy-modal"
    >
      <div className="energy-modal__content" data-testid={`energy-modal-${current}`}>
        <div className="energy-modal__hero" aria-hidden="true">
          <span className="energy-modal__hero-bolt"><Icon name="bolt" filled /></span>
          <span className="energy-modal__hero-orbit energy-modal__hero-orbit--one" />
          <span className="energy-modal__hero-orbit energy-modal__hero-orbit--two" />
        </div>

        <p className="energy-modal__eyebrow">{uz.energy.eyebrow}</p>
        <h3>{copy.title}</h3>
        <p className="energy-modal__description">{copy.text}</p>

        <div className="energy-modal__cells" aria-label={`${current} / ${maximum} energiya`}>
          {Array.from({ length: maximum }, (_, index) => (
            <span key={index} className={`energy-modal__cell ${index < current ? "is-full" : ""}`}>
              <Icon name="bolt" filled />
            </span>
          ))}
        </div>
        <p className="energy-modal__one-at-a-time">{uz.energy.oneAtATime}</p>

        {full ? (
          <div className="energy-modal__clock energy-modal__clock--ready">
            <Icon name="check_circle" filled />
            <div><span>{uz.energy.restorationFinished}</span><strong>{uz.energy.ready}</strong></div>
            <div className="energy-modal__clock-score"><strong>5 / 5</strong><span>{uz.energy.startNew}</span></div>
          </div>
        ) : (
          <div className="energy-modal__clock">
            <Icon name="schedule" />
            <div><span>{uz.energy.nextRefill}</span><strong>{remaining(energy?.nextRefillAt ?? null, now)}</strong></div>
            <div className="energy-modal__clock-score"><span>{uz.energy.fullRefill}</span><strong>{remaining(energy?.fullRefillAt ?? null, now)}</strong></div>
          </div>
        )}

        <div className="energy-modal__actions">
          <button type="button" className="energy-modal__primary" onClick={onPrimary}>{primary}<Icon name="arrow_forward" /></button>
          <button type="button" className="energy-modal__secondary" onClick={onClose}>{secondary}</button>
        </div>
        <p className="energy-modal__hint"><Icon name="info" />{uz.energy.hint}</p>
      </div>
    </DesignModal>
  );
}
