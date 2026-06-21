import { Suspense, useEffect } from "react";
import { LiveTutorPending } from "@/components/LiveTutorPending";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { AppShell } from "@/components/AppShell";
import { ScrollToTop } from "@/components/ScrollToTop";
import { AppPending } from "@/components/AppPending";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { AdminShell } from "@/components/admin/AdminShell";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { useAuth } from "./auth";
import { hasOnboarded, hasSeenGoalPrompt } from "./session";
import { placementStorage } from "@/pages/onboarding/placementStorage";
import { LearningGoal } from "@/api/types";
import { getRouteMetadata } from "./routeMetadata";
import {
  clearReturnTarget,
  getReturnTarget,
  rememberReturnTarget,
  returnTargetOr,
} from "./returnTarget";
import { buildHomeViewModel } from "@/pages/home-concepts/homeViewModel";
import { pendingHomeFallback } from "@/pages/home-concepts/homeRecommendation";
import { getLearnerId } from "./session";
import { isDue } from "@/lib/srsStage";
import { uz } from "@/content/uz";

function currentTarget(location: ReturnType<typeof useLocation>): string {
  return `${location.pathname}${location.search}${location.hash}`;
}

export function usesHomeChrome(pathname: string): boolean {
  return getRouteMetadata(pathname).shellMode !== "lessonFrame";
}

/** Gate guarding the whole app: only signed-in (Google) users get past. */
export function RequireAuth() {
  const { status } = useAuth();
  const location = useLocation();
  if (status === "loading") {
    return <AppPending />;
  }
  if (status === "unauthenticated") {
    rememberReturnTarget(currentTarget(location));
    return <Navigate to="/login" replace />;
  }
  return <Outlet />;
}

export function PublicPageLayout() {
  return <div className="ea-public-page-scope" data-design-system="englishai-play"><Outlet /></div>;
}

export function OnboardingPageLayout() {
  return <div className="ea-onboarding-page-scope" data-design-system="englishai-play"><Outlet /></div>;
}

export function ShellLayout() {
  const location = useLocation();
  const { pathname } = location;
  const { user } = useAuth();
  const learnerId = getLearnerId();
  const metadata = getRouteMetadata(pathname);
  const useHomeChrome = usesHomeChrome(pathname);
  const adminHubFrame = pathname === "/admin";
  const statusState = useAsync(
    () => api.gamification.status(learnerId),
    useHomeChrome ? [learnerId] : ["disabled-home-chrome"],
    useHomeChrome
  );
  const levelState = useAsync(
    () => api.levels.map(learnerId),
    useHomeChrome ? [learnerId] : ["disabled-home-level"],
    useHomeChrome
  );
  const practiceWordsState = useAsync(
    () => api.speaking.practiceWords(learnerId),
    useHomeChrome ? [learnerId] : ["disabled-home-chrome"],
    useHomeChrome
  );
  const savedWordsState = useAsync(
    () => api.vocabulary.list(learnerId),
    useHomeChrome ? [learnerId] : ["disabled-home-chrome"],
    useHomeChrome
  );
  const homeChromeModel = useHomeChrome
    ? buildHomeViewModel({
        name: user?.displayName?.trim() || uz.home.defaultName,
        pictureUrl: user?.pictureUrl,
        activeTopic: levelState.data?.topics.find((topic) => topic.id === levelState.data?.activeTopicId) ?? null,
        fallbackKind: pendingHomeFallback(learnerId),
        streakAtRisk: statusState.data?.isStreakAtRisk,
        goalMet: statusState.data?.isGoalMet,
        practiceWordCount: practiceWordsState.data?.length ?? 0,
        savedCount: savedWordsState.data?.length ?? 0,
        dueSavedCount: (savedWordsState.data ?? []).filter((word) => isDue(word)).length,
      })
    : undefined;

  useEffect(() => {
    if (getReturnTarget() === currentTarget(location)) clearReturnTarget();
  }, [location]);

  // The route chunk suspends INSIDE the shell, never above it. With only the root boundary
  // (main.tsx) the fallback was laid out against the whole viewport, so its left edge slid
  // under the fixed 248px sidebar. Rendering it here keeps it inside the padded workspace.
  const content = (
    <>
      <ScrollToTop />
      <Suspense fallback={pathname === "/home" ? null : pathname.startsWith("/app/speaking/live-tutor/") ? <LiveTutorPending /> : <LoadingSkeleton variant="page" />}>
        <Outlet />
      </Suspense>
    </>
  );

  return (
    <AppShell
      lessonFrame={metadata.shellMode === "lessonFrame"}
      conceptFrame={pathname === "/home" || adminHubFrame}
      homeChromeModel={homeChromeModel}
    >
      {content}
    </AppShell>
  );
}

