import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import seo from "./content/seo.json";
import publicSeoRoutes from "./content/publicSeo.json";
import { publicPages } from "./content/publicContent";

import { uz } from "./content/uz";

const root = resolve(process.cwd());
const read = (path: string) => readFileSync(resolve(root, path), "utf8");

function structuredData() {
  const html = read("index.html");
  const match = html.match(
    /<script id="englishai-structured-data" type="application\/ld\+json">([\s\S]*?)<\/script>/,
  );
  expect(match).not.toBeNull();
  return JSON.parse(match![1]) as {
    "@graph": Array<Record<string, unknown>>;
  };
}

describe("public SEO and AI discovery contract", () => {
  it("publishes canonical, language and social metadata", () => {
    const html = read("index.html");

    expect(html).toContain(`<link rel="canonical" href="${seo.url}" />`);
    expect(html).toContain(`<link rel="alternate" hreflang="uz" href="${seo.url}" />`);
    expect(html).toContain(`<link rel="alternate" hreflang="x-default" href="${seo.url}" />`);
    expect(html).toContain(`<meta property="og:image:type" content="image/png" />`);
    expect(html).toContain(seo.image);
    expect(html).toContain(seo.logo);
    expect(html).not.toContain("og-image.png");
    expect(html).not.toContain("brand-icon-512.png");
  });

  it("links a complete Schema.org entity graph with stable identifiers", () => {
    const graph = structuredData()["@graph"];
    const types = graph.map((entry) => entry["@type"]);

    expect(types).toEqual([
      "Organization",
      "WebSite",
      "WebPage",
      "EducationalOrganization",
      "SoftwareApplication",
      "FAQPage",
    ]);
    expect(graph.map((entry) => entry["@id"])).toEqual([
      `${seo.url}#organization`,
      `${seo.url}#website`,
      `${seo.url}#webpage`,
      `${seo.url}#educational-organization`,
      `${seo.url}#application`,
      `${seo.url}#faq`,
    ]);
  });

  it("keeps visible and structured FAQs on the authoritative source", () => {
    const faq = structuredData()["@graph"].find((entry) => entry["@type"] === "FAQPage") as {
      mainEntity: Array<{ name: string; acceptedAnswer: { text: string } }>;
    };
    const expected = seo.faq.map(({ question, answer }) => ({ q: question, a: answer }));

    expect(uz.marketing.faqs).toEqual(expected);
    expect(faq.mainEntity.map((item) => ({ q: item.name, a: item.acceptedAnswer.text }))).toEqual(expected);
  });

  it("provides concise and full machine-readable product facts", () => {
    const concise = read("public/llms.txt");
    const full = read("public/llms-full.txt");

    for (const module of seo.modules) {
      expect(concise).toContain(`**${module.name}:**`);
      expect(full).toContain(`**${module.name}:**`);
    }
    expect(concise).toContain("https://englishai.uz/llms-full.txt");
    expect(full).toContain("## Manba va iqtibos siyosati");
    expect(full).not.toMatch(/api[_ -]?key|password|secret/i);
  });

  it("allows public facts to major AI crawlers while protecting private routes", () => {
    const robots = read("public/robots.txt");

    for (const bot of ["GPTBot", "OAI-SearchBot", "ClaudeBot", "Claude-SearchBot", "PerplexityBot", "Google-Extended", "Applebot-Extended"]) {
      expect(robots).toContain(`User-agent: ${bot}`);
    }
    for (const path of ["/api/", "/home", "/admin", "/profile", "/app/"]) {
      expect(robots).toContain(`Disallow: ${path}`);
    }
    expect(robots).toContain("Allow: /llms.txt");
    expect(robots).toContain(`Sitemap: ${seo.url}sitemap.xml`);
  });

  it("lists all public canonical pages with reciprocal language alternates in the sitemap", () => {
    const sitemap = read("public/sitemap.xml");
    const locations = [...sitemap.matchAll(/<loc>(.*?)<\/loc>/g)].map((match) => match[1]);

    expect(locations).toEqual([
      ...publicSeoRoutes.map((route) => route.canonical),
      ...publicPages.map((page) => `${seo.url.replace(/\/$/, "")}${page.path}/`),
    ]);
    for (const route of publicSeoRoutes) {
      expect(sitemap).toContain(`<lastmod>${route.lastModified}</lastmod>`);
      expect(sitemap).toContain(`hreflang="${route.locale}" href="${route.canonical}"`);
      expect(sitemap).toContain(`hreflang="x-default" href="https://englishai.uz${route.xDefaultPath}"`);
    }
  });

  it("marks authenticated SPA responses noindex without hiding public sources", () => {
    const nginx = read("nginx.conf");

    expect(nginx).toContain('add_header X-Robots-Tag "noindex, nofollow, noarchive" always;');
    expect(nginx).toContain("include /etc/nginx/seo-routes.conf;");
    expect(read("seo-routes.conf")).toContain("location = /ai-ingliz-tutor");
    expect(read("seo-routes.conf")).toContain("location = /en/ielts");
    expect(read("seo-routes.conf")).toContain("try_files /landing/index.html =404;");
    expect(nginx).toContain("location = /landing");
    expect(read("Dockerfile")).toContain("COPY seo-routes.conf /etc/nginx/seo-routes.conf");
    expect(nginx).toMatch(/location ~ \^\/\(robots\\\.txt\|sitemap\\\.xml\|llms\\\.txt\|llms-full\\\.txt\)\$/);
  });

  it("serves real search/social assets instead of SPA fallback", () => {
    for (const path of [
      "public/favicon.ico",
      "public/assets/og-image-20260815.png",
      "public/site.webmanifest",
      "public/assets/favicon-20260815-32.png",
      "public/assets/favicon-20260815-48.png",
      "public/assets/brand-icon-20260815-48.png",
      "public/assets/brand-icon-20260815-96.png",
      "public/assets/brand-icon-20260815-192.png",
      "public/assets/brand-icon-20260815-512.png",
      "public/assets/apple-touch-icon-20260815.png",
    ]) {
      expect(() => readFileSync(resolve(root, path))).not.toThrow();
    }
    expect(read("nginx.conf")).toContain("location = /site.webmanifest");
  });
});
