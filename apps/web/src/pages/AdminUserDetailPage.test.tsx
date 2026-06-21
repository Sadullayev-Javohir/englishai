import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AdminRole } from "@/api/types";
import { AdminUserDetailPage } from "./AdminUserDetailPage";
const { user, users } = vi.hoisted(() => ({ user: vi.fn(), users: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { admin: { user, users } } }));
afterEach(() => { cleanup(); user.mockReset(); users.mockReset(); });
describe("AdminUserDetailPage", () => {
  it("shows the complete admin user projection", async () => {
    user.mockResolvedValue({ id:"u1", email:"ali@example.com", displayName:"Ali", preferredName:"Alijon", username:"ali", pictureUrl:null, role:AdminRole.None, registeredAt:"2026-01-01T10:00:00Z", lastLoginAt:"2026-02-01T11:30:00Z", proTrialExpiresAt:"2026-03-01T00:00:00Z", isProTrialActive:true, hasOnboarded:true, level:"A2", learningGoal:"Career", profileCreatedAt:"2026-01-02T00:00:00Z", profileUpdatedAt:"2026-02-01T00:00:00Z", lastActivityAt:"2026-02-01T12:00:00Z", confirmationTestPassedAt:null, lastWinBackStage:"Active", skillSeedCount:6, activityCount:18, errorObservationCount:4, subscriptionStatus:"Premium", subscriptionPlan:"Monthly", subscriptionExpiresAt:"2026-03-01T00:00:00Z", subscriptionCreatedAt:"2026-01-03T00:00:00Z", subscriptionUpdatedAt:"2026-02-01T00:00:00Z" });
    render(<MemoryRouter initialEntries={["/admin/users/u1"]}><Routes><Route path="/admin/users/:userId" element={<AdminUserDetailPage />} /></Routes></MemoryRouter>);
    expect(await screen.findByRole("heading", { name:"Ali" })).toBeTruthy();
    expect(screen.getByText("Alijon")).toBeTruthy();
    expect(screen.getByText("Career")).toBeTruthy();
    expect(screen.getByText("Monthly")).toBeTruthy();
    expect(user).toHaveBeenCalledWith("u1");
  });
  it("falls back to the existing users endpoint when detail is unavailable", async () => {
    user.mockRejectedValue(new Error("404"));
    users.mockResolvedValue({ users:[{ id:"u1", email:"ali@example.com", displayName:"Ali", username:"ali", pictureUrl:null, role:AdminRole.None, registeredAt:"2026-01-01T10:00:00Z", lastLoginAt:"2026-02-01T11:30:00Z", hasOnboarded:true, level:"A2", lastActivityAt:null, subscriptionStatus:"Free", subscriptionPlan:null, subscriptionExpiresAt:null }] });
    render(<MemoryRouter initialEntries={["/admin/users/u1"]}><Routes><Route path="/admin/users/:userId" element={<AdminUserDetailPage />} /></Routes></MemoryRouter>);
    expect(await screen.findByRole("heading", { name:"Ali" })).toBeTruthy();
    expect(users).toHaveBeenCalled();
  });
});
