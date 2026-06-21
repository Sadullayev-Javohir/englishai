import { ArrowLeft, Bell, Star, Zap } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import { api } from "@/api/client";
import type { EnergyDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import { useAsync } from "@/lib/useAsync";
import { useNotificationsModal } from "@/lib/useNotificationsModal";
import { useUnreadNotifications } from "@/lib/useUnreadNotifications";
import { useEnergy } from "@/components/game/EnergyProvider";

/** Pen 72: real account balances, not the example's 360 XP / 4 energy. */
export function VideoPlayerHeader({ energy: suppliedEnergy }: { energy?: EnergyDto | null }) {
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const unread = useUnreadNotifications();
  const { openNotifications } = useNotificationsModal();
  const location = useLocation();
  const { energy: sharedEnergy, openEnergyModal } = useEnergy();
  const energy = sharedEnergy ?? suppliedEnergy ?? null;
  const returnTo = (location.state as { returnTo?: unknown } | null)?.returnTo;
  const backTo = typeof returnTo === "string" && /^\/(?:video(?:\/[^?#]*)?|progress)(?:[?#].*)?$/.test(returnTo)
    ? returnTo : "/video";

  return (
    <header className="video-player-header" aria-label="Video navigatsiyasi">
      <div className="video-player-header__leading">
        <Link to={backTo} className="video-player-header__back" aria-label="Orqaga"><ArrowLeft size={20} aria-hidden /></Link>
        <Link to="/home" aria-label="EnglishAI bosh sahifa"><EnglishAiLogo size={40} withWordmark /></Link>
      </div>
      <div className="video-player-header__status">
        <Link to="/progress" className="video-player-stat video-player-stat--xp" aria-label={`Tajriba ballari: ${points?.lifetimeXp ?? "—"} XP`}>
          <Star size={18} fill="currentColor" aria-hidden /><span>{points ? `${points.lifetimeXp} XP` : "— XP"}</span>
        </Link>
        <button type="button" onClick={() => openEnergyModal()} className="video-player-stat video-player-stat--energy" aria-label={`Energiya: ${energy ? `${energy.current} / ${energy.maximum}` : "yuklanmoqda"}`}>
          <Zap size={20} fill="currentColor" aria-hidden /><span>{energy ? `${energy.current} / ${energy.maximum}` : "— / —"}</span>
        </button>
        <span className="video-player-header__module">Video</span>
        <button type="button" className="video-player-header__bell" aria-label="Bildirishnomalar" onClick={openNotifications}>
          <Bell size={20} aria-hidden />{unread > 0 && <span>{unread > 99 ? "99+" : unread}</span>}
        </button>
      </div>
    </header>
  );
}
