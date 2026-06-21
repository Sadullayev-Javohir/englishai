import type { Location, NavigateFunction } from "react-router-dom";

export interface RouteReturnState {
  returnTo?: "/progress";
}

export const PROGRESS_RETURN_STATE = { returnTo: "/progress" } satisfies RouteReturnState;

export function routeReturnTarget(location: Pick<Location, "state">, fallback = "/home"): string {
  const state = location.state as RouteReturnState | null;
  return state?.returnTo === "/progress" ? "/progress" : fallback;
}

/** Pre-restructure public paths the progress backend still emits as `targetRoute`. `/vocabulary`
 * and `/grammar` are now SEO landing pages, so a learner CTA must not land there — map each to
 * its real in-app destination. */
const LEGACY_LEARNING_ROUTES: Record<string, string> = {
  "/vocabulary": "/app/vocabulary/topics",
  "/grammar": "/app/grammar",
  "/speaking": "/app/speaking",
};

/** Normalises a learning route so legacy/public paths resolve to the real app page. Routes that
 * are already correct (including every `/app/...` path) pass through unchanged. */
export function resolveLearningRoute(route: string): string {
  if (!route) return "/home";
  const clean = route.replace(/\/+$/, "") || "/";
  return LEGACY_LEARNING_ROUTES[clean] ?? route;
}

export function navigateFromProgress(navigate: NavigateFunction, route: string): void {
  navigate(resolveLearningRoute(route), { state: PROGRESS_RETURN_STATE });
}
