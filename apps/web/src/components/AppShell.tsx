import { useEffect } from "react";
import { createPortal } from "react-dom";
import { ParrotLogo } from "./ParrotLogo";
import { useLocation } from "react-router-dom";
import { useStudyTimer } from "@/lib/useStudyTimer";
import { NotificationsHubClient } from "@/api/notificationsHub";
import { bumpNotifications } from "@/lib/notificationsRefresh";
import { initPushNotifications } from "@/lib/push";
import { syncNativeWidgets } from "@/lib/nativeWidgets";
import { initNativeDeepLinks } from "@/lib/nativeDeepLinks";
import { LearningAssistant } from "./LearningAssistant";
import { PaywallProvider } from "./PaywallProvider";
import { HeartsProvider } from "@/components/game/HeartsProvider";
import { EnergyProvider } from "@/components/game/EnergyProvider";
import { usePageAnimations } from "@/lib/usePageAnimations";
import { NotificationModal } from "./NotificationModal";
import { getRouteMetadata } from "@/app/routeMetadata";
import { PageContainer, ResponsiveViewport } from "@/components/ui/ResponsiveViewport";
import { MobileNav, SportSideNav, SportTopBar, CatalogTopBar } from "@/pages/home-concepts/HomeConceptLab";
import type { HomeViewModel } from "@/pages/home-concepts/homeViewModel";
import { VocabularyChrome } from "@/components/vocabulary/VocabularyChrome";
import { GrammarChrome } from "@/components/grammar/GrammarChrome";
import { ListeningChrome } from "@/components/listening/ListeningChrome";
import { ReadingChrome } from "@/components/reading/ReadingChrome";
import { SpeakingChrome } from "@/components/speaking/SpeakingChrome";
import { WritingChrome } from "@/components/writing/WritingChrome";

// How often the safety-net poll re-checks for new notifications when SignalR is unavailable.
const NOTIFICATIONS_POLL_MS = 45_000;

export function shouldShowLearningAssistant(pathname: string): boolean {
  // Playlist episode screens have their own contextual AI card/sheet. A second floating
  // trigger covers the compact mobile queue and duplicates the learning action.
  return !pathname.startsWith("/admin")
    && !/^\/video\/playlists\/[^/]+\/\d+$/.test(pathname)
    && !/^\/video\/[^/]+\/play$/.test(pathname);
}

/**
 * Delivers super-admin broadcasts to this session the instant they are sent, plus a slow
 * fallback poll so notifications surface even if the WebSocket never opens.
 */
function useNotificationsRealtime() {
  useEffect(() => {
    let active = true;
    let connected = false;
    const startTimer = window.setTimeout(() => {
      if (!active) return;
      client.start().then(() => { connected = true; }).catch(() => {});
    }, 500);
    const client = new NotificationsHubClient({
      onBroadcast: () => bumpNotifications(),
      onReconnected: () => bumpNotifications(),
    });
    const refreshIfVisible = () => {
      if (document.visibilityState === "visible") bumpNotifications();
    };
    window.addEventListener("focus", refreshIfVisible);
    document.addEventListener("visibilitychange", refreshIfVisible);
    const poll = window.setInterval(refreshIfVisible, NOTIFICATIONS_POLL_MS);

    return () => {
      active = false;
      window.clearTimeout(startTimer);
      if (connected && !import.meta.env.DEV) client.stop().catch(() => {});
      window.removeEventListener("focus", refreshIfVisible);
      document.removeEventListener("visibilitychange", refreshIfVisible);
      window.clearInterval(poll);
    };
  }, []);
}

/** Desktop uses the top HUD and left sidebar; tablet/mobile use responsive top and bottom bars.
 *  Immersive lessons hide all shared chrome and own the full viewport. */
