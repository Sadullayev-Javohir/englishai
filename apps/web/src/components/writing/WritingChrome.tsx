import type { ReactNode } from "react";
import { Bell, Star, Zap } from "lucide-react";
import { Link } from "react-router-dom";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import { useEnergy } from "@/components/game/EnergyProvider";
import { useAsync } from "@/lib/useAsync";
import { useNotificationsModal } from "@/lib/useNotificationsModal";
import { useUnreadNotifications } from "@/lib/useUnreadNotifications";
import "./WritingChrome.css";

/** Pen 41–45 share a single Writing shell. The dark URL strip in the Pen file
 * is documentation only; this is the application header below it. */
export function WritingChrome({ children, catalog = false }: { children: ReactNode; catalog?: boolean }) {
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { energy, openEnergyModal } = useEnergy();
  const unread = useUnreadNotifications();
  const { openNotifications } = useNotificationsModal();
  const backTarget = catalog ? "/home" : "/writing";

  return (
    <div className={`writing-design${catalog ? " writing-design--catalog" : " writing-design--lesson"}`} data-anim={catalog ? undefined : "writing"}>
      <header className="writing-header">
        <div className="writing-header__leading">
          <Link className="writing-header__back" to={backTarget} aria-label="Orqaga">
            <span aria-hidden="true">←</span>
          </Link>
          <Link to="/home" className="writing-header__brand" aria-label="EnglishAI bosh sahifa">
            <EnglishAiLogo size={40} withWordmark />
          </Link>
        </div>
        <div className="writing-header__status">
          <span className="writing-header__xp"><Star size={16} />{points ? `${points.lifetimeXp} XP` : "— XP"}</span>
          <button type="button" className="writing-header__energy" onClick={() => openEnergyModal()}><Zap size={18} />{energy ? `${energy.current} / ${energy.maximum}` : "— / —"}</button>
          <span className="writing-tag">Writing</span>
          <button type="button" className="writing-header__bell" aria-label="Bildirishnomalar" onClick={openNotifications}>
            <Bell size={22} />
            {unread > 0 && <span>{unread > 9 ? "9+" : unread}</span>}
          </button>
        </div>
      </header>
      {children}
    </div>
  );
}
