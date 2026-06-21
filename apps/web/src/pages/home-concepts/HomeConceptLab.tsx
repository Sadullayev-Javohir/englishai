import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { useLocation, useNavigate } from "react-router-dom";
import { Icon } from "@/components/ui/Icon";
import { ParrotLogo } from "@/components/ParrotLogo";
import { UserAvatar } from "@/components/UserAvatar";
import { NotificationBell } from "@/components/NotificationBell";
import { useEnergy } from "@/components/game/EnergyProvider";
import { AppButton } from "@/components/design";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { formatCompactNumber } from "@/lib/formatCompactNumber";
import { openSkill } from "@/lib/skillSteps";
import { consumeHomeFallback } from "./homeRecommendation";
import type { HomeAction, HomeViewModel } from "./homeViewModel";
import { Bookmark, ChartNoAxesCombined, Clapperboard, House, Route, ShieldCheck, Star, Trophy, UserRound, Zap } from "lucide-react";
import { getNativeCapabilities } from "@/api/nativeCapabilities";
import { DailyHabit, DailyLesson, DailyWord, DueReviewCard, HomeDiscoveries, HomeStats, SkillLauncher, VideoRecommendations } from "./HomeDashboardSections";
import { homeFocus } from "./homeDashboardContent";
import { HomeEntryModal, selectHomeEntryPrompt, type HomeEntryPrompt } from "./HomeEntryModal";
import "./homeConceptLab.css";

export type HomeConceptModel = HomeViewModel;
const NAV = [
  { to: "/home", icon: "home", label: "Bugun" },
  { to: "/levels", icon: "route", label: "O‘quv yo‘li" },
  { to: "/progress", icon: "insights", label: "Natijalar" },
  { to: "/leaderboard", icon: "emoji_events", label: "Reyting" },
];
export function isHomeNavRoute(pathname: string) { return pathname === "/home"; }
export function isPracticeNavRoute(pathname: string) {
  return ["/app/speaking", "/app/vocabulary/topics", "/app/grammar", "/reading", "/writing", "/listening", "/books", "/video"].some(p => pathname === p || pathname.startsWith(p + "/"));
}
function Brand() { return <span className="play-brand"><ParrotLogo size={40} withWordmark /></span>; }

function HomeNavIcon({ to }: { to: string }) {
  const icons: Record<string, typeof House> = {
    "/home": House, "/levels": Route, "/video": Clapperboard,
    "/progress": ChartNoAxesCombined, "/leaderboard": Trophy, "/profile": UserRound,
  };
  const NavIcon = icons[to] ?? House;
  return <NavIcon size={20} aria-hidden />;
}

