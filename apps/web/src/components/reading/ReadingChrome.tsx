import type { ReactNode } from "react";
import { ArrowLeft, Bell, Star, Zap } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import { useEnergy } from "@/components/game/EnergyProvider";
import { useAsync } from "@/lib/useAsync";
import { useNotificationsModal } from "@/lib/useNotificationsModal";
import { useUnreadNotifications } from "@/lib/useUnreadNotifications";
import { lessonBackTarget, lessonOriginFrom } from "@/lib/lessonNavigation";
import "./ReadingChrome.css";

/** Pen 31–35. Canvas annotations and the drawn phone status bar are not app UI. */
export function ReadingChrome({ children, catalog }: { children: ReactNode; catalog: boolean }) {
  const location = useLocation();
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { energy, openEnergyModal } = useEnergy();
  const unread = useUnreadNotifications();
  const { openNotifications } = useNotificationsModal();
  return (
    <div className={`reading-design reading-design--${catalog ? "catalog" : "lesson"}`} data-anim="reading">
      <header className="reading-header" aria-label="Reading navigatsiyasi">
        <div className="reading-header__leading">
          <Link to={catalog ? "/home" : lessonBackTarget("reading", lessonOriginFrom(location))} className="reading-header__back" aria-label="Orqaga"><ArrowLeft size={20} /></Link>
          <Link to="/home" className="reading-header__brand" aria-label="EnglishAI bosh sahifa"><EnglishAiLogo size={36} withWordmark /></Link>
        </div>
        <div className="reading-header__status">
          <span className="reading-header__xp"><Star size={16} />{points ? `${points.lifetimeXp} XP` : "— XP"}</span>
          <button type="button" className="reading-header__energy" onClick={() => openEnergyModal()}><Zap size={18} />{energy ? `${energy.current} / ${energy.maximum}` : "— / —"}</button>
          <span className="reading-tag reading-header__module">Reading</span>
          <button type="button" className="reading-header__bell" aria-label="Bildirishnomalar" onClick={openNotifications}>
            <Bell size={22} />{unread > 0 && <span>{unread > 9 ? "9+" : unread}</span>}
          </button>
        </div>
      </header>
      {children}
    </div>
  );
}
