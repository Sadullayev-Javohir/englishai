// Postbuild prerender: keep dist/index.html as the blank authenticated SPA shell, and emit
// the canonical marketing landing as dist/landing/index.html. Nginx serves that artifact
// only for the exact root URL, so refreshing an app route never paints marketing HTML first.
import { mkdir, readFile, writeFile, rm } from "node:fs/promises";
import { fileURLToPath, pathToFileURL } from "node:url";
import { dirname, resolve } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "..");
const ssrEntry = resolve(root, ".prerender/entry-prerender.js");
const indexHtml = resolve(root, "dist/index.html");
const ROOT_MARKER = '<div id="root"></div>';

function escapeHtml(value) {
  return value.replaceAll("&", "&amp;").replaceAll('"', "&quot;").replaceAll("<", "&lt;").replaceAll(">", "&gt;");
}

function replaceMeta(html, pattern, replacement, label) {
  if (!pattern.test(html)) throw new Error(`Could not find ${label} in dist/index.html.`);
  return html.replace(pattern, replacement);
}

function pageSchema(page) {
  if (page.seoRoute) {
    const organizationId = "https://englishai.uz/#organization";
    const websiteId = "https://englishai.uz/#website";
    return {
      "@context": "https://schema.org",
      "@graph": [
        { "@type": "Organization", "@id": organizationId, name: "EnglishAI.uz", alternateName: ["EnglishAI", "English AI", "EnglishAIuz"], url: "https://englishai.uz/", logo: { "@type": "ImageObject", url: "https://englishai.uz/assets/brand/icon-512.png" }, sameAs: ["https://www.linkedin.com/company/englishai-uz/", "https://t.me/englishaiuz"] },
        { "@type": "WebSite", "@id": websiteId, name: "EnglishAI.uz", alternateName: ["EnglishAI", "English AI", "EnglishAIuz"], url: "https://englishai.uz/", inLanguage: ["uz", "en"], publisher: { "@id": organizationId } },
        { "@type": "SoftwareApplication", "@id": "https://englishai.uz/#application", name: "EnglishAI.uz", applicationCategory: "EducationalApplication", operatingSystem: "Web, Android", url: "https://englishai.uz/", image: `https://englishai.uz${page.image}`, offers: { "@type": "Offer", price: "0", priceCurrency: "UZS" }, publisher: { "@id": organizationId } },
        { "@type": "WebPage", "@id": `${page.canonical}#webpage`, url: page.canonical, name: page.title, headline: page.heading, description: page.description, inLanguage: page.locale, isPartOf: { "@id": websiteId }, primaryImageOfPage: { "@type": "ImageObject", url: `https://englishai.uz${page.image}` } },
        ...(page.breadcrumbs.length > 1 ? [{ "@type": "BreadcrumbList", itemListElement: page.breadcrumbs.map((item, index) => ({ "@type": "ListItem", position: index + 1, name: item.name, item: `https://englishai.uz${item.path}` })) }] : []),
        { "@type": "FAQPage", mainEntity: page.faq.map(({ question, answer }) => ({ "@type": "Question", name: question, acceptedAnswer: { "@type": "Answer", text: answer } })) },
      ],
    };
  }
  const canonical = `https://englishai.uz${page.path === "/" ? "/" : `${page.path}/`}`;
  const graph = [
    {
      "@type": page.schemaType,
      "@id": `${canonical}#article`,
      headline: page.title,
      description: page.description,
      inLanguage: "uz",
      datePublished: page.publishedAt,
      dateModified: page.reviewedAt,
      author: { "@type": "Organization", name: page.author, url: "https://englishai.uz/about/" },
      publisher: { "@type": "Organization", name: "EnglishAI.uz", url: "https://englishai.uz/" },
      mainEntityOfPage: canonical,
      about: { "@id": "https://englishai.uz/#application" },
    },
    {
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "EnglishAI.uz", item: "https://englishai.uz/" },
        ...(page.path === "/learn" ? [] : [{ "@type": "ListItem", position: 2, name: "Qo‘llanmalar", item: "https://englishai.uz/learn/" }]),
        { "@type": "ListItem", position: page.path === "/learn" ? 2 : 3, name: page.title, item: canonical },
      ],
    },
    {
      "@type": "WebPage",
      "@id": `${canonical}#webpage`,
      url: canonical,
      name: page.title,
      description: page.description,
      inLanguage: "uz",
      about: { "@id": "https://englishai.uz/#application" },
    },
  ];
  if (page.faq.length) {
    graph.push({
      "@type": "FAQPage",
      "@id": `${canonical}#faq`,
      mainEntity: page.faq.map(({ question, answer }) => ({
        "@type": "Question", name: question, acceptedAnswer: { "@type": "Answer", text: answer },
      })),
    });
  }
  return { "@context": "https://schema.org", "@graph": graph };
}

