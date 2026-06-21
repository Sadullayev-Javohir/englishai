import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { ArrowLeft, Bell, Bolt, ChartNoAxesCombined, House, Route, Shapes, Star, UserRound } from "lucide-react";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import { routeReturnTarget } from "@/lib/routeReturn";
import { tapLight } from "@/lib/haptics";
import { useAsync } from "@/lib/useAsync";
import { useNotificationsModal } from "@/lib/useNotificationsModal";
import { useUnreadNotifications } from "@/lib/useUnreadNotifications";
import { useEnergy } from "@/components/game/EnergyProvider";

const focus = "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-ea-primary";

/** Screen 56 owns its complete header rather than inheriting the dashboard chrome. */
export function VideoCatalogHeader() {
  const navigate = useNavigate();
  const location = useLocation();
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { energy, openEnergyModal } = useEnergy();
  const unread = useUnreadNotifications();
  const { openNotifications } = useNotificationsModal();

  return (
    <header className="video-catalog-header" aria-label="Video navigatsiyasi">
      <div className="video-catalog-header__leading">
        <button
          type="button"
          aria-label="Orqaga"
          onClick={() => navigate(routeReturnTarget(location))}
          className={`video-catalog-header__back ${focus}`}
        >
          <ArrowLeft size={20} aria-hidden />
        </button>
        <button type="button" aria-label="EnglishAI bosh sahifa" onClick={() => navigate("/home")} className={`video-catalog-header__brand ${focus}`}>
          <EnglishAiLogo size={40} imgSize={40} withWordmark />
        </button>
      </div>

      <div className="video-catalog-header__status">
        <button type="button" title="Tajriba ballari" onClick={() => navigate("/progress")} className={`video-catalog-header__stat video-catalog-header__stat--xp ${focus}`}>
          <Star size={18} fill="currentColor" aria-hidden />
          <span>{points ? `${points.lifetimeXp} XP` : "— XP"}</span>
        </button>
        <button type="button" title="Energiya holati" onClick={() => openEnergyModal()} className={`video-catalog-header__stat video-catalog-header__stat--energy ${focus}`}>
          <Bolt size={20} fill="currentColor" aria-hidden />
          <span>{energy ? `${energy.current} / ${energy.maximum}` : "— / —"}</span>
        </button>
        <span className="video-catalog-header__module">Video</span>
        <button
          type="button"
          aria-label="Bildirishnomalar"
          onClick={() => { tapLight(); openNotifications(); }}
          className={`video-catalog-header__notifications ${focus}`}
        >
          <Bell size={20} aria-hidden />
          {unread > 0 && <span>{unread > 99 ? "99+" : unread}</span>}
        </button>
      </div>
    </header>
  );
}

export function VideoCatalogMobileNav() {
  const navigate = useNavigate();
  const [keyboardOpen, setKeyboardOpen] = useState(false);

  useEffect(() => {
    const viewport = window.visualViewport;
    if (!viewport) return;
    const update = () => setKeyboardOpen(window.innerHeight - viewport.height > 150);
    viewport.addEventListener("resize", update);
    return () => viewport.removeEventListener("resize", update);
  }, []);

  const items = [
    { label: "Bugun", to: "/home", icon: House },
    { label: "Yo‘l", to: "/levels", icon: Route },
    { label: "Mashq", to: "/home#home-skills-heading", icon: Shapes },
    { label: "Natija", to: "/progress", icon: ChartNoAxesCombined },
    { label: "Profil", to: "/profile", icon: UserRound },
  ];

  return (
    <nav aria-label="Mobil navigatsiya" className={`video-catalog-nav ${keyboardOpen ? "video-catalog-nav--hidden" : ""}`}>
      <div className="video-catalog-nav__surface">
        {items.map(({ label, to, icon: NavIcon }, index) => (
          <button
            key={to}
            type="button"
            onClick={() => { tapLight(); navigate(to); }}
            className={`video-catalog-nav__item ${focus} ${index === 0 ? "is-active" : ""}`}
          >
            <NavIcon size={20} aria-hidden />
            <span>{label}</span>
          </button>
        ))}
      </div>
    </nav>
  );
}
