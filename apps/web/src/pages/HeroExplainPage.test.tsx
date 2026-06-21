import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { HeroExplainPage } from "./HeroExplainPage";

let authStatus: "authenticated" | "unauthenticated" = "unauthenticated";

vi.mock("@/app/auth", () => ({ useAuth: () => ({ status: authStatus }) }));
vi.mock("@/components/hero/ThreeCanvas", () => ({ ThreeCanvas: () => <div data-testid="hero-3d-scene" /> }));
vi.mock("framer-motion", async () => {
  const React = await import("react");
  const motionProps = new Set(["animate", "initial", "layout", "transition", "variants", "viewport", "whileHover", "whileInView", "whileTap"]);
  const motion = new Proxy({}, {
    get: (_target, tag: string) => React.forwardRef(
      ({ children, ...props }: Record<string, unknown>, ref) => {
        const elementProps = Object.fromEntries(Object.entries(props).filter(([key]) => !motionProps.has(key)));
        return React.createElement(tag, { ...elementProps, ref } as React.Attributes, children as React.ReactNode);
      },
    ),
  });
  return { motion, useReducedMotion: () => true };
});

afterEach(() => {
  cleanup();
  authStatus = "unauthenticated";
});

function renderPage() {
  render(
    <MemoryRouter initialEntries={["/hero"]}>
      <Routes>
        <Route path="/hero" element={<HeroExplainPage />} />
        <Route path="/" element={<div>LANDING_DESTINATION</div>} />
        <Route path="/login" element={<div>LOGIN_DESTINATION</div>} />
        <Route path="/home" element={<div>HOME_DESTINATION</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("HeroExplainPage campaign flow", () => {
  it("returns to the canonical landing page from the brand control", () => {
    renderPage();
    fireEvent.click(screen.getByRole("button", { name: "EnglishAI.uz bosh sahifasi" }));
    expect(screen.getByText("LANDING_DESTINATION")).toBeTruthy();
  });

  it("sends a signed-out visitor to login", () => {
    renderPage();
    fireEvent.click(screen.getAllByRole("button", { name: "Bepul boshlash" })[0]);
    expect(screen.getByText("LOGIN_DESTINATION")).toBeTruthy();
  });

  it("sends an authenticated learner directly home", () => {
    authStatus = "authenticated";
    renderPage();
    fireEvent.click(screen.getAllByRole("button", { name: "Darsni davom ettirish" })[0]);
    expect(screen.getByText("HOME_DESTINATION")).toBeTruthy();
  });

  it("renders four product proof cards in a named region", () => {
    renderPage();
    expect(screen.getByRole("list", { name: "EnglishAI asosiy imkoniyatlari" }).children).toHaveLength(4);
  });
});
