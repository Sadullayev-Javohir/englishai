import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { UsernameSetupPage } from "./UsernameSetupPage";

vi.mock("@/app/auth", () => ({
  useAuth: () => ({
    user: { displayName: "Test User" },
    applyUser: vi.fn(),
  }),
}));
vi.mock("@/lib/useUsernameCheck", () => ({
  useUsernameCheck: () => ({ kind: "idle" }),
  canSubmitUsername: () => false,
  normalizeUsername: (value: string) => value,
}));
vi.mock("@/api/client", () => ({
  ApiError: class ApiError extends Error {},
  api: { auth: { updateProfile: vi.fn() } },
}));
vi.mock("framer-motion", async () => {
  const React = await import("react");
  const motion = new Proxy({}, {
    get: (_target, tag: string) => React.forwardRef(
      (props: Record<string, unknown>, ref) => {
        const { children } = props;
        const htmlProps = { ...props };
        delete htmlProps.initial;
        delete htmlProps.animate;
        delete htmlProps.variants;
        delete htmlProps.transition;
        delete htmlProps.whileInView;
        delete htmlProps.viewport;
        delete htmlProps.children;
        return React.createElement(tag, { ...htmlProps, ref } as React.Attributes, children as React.ReactNode);
      },
    ),
  });
  return { motion, useReducedMotion: () => true };
});

afterEach(cleanup);

describe("UsernameSetupPage", () => {
  it("presents setup as the first onboarding step", () => {
    render(<MemoryRouter><UsernameSetupPage /></MemoryRouter>);
    expect(screen.getByText("1 / 3 · Shaxsiy profilingiz")).toBeTruthy();
  });

  it("renders the companion message and learner profile preview", () => {
    render(<MemoryRouter><UsernameSetupPage /></MemoryRouter>);
    expect(screen.getByText("Tanishganimdan xursandman!")).toBeTruthy();
    expect(screen.getByText("Yangi o‘rganuvchi")).toBeTruthy();
  });

  it("keeps the username field and continue action accessible", () => {
    const view = render(<MemoryRouter><UsernameSetupPage /></MemoryRouter>);
    const field = screen.getByLabelText("Foydalanuvchi nomi") as HTMLInputElement;
    expect(field.getAttribute("aria-describedby")).toBe("username-status");
    expect(view.container.querySelector(".username-field--setup")).toBeTruthy();
    expect((screen.getByRole("button", { name: "Davom etish" }) as HTMLButtonElement).disabled).toBe(true);
  });

  it("uses the scoped username page namespace", () => {
    const view = render(<MemoryRouter><UsernameSetupPage /></MemoryRouter>);
    expect(view.container.querySelector(".username-setup")).toBeTruthy();
    // The setup form uses the same chrome as the other seven Pen screens.
    expect(view.container.querySelector(".username-setup .play-username")).toBeTruthy();
  });
});
