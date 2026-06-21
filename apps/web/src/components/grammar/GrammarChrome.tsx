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
import "./GrammarChrome.css";

/** Pen 21–30. Annotation strips and phone system status are not web UI. */
export function GrammarChrome({ children, catalog }: { children: ReactNode; catalog: boolean }) {
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { energy, openEnergyModal } = useEnergy();
  const unread = useUnreadNotifications();
  const { openNotifications } = useNotificationsModal();
  return (
    <div className={`grammar-design grammar-design--${catalog ? "catalog" : "lesson"}`}>
      <header className="grammar-header" aria-label="Grammar navigatsiyasi">
        <div className="grammar-header__leading">
          <Link className="grammar-header__back" to={catalog ? "/home" : "/app/grammar"} aria-label="Orqaga"><ArrowLeft size={20} /></Link>
          <Link to="/home" className="grammar-header__brand" aria-label="EnglishAI bosh sahifa"><EnglishAiLogo size={40} withWordmark /></Link>
        </div>
        <div className="grammar-header__status">
          <span className="grammar-header__xp"><Star size={16} />{points ? `${points.lifetimeXp} XP` : "— XP"}</span>
          <button type="button" className="grammar-header__energy" onClick={() => openEnergyModal()}><Zap size={18} />{energy ? `${energy.current} / ${energy.maximum}` : "— / —"}</button>
          <span className="grammar-tag grammar-header__module">Grammar</span>
          <button type="button" className="grammar-header__bell" aria-label="Bildirishnomalar" onClick={openNotifications}>
            <Bell size={22} />{unread > 0 && <span>{unread > 9 ? "9+" : unread}</span>}
          </button>
        </div>
      </header>
      {children}
    </div>
  );
}
