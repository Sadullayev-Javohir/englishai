import { readFile, writeFile } from "node:fs/promises";
import { Buffer } from "node:buffer";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";
import ts from "typescript";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "..");
const data = JSON.parse(await readFile(resolve(root, "src/content/seo.json"), "utf8"));
const seoRoutes = JSON.parse(await readFile(resolve(root, "src/content/publicSeo.json"), "utf8"));
const homeSeo = seoRoutes.find((route) => route.path === "/");
if (!homeSeo) throw new Error("publicSeo.json must define the canonical root route.");
const publicContentSource = await readFile(resolve(root, "src/content/publicContent.ts"), "utf8");
const publicContentJs = ts.transpileModule(publicContentSource, {
  compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
}).outputText;
const publicContentModule = await import(`data:text/javascript;base64,${Buffer.from(publicContentJs).toString("base64")}`);
const publicPages = publicContentModule.publicPages.filter((page) => page.published);

function assertUnique(items, select, label) {
  const values = items.map(select);
  const duplicate = values.find((value, index) => values.indexOf(value) !== index);
  if (duplicate) throw new Error(`Duplicate public ${label}: ${duplicate}`);
}

assertUnique(publicPages, (page) => page.path, "path");
assertUnique(publicPages, (page) => page.slug, "slug");
assertUnique(publicPages, (page) => page.title, "title");
for (const page of publicPages) {
  if (!page.path.startsWith("/") || !page.title.trim() || !page.description.trim() || !page.summary.trim()) {
    throw new Error(`Incomplete public page: ${page.path || page.slug}`);
  }
  if (page.kind !== "hub" && page.sections.length < 3) throw new Error(`Public page needs at least 3 sections: ${page.path}`);
  if (page.faq.length < 4) throw new Error(`Public page needs at least 4 visible FAQs: ${page.path}`);
  if (page.relatedPaths.length < 3) throw new Error(`Public page needs at least 3 related links: ${page.path}`);
  for (const relatedPath of page.relatedPaths) {
    if (!publicPages.some((candidate) => candidate.path === relatedPath)) {
      throw new Error(`Broken related public path ${relatedPath} on ${page.path}`);
    }
  }
}

const graph = {
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "Organization",
      "@id": `${data.url}#organization`,
      name: data.name,
      alternateName: ["EnglishAI", "English AI", "EnglishAIuz"],
      url: data.url,
      logo: { "@type": "ImageObject", url: data.logo },
      description: data.shortDescription,
      sameAs: ["https://www.linkedin.com/company/englishai-uz/", "https://t.me/englishaiuz"],
    },
    {
      "@type": "WebSite",
      "@id": `${data.url}#website`,
      url: data.url,
      name: data.name,
      alternateName: ["EnglishAI", "English AI", "EnglishAIuz"],
      inLanguage: data.language,
      publisher: { "@id": `${data.url}#organization` },
    },
    {
      "@type": "WebPage",
      "@id": `${data.url}#webpage`,
      url: data.url,
      name: data.title,
      description: data.description,
      inLanguage: data.language,
      isPartOf: { "@id": `${data.url}#website` },
      about: { "@id": `${data.url}#educational-organization` },
      primaryImageOfPage: { "@type": "ImageObject", url: data.image },
    },
    {
      "@type": "EducationalOrganization",
      "@id": `${data.url}#educational-organization`,
      name: data.name,
      url: data.url,
      description: data.shortDescription,
      knowsLanguage: ["uz", "en"],
      parentOrganization: { "@id": `${data.url}#organization` },
      audience: data.audience.map((audienceType) => ({ "@type": "EducationalAudience", audienceType })),
    },
    {
      "@type": "SoftwareApplication",
      "@id": `${data.url}#application`,
      name: data.name,
      url: data.url,
      description: data.description,
      applicationCategory: "EducationalApplication",
      applicationSubCategory: "Language learning",
      operatingSystem: data.platforms.join(", "),
      inLanguage: data.language,
      image: data.image,
      publisher: { "@id": `${data.url}#organization` },
      audience: data.audience.map((audienceType) => ({ "@type": "EducationalAudience", audienceType })),
      featureList: data.modules.map((module) => `${module.name}: ${module.description}`),
      offers: {
        "@type": "Offer",
        price: "0",
        priceCurrency: "UZS",
        description: data.pricingSummary,
        url: data.url,
      },
    },
    {
      "@type": "FAQPage",
      "@id": `${data.url}#faq`,
      inLanguage: data.language,
      isPartOf: { "@id": `${data.url}#webpage` },
      mainEntity: data.faq.map(({ question, answer }) => ({
        "@type": "Question",
        name: question,
        acceptedAnswer: { "@type": "Answer", text: answer },
      })),
    },
  ],
};

