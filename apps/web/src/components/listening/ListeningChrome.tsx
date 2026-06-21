import type { ReactNode } from "react";
import { ArrowLeft, Bell, Star, Zap } from "lucide-react";
import { Link } from "react-router-dom";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import { useEnergy } from "@/components/game/EnergyProvider";
import { useAsync } from "@/lib/useAsync";
import { useNotificationsModal } from "@/lib/useNotificationsModal";
import { useUnreadNotifications } from "@/lib/useUnreadNotifications";
import "./ListeningChrome.css";

/** Pen 36–40. The header and lesson share ONE viewport, not two 100vh surfaces. */
export function ListeningChrome({ children, catalog }: { children: ReactNode; catalog: boolean }) {
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { energy, openEnergyModal } = useEnergy();
  const unread = useUnreadNotifications();
  const { openNotifications } = useNotificationsModal();
  return (
    <div className={`listening-design${catalog ? " listening-design--catalog" : " listening-design--lesson"}`}>
      <header className="listening-header" aria-label="Listening navigatsiyasi">
        <div className="listening-header__leading">
          <Link to={catalog ? "/home" : "/listening"} className="listening-header__back" aria-label="Orqaga"><ArrowLeft size={20} /></Link>
          <Link to="/home" className="listening-header__brand" aria-label="EnglishAI bosh sahifa"><EnglishAiLogo size={36} withWordmark /></Link>
        </div>
        <div className="listening-header__status">
          <span className="listening-header__xp"><Star size={16} />{points ? `${points.lifetimeXp} XP` : "— XP"}</span>
          <button type="button" className="listening-header__energy" onClick={() => openEnergyModal()}><Zap size={18} />{energy ? `${energy.current} / ${energy.maximum}` : "— / —"}</button>
          <span className="listening-tag listening-header__module">Listening</span>
          <button type="button" className="listening-header__bell" aria-label="Bildirishnomalar" onClick={openNotifications}>
            <Bell size={21} />{unread > 0 && <span>{unread > 9 ? "9+" : unread}</span>}
          </button>
        </div>
      </header>
      {children}
    </div>
  );
}
