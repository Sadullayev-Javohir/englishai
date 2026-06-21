import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, useLocation } from "react-router-dom";
import { NotificationBell } from "./NotificationBell";
import { readFileSync } from "node:fs";

vi.mock("@/lib/useUnreadNotifications", () => ({ useUnreadNotifications: () => 2 }));
vi.mock("@/lib/haptics", () => ({ tapLight: vi.fn() }));

function LocationProbe() {
  const location = useLocation();
  return <output>{location.pathname}{location.search}</output>;
}

afterEach(cleanup);

describe("NotificationBell", () => {
  it("uses theme tokens instead of a white unread-free surface", () => {
    const source = readFileSync("src/components/NotificationBell.tsx", "utf8");
    const css = readFileSync("src/index.css", "utf8");

    expect(source).toContain("!bg-[var(--ea-surface)]");
    expect(source).toContain("!ring-[var(--ea-border)]");
    expect(source).not.toContain('"!bg-white !text-[var(--ea-text)] !ring-white"');
    expect(css).toMatch(/button\[aria-label="Bildirishnomalar"\]\[data-has-unread="false"\] \{\s*background: var\(--ea-surface\) !important;/);
    expect(css).toMatch(/button\[aria-label="Bildirishnomalar"\]\[data-has-unread="false"\] :where\(svg, \.material-symbols-rounded\) \{\s*color: var\(--ea-text\) !important;\s*stroke: currentColor !important;/);
    expect(css).not.toMatch(/button\[aria-label="Bildirishnomalar"\]\[data-has-unread="false"\] \{\s*background: #fff !important;/);
  });

  it("opens notifications through the current route query", () => {
    render(
      <MemoryRouter initialEntries={["/app/grammar?tab=topics"]}>
        <NotificationBell />
        <LocationProbe />
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole("button", { name: "Bildirishnomalar" }));

    expect(screen.getByText("/app/grammar?tab=topics&notifications=open")).toBeTruthy();
  });

  it("uses the red background while unread notifications exist", () => {
    const css = readFileSync("src/index.css", "utf8");

    render(
      <MemoryRouter>
        <NotificationBell />
      </MemoryRouter>,
    );

    const button = screen.getByRole("button", { name: "Bildirishnomalar" });
    expect(button.dataset.hasUnread).toBe("true");
    expect(button.className).toContain("!bg-error");
    expect(css).toMatch(/button\[aria-label="Bildirishnomalar"\]\[data-has-unread="true"\] :where\(svg, \.material-symbols-rounded\) \{\s*color: #fff !important;\s*stroke: currentColor !important;/);
  });
});
