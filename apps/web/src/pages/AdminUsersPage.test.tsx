import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AdminRole } from "@/api/types";
import { AdminUsersPage } from "./AdminUsersPage";

const { users, setAdmin } = vi.hoisted(() => ({ users: vi.fn(), setAdmin: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { admin: { users, setAdmin } } }));
const payload = { viewerRole: AdminRole.SuperAdmin, totalUsers: 2, adminCount: 1, onboardedCount: 2, premiumCount: 1, users: [
  { id: "learner", email: "learner@example.com", displayName: "Ali Learner", username: "ali", pictureUrl: null, role: AdminRole.None, registeredAt: "2026-01-01", lastLoginAt: "2026-02-01", hasOnboarded: true, level: "A2", lastActivityAt: null, subscriptionStatus: "Premium", subscriptionPlan: null, subscriptionExpiresAt: null },
  { id: "admin", email: "admin@example.com", displayName: "Admin User", username: null, pictureUrl: null, role: AdminRole.Admin, registeredAt: "2026-01-02", lastLoginAt: "2026-02-02", hasOnboarded: true, level: "B1", lastActivityAt: null, subscriptionStatus: "Free", subscriptionPlan: null, subscriptionExpiresAt: null },
] };
afterEach(() => { cleanup(); users.mockReset(); setAdmin.mockReset(); localStorage.clear(); });

describe("AdminUsersPage", () => {
  it("filters users and exposes the admin back action", async () => {
    users.mockResolvedValue(payload);
    render(<MemoryRouter><AdminUsersPage /></MemoryRouter>);
    expect((await screen.findAllByText("Ali Learner")).length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: /Orqaga/ })).toBeTruthy();
    fireEvent.change(screen.getByPlaceholderText("Qidirish (ism yoki email)"), { target: { value: "Admin User" } });
    expect(screen.queryAllByText("Ali Learner")).toHaveLength(0);
    expect(screen.getAllByText("Admin User").length).toBeGreaterThan(0);
  });
  it("confirms a role change in a dialog", async () => {
    users.mockResolvedValue(payload); setAdmin.mockResolvedValue({});
    render(<MemoryRouter><AdminUsersPage /></MemoryRouter>);
    await screen.findAllByText("Ali Learner");
    fireEvent.click(screen.getAllByRole("button", { name: /Admin qilish/ })[0]);
    expect(screen.getByRole("dialog")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Tasdiqlash" }));
    await waitFor(() => expect(setAdmin).toHaveBeenCalledWith("learner", true));
  });
  it("opens a designed role filter dialog and applies the selection", async () => {
    users.mockResolvedValue(payload);
    render(<MemoryRouter><AdminUsersPage /></MemoryRouter>);
    await screen.findAllByText("Ali Learner");
    fireEvent.click(await screen.findByRole("button", { name: /Barcha rollar/ }));
    expect(screen.getByRole("dialog", { name: "Rol" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "O'quvchi" }));
    expect(screen.queryAllByText("Admin User")).toHaveLength(0);
  });
});
