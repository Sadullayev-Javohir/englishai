import { describe, expect, it } from "vitest";
import seoRoutes from "./content/publicSeo.json";

const ORIGIN = "https://englishai.uz";

describe("public SEO route registry", () => {
  it("defines exactly 14 indexable Uzbek and English routes", () => {
    expect(seoRoutes).toHaveLength(14);
    expect(seoRoutes.filter((route) => route.locale === "uz")).toHaveLength(7);
    expect(seoRoutes.filter((route) => route.locale === "en")).toHaveLength(7);
    expect(seoRoutes.every((route) => route.indexable)).toBe(true);
  });

  it("uses unique canonical URLs, titles, descriptions and one useful heading per route", () => {
    for (const field of ["path", "canonical", "title", "description", "heading"] as const) {
      const values = seoRoutes.map((route) => route[field]);
      expect(new Set(values).size, `${field} must be unique`).toBe(seoRoutes.length);
      expect(values.every(Boolean), `${field} must be populated`).toBe(true);
    }
    expect(seoRoutes.every((route) => route.canonical === `${ORIGIN}${route.path}`)).toBe(true);
  });

  it("has reciprocal Uzbek/English alternates and x-default on the Uzbek page", () => {
    for (const route of seoRoutes) {
      const alternate = seoRoutes.find((candidate) => candidate.path === route.alternatePath);
      expect(alternate, `${route.path} alternate exists`).toBeTruthy();
      expect(alternate?.alternatePath).toBe(route.path);
      expect(alternate?.locale).not.toBe(route.locale);
      expect(route.xDefaultPath).toBe(route.locale === "uz" ? route.path : route.alternatePath);
    }
  });

  it("ships substantial distinct content, visible FAQs and breadcrumb data", () => {
    for (const route of seoRoutes) {
      expect(route.intro.length).toBeGreaterThan(100);
      expect(route.benefits.length).toBeGreaterThanOrEqual(3);
      expect(route.steps.length).toBeGreaterThanOrEqual(3);
      expect(route.faq.length).toBeGreaterThanOrEqual(3);
      expect(route.breadcrumbs.at(-1)?.path).toBe(route.path);
    }
  });
});
