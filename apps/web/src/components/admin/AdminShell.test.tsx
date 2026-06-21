import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LearningGoal, type AuthenticatedUserDto } from "@/api/types";
import { AuthContext, type AuthContextValue } from "@/app/auth";
import { AdminShell } from "./AdminShell";

const access = vi.hoisted(() => ({ canManageAdmins: true }));

vi.mock("@/api/client", () => ({
  api: {
    admin: { access: () => Promise.resolve({ isAdmin: true, canManageAdmins: access.canManageAdmins }) },
    vocabulary: { notifications: () => Promise.resolve([]) },
  },
}));

vi.mock("@/api/notificationsHub", () => ({
  NotificationsHubClient: class {
    start() { return Promise.resolve(); }
    stop() { return Promise.resolve(); }
  },
}));

const user: AuthenticatedUserDto = {
  id: "admin-1",
  email: "admin@example.com",
  displayName: "Admin",
  username: "admin",
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

function renderShell(path = "/admin") {
  render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={[path]}>
        <AdminShell variant="hub"><div>Admin content</div></AdminShell>
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

afterEach(() => {
  cleanup();
  access.canManageAdmins = true;
});

describe("AdminShell responsive navigation", () => {
  it("keeps four primary links and exposes the rest through Boshqa", async () => {
    renderShell();

    const mobileNav = screen.getByRole("navigation", { name: "Mobil admin navigatsiyasi" });
    expect(mobileNav.querySelectorAll("a")).toHaveLength(4);
    expect(screen.getByRole("button", { name: "Boshqa" }).getAttribute("aria-expanded")).toBe("false");

    fireEvent.click(screen.getByRole("button", { name: "Boshqa" }));
    expect(await screen.findByRole("dialog", { name: "Boshqa admin bo‘limlari" })).toBeTruthy();
    expect(screen.getByRole("navigation", { name: "Qo‘shimcha admin navigatsiyasi" }).querySelectorAll("a")).toHaveLength(7);
  });

  it("hides super-admin links from the Boshqa sheet", async () => {
    access.canManageAdmins = false;
    renderShell();
    await waitFor(() => expect(screen.queryByRole("link", { name: /Bildirishnomalar boshqaruvi/i })).toBeNull());

    fireEvent.click(screen.getByRole("button", { name: "Boshqa" }));
    const moreNav = await screen.findByRole("navigation", { name: "Qo‘shimcha admin navigatsiyasi" });
    expect(moreNav.querySelectorAll("a")).toHaveLength(4);
    expect(screen.queryByRole("link", { name: /Server holati/i })).toBeNull();
  });

  it("marks Boshqa active for a secondary route and closes the sheet on selection", async () => {
    renderShell("/admin/grammar");
    const moreButton = screen.getByRole("button", { name: "Boshqa" });
    expect(moreButton.className).toContain("is-active");

    fireEvent.click(moreButton);
    const moreNav = await screen.findByRole("navigation", { name: "Qo‘shimcha admin navigatsiyasi" });
    fireEvent.click(within(moreNav).getByRole("link", { name: /Tinglash mashqlari/i }));
    await waitFor(() => expect(screen.queryByRole("dialog", { name: "Boshqa admin bo‘limlari" })).toBeNull());
  });
});