/** First post-sign-up step: a brand-new account has no username yet. */
export function RequireUsername() {
  const { user } = useAuth();
  const location = useLocation();
  if (user && !user.username) {
    rememberReturnTarget(currentTarget(location));
    return <Navigate to="/username" replace />;
  }
  return <Outlet />;
}

/** Mirror of RequireUsername for the setup screen itself. */
export function UsernameGate() {
  const { user } = useAuth();
  if (user?.username) return <Navigate to={returnTargetOr("/home")} replace />;
  return <Outlet />;
}

export function RequireDemographics() {
  const { user } = useAuth();
  const location = useLocation();
  if (user && !user.hasCompletedDemographics) {
    rememberReturnTarget(currentTarget(location));
    return <Navigate to="/onboarding/profile-details" replace />;
  }
  return <Outlet />;
}

export function DemographicsGate() {
  const { user } = useAuth();
  const location = useLocation();
  const isProfileEdit = new URLSearchParams(location.search).get("edit") === "1";
  if (user?.hasCompletedDemographics && !isProfileEdit)
    return <Navigate to={returnTargetOr(user.hasOnboarded ? "/home" : "/welcome")} replace />;
  return <Outlet />;
}

/** Goal-based onboarding step. */
export function GoalGate() {
  const { user } = useAuth();
  const location = useLocation();
  const onboarded = hasOnboarded(Boolean(user?.hasOnboarded));
  const needsGoal =
    user &&
    onboarded &&
    user.learningGoal === LearningGoal.Unspecified &&
    !hasSeenGoalPrompt();
  if (needsGoal) {
    rememberReturnTarget(currentTarget(location));
    return <Navigate to="/onboarding/goal" replace />;
  }
  return <Outlet />;
}

/** Mirror of GoalGate for the goal screen itself. */
export function OnboardingGoalGate() {
  const { user } = useAuth();
  if (!hasOnboarded(Boolean(user?.hasOnboarded)))
    return <Navigate to="/welcome" replace />;
  if (
    user &&
    (user.learningGoal !== LearningGoal.Unspecified || hasSeenGoalPrompt())
  )
    return <Navigate to={returnTargetOr("/home")} replace />;
  return <Outlet />;
}

/** Guards the onboarding screens (welcome / assessment / placement). */
export function OnboardingGate() {
  const { user } = useAuth();
  const location = useLocation();
  // Finalization creates the learner profile before the HTTP response arrives.
  // Keep result reloads and a completed-but-not-yet-finalized session reachable.
  if (location.pathname === "/placement/result"
      || (location.pathname === "/placement" && user && placementStorage(user.id).readSession())) return <Outlet />;
  if (hasOnboarded(Boolean(user?.hasOnboarded)))
    return <Navigate to={returnTargetOr("/home")} replace />;
  return <Outlet />;
}

/** Guards the app proper: requires a chosen level. */
export function RequireOnboarded() {
  const { user } = useAuth();
  const location = useLocation();
  if (!hasOnboarded(Boolean(user?.hasOnboarded))) {
    rememberReturnTarget(currentTarget(location));
    return <Navigate to="/welcome" replace />;
  }
  return <Outlet />;
}

/** Admin area gate. */
export function RequireAdmin() {
  const { data, loading, error } = useAsync(() => api.admin.access(), []);
  if (loading) {
    return <LoadingSkeleton variant="dashboard" />;
  }
  if (error || !data?.isAdmin) return <Navigate to="/home" replace />;
  return <Outlet />;
}

export function AdminAreaLayout() {
  // Same contract as ShellLayout: the fallback belongs inside the workspace the admin
  // sidebar has already offset, not across the viewport underneath it.
  return (
    <AdminShell variant="hub">
      <Suspense fallback={<LoadingSkeleton variant="dashboard" />}>
        <Outlet />
      </Suspense>
    </AdminShell>
  );
}

/** Super-admin-only area gate. */
export function RequireSuperAdmin() {
  const { data, loading, error } = useAsync(() => api.admin.access(), []);
  if (loading) {
    return <LoadingSkeleton variant="dashboard" />;
  }
  if (error || !data?.canManageAdmins) return <Navigate to="/admin" replace />;
  return <Outlet />;
}
