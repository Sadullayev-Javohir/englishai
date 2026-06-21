import { render, screen } from "@testing-library/react";
import {
  MemoryRouter,
  Route,
  Routes,
  useLocation,
} from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LearningGoal, type AuthenticatedUserDto } from "@/api/types";
import { AuthContext, type AuthContextValue } from "./auth";
import {
  DemographicsGate,
  GoalGate,
  RequireDemographics,
  RequireAuth,
  RequireOnboarded,
  RequireUsername,
  usesHomeChrome,
} from "./layout";
import { getReturnTarget } from "./returnTarget";
import { setLearnerId, setStoredLevel } from "./session";

const baseUser: AuthenticatedUserDto = {
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

function authValue(
  status: AuthContextValue["status"],
  user: AuthenticatedUserDto | null,
): AuthContextValue {
  return {
    status,
    user,
    signInWithGoogle: vi.fn(),
    signOut: vi.fn(),
    applyUser: vi.fn(),
  };
}

function LocationProbe() {
  const location = useLocation();
  return <div>{`${location.pathname}${location.search}${location.hash}`}</div>;
}

function renderGate(gate: React.ReactElement, auth: AuthContextValue) {
  render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter
        initialEntries={["/video/lesson-1/quiz?mode=review#question-2"]}
      >
        <Routes>
          <Route element={gate}>
            <Route path="/video/:id/quiz" element={<LocationProbe />} />
          </Route>
          <Route path="/login" element={<LocationProbe />} />
          <Route path="/username" element={<LocationProbe />} />
          <Route path="/onboarding/profile-details" element={<LocationProbe />} />
          <Route path="/welcome" element={<LocationProbe />} />
          <Route path="/onboarding/goal" element={<LocationProbe />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

function renderDemographicsGate(auth: AuthContextValue, initialEntry: string) {
  render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route element={<DemographicsGate />}>
            <Route path="/onboarding/profile-details" element={<LocationProbe />} />
          </Route>
          <Route path="/home" element={<LocationProbe />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

describe("protected route return targets", () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it("remembers the complete deep link before the authentication redirect", () => {
    renderGate(<RequireAuth />, authValue("unauthenticated", null));

    expect(screen.getByText("/login")).toBeTruthy();
    expect(getReturnTarget()).toBe(
      "/video/lesson-1/quiz?mode=review#question-2",
    );
  });

  it("keeps the original target while the username gate redirects", () => {
    const user = { ...baseUser, username: null };
    renderGate(<RequireUsername />, authValue("authenticated", user));

    expect(screen.getByText("/username")).toBeTruthy();
    expect(getReturnTarget()).toBe(
      "/video/lesson-1/quiz?mode=review#question-2",
    );
  });

  it("keeps the original target while onboarding redirects", () => {
    const user = { ...baseUser, hasOnboarded: false };
    renderGate(<RequireOnboarded />, authValue("authenticated", user));

    expect(screen.getByText("/welcome")).toBeTruthy();
    expect(getReturnTarget()).toBe(
      "/video/lesson-1/quiz?mode=review#question-2",
    );
  });

  it("asks existing accounts for missing demographics once", () => {
    const user = { ...baseUser, hasCompletedDemographics: false, birthDate: null, gender: null, acquisitionSource: null };
    renderGate(<RequireDemographics />, authValue("authenticated", user));
    expect(screen.getByText("/onboarding/profile-details")).toBeTruthy();
    expect(getReturnTarget()).toBe("/video/lesson-1/quiz?mode=review#question-2");
  });

  it("allows a completed user to reopen demographics in profile edit mode", () => {
    renderDemographicsGate(authValue("authenticated", baseUser), "/onboarding/profile-details?edit=1");
    expect(screen.getByText("/onboarding/profile-details?edit=1")).toBeTruthy();
  });

  it("redirects a completed user outside profile edit mode", () => {
    renderDemographicsGate(authValue("authenticated", baseUser), "/onboarding/profile-details");
    expect(screen.getByText("/home")).toBeTruthy();
  });

  it("keeps the original target while goal selection redirects", () => {
    setLearnerId(baseUser.id);
    setStoredLevel(1);
    const user = { ...baseUser, learningGoal: LearningGoal.Unspecified };
    renderGate(<GoalGate />, authValue("authenticated", user));

    expect(screen.getByText("/onboarding/goal")).toBeTruthy();
    expect(getReturnTarget()).toBe(
      "/video/lesson-1/quiz?mode=review#question-2",
    );
  });

});

describe("home dashboard chrome routes", () => {
  it("keeps every standard page inside the home design shell", () => {
    expect(usesHomeChrome("/app/speaking")).toBe(true);
    expect(usesHomeChrome("/app/speaking/topic/40982faf-b26c-49da-ad0f-8b63429e9d79")).toBe(false);
    expect(usesHomeChrome("/app/speaking/free-talk/daily-life")).toBe(false);
    expect(usesHomeChrome("/app/speaking/role-talk/restaurant")).toBe(false);
    expect(usesHomeChrome("/writing")).toBe(true);
    expect(usesHomeChrome("/admin/users")).toBe(true);
    expect(usesHomeChrome("/writing/task/travel")).toBe(false);
  });

  it("keeps the admin hub eligible for data while its own frame owns navigation", () => {
    expect(usesHomeChrome("/admin")).toBe(true);
  });
});
