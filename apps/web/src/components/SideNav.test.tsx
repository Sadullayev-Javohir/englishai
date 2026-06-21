import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AuthContext, type AuthContextValue } from "@/app/auth";
import { SideNav } from "./SideNav";

// SideNav renders <NotificationBell>, which pulls the notifications feed on
// mount. Without this branch the hook dereferences `api.vocabulary` and the
// whole tree throws before the account footer is asserted.
vi.mock("@/api/client", () => ({
  api: {
    admin: { access: vi.fn().mockResolvedValue({ isAdmin: false }) },
    vocabulary: { notifications: vi.fn().mockResolvedValue([]) },
  },
}));

const authValue: AuthContextValue = {
  status: "authenticated",
  user: {
    id: "learner-1",
    email: "javohir@example.com",
    displayName: "Javohir",
    username: "javohir",
    pictureUrl: null,
    hasOnboarded: true,
    preferredName: "Javohir",
    learningGoal: 0,
  birthDate: "2000-01-01",
  gender: 1,
  acquisitionSource: 1,
  acquisitionSourceOther: null,
  hasCompletedDemographics: true,
  },
  signInWithGoogle: vi.fn(),
  signOut: vi.fn(),
  applyUser: vi.fn(),
};

afterEach(() => {
  cleanup();
  window.localStorage.clear();
});

describe("SideNav account footer", () => {
  it("shows the authenticated profile image in the sidebar", () => {
    const pictureUrl = "/api/auth/avatar/learner-1/avatar.png";

    render(
      <AuthContext.Provider value={{ ...authValue, user: { ...authValue.user!, pictureUrl } }}>
        <MemoryRouter initialEntries={["/home"]}>
          <SideNav />
        </MemoryRouter>
      </AuthContext.Provider>,
    );

    const profileLink = screen.getByRole("link", { name: "Tizimga kirgan: javohir" });
    const profileImage = profileLink.querySelector("img");

    expect(profileImage?.getAttribute("src")).toBe(pictureUrl);
    expect(profileImage?.classList.contains("bg-[var(--ea-surface)]")).toBe(true);
  });

  it("renders the signed-in account at the bottom and opens /profile", () => {
    render(
      <AuthContext.Provider value={authValue}>
        <MemoryRouter initialEntries={["/home"]}>
          <Routes>
            <Route path="/home" element={<SideNav />} />
            <Route path="/profile" element={<div>Profil sahifasi</div>} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>,
    );

    const profileLink = screen.getByRole("link", { name: "Tizimga kirgan: javohir" });
    expect(profileLink.getAttribute("href")).toBe("/profile");
    expect(screen.getByText("@javohir")).toBeTruthy();

    fireEvent.click(profileLink);
    expect(screen.getByText("Profil sahifasi")).toBeTruthy();
  });

  it("uses the panel control to collapse and restore the sidebar state", () => {
    render(
      <AuthContext.Provider value={authValue}>
        <MemoryRouter initialEntries={["/home"]}>
          <SideNav />
        </MemoryRouter>
      </AuthContext.Provider>,
    );

    const sidebar = screen.getByTestId("desktop-side-nav");
    const collapseButton = screen.getByRole("button", { name: "Menyuni ixchamlashtirish" });

    expect(collapseButton.querySelector("svg")).toBeTruthy();
    fireEvent.click(collapseButton);

    expect(sidebar.classList.contains("is-collapsed")).toBe(true);
    expect(screen.getByRole("button", { name: "Menyuni kengaytirish" })).toBeTruthy();
    expect(document.documentElement.dataset.learnerSidebar).toBe("collapsed");
  });

  it("opens the Speaking hub from Speaking", () => {
    render(
      <AuthContext.Provider value={authValue}>
        <MemoryRouter initialEntries={["/home"]}>
          <SideNav />
        </MemoryRouter>
      </AuthContext.Provider>,
    );

    expect(screen.getByRole("link", { name: "Speaking" }).getAttribute("href"))
      .toBe("/app/speaking");
  });
});