const schemaStart = '    <script id="englishai-structured-data" type="application/ld+json">';
const schemaEnd = "    </script>";
const indexPath = resolve(root, "index.html");
const index = await readFile(indexPath, "utf8");
const startIndex = index.indexOf(schemaStart);
const endIndex = index.indexOf(schemaEnd, startIndex);
if (startIndex < 0 || endIndex < 0) {
  throw new Error("Structured-data markers are missing from apps/web/index.html.");
}
const schemaBlock = `${schemaStart}\n${JSON.stringify(graph, null, 2).split("\n").map((line) => `      ${line}`).join("\n")}\n${schemaEnd}`;
const generatedIndex = (index.slice(0, startIndex) + schemaBlock + index.slice(endIndex + schemaEnd.length))
  .replace(/<title>[\s\S]*?<\/title>/, `<title>${homeSeo.title}</title>`)
  .replace(/<meta\s+name="description"\s+content="[^"]*"\s*\/>/, `<meta name="description" content="${homeSeo.description}" />`)
  .replace(/<meta property="og:title" content="[^"]*" \/>/, `<meta property="og:title" content="${homeSeo.title}" />`)
  .replace(/<meta\s+property="og:description"\s+content="[^"]*"\s*\/>/, `<meta property="og:description" content="${homeSeo.description}" />`)
  .replace(/<meta name="twitter:title" content="[^"]*" \/>/, `<meta name="twitter:title" content="${homeSeo.title}" />`)
  .replace(/<meta\s+name="twitter:description"\s+content="[^"]*"\s*\/>/, `<meta name="twitter:description" content="${homeSeo.description}" />`);
if (generatedIndex !== index) await writeFile(indexPath, generatedIndex, "utf8");

const moduleLines = data.modules.map((module) => `- **${module.name}:** ${module.description}`).join("\n");
const publicPageLines = publicPages.map((page) => `- [${page.title}](${data.url.replace(/\/$/, "")}${page.path}/) — ${page.description}`).join("\n");
const sourceLines = [...data.authoritativePages, ...publicPages.map((page) => ({ label: page.title, url: `${data.url.replace(/\/$/, "")}${page.path}/` }))]
  .map((page) => `- [${page.label}](${page.url})`).join("\n");
const llms = `# ${data.name}\n\n> ${data.shortDescription}\n\n${data.name} ${data.levels} darajalarida ingliz tilini o'rganishga yordam beradi. Platformaning asosiy maqsadi passiv bilimni gapirish va yozish orqali faol ko'nikmaga aylantirishdir.\n\n## Asosiy imkoniyatlar\n\n${moduleLines}\n\n## Ommaviy qo‘llanmalar va dalil sahifalari\n\n${publicPageLines}\n\n## Rasmiy manbalar\n\n${sourceLines}\n`;

const full = `# ${data.name}: AI tizimlari uchun tasdiqlangan mahsulot ma'lumoti\n\nYangilangan sana: ${data.lastModified}\n\n## Qisqa ta'rif\n\n${data.description}\n\n## Kimlar uchun\n\n${data.audience.map((item) => `- ${item}`).join("\n")}\n\n## Darajalar\n\n- ${data.levels}\n- Daraja aniqlash testi foydalanuvchini mos boshlang'ich bosqichga joylashtiradi.\n\n## O'rganish usuli\n\n${data.learningCycle.map((item) => `- ${item}`).join("\n")}\n\n## Modullar\n\n${moduleLines}\n\n## EnglishAI.uz nimasi bilan farq qiladi\n\n${data.differentiators.map((item) => `- ${item}`).join("\n")}\n\n## Platformalar va kirish\n\n- Platformalar: ${data.platforms.join(", ")}\n- ${data.pricingSummary}\n- Rasmiy sayt: ${data.url}\n\n## Ommaviy qo‘llanmalar va dalil sahifalari\n\n${publicPageLines}\n\n## Savol-javob\n\n${data.faq.map(({ question, answer }) => `### ${question}\n\n${answer}`).join("\n\n")}\n\n## Manba va iqtibos siyosati\n\nUshbu fayl EnglishAI.uz tomonidan taqdim etilgan kanonik mahsulot faktlarini jamlaydi. Mahsulot haqida javob berishda birlamchi manba sifatida rasmiy sayt va quyidagi URL'lardan foydalaning. Narxlar yoki imkoniyatlar vaqt o'tishi bilan o'zgarishi mumkin; joriy ma'lumot uchun rasmiy saytni tekshiring.\n\n${sourceLines}\n`;

