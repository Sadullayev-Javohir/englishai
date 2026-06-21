import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import seoRoutes from "@/content/publicSeo.json";
import { PublicSeoPage, buildPublicSeoGraph, type PublicSeoRoute } from "./PublicSeoPage";

describe("PublicSeoPage", () => {
  it("renders substantial localized content with one heading, internal links, FAQ and CTA", () => {
    render(
      <MemoryRouter initialEntries={["/en/vocabulary"]}>
        <PublicSeoPage />
      </MemoryRouter>,
    );

    expect(screen.getAllByRole("heading", { level: 1 })).toHaveLength(1);
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe("Learn English vocabulary you can actually use");
    expect(screen.getAllByRole("heading", { level: 2 }).length).toBeGreaterThanOrEqual(3);
    expect(screen.getAllByRole("link", { name: "Start free" }).every((link) => link.getAttribute("href") === "/login")).toBe(true);
    expect(screen.getByRole("link", { name: "O‘zbekcha" }).getAttribute("href")).toBe("/vocabulary");
    expect(screen.getAllByRole("link", { name: /Grammar/i }).length).toBeGreaterThan(0);
    expect(screen.getByText("How many words should I learn daily?")).toBeTruthy();
  });

  it("builds visible-content-aligned page, breadcrumb and FAQ schema", () => {
    const route = seoRoutes.find((item) => item.path === "/grammar")!;
    const graph = buildPublicSeoGraph(route as PublicSeoRoute);
    const types = graph["@graph"].map((entry) => entry["@type"]);

    expect(types).toContain("WebPage");
    expect(types).toContain("BreadcrumbList");
    expect(types).toContain("FAQPage");
    expect(JSON.stringify(graph)).toContain(route.faq[0].question);
    expect(JSON.stringify(graph)).toContain(route.heading);
  });
});
