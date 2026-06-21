import { act, cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LearningGoal, type AuthenticatedUserDto } from "@/api/types";
import { AuthContext, type AuthContextValue } from "./auth";
import { AdminAreaLayout, ShellLayout } from "./layout";

const { loadDueReviews, loadSubscription } = vi.hoisted(() => ({
  loadDueReviews: vi.fn().mockResolvedValue([{ id: "word-1", sourceTopicId: null }]),
  loadSubscription: vi.fn().mockResolvedValue({
    isTrialActive: true,
    trialExpiresAt: "2030-01-01T23:59:59Z",
  }),
}));

vi.mock("@/lib/useStudyTimer", () => ({ useStudyTimer: vi.fn() }));
vi.mock("@/lib/usePageAnimations", () => ({ usePageAnimations: vi.fn() }));
vi.mock("@/lib/push", () => ({ initPushNotifications: vi.fn() }));
vi.mock("@/api/notificationsHub", () => ({
  NotificationsHubClient: class {
    start() { return Promise.resolve(); }
    stop() { return Promise.resolve(); }
  },
}));
vi.mock("@/components/PaywallProvider", () => ({ PaywallProvider: ({ children }: { children: React.ReactNode }) => children }));
vi.mock("@/components/game/HeartsProvider", () => ({ HeartsProvider: ({ children }: { children: React.ReactNode }) => children }));
vi.mock("@/components/game/EnergyProvider", () => ({ EnergyProvider: ({ children }: { children: React.ReactNode }) => children }));
vi.mock("@/pages/home-concepts/HomeConceptLab", () => ({
  SportSideNav: () => <div data-testid="home-side-nav" />,
  SportTopBar: () => <div data-testid="home-top-bar" />,
  MobileNav: () => <div data-testid="home-mobile-nav" />,
}));
vi.mock("@/components/LearningAssistant", () => ({ LearningAssistant: () => null }));
vi.mock("@/components/NotificationModal", () => ({ NotificationModal: () => null }));
vi.mock("@/components/ScrollToTop", () => ({ ScrollToTop: () => null }));
vi.mock("@/api/client", () => ({
  api: {
    gamification: { status: () => new Promise(() => {}) },
    vocabulary: {
      due: loadDueReviews,
      list: () => new Promise(() => {}),
      notifications: () => Promise.resolve([]),
    },
    admin: { access: () => Promise.resolve({ isAdmin: true, canManageAdmins: true }) },
    subscription: { get: loadSubscription },
  },
}));

const user: AuthenticatedUserDto = {
  id: "learner-1",
  email: "learner@example.com",
  displayName: "Learner",
  username: "learner",
  pictureUrl: null,
  hasOnboarded: true,
  preferredName: null,
  learningGoal: LearningGoal.Travel,
  birthDate: "2000-01-01",
  gender: 1,
  acquisitionSource: 1,
  acquisitionSourceOther: null,
  hasCompletedDemographics: true,
};

const auth: AuthContextValue = {
  status: "authenticated",
  user,
  signInWithGoogle: vi.fn(),
  signOut: vi.fn(),
  applyUser: vi.fn(),
};

/** Stands in for a route whose lazy chunk has not arrived yet. */
const pendingChunk = new Promise<never>(() => {});
function SuspendingPage(): never {
  throw pendingChunk;
}

function renderRoute(layout: React.ReactElement, path: string, page = <SuspendingPage />) {
  render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route element={layout}>
            <Route path={path} element={page} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  );
  return document.querySelector(".ea-loading-skeleton");
}

afterEach(cleanup);

describe("authenticated app entry", () => {
  it.each(["/home", "/progress", "/app/vocabulary/review"])(
    "opens %s without automatic notices or notice-only requests",
    async (path) => {
      sessionStorage.clear();
      renderRoute(<ShellLayout />, path, <h1>Ready to learn</h1>);
      await act(async () => {});

      expect(screen.getByRole("heading", { name: "Ready to learn" })).toBeTruthy();
      expect(screen.queryByRole("dialog")).toBeNull();
      expect(loadDueReviews).not.toHaveBeenCalled();
      expect(loadSubscription).not.toHaveBeenCalled();
      expect(sessionStorage.length).toBe(0);
    },
  );
});

// Regression: the only Suspense boundary used to live at the app root, above the shells, so a
// pending route chunk rendered its skeleton against the whole viewport - sliding underneath the
// fixed left sidebar. The fallback must sit inside the workspace the sidebar has offset.
describe("route chunk fallbacks stay inside the sidebar-offset workspace", () => {
  it("keeps the learner fallback inside the shell page container", () => {
    const skeleton = renderRoute(<ShellLayout />, "/progress");

    expect(skeleton).toBeTruthy();
    expect(skeleton!.closest('[data-testid="app-shell-page"]')).toBeTruthy();
    expect(screen.getByTestId("home-side-nav")).toBeTruthy();
  });

  it("does not show the generic route-chunk skeleton on /home", () => {
    const skeleton = renderRoute(<ShellLayout />, "/home");

    expect(skeleton).toBeNull();
  });

  it("keeps the admin fallback inside the admin workspace", () => {
    const skeleton = renderRoute(<AdminAreaLayout />, "/admin/users");

    expect(skeleton).toBeTruthy();
    expect(skeleton!.closest(".ea-admin-shell__main")).toBeTruthy();
    expect(document.querySelector(".ea-admin-shell__sidebar")).toBeTruthy();
  });
});