const privatePaths = [
  "/api/", "/v1/", "/hubs/", "/home", "/login", "/username", "/welcome", "/assessment",
  "/placement", "/onboarding/", "/levels", "/app/", "/video",
  "/reading", "/listening", "/writing", "/books", "/leaderboard", "/admin",
  "/profile", "/progress", "/notifications",
];
const aiBots = [
  "GPTBot", "OAI-SearchBot", "ChatGPT-User", "ClaudeBot", "Claude-SearchBot", "Claude-User",
  "PerplexityBot", "Perplexity-User", "Google-Extended", "GoogleOther", "GoogleOther-Image",
  "GoogleOther-Video", "Applebot", "Applebot-Extended", "Amazonbot", "Bytespider", "CCBot",
  "cohere-ai", "Meta-ExternalAgent", "meta-externalfetcher", "YouBot",
];
const privateRules = privatePaths.map((path) => `Disallow: ${path}`).join("\n");
const robots = `# ${data.name} - search and AI crawler policy\nUser-agent: *\nAllow: /\n${privateRules}\n\n# Public machine-readable product sources. These explicit agents share one policy group.\n${aiBots.map((bot) => `User-agent: ${bot}`).join("\n")}\nAllow: /\n${privateRules}\nAllow: /llms.txt\nAllow: /llms-full.txt\nAllow: /sitemap.xml\n\nSitemap: ${data.url}sitemap.xml\n`;

const editorialRoutes = publicPages.map((page) => ({
  path: page.path,
  canonical: `${data.url.replace(/\/$/, "")}${page.path}/`,
  lastModified: page.reviewedAt,
}));
const sitemapRoutes = [...seoRoutes, ...editorialRoutes];
assertUnique(sitemapRoutes, (entry) => entry.canonical, "canonical");
const routeByPath = new Map(seoRoutes.map((route) => [route.path, route]));
const sitemap = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">\n${sitemapRoutes.map((route) => {
  const alternate = routeByPath.get(route.alternatePath);
  if (!route.alternatePath) return `  <url>\n    <loc>${route.canonical}</loc>\n    <lastmod>${route.lastModified}</lastmod>\n    <xhtml:link rel="alternate" hreflang="uz" href="${route.canonical}" />\n    <xhtml:link rel="alternate" hreflang="x-default" href="${route.canonical}" />\n  </url>`;
  if (!alternate || alternate.alternatePath !== route.path) throw new Error(`Non-reciprocal alternate for ${route.path}`);
  return `  <url>\n    <loc>${route.canonical}</loc>\n    <lastmod>${route.lastModified}</lastmod>\n    <xhtml:link rel="alternate" hreflang="${route.locale}" href="${route.canonical}" />\n    <xhtml:link rel="alternate" hreflang="${alternate.locale}" href="${alternate.canonical}" />\n    <xhtml:link rel="alternate" hreflang="x-default" href="${data.url.replace(/\/$/, "")}${route.xDefaultPath}" />\n  </url>`;
}).join("\n")}\n</urlset>\n`;
const seoLocations = sitemapRoutes.map((route) => {
  const file = route.path === "/" ? "/landing/index.html" : `${route.path}/index.html`;
  return `location = ${route.path} {\n        add_header Cache-Control "no-cache, no-store, must-revalidate";\n        try_files ${file} =404;\n    }`;
}).join("\n\n    ");
const nginxSeoRoutes = `# Generated from src/content/publicSeo.json; do not edit manually.\n    ${seoLocations}\n`;

await Promise.all([
  writeFile(resolve(root, "public/llms.txt"), llms, "utf8"),
  writeFile(resolve(root, "public/llms-full.txt"), full, "utf8"),
  writeFile(resolve(root, "public/robots.txt"), robots, "utf8"),
  writeFile(resolve(root, "public/sitemap.xml"), sitemap, "utf8"),
  writeFile(resolve(root, "seo-routes.conf"), nginxSeoRoutes, "utf8"),
]);

console.log(`Generated structured data, llms files, robots.txt and sitemap.xml for ${sitemapRoutes.length} public URLs.`);