export function AppShell({
   children,
   lessonFrame = false,
   conceptFrame = false,
   homeChromeModel,
 }: {
   children: React.ReactNode;
   /** When true, the route renders in the lesson jungle frame (no app chrome). */
   lessonFrame?: boolean;
   /** Temporary full-canvas Home concept comparison: the page owns all chrome. */
   conceptFrame?: boolean;
   /** Reuses the protected home dashboard chrome for approved learner routes. */
   homeChromeModel?: HomeViewModel;
 }) {
  useStudyTimer();
  useNotificationsRealtime();
  usePageAnimations();
  useEffect(() => {
    void initPushNotifications();
    void initNativeDeepLinks();
    void syncNativeWidgets();
    const refreshNativeServices = () => {
      void initPushNotifications();
      void syncNativeWidgets();
    };
    const refreshIfVisible = () => {
      if (document.visibilityState === "visible") refreshNativeServices();
    };
    window.addEventListener("app:native-resume", refreshNativeServices);
    document.addEventListener("visibilitychange", refreshIfVisible);
    return () => {
      window.removeEventListener("app:native-resume", refreshNativeServices);
      document.removeEventListener("visibilitychange", refreshIfVisible);
    };
  }, []);
  const { pathname } = useLocation();
  const metadata = getRouteMetadata(pathname);
  const supportRoute = pathname === "/support";
  const profileRoute = pathname === "/profile";
  // Screens 56 / 56D and the playlist detail all own the same Video catalog chrome.
  // Keeping only `/video` here caused the search and playlist routes to inherit the
  // dashboard sidebar/top bar, duplicating their header and breaking the Pen canvas.
  const videoCatalogFrame = pathname === "/video"
    || pathname === "/video/search"
    || /^\/video\/playlist\/[^/]+$/.test(pathname)
    || /^\/video\/playlists\/[^/]+$/.test(pathname);
  const readingFrame = pathname === "/reading" || /^\/reading\/topic\/[^/]+$/.test(pathname);
  const grammarFrame = pathname === "/app/grammar" || /^\/app\/grammar\/topic\/[^/]+$/.test(pathname);
  const listeningFrame = pathname === "/listening" || /^\/listening\/topic\/[^/]+$/.test(pathname);
  const vocabularyFrame = pathname === "/app/vocabulary/topics" || /^\/app\/vocabulary\/topic\/[^/]+(?:\/pronunciation\/[^/]+)?$/.test(pathname);
  const speakingFrame =
    pathname === "/app/speaking" ||
    pathname === "/app/speaking/topics" ||
    pathname.startsWith("/app/speaking/topic/") ||
    pathname.startsWith("/app/speaking/pronunciation/") ||
    pathname === "/app/speaking/practice-words";
  const writingFrame = pathname === "/writing" || /^\/writing\/(?:topic|task)\/[^/]+$/.test(pathname);
  const catalogFrame = ["/app/vocabulary/topics", "/app/grammar", "/reading", "/listening", "/writing", "/books", "/video"].includes(pathname)
    || videoCatalogFrame;
  const immersive = (lessonFrame || metadata.shellMode === "lessonFrame") && !writingFrame;
  const showLearningAssistant = shouldShowLearningAssistant(pathname);
  const useHomeChrome = !readingFrame && !immersive && !conceptFrame && !vocabularyFrame && !listeningFrame && !grammarFrame && !speakingFrame && !writingFrame;
  const lessonBrand = !readingFrame && immersive && !vocabularyFrame && !listeningFrame && !grammarFrame && !pathname.startsWith("/video/") && !pathname.startsWith("/app/speaking/");
  const speakingPracticeRoute =
    pathname === "/app/speaking/free" ||
    pathname.startsWith("/app/speaking/topic/") ||
    pathname.startsWith("/app/speaking/free-talk/") ||
    pathname.startsWith("/app/speaking/pronunciation/") ||
    pathname.startsWith("/app/speaking/role-talk/");
  const hideTopBar =
    supportRoute ||
    /^\/vocabulary\/saved\/[^/]+\/practice$/.test(pathname) ||
    speakingPracticeRoute;
  // Screen 61 owns the canonical learner navigation. Standard learner pages reuse
  // that same responsive shell; catalog and immersive lesson routes retain their
  // purpose-built chrome so their dedicated Pen layouts are not duplicated.
  const sharedHomeChrome = useHomeChrome && !catalogFrame && Boolean(homeChromeModel);

  return (
    <PaywallProvider>
      <HeartsProvider>
        <EnergyProvider>
        {/* Background video layer is mounted once at the app root (main.tsx),
            outside the router, so it persists across all routes. No backdrop
            here anymore. */}

        <ResponsiveViewport className="relative text-ea-text" data-design-system="englishai-play">
          <a
            href="#main"
            className="sr-only focus:not-sr-only focus:absolute focus:left-3 focus:top-3 focus:z-[80] focus:rounded-full focus:bg-ea-surface focus:px-4 focus:py-2 focus:font-duo focus:font-bold"
          >
            Asosiy kontentga o'tish
          </a>

          <div
            data-testid="app-shell-layout"
            className={readingFrame || immersive || conceptFrame || vocabularyFrame || listeningFrame || grammarFrame || speakingFrame || writingFrame ? "min-h-dvh" : `play-shell${sharedHomeChrome ? " play-home-shared-shell" : ""}${supportRoute ? " ea-support-route-shell" : ""}`}
          >
            {sharedHomeChrome && homeChromeModel ? <SportSideNav model={homeChromeModel} homeDesign /> : null}

            <main
              id="main"
              key={pathname}
              className={
                readingFrame || vocabularyFrame || listeningFrame || grammarFrame || speakingFrame || writingFrame
                  ? "ea-canvas-page-scope min-h-dvh w-full"
                  : immersive
                  ? `ea-lesson-page-scope min-h-dvh w-full${lessonBrand ? " play-lesson-page" : ""}`
                  : conceptFrame
                    ? "ea-canvas-page-scope min-h-dvh w-full"
                    : `ea-unified-page-scope play-workspace min-w-0${sharedHomeChrome ? " play-home-shared-workspace" : ""}${catalogFrame ? " play-catalog-workspace" : ""}${videoCatalogFrame ? " play-video-catalog-workspace" : ""}${supportRoute ? " ea-support-route-main" : ""}`
              }
              style={readingFrame || conceptFrame || (vocabularyFrame && !catalogFrame) || listeningFrame || grammarFrame || speakingFrame || writingFrame ? { transform: "none", animation: "none" } : undefined}
            >
              {readingFrame ? <ReadingChrome catalog={pathname === "/reading"}>{children}</ReadingChrome> : speakingFrame ? <SpeakingChrome catalog={pathname === "/app/speaking/topics"}>{children}</SpeakingChrome> : grammarFrame ? <GrammarChrome catalog={pathname === "/app/grammar"}>{children}</GrammarChrome> : listeningFrame ? <ListeningChrome catalog={pathname === "/listening"}>{children}</ListeningChrome> : vocabularyFrame ? <VocabularyChrome catalog={pathname === "/app/vocabulary/topics"}>{children}</VocabularyChrome> : writingFrame ? <WritingChrome catalog={pathname === "/writing"}>{children}</WritingChrome> : immersive || conceptFrame ? (
                children
              ) : (
                <>
                  {!hideTopBar && !videoCatalogFrame && (catalogFrame ? <CatalogTopBar title={metadata.title} /> : <SportTopBar model={homeChromeModel} homeDesign={sharedHomeChrome} />)}
                  {/* `.nh-sport` already applies `--ea-page-gutter`; the container
                      must not add a second one, or page content sits 32px inside
                      the top bar's left edge instead of aligned with it. */}
                  <div className={`play-page-content min-w-0 w-full${supportRoute ? " ea-support-route-stage" : ""}${profileRoute ? " play-profile-page-content" : ""}`}><PageContainer data-testid="app-shell-page" width={supportRoute || videoCatalogFrame || profileRoute ? "full" : "wide"} padded={false} className={`min-w-0 w-full${supportRoute ? " ea-support-route-container" : ""}`}>{children}</PageContainer></div>
                </>
              )}
            </main>
          </div>

          {lessonBrand && typeof document !== "undefined" && createPortal(<header className="play-topbar play-lesson-brand" aria-label="Dars navigatsiyasi"><ParrotLogo size={36} imgSize={36} withWordmark /><div className="play-hud"><span className="play-xp">{metadata.title}</span></div></header>, document.body)}
          {!immersive && !conceptFrame && !speakingPracticeRoute && !videoCatalogFrame && !vocabularyFrame && !speakingFrame && !writingFrame && <MobileNav homeDesign={sharedHomeChrome} />}
          {showLearningAssistant && <LearningAssistant />}
          {(!immersive || readingFrame || vocabularyFrame || listeningFrame || grammarFrame) && <NotificationModal />}
        </ResponsiveViewport>
        </EnergyProvider>
      </HeartsProvider>
    </PaywallProvider>
  );
}
