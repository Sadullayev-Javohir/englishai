import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { AUDIT_ROUTES } from "./routeAuditManifest";

const routerSource = readFileSync(resolve(__dirname, "router.tsx"), "utf8");
const routerPatterns = Array.from(routerSource.matchAll(/path:\s*"([^"]+)"/g), (match) => match[1]);

function sortedUnique(values: string[]) {
  return [...new Set(values)].sort();
}

describe("route audit manifest", () => {
  it("covers every production router path exactly once", () => {
    const productionPatterns = routerPatterns.filter((path) => path !== "/dev/lesson-foundation");
    expect(sortedUnique(AUDIT_ROUTES.map((route) => route.pattern))).toEqual(sortedUnique(productionPatterns));
    expect(AUDIT_ROUTES).toHaveLength(sortedUnique(productionPatterns).length);
  });

  it("uses concrete paths for every dynamic route", () => {
    const unresolved = AUDIT_ROUTES.filter((route) => route.path.includes(":"));
    expect(unresolved).toEqual([]);
  });

  it("requires populated-data evidence on representative catalogs and dashboards", () => {
    const populatedRoutes = AUDIT_ROUTES.filter((route) => route.expectedText);
    expect(populatedRoutes.map((route) => route.pattern)).toEqual(expect.arrayContaining([
      "/",
      "/home",
      "/video",
      "/reading",
      "/books",
      "/app/grammar",
      "/writing",
      "/listening",
      "/app/vocabulary/topics",
      "/leaderboard",
      "/profile",
      "/admin/users",
    ]));
  });
});
