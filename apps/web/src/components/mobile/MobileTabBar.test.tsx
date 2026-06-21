import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { MobileTabBar } from "./MobileTabBar";
import { uz } from "@/content/uz";

afterEach(cleanup);

describe("MobileTabBar", () => {
  it("uses route metadata to mark the active destination", () => {
    render(
      <MemoryRouter initialEntries={["/progress"]}>
        <MobileTabBar />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: uz.nav.progress }).className).toContain("is-active");
    expect(screen.getByRole("link", { name: uz.nav.progress }).getAttribute("aria-current")).toBe("page");
    expect(screen.getByTestId("compact-tab-surface").className).toContain("ea-bottom-nav__surface");
    expect(screen.queryByRole("link", { name: "Speaking" })).toBeNull();
  });
});
