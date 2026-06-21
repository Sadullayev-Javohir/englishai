import { describe, expect, it } from "vitest";
import { getPublicPage, guidePages, publicPages, publicPaths, trustPages } from "./publicContent";

describe("public content catalog", () => {
  it("publishes the hub, six pillar guides and six trust pages", () => {
    expect(guidePages).toHaveLength(6);
    expect(trustPages).toHaveLength(6);
    expect(publicPages).toHaveLength(13);
    expect(publicPages.every((page) => page.published)).toBe(true);
  });

  it("keeps paths, slugs and titles unique", () => {
    expect(new Set(publicPaths).size).toBe(publicPages.length);
    expect(new Set(publicPages.map((page) => page.slug)).size).toBe(publicPages.length);
    expect(new Set(publicPages.map((page) => page.title)).size).toBe(publicPages.length);
  });

  it("requires substantive content, visible FAQs, sources and valid related links", () => {
    for (const page of publicPages) {
      expect(page.description.length).toBeGreaterThan(60);
      expect(page.summary.length).toBeGreaterThan(80);
      expect(page.faq.length).toBeGreaterThanOrEqual(4);
      expect(page.sources.length).toBeGreaterThan(0);
      expect(page.relatedPaths.length).toBeGreaterThanOrEqual(3);
      expect(page.cta.href.startsWith("/")).toBe(true);
      if (page.kind !== "hub") expect(page.sections.length).toBeGreaterThanOrEqual(3);
      for (const relatedPath of page.relatedPaths) expect(getPublicPage(relatedPath)).toBeDefined();
    }
  });

  it("normalizes trailing slashes without inventing doorway aliases", () => {
    expect(getPublicPage("/learn/")?.path).toBe("/learn");
    expect(getPublicPage("/learn/not-a-real-guide")).toBeUndefined();
  });
});