function injectPage(baseHtml, appHtml, page) {
  if (page.seoRoute) {
    let html = baseHtml.replace(ROOT_MARKER, `<div id="root">${appHtml}</div>`);
    const alternate = page.alternatePath;
    const xDefault = `https://englishai.uz${page.xDefaultPath}`;
    html = html.replace(/(<html\b[^>]*\blang=")[^"]*(")/, `$1${page.locale}$2`);
    html = replaceMeta(html, /<title>[\s\S]*?<\/title>/, `<title>${escapeHtml(page.title)}</title>`, "title");
    html = replaceMeta(html, /<meta\s+name="description"\s+content="[^"]*"\s*\/>/, `<meta name="description" content="${escapeHtml(page.description)}" />`, "description");
    html = replaceMeta(html, /<link rel="canonical" href="[^"]*" \/>/, `<link rel="canonical" href="${page.canonical}" />`, "canonical");
    html = replaceMeta(html, /<link rel="alternate" hreflang="uz" href="[^"]*" \/>\s*<link rel="alternate" hreflang="x-default" href="[^"]*" \/>/, `<link rel="alternate" hreflang="${page.locale}" href="${page.canonical}" />\n    <link rel="alternate" hreflang="${page.locale === "uz" ? "en" : "uz"}" href="https://englishai.uz${alternate}" />\n    <link rel="alternate" hreflang="x-default" href="${xDefault}" />`, "hreflang set");
    html = replaceMeta(html, /<meta property="og:title" content="[^"]*" \/>/, `<meta property="og:title" content="${escapeHtml(page.title)}" />`, "og:title");
    html = replaceMeta(html, /<meta\s+property="og:description"\s+content="[^"]*"\s*\/>/, `<meta property="og:description" content="${escapeHtml(page.description)}" />`, "og:description");
    html = replaceMeta(html, /<meta property="og:url" content="[^"]*" \/>/, `<meta property="og:url" content="${page.canonical}" />`, "og:url");
    html = replaceMeta(html, /<meta property="og:image" content="[^"]*" \/>/, `<meta property="og:image" content="https://englishai.uz${page.image}" />`, "og:image");
    html = replaceMeta(html, /<meta property="og:image:secure_url" content="[^"]*" \/>/, `<meta property="og:image:secure_url" content="https://englishai.uz${page.image}" />`, "og:image:secure_url");
    html = replaceMeta(html, /<meta property="og:locale" content="[^"]*" \/>/, `<meta property="og:locale" content="${page.locale === "uz" ? "uz_UZ" : "en_US"}" />`, "og:locale");
    html = replaceMeta(html, /<meta name="twitter:title" content="[^"]*" \/>/, `<meta name="twitter:title" content="${escapeHtml(page.title)}" />`, "twitter:title");
    html = replaceMeta(html, /<meta\s+name="twitter:description"\s+content="[^"]*"\s*\/>/, `<meta name="twitter:description" content="${escapeHtml(page.description)}" />`, "twitter:description");
    html = replaceMeta(html, /<meta name="twitter:image" content="[^"]*" \/>/, `<meta name="twitter:image" content="https://englishai.uz${page.image}" />`, "twitter:image");
    html = replaceMeta(html, /<script id="englishai-structured-data" type="application\/ld\+json">[\s\S]*?<\/script>/, `<script id="englishai-structured-data" type="application/ld+json">${JSON.stringify(pageSchema(page))}</script>`, "structured data");
    return html;
  }
  const canonical = `https://englishai.uz${page.path === "/" ? "/" : `${page.path}/`}`;
  let html = baseHtml.replace(ROOT_MARKER, `<div id="root">${appHtml}</div>`);
  html = replaceMeta(html, /<title>[\s\S]*?<\/title>/, `<title>${escapeHtml(page.title)} | EnglishAI.uz</title>`, "title");
  html = replaceMeta(html, /<meta\s+name="description"\s+content="[^"]*"\s*\/>/, `<meta name="description" content="${escapeHtml(page.description)}" />`, "description");
  html = replaceMeta(html, /<link rel="canonical" href="[^"]*" \/>/, `<link rel="canonical" href="${canonical}" />`, "canonical");
  html = replaceMeta(html, /<link rel="alternate" hreflang="uz" href="[^"]*" \/>/, `<link rel="alternate" hreflang="uz" href="${canonical}" />`, "hreflang");
  html = replaceMeta(html, /<meta property="og:title" content="[^"]*" \/>/, `<meta property="og:title" content="${escapeHtml(page.title)}" />`, "og:title");
  html = replaceMeta(html, /<meta\s+property="og:description"\s+content="[^"]*"\s*\/>/, `<meta property="og:description" content="${escapeHtml(page.description)}" />`, "og:description");
  html = replaceMeta(html, /<meta property="og:url" content="[^"]*" \/>/, `<meta property="og:url" content="${canonical}" />`, "og:url");
  html = replaceMeta(html, /<meta property="og:image" content="[^"]*" \/>/, `<meta property="og:image" content="https://englishai.uz/assets/og-image-20260813.png" />`, "og:image");
  html = replaceMeta(html, /<meta property="og:image:secure_url" content="[^"]*" \/>/, `<meta property="og:image:secure_url" content="https://englishai.uz/assets/og-image-20260813.png" />`, "og:image:secure_url");
  html = replaceMeta(html, /<meta name="twitter:title" content="[^"]*" \/>/, `<meta name="twitter:title" content="${escapeHtml(page.title)}" />`, "twitter:title");
  html = replaceMeta(html, /<meta\s+name="twitter:description"\s+content="[^"]*"\s*\/>/, `<meta name="twitter:description" content="${escapeHtml(page.description)}" />`, "twitter:description");
  html = replaceMeta(html, /<meta name="twitter:image" content="[^"]*" \/>/, `<meta name="twitter:image" content="https://englishai.uz/assets/og-image-20260813.png" />`, "twitter:image");
  html = replaceMeta(html, /<script id="englishai-structured-data" type="application\/ld\+json">[\s\S]*?<\/script>/, `<script id="englishai-structured-data" type="application/ld+json">${JSON.stringify(pageSchema(page))}</script>`, "structured data");
  return html;
}

async function main() {
  const { render, metadata, publicPaths } = await import(pathToFileURL(ssrEntry).href);
  const baseHtml = await readFile(indexHtml, "utf8");
  if (!baseHtml.includes(ROOT_MARKER)) {
    throw new Error(`Could not find '${ROOT_MARKER}' in dist/index.html - prerender aborted.`);
  }
  const landingHtml = render("/");
  if (!landingHtml || landingHtml.length < 100) throw new Error("Landing prerender output is suspiciously small.");
  const landingDir = resolve(root, "dist", "landing");
  await mkdir(landingDir, { recursive: true });
  await writeFile(resolve(landingDir, "index.html"), baseHtml.replace(ROOT_MARKER, `<div id="root">${landingHtml}</div>`), "utf8");

  for (const path of publicPaths) {
    const page = metadata(path);
    if (!page?.published) throw new Error(`Missing published metadata for ${path}.`);
    const appHtml = render(path);
    if (!appHtml || appHtml.length < 300) throw new Error(`Prerender output for ${path} is suspiciously small.`);
    const outDir = resolve(root, "dist", path.slice(1));
    await mkdir(outDir, { recursive: true });
    await writeFile(resolve(outDir, "index.html"), injectPage(baseHtml, appHtml, page), "utf8");
  }
  await rm(resolve(root, ".prerender"), { recursive: true, force: true });

  console.log(`✓ Prerendered isolated landing page and ${publicPaths.length} public content routes.`);
}

main().catch((err) => {
  console.error("✗ Prerender failed:", err.message);
  process.exit(1);
});
