import type { ReactNode } from "react";
import { ArrowLeft, ArrowRight, Bell, Star, Zap } from "lucide-react";
import { Link } from "react-router-dom";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import { useEnergy } from "@/components/game/EnergyProvider";
import { useAsync } from "@/lib/useAsync";
import { useNotificationsModal } from "@/lib/useNotificationsModal";
import { useUnreadNotifications } from "@/lib/useUnreadNotifications";
import "./VocabularyChrome.css";
import "./VocabularyResponsive.css";
import { VOCABULARY_LESSON_STEPS, type VocabularyLessonStep } from "./vocabularyLessonSteps";
import "./VocabularyProgress.css";
import { LessonProgress } from "@/components/lesson/LessonProgress";

/** Pen 11–20 share this chrome. The canvas's URL annotation is not application UI. */
export function VocabularyChrome({ children, catalog = false }: { children: ReactNode; catalog?: boolean }) {
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { energy, openEnergyModal } = useEnergy();
  const unread = useUnreadNotifications();
  const { openNotifications } = useNotificationsModal();
  return (
    <div className={`vocabulary-design${catalog ? "" : " vocabulary-design--lesson"}`} data-anim={catalog ? undefined : "vocabulary"}>
      <header className="vocabulary-header">
        <div className="vocabulary-header__leading">
          <Link className="vocabulary-header__back" to={catalog ? "/home" : "/app/vocabulary/topics"} aria-label="Orqaga"><ArrowLeft size={20} /></Link>
          <Link to="/home" className="vocabulary-header__brand" aria-label="EnglishAI bosh sahifa"><EnglishAiLogo size={40} withWordmark /></Link>
        </div>
        <div className="vocabulary-header__status">
          <span className="vocabulary-header__xp"><Star size={16} />{points ? `${points.lifetimeXp} XP` : "— XP"}</span>
          <button type="button" className="vocabulary-header__energy" onClick={() => openEnergyModal()}><Zap size={18} />{energy ? `${energy.current} / ${energy.maximum}` : "— / —"}</button>
          <span className="vocabulary-tag">Vocabulary</span>
          <button type="button" className="vocabulary-header__bell" aria-label="Bildirishnomalar" onClick={openNotifications}>
            <Bell size={22} />
            {unread > 0 && <span>{unread > 9 ? "9+" : unread}</span>}
          </button>
        </div>
      </header>
      {children}
    </div>
  );
}

export function VocabularyProgress({ step, hearts, onBack, backTarget, substep }: {
  step: VocabularyLessonStep; hearts?: number; onBack?: () => void; backTarget?: string; substep?: string;
}) {
  return <LessonProgress steps={VOCABULARY_LESSON_STEPS} step={step} complete={step === "result"}
    hearts={hearts} onBack={onBack} backTarget={backTarget} substep={substep} classPrefix="vocabulary-progress" />;
}

export function VocabularyAction({ children, onClick, disabled, type = "button", className = "" }: {
  children: ReactNode; onClick?: () => void; disabled?: boolean; type?: "button" | "submit"; className?: string;
}) {
  return <button type={type} className={`vocabulary-primary ${className}`} onClick={onClick} disabled={disabled}>{children}<ArrowRight size={20} /></button>;
}

export function VocabularyHeading({ title, subtitle }: { title: string; subtitle?: string }) {
  return <div className="vocabulary-heading"><span className="vocabulary-tag">VOCABULARY</span><h1>{title}</h1>{subtitle && <p>{subtitle}</p>}</div>;
}
