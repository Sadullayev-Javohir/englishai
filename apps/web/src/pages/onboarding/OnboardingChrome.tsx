import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, ArrowRight, Circle, CircleCheck } from "lucide-react";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import "./OnboardingChrome.css";

/** Shared chrome for englishai.pen WEB frames 02–09.
 * The dark “WEB 1440 / route” strip belongs to the design document, not the UI.
 */
export function OnboardingChrome({
  children, className = "", step, backTo = "/", onBack, testId, secureAssessment = false,
}: {
  children: ReactNode;
  className?: string;
  step?: number;
  backTo?: string;
  onBack?: () => void;
  testId?: string;
  secureAssessment?: boolean;
}) {
  const navigate = useNavigate();
  return (
    <div className={`onboarding-play ${className}`} data-testid={testId} data-secure-assessment-surface={secureAssessment || undefined}>
      <header className="onboarding-play__nav">
        <div className="onboarding-play__brand">
          <button type="button" className="onboarding-play__back" aria-label="Ortga qaytish" onClick={onBack ?? (() => navigate(backTo))}>
            <ArrowLeft size={20} aria-hidden />
          </button>
          <Link to="/" aria-label="EnglishAI.uz bosh sahifasi" onClick={secureAssessment ? (event) => { event.preventDefault(); onBack?.(); } : undefined}><EnglishAiLogo size={40} withWordmark /></Link>
        </div>
        {step && (
          <div className="onboarding-play__setup">
            <StepProgress value={step} total={3} label="Profilni sozlash jarayoni" />
            <p>{step} / 3 · Shaxsiy profilingiz</p>
          </div>
        )}
      </header>
      <main className="onboarding-play__main">{children}</main>
      <footer className="onboarding-play__footer">
        <span>EnglishAI · Bir qadam yaqinroq.</span>
      </footer>
    </div>
  );
}

export function PlayButton({ children, className = "", arrow = true, ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { arrow?: boolean }) {
  return <button type="button" {...props} className={`onboarding-play__button ${className}`}>{children}{arrow && <ArrowRight size={20} aria-hidden />}</button>;
}

export function PlayChip({ children, tone = "purple" }: { children: ReactNode; tone?: "purple" | "green" | "white" }) {
  return <span className={`onboarding-play__chip onboarding-play__chip--${tone}`}>{children}</span>;
}

export function StepProgress({ value, total, label }: { value: number; total: number; label: string }) {
  const safeTotal = Math.max(1, total);
  const safeValue = Math.max(0, Math.min(value, safeTotal));
  return (
    <div className="onboarding-play__progress" role="progressbar" aria-label={label} aria-valuemin={0} aria-valuemax={safeTotal} aria-valuenow={safeValue}>
      {Array.from({ length: safeTotal }, (_, index) => <span key={index} className={index < safeValue ? "is-complete" : ""} />)}
    </div>
  );
}

export function PlayCompanion({ message, className = "", secure = false }: { message: ReactNode; className?: string; secure?: boolean }) {
  return (
    <aside className={`onboarding-play__companion ${className}`}>
      <p className="onboarding-play__speech">{message}</p>
      <img src="/assets/play/parrot.svg" alt="EnglishAI to‘tiqushi" />
      {secure && <PlayChip tone="green">Ma’lumotlaringiz xavfsiz</PlayChip>}
    </aside>
  );
}

export function PlayOption({
  children, icon, selected, className = "", compact = false, ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { icon: ReactNode; selected: boolean; compact?: boolean }) {
  return (
    <button type="button" {...props} aria-pressed={selected} className={`onboarding-play__option ${selected ? "is-selected" : ""} ${compact ? "is-compact" : ""} ${className}`}>
      <span className="onboarding-play__option-icon">{icon}</span>
      <span className="onboarding-play__option-label">{children}</span>
      {!compact && (selected ? <CircleCheck className="onboarding-play__selection" size={20} aria-hidden /> : <Circle className="onboarding-play__selection" size={20} aria-hidden />)}
    </button>
  );
}
