import type { ReactNode } from "react";
import { ArrowLeft, Heart } from "lucide-react";
import { Link } from "react-router-dom";
import "./LessonProgress.css";

export interface LessonStep {
  id: string;
  label: string;
}

/** Shared vocabulary/grammar stepper. Labels describe actual sections, not
 * decorative segments; optional question detail does not add phantom steps. */
export function LessonProgress({
  steps, step, complete = false, substep, hearts, heartLabel,
  onBack, backTarget, backLabel = "Orqaga", backDisabled, action, classPrefix = "lesson-progress",
}: {
  steps: readonly LessonStep[];
  step: string;
  complete?: boolean;
  substep?: string;
  hearts?: number;
  heartLabel?: string;
  onBack?: () => void;
  backTarget?: string;
  backLabel?: string;
  backDisabled?: boolean;
  action?: ReactNode;
  /** Retains feature selectors without duplicating layout or state logic. */
  classPrefix?: string;
}) {
  const index = steps.findIndex(item => item.id === step);
  if (index < 0) return null;
  const current = index + 1;
  const label = steps[index].label;
  const names = (suffix = "") => `lesson-progress${suffix}${classPrefix === "lesson-progress" ? "" : ` ${classPrefix}${suffix}`}`;
  return (
    <div className={names()} data-lesson-step={step} data-complete={complete}>
      {backTarget
        ? <Link to={backTarget} aria-label={backLabel}><ArrowLeft size={20} /></Link>
        : onBack && <button type="button" disabled={backDisabled} onClick={onBack} aria-label={backLabel}><ArrowLeft size={20} /></button>}
      <div className={names("__body")}>
        <div className={names("__track")} role="progressbar" aria-label="Dars bosqichlari"
          aria-valuemin={1} aria-valuemax={steps.length} aria-valuenow={current}
          aria-valuetext={`${current} / ${steps.length} · ${label}${substep ? ` · ${substep}` : ""}`}>
          {steps.map((item, itemIndex) => (
            <span key={item.id} className={`${names("__segment")}${itemIndex < index || complete ? " is-done" : ""}${itemIndex === index ? " is-current" : ""}`}
              aria-current={itemIndex === index ? "step" : undefined}>
              <small>{item.label}</small>
            </span>
          ))}
        </div>
        <span className={names("__mobile-label")}>{label}{substep && <span className={names("__substep")}>{substep}</span>}</span>
        {substep && <span className="lesson-progress__detail">{substep}</span>}
      </div>
      <span className={names("__position")}>{current} / {steps.length}</span>
      {hearts !== undefined && <span className={names("__hearts")} aria-label={heartLabel ?? `${hearts} ta jon`}><Heart size={18} />{hearts}</span>}
      {action}
    </div>
  );
}
