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
import "./SpeakingChrome.css";

/** Pen 46. Speaking catalog routes own their light header instead of inheriting dashboard chrome. */
export function SpeakingChrome({ children, catalog }: { children: ReactNode; catalog: boolean }) {
  const location = useLocation();
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { energy, openEnergyModal } = useEnergy();
  const unread = useUnreadNotifications();
  const { openNotifications } = useNotificationsModal();
  const backTarget = catalog ? "/app/speaking" : location.key === "default" ? "/home" : -1;

  return (
    <div className={`speaking-design speaking-design--${catalog ? "catalog" : "hub"}`} data-testid="speaking-chrome" data-catalog={catalog}>
      <header className="speaking-header" aria-label="Speaking navigatsiyasi">
        <div className="speaking-header__leading">
          {typeof backTarget === "string" ? (
            <Link to={backTarget} className="speaking-header__back" aria-label="Orqaga"><ArrowLeft size={20} /></Link>
          ) : (
            <button type="button" className="speaking-header__back" aria-label="Orqaga" onClick={() => window.history.back()}><ArrowLeft size={20} /></button>
          )}
          <Link to="/home" className="speaking-header__brand" aria-label="EnglishAI bosh sahifa"><EnglishAiLogo size={36} withWordmark /></Link>
        </div>
        <div className="speaking-header__status">
          <span className="speaking-header__xp"><Star size={16} />{points ? `${points.lifetimeXp} XP` : "— XP"}</span>
          <button type="button" className="speaking-header__energy" onClick={() => openEnergyModal()}><Zap size={18} />{energy ? `${energy.current} / ${energy.maximum}` : "— / —"}</button>
          <span className="speaking-tag speaking-header__module">Speaking</span>
          <button type="button" className="speaking-header__bell" aria-label="Bildirishnomalar" onClick={openNotifications}>
            <Bell size={22} />{unread > 0 && <span>{unread > 9 ? "9+" : unread}</span>}
          </button>
        </div>
      </header>
      {children}
    </div>
  );
}
