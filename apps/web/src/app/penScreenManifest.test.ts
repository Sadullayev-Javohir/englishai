import { describe, it, expect } from "vitest";
import screens from "./penScreenManifest.json";
import { AUDIT_ROUTES } from "./routeAuditManifest";

describe("EnglishAI Pen screen inventory", () => {
  it("pairs all 72 states with desktop and mobile references", () => {
    expect(screens).toHaveLength(144);
    expect(new Set(screens.map(s => s.nodeId)).size).toBe(144);
    const numbers = [...new Set(screens.map(s => s.number))];
    expect(numbers).toHaveLength(72);
    for (const number of numbers) {
      expect(screens.filter(s => s.number === number).map(s => s.width).sort()).toEqual([1440,390]);
    }
  });
  it("maps every design to an existing canonical route", () => {
    const routes = new Set(AUDIT_ROUTES.map(r => r.pattern));
    for (const screen of screens) {
      const route = screen.route.split(" · ")[0].split("?")[0];
      expect(routes.has(route), `${screen.nodeId}: ${route}`).toBe(true);
    }
  });
});
