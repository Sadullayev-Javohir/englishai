import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { getRouteMetadata } from "@/app/routeMetadata";
import { HomeBackButton } from "./HomeBackButton";

afterEach(cleanup);

function CurrentPath() {
  return <span>{useLocation().pathname}</span>;
}

describe("HomeBackButton", () => {
  it("navigates directly to the home page", () => {
    render(
      <MemoryRouter initialEntries={["/app/grammar"]}>
        <HomeBackButton />
        <Routes>
          <Route path="*" element={<CurrentPath />} />
        </Routes>
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole("button", { name: "Orqaga" }));

    expect(screen.getByText("/home")).toBeTruthy();
  });

  it("returns to progress when the page was opened from progress", () => {
    render(
      <MemoryRouter initialEntries={[{ pathname: "/app/grammar", state: { returnTo: "/progress" } }]}>
        <HomeBackButton />
        <Routes>
          <Route path="*" element={<CurrentPath />} />
        </Routes>
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole("button", { name: "Orqaga" }));

    expect(screen.getByText("/progress")).toBeTruthy();
  });

  it("ignores unsupported return targets", () => {
    render(
      <MemoryRouter initialEntries={[{ pathname: "/app/grammar", state: { returnTo: "https://example.com" } }]}>
        <HomeBackButton />
        <Routes>
          <Route path="*" element={<CurrentPath />} />
        </Routes>
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole("button", { name: "Orqaga" }));

    expect(screen.getByText("/home")).toBeTruthy();
  });

  it("uses route metadata for catalog and nested route behavior", () => {
    expect(getRouteMetadata("/app/grammar").homeBack).toBe(true);
    expect(getRouteMetadata("/app/speaking/role-talk").homeBack).toBe(true);
    expect(getRouteMetadata("/progress").homeBack).toBe(true);
    expect(getRouteMetadata("/leaderboard").homeBack).toBe(false);
    expect(getRouteMetadata("/app/grammar/topic/example").shellMode).toBe("lessonFrame");
  });
});
