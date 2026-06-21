import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LoginPage } from "./LoginPage";

vi.mock("@/app/auth", () => ({
  useAuth: () => ({
    status: "unauthenticated",
    signInWithGoogle: vi.fn(),
    applyUser: vi.fn(),
  }),
}));
vi.mock("@/api/nativeAuth", () => ({ isNativePlatform: () => true }));
vi.mock("@/api/nativeGoogleSignIn", () => ({ nativeGoogleSignIn: vi.fn() }));
vi.mock("@/api/client", () => ({
  ApiError: class ApiError extends Error {},
  api: {
    auth: {
      config: vi.fn().mockResolvedValue({ googleClientId: "" }),
      dev: vi.fn(),
    },
  },
}));
vi.mock("@/lib/referral", () => ({
  getStoredReferralCode: () => null,
  setStoredReferralCode: vi.fn(),
}));

afterEach(cleanup);

describe("LoginPage native layout", () => {
  it("uses the shared Pen chrome and measured sign-in composition", async () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    const viewport = await screen.findByTestId("login-viewport");
    expect(viewport.className).toContain("onboarding-play");
    expect(viewport.querySelector(".play-login__signin")).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Salom, do‘stim!" })).toBeTruthy();
  });

  it("returns to the public landing page from the brand control", async () => {
    render(
      <MemoryRouter initialEntries={["/login"]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<div>LANDING_DESTINATION</div>} />
        </Routes>
      </MemoryRouter>,
    );

    fireEvent.click(await screen.findByRole("link", { name: "EnglishAI.uz bosh sahifasi" }));
    expect(screen.getByText("LANDING_DESTINATION")).toBeTruthy();
  });

  it("uses the original Pen parrot illustration", async () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    expect(await screen.findByRole("img", { name: "EnglishAI bilan yangi boshlanish" })).toBeTruthy();
  });
});
