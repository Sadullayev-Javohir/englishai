import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { RootEntry } from "./RootEntry";

let authStatus: "authenticated" | "unauthenticated" = "unauthenticated";

vi.mock("./auth", () => ({
  useAuth: () => ({ status: authStatus }),
}));

vi.mock("@/api/nativeAuth", () => ({
  isNativePlatform: () => false,
}));

vi.mock("@/pages/LandingPage", () => ({
  LandingPage: () => <div>LANDING_PAGE</div>,
}));

afterEach(() => {
  cleanup();
  authStatus = "unauthenticated";
});

function renderRoot() {
  render(
    <MemoryRouter initialEntries={["/"]}>
      <Routes>
        <Route path="/" element={<RootEntry />} />
        <Route path="/home" element={<div>HOME_PAGE</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("RootEntry", () => {
  it("shows the canonical landing page to signed-out visitors", () => {
    renderRoot();
    expect(screen.getByText("LANDING_PAGE")).toBeTruthy();
  });

  it("sends an authenticated learner directly home", () => {
    authStatus = "authenticated";
    renderRoot();
    expect(screen.getByText("HOME_PAGE")).toBeTruthy();
  });
});