export function SportSideNav({ model, homeDesign = false }: { model: HomeViewModel; homeDesign?: boolean }) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const profileRoute = pathname === "/profile";
  const { data: access } = useAsync(() => api.admin.access(), []);
  if (homeDesign) {
    const items = profileRoute
      ? [...NAV, { to: "/profile", icon: "person", label: "Profil" }]
      : [...NAV.slice(0, 2), ...(getNativeCapabilities().video ? [{ to: "/video", icon: "movie", label: "Video darslar" }] : []), ...NAV.slice(2)];
    return <aside aria-label={`${model.name} o‘quv navigatsiyasi`} className={`hidden ${profileRoute ? "w-56" : "w-60"} shrink-0 self-stretch border-r border-ea-border px-4 py-7 min-[1200px]:block`}>
      <div className="sticky top-[112px] flex flex-col gap-[9px]">
        <span className="text-[10px] font-extrabold leading-[1.5] text-[var(--home-muted)]">O‘QUV MARKAZI</span>
        <nav aria-label="Asosiy navigatsiya" className="flex flex-col gap-[9px]">
          {items.map(item => <button key={item.to} type="button" aria-current={pathname === item.to ? "page" : undefined} onClick={() => navigate(item.to)} className={`flex h-12 items-center gap-3 rounded-[14px] px-3 text-left text-[13px] leading-[1.5] ${homeFocus} ${pathname === item.to ? "bg-ea-primary-soft font-extrabold text-ea-primary" : "font-semibold text-[var(--home-muted)] hover:bg-ea-primary-soft"}`}><HomeNavIcon to={item.to} /><span>{item.label}</span></button>)}
          {!profileRoute && access?.canManageAdmins && <button type="button" onClick={() => navigate("/admin")} className={`flex h-12 items-center gap-3 rounded-[14px] px-3 text-left text-[13px] font-semibold text-[var(--home-muted)] hover:bg-ea-primary-soft ${homeFocus}`}><ShieldCheck size={20} aria-hidden />Admin</button>}
        </nav>
      </div>
    </aside>;
  }
  return <aside className="play-sidebar" aria-label={`${model.name} o‘quv navigatsiyasi`}><span className="play-eyebrow">O‘QUV MARKAZI</span><nav aria-label="Asosiy navigatsiya">{NAV.map(item => <button key={item.to} type="button" aria-current={pathname === item.to || (item.to !== "/home" && pathname.startsWith(item.to + "/")) ? "page" : undefined} onClick={() => navigate(item.to)}><Icon name={item.icon} /><span>{item.label}</span></button>)}<button type="button" className="play-sidebar__notifications" onClick={() => navigate("/home?notifications=open")}><Icon name="notifications" />Bildirishnomalar</button>{access?.canManageAdmins && <button type="button" onClick={() => navigate("/admin")} aria-current={pathname.startsWith("/admin") ? "page" : undefined}><Icon name="admin_panel_settings" />Admin</button>}</nav></aside>;
}
export function SportTopBar({ model, homeDesign = false }: { model?: HomeViewModel; homeDesign?: boolean }) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const profileRoute = pathname === "/profile";
  const learnerId = getLearnerId();
  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);
  const { energy, openEnergyModal } = useEnergy();
  const hasScreen62Back = pathname === "/levels";
  const hasScreen63Back = pathname === "/progress";
  const hasScreen64Back = pathname === "/leaderboard";
  const hasBack = hasScreen62Back || hasScreen63Back || hasScreen64Back || profileRoute;
  if (homeDesign) {
    const header = <header aria-label="EnglishAI navigatsiyasi" className={`play-home-design fixed inset-x-0 top-0 z-[35] flex min-h-[81px] items-center gap-3 border-b border-ea-border bg-ea-surface px-5 py-4 pt-[max(16px,env(safe-area-inset-top))] min-[1200px]:h-[84px] min-[1200px]:px-10 min-[1200px]:py-5${profileRoute ? " profile-route-topbar" : ""}`}>
      <div className="flex shrink-0 items-center gap-3">
        {hasBack && (
          <button
            type="button"
            aria-label="Oldingi sahifaga qaytish"
            onClick={() => window.history.length > 1 ? navigate(-1) : navigate("/home")}
            className={`grid h-11 w-11 place-items-center rounded-[14px] border border-ea-border bg-ea-bg text-ea-text min-[1200px]:h-11 min-[1200px]:w-11 ${homeFocus}`}
          >
            <Icon name="arrow_back" />
          </button>
        )}
        <button type="button" aria-label="EnglishAI bosh sahifa" onClick={() => navigate("/home")} className={`inline-flex shrink-0 items-center gap-[6px] ${homeFocus}`}>
          <ParrotLogo size={40} withWordmark className="max-[1199px]:[&_[data-englishai-logo-tile]]:!h-8 max-[1199px]:[&_[data-englishai-logo-tile]]:!w-8 max-[1199px]:[&_img]:!h-8 max-[1199px]:[&_img]:!w-8" />
        </button>
      </div>
      <div className="ml-auto flex items-center gap-3 min-[1200px]:gap-2">
        <button type="button" title="Tajriba ballari" onClick={() => navigate("/progress")} className={`${profileRoute ? "inline-flex" : "hidden min-[1200px]:inline-flex"} h-9 items-center gap-[6px] rounded-xl bg-ea-primary-soft px-[10px] text-[12px] font-extrabold leading-[1.25] text-[var(--home-xp)] ${homeFocus}`}><Star size={18} className="text-ea-primary" aria-hidden />{points ? formatCompactNumber(points.lifetimeXp) : "—"} XP</button>
        <button type="button" aria-label="Energiya holati" onClick={() => openEnergyModal()} className={`${profileRoute ? "inline-flex" : "hidden min-[1200px]:inline-flex"} h-9 items-center gap-[6px] rounded-xl bg-[var(--home-energy-bg)] px-[10px] text-[12px] font-extrabold leading-[1.25] text-[var(--home-energy)] ${homeFocus}`}><Zap size={20} className="text-[var(--home-energy-icon)]" aria-hidden />{energy ? `${energy.current} / ${energy.maximum}` : "—"}</button>
        <NotificationBell variant="home" />
        <button type="button" aria-label="Profil" onClick={() => navigate("/profile")} className={`h-[38px] w-[38px] overflow-hidden rounded-full bg-ea-primary-soft text-[12px] font-extrabold text-ea-primary ${homeFocus}`}><UserAvatar pictureUrl={model?.pictureUrl} name={model?.name} className="h-full w-full" /></button>
      </div>
    </header>;
    return typeof document === "undefined" ? header : createPortal(header, document.body);
  }
  const header = <header className={`play-topbar${hasBack ? " lvmap-shell-topbar" : ""}${hasScreen63Back ? " progress-pen-topbar" : ""}${hasScreen64Back ? " leaderboard-pen-topbar" : ""}`}>
    <div className="lvmap-shell-topbar__brand">
      {hasBack && (
        <button
          type="button"
          className="lvmap-shell-topbar__back"
          aria-label="Oldingi sahifaga qaytish"
          onClick={() => window.history.length > 1 ? navigate(-1) : navigate("/home")}
        >
          <Icon name="arrow_back" />
        </button>
      )}
      <button className="play-brand-link" type="button" aria-label="EnglishAI bosh sahifa" onClick={() => navigate("/home")}><Brand /></button>
    </div>
    <div className="play-hud"><button type="button" className="play-xp" title="Tajriba ballari" onClick={() => navigate("/progress")}>{points ? formatCompactNumber(points.lifetimeXp) : "—"} XP</button><button className="play-energy" type="button" aria-label="Energiya holati" onClick={() => openEnergyModal()}>{energy ? `${energy.current} / ${energy.maximum}` : "—"}</button><NotificationBell /><button className="play-avatar" type="button" aria-label="Profil" onClick={() => navigate("/profile")}><UserAvatar pictureUrl={model?.pictureUrl} name={model?.name} /></button></div>
  </header>;
  return typeof document === "undefined" ? header : createPortal(header, document.body);
}
export function MobileNav({ homeDesign = false }: { homeDesign?: boolean } = {}) {
  const navigate = useNavigate(); const { pathname } = useLocation();
  const profileRoute = pathname === "/profile";
  const [keyboard, setKeyboard] = useState(false);
  useEffect(() => { const viewport = window.visualViewport; if (!viewport) return; const update = () => setKeyboard(window.innerHeight - viewport.height > 150); viewport.addEventListener("resize", update); return () => viewport.removeEventListener("resize", update); }, []);
  if (homeDesign) {
    const items = profileRoute
      ? [NAV[0], NAV[1], NAV[2], { to: "/profile", icon: "person", label: "Profil" }]
      : [NAV[0], NAV[1], ...(getNativeCapabilities().video ? [{ to: "/video", icon: "movie", label: "Video" }] : []), NAV[2], NAV[3]];
    const nav = <nav aria-label="Mobil navigatsiya" className={`play-home-design fixed inset-x-0 bottom-0 z-40 items-center gap-3 border-t border-ea-border bg-ea-surface px-3 pb-[max(18px,env(safe-area-inset-bottom))] pt-2 min-[1200px]:hidden [[data-native-keyboard-open]_&]:hidden${profileRoute ? " profile-route-mobile-nav" : ""} ${keyboard ? "hidden" : "flex"}`}>
      {items.map(item => <button type="button" key={item.to} onClick={() => navigate(item.to)} aria-current={pathname === item.to ? "page" : undefined} className={`flex min-w-0 flex-1 flex-col items-center gap-3 py-[10px] text-[9px] font-bold leading-[1.5] ${homeFocus} ${pathname === item.to ? "text-ea-primary" : "text-[var(--home-muted)]"}`}><HomeNavIcon to={item.to} /><span className="whitespace-nowrap">{item.label}</span></button>)}
    </nav>;
    return typeof document === "undefined" ? nav : createPortal(nav, document.body);
  }
  const items = [NAV[0], NAV[1], NAV[2], NAV[3]];
  const nav = <nav aria-label="Mobil navigatsiya" className={`play-mobile-nav${keyboard ? " is-keyboard-visible" : ""}`}>{items.map(item => <button type="button" key={item.to} aria-current={pathname === item.to ? "page" : undefined} onClick={() => navigate(item.to)}><Icon name={item.icon} /><span>{item.label}</span></button>)}</nav>;
  return typeof document === "undefined" ? nav : createPortal(nav, document.body);
}
export function HomeConceptLab({ model, loading, error, onRetry, wordSaved, onWordSaved, entryPrompts, entryDay }: { model: HomeViewModel; loading?: boolean; error?: unknown; onRetry?: () => void; wordSaved?: boolean; onWordSaved?: () => void; entryPrompts?: HomeEntryPrompt[]; entryDay?: string }) {
  const navigate = useNavigate();
  const learnerId = getLearnerId();
  const openAction = (action: HomeAction) => { if (action.locked) return; if (action.fallbackKind) consumeHomeFallback(learnerId); if (action.topicId && action.topicTitle && action.skillKey) { openSkill(navigate, { key: action.skillKey, icon: action.icon, module: action.key }, action.topicId, action.topicTitle, "levels"); } else navigate(action.to); };
  const [dismissedStreakAlert, setDismissedStreakAlert] = useState(false);
  const [entryPrompt, setEntryPrompt] = useState<HomeEntryPrompt | null>(null);
  const [entryPromptInitialized, setEntryPromptInitialized] = useState(false);
  const dismissStreakAlert = () => { setDismissedStreakAlert(true); localStorage.setItem("englishai.home-alert.dismissed.streak", "1"); };
  const showStreakAlert = model.streakAtRisk && !dismissedStreakAlert && localStorage.getItem("englishai.home-alert.dismissed.streak") !== "1";
  const entrySuppressKey = entryDay ? `englishai.home-entry-modal.suppressed.${learnerId}.${entryDay}` : null;
  useEffect(() => {
    if (entryPromptInitialized || !entryPrompts || !entryDay) return;
    setEntryPromptInitialized(true);
    if (entrySuppressKey && localStorage.getItem(entrySuppressKey) === "1") return;
    setEntryPrompt(selectHomeEntryPrompt(entryPrompts));
  }, [entryDay, entryPromptInitialized, entryPrompts, entrySuppressKey]);
  const suppressEntryPromptForToday = () => {
    if (entrySuppressKey) localStorage.setItem(entrySuppressKey, "1");
    setEntryPrompt(null);
  };
  return <div className="play-home-design min-h-dvh bg-ea-bg pt-[calc(81px+env(safe-area-inset-top))] text-ea-text min-[1200px]:pt-[84px]" data-testid="home-dashboard" data-theme-scope="englishai-play">
    <SportTopBar model={model} homeDesign />
    <div className="flex min-h-[calc(100dvh-84px)]">
    <SportSideNav model={model} homeDesign />
    <div className="min-w-0 flex-1">
    <div className="flex items-center gap-3 px-5 pt-3 min-[1200px]:hidden"><button type="button" onClick={() => navigate("/app/vocabulary/saved")} className={`inline-flex h-12 items-center justify-center gap-2 rounded-[14px] border border-ea-border bg-ea-surface px-[18px] text-[13px] font-extrabold text-ea-primary ${homeFocus}`}><Bookmark size={18} aria-hidden />Saqlangan</button></div>
    <div aria-busy={loading} className="mx-auto flex max-w-[1440px] flex-col gap-5 px-5 pb-[121px] pt-5 min-[1200px]:gap-6 min-[1200px]:px-10 min-[1200px]:pb-10 min-[1200px]:pt-7">
    {error ? <div role="alert"><h1>Ma’lumot yuklanmadi</h1><p>Ulanishni tekshirib, yana urinib ko‘ring.</p><AppButton onClick={onRetry}>Qayta urinish</AppButton></div> : loading ? <div role="status" className="play-home-skeleton"><span className="sr-only">Yuklanmoqda</span>{[1,2,3,4].map(n => <div key={n} />)}</div> : <>
    <div className="grid min-w-0 grid-cols-1 gap-5 min-[1200px]:grid-cols-[minmax(0,1fr)_300px] min-[1200px]:gap-x-5 min-[1200px]:gap-y-6">
    <h1 className="order-1 min-w-0 self-center break-words font-duo text-[28px] font-black leading-[1.1] tracking-[-0.5px] min-[1200px]:col-start-1 min-[1200px]:row-start-1 min-[1200px]:max-w-[calc(100%-110px)] min-[1200px]:text-[38px]">Salom, {model.name}!</h1>
    <div className="order-4 min-[1200px]:col-start-2 min-[1200px]:row-start-1 min-[1200px]:justify-self-end"><HomeStats model={model} /></div>
    <div className="order-2 min-w-0 min-[1200px]:col-start-1 min-[1200px]:row-start-2"><DailyLesson model={model} onAction={openAction} /></div>
    <div className="order-3 min-[1200px]:col-start-2 min-[1200px]:row-start-2"><DailyHabit model={model} /></div>
    <div className="order-5 min-w-0 min-[1200px]:col-span-2 min-[1200px]:row-start-3"><SkillLauncher model={model} onAction={openAction} /></div>
    {getNativeCapabilities().video && <div className="order-7 min-w-0 min-[1200px]:col-start-1 min-[1200px]:row-start-4"><VideoRecommendations /></div>}
    <div className="contents min-[1200px]:col-start-2 min-[1200px]:row-start-4 min-[1200px]:flex min-[1200px]:flex-col min-[1200px]:gap-4">
      <div className="order-6 min-w-0 min-[1200px]:order-none"><DueReviewCard model={model} /></div>
      <div className="order-9 min-w-0 min-[1200px]:order-none"><DailyWord alreadySaved={wordSaved} onSaved={onWordSaved} /></div>
    </div>
    <div className="order-8 min-w-0 min-[1200px]:col-span-2 min-[1200px]:row-start-5"><HomeDiscoveries /></div>
    </div>
    {showStreakAlert && <aside className="play-home-alert" role="status"><button type="button" onClick={() => { dismissStreakAlert(); openAction(model.nextAction); }}><Icon name="local_fire_department"/><span><strong>Streakni saqlang</strong><small>Bugungi mashq bilan seriyani davom ettiring.</small></span></button><button type="button" aria-label="Bildirishnomani yopish" onClick={dismissStreakAlert}><Icon name="close"/></button></aside>}
    </>}
    </div></div></div><MobileNav homeDesign /><HomeEntryModal prompt={entryPrompt} open={Boolean(entryPrompt)} onClose={() => setEntryPrompt(null)} onSuppressToday={suppressEntryPromptForToday} /></div>;
}

export function CatalogTopBar({ title }: { title: string }) {
  const navigate = useNavigate();
  const header = <header className="play-topbar play-catalog-topbar"><button className="play-brand-link" type="button" aria-label="Bosh sahifa" onClick={() => navigate("/home")}><Brand /></button><div className="play-hud"><span className="play-xp">{title}</span><button type="button" aria-label="Bosh sahifaga qaytish" onClick={() => navigate("/home")}><Icon name="close" /></button></div></header>;
  return typeof document === "undefined" ? header : createPortal(header, document.body);
}
