import { ArrowLeft, ArrowRight, Heart } from "lucide-react";
import { Link } from "react-router-dom";
import type { ReactNode } from "react";
import { uz } from "@/content/uz";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import "./VideoLessonChrome.css";

export function VideoLessonHeader({ backTo, onBack }: { backTo?: string; onBack?: () => void }) {
  const backClass = "video-lesson-back";
  return (
    <header className="video-lesson-header">
      {onBack
        ? <button type="button" onClick={onBack} className={backClass} aria-label={uz.common.back}><ArrowLeft size={22} /></button>
        : <Link to={backTo ?? "/video"} className={backClass} aria-label={uz.common.back}><ArrowLeft size={22} /></Link>}
      <Link to="/home" aria-label="EnglishAI" className="video-lesson-wordmark"><EnglishAiLogo size={36} withWordmark /></Link>
    </header>
  );
}

export function VideoLessonProgress({ current, total, onBack, hearts }: {
  current: number; total: number; onBack: () => void; hearts?: number;
}) {
  return (
    <div className="video-lesson-progress">
      <button type="button" onClick={onBack} aria-label={uz.common.back}><ArrowLeft size={20} /></button>
      <div className="video-lesson-progress__track" role="progressbar" aria-label="Mashq jarayoni" aria-valuemin={0} aria-valuemax={total} aria-valuenow={current}>
        {Array.from({ length: 8 }, (_, i) => <span key={i} className={i < Math.ceil(8 * current / Math.max(1, total)) ? "is-done" : ""} />)}
      </div>
      <span>{current} / {total}</span>
      {hearts !== undefined && <span className="video-lesson-hearts"><Heart size={18} />{hearts}</span>}
    </div>
  );
}

export function VideoLessonHeading({ children }: { children: ReactNode }) {
  return <div className="video-lesson-heading"><span className="video-lesson-tag">VIDEO</span><h1>{children}</h1></div>;
}

export function VideoLessonAction({ children, onClick, disabled = false }: {
  children: ReactNode; onClick: () => void; disabled?: boolean;
}) {
  return <button type="button" className="video-lesson-primary" onClick={onClick} disabled={disabled}>{children}<ArrowRight size={20} /></button>;
}
