import { useEffect } from "react";
import { Link, useLocation } from "react-router-dom";
import { ParrotLogo } from "@/components/ParrotLogo";
import { Icon } from "@/components/ui/Icon";
import seoRoutesData from "@/content/publicSeo.json";
import { officialContacts } from "@/content/officialContacts";
import "./LandingPage.css";
import "./PublicSeoPage.css";

const ORIGIN = "https://englishai.uz";
const ORGANIZATION_ID = `${ORIGIN}/#organization`;
const WEBSITE_ID = `${ORIGIN}/#website`;
const APP_ID = `${ORIGIN}/#application`;

type Locale = "uz" | "en";
interface SeoFaq { question: string; answer: string }
interface SeoBreadcrumb { name: string; path: string }
export interface PublicSeoRoute {
  path: string;
  locale: Locale;
  alternatePath: string;
  xDefaultPath: string;
  canonical: string;
  title: string;
  description: string;
  heading: string;
  eyebrow: string;
  intro: string;
  benefits: string[];
  steps: string[];
  faq: SeoFaq[];
  breadcrumbs: SeoBreadcrumb[];
  image: string;
  schema: string[];
  lastModified: string;
  indexable: boolean;
}

export const publicSeoRoutes = seoRoutesData as PublicSeoRoute[];

const labels = {
  uz: {
    language: "English",
    languageLabel: "English",
    cta: "Bepul boshlash",
    benefits: "EnglishAI bilan qanday natija olasiz?",
    steps: "Qanday ishlaydi?",
    faq: "Ko‘p beriladigan savollar",
    explore: "Boshqa o‘rganish yo‘llari",
    nav: "SEO mavzulari",
    footer: "Ingliz tilini faol ishlatish uchun qurilgan AI platforma.",
  },
  en: {
    language: "O‘zbekcha",
    languageLabel: "O‘zbekcha",
    cta: "Start free",
    benefits: "What you will achieve with EnglishAI",
    steps: "How it works",
    faq: "Frequently asked questions",
    explore: "Explore other learning paths",
    nav: "Learning topics",
    footer: "An AI platform built to turn English knowledge into active communication.",
  },
} as const;

function absolute(path: string): string {
  return path === "/" ? `${ORIGIN}/` : `${ORIGIN}${path}`;
}

function setMeta(selector: string, attribute: string, value: string): void {
  const element = document.head.querySelector<HTMLMetaElement>(selector);
  if (element) element.setAttribute(attribute, value);
}

function setLink(selector: string, href: string): void {
  const element = document.head.querySelector<HTMLLinkElement>(selector);
  if (element) element.href = href;
}

export function buildPublicSeoGraph(route: PublicSeoRoute) {
  const pageId = `${route.canonical}#webpage`;
  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "Organization",
        "@id": ORGANIZATION_ID,
        name: "EnglishAI.uz",
        alternateName: ["EnglishAI", "English AI", "EnglishAIuz"],
        url: `${ORIGIN}/`,
        logo: { "@type": "ImageObject", url: `${ORIGIN}/assets/brand/icon-512.png`, width: 512, height: 512 },
        sameAs: [officialContacts.companyLinkedIn, officialContacts.telegram],
      },
      {
        "@type": "WebSite",
        "@id": WEBSITE_ID,
        url: `${ORIGIN}/`,
        name: "EnglishAI.uz",
        alternateName: ["EnglishAI", "English AI", "EnglishAIuz"],
        inLanguage: ["uz", "en"],
        publisher: { "@id": ORGANIZATION_ID },
      },
      {
        "@type": "SoftwareApplication",
        "@id": APP_ID,
        name: "EnglishAI.uz",
        applicationCategory: "EducationalApplication",
        operatingSystem: "Web, Android",
        url: `${ORIGIN}/`,
        image: `${ORIGIN}${route.image}`,
        offers: { "@type": "Offer", price: "0", priceCurrency: "UZS" },
        publisher: { "@id": ORGANIZATION_ID },
      },
      {
        "@type": "WebPage",
        "@id": pageId,
        url: route.canonical,
        name: route.title,
        headline: route.heading,
        description: route.description,
        inLanguage: route.locale,
        isPartOf: { "@id": WEBSITE_ID },
        about: { "@id": APP_ID },
        primaryImageOfPage: { "@type": "ImageObject", url: `${ORIGIN}${route.image}` },
      },
      ...(route.breadcrumbs.length > 1 ? [{
        "@type": "BreadcrumbList",
        "@id": `${route.canonical}#breadcrumb`,
        itemListElement: route.breadcrumbs.map((item, index) => ({
          "@type": "ListItem",
          position: index + 1,
          name: item.name,
          item: absolute(item.path),
        })),
      }] : []),
      {
        "@type": "FAQPage",
        "@id": `${route.canonical}#faq`,
        inLanguage: route.locale,
        isPartOf: { "@id": pageId },
        mainEntity: route.faq.map((item) => ({
          "@type": "Question",
          name: item.question,
          acceptedAnswer: { "@type": "Answer", text: item.answer },
        })),
      },
    ],
  };
}

export function applyPublicSeoHead(route: PublicSeoRoute): void {
  document.documentElement.lang = route.locale;
  document.title = route.title;
  setMeta('meta[name="description"]', "content", route.description);
  setMeta('meta[name="robots"]', "content", "index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1");
  setMeta('meta[property="og:title"]', "content", route.title);
  setMeta('meta[property="og:description"]', "content", route.description);
  setMeta('meta[property="og:url"]', "content", route.canonical);
  setMeta('meta[property="og:locale"]', "content", route.locale === "uz" ? "uz_UZ" : "en_US");
  setMeta('meta[name="twitter:title"]', "content", route.title);
  setMeta('meta[name="twitter:description"]', "content", route.description);
  setLink('link[rel="canonical"]', route.canonical);

  document.head.querySelectorAll('link[rel="alternate"][hreflang]').forEach((node) => node.remove());
  const alternate = publicSeoRoutes.find((item) => item.path === route.alternatePath)!;
  for (const [hreflang, href] of [[route.locale, route.canonical], [alternate.locale, alternate.canonical], ["x-default", absolute(route.xDefaultPath)]]) {
    const link = document.createElement("link");
    link.rel = "alternate";
    link.hreflang = hreflang;
    link.href = href;
    document.head.append(link);
  }

  const schema = document.querySelector<HTMLScriptElement>("#englishai-structured-data");
  if (schema) schema.textContent = JSON.stringify(buildPublicSeoGraph(route));
}

export function PublicSeoPage() {
  const { pathname } = useLocation();
  const normalizedPath = pathname !== "/" && pathname.endsWith("/") ? pathname.slice(0, -1) : pathname;
  const route = publicSeoRoutes.find((item) => item.path === normalizedPath || item.path === pathname) ?? publicSeoRoutes[0];
  const copy = labels[route.locale];
  const related = publicSeoRoutes.filter((item) => item.locale === route.locale && item.path !== route.path);

  useEffect(() => applyPublicSeoHead(route), [route]);

  return (
    <div className="pl-page seo-page">
      <header className="pl-header">
        <div className="pl-container pl-header__inner">
          <Link to={route.locale === "uz" ? "/" : "/en/"} className="pl-brand" aria-label="EnglishAI.uz">
            <ParrotLogo size={42} withWordmark className="pl-brand__logo" />
          </Link>
          <nav className="seo-header-nav" aria-label={copy.nav}>
            {related.slice(0, 4).map((item) => <Link key={item.path} to={item.path}>{item.heading.split(" ").slice(0, 3).join(" ")}</Link>)}
          </nav>
          <div className="pl-header__actions">
            <a className="seo-language" href={route.alternatePath} hrefLang={route.locale === "uz" ? "en" : "uz"}>{copy.languageLabel}</a>
            <Link to="/login" className="pl-button pl-button--primary pl-header__cta">{copy.cta}<Icon name="arrow_forward" /></Link>
          </div>
        </div>
      </header>

      <main>
        <section className="seo-hero pl-container">
          <div className="seo-hero__copy">
            <nav aria-label="Breadcrumb" className="seo-breadcrumbs">
              {route.breadcrumbs.map((item, index) => <span key={item.path}>{index > 0 && <Icon name="chevron_right" />}<Link to={item.path}>{item.name}</Link></span>)}
            </nav>
            <span className="pl-pill pl-pill--lime"><Icon name="bolt" filled />{route.eyebrow}</span>
            <h1>{route.heading}</h1>
            <p>{route.intro}</p>
            <div className="seo-hero__actions">
              <Link to="/login" className="pl-button pl-button--primary pl-button--large">{copy.cta}<Icon name="arrow_forward" /></Link>
              <a href="#qanday" className="pl-button pl-button--secondary pl-button--large">{copy.steps}<Icon name="expand_more" /></a>
            </div>
          </div>
          <div className="seo-product-card" aria-label="EnglishAI product preview">
            <div className="seo-product-card__top"><ParrotLogo size={68} imgSize={56} rounded="1.2rem" /><div><span>EnglishAI.uz</span><strong>{route.locale === "uz" ? "Bugungi faol mashq" : "Today’s active practice"}</strong></div></div>
            <div className="seo-product-card__mission"><Icon name="record_voice_over" /><div><small>AI tutor</small><strong>{route.heading}</strong></div><span>12 min</span></div>
            <div className="seo-product-card__skills">{["Vocabulary", "Grammar", "Reading", "Writing", "Speaking", "Listening"].map((skill, index) => <span key={skill} className={`seo-skill seo-skill--${index + 1}`}>{skill}</span>)}</div>
          </div>
        </section>

        <section className="seo-section seo-section--soft">
          <div className="pl-container">
            <div className="seo-section__heading"><span className="pl-eyebrow">EnglishAI.uz</span><h2>{copy.benefits}</h2></div>
            <div className="seo-card-grid">{route.benefits.map((benefit, index) => <article key={benefit} className={`seo-card seo-card--${index + 1}`}><span>{String(index + 1).padStart(2, "0")}</span><Icon name={["check_circle", "bolt", "trending_up"][index]} filled /><h3>{benefit}</h3></article>)}</div>
          </div>
        </section>

        <section id="qanday" className="seo-section pl-container">
          <div className="seo-section__heading"><span className="pl-eyebrow">01 → 03</span><h2>{copy.steps}</h2></div>
          <ol className="seo-steps">{route.steps.map((step, index) => <li key={step}><span>{index + 1}</span><div><small>{route.locale === "uz" ? `${index + 1}-bosqich` : `Step ${index + 1}`}</small><h3>{step}</h3></div></li>)}</ol>
        </section>

        <section className="seo-section seo-section--ink">
          <div className="pl-container">
            <div className="seo-section__heading"><span className="pl-eyebrow">EnglishAI Hub</span><h2>{copy.explore}</h2></div>
            <nav className="seo-link-grid" aria-label={copy.explore}>{related.map((item) => <Link key={item.path} to={item.path}><span>{item.eyebrow}</span><strong>{item.heading}</strong><Icon name="arrow_forward" /></Link>)}</nav>
          </div>
        </section>

        <section className="seo-section pl-container">
          <div className="seo-section__heading"><span className="pl-eyebrow">FAQ</span><h2>{copy.faq}</h2></div>
          <div className="pl-faq-list">{route.faq.map((item) => <details key={item.question} className="pl-faq"><summary>{item.question}<Icon name="expand_more" /></summary><p>{item.answer}</p></details>)}</div>
        </section>

        <section className="pl-closing pl-container">
          <div className="pl-closing__content"><span className="pl-pill pl-pill--lime">EnglishAI.uz</span><h2>{route.heading}</h2><p>{route.description}</p><Link to="/login" className="pl-button pl-button--primary pl-button--large">{copy.cta}<Icon name="arrow_forward" /></Link></div>
          <div className="pl-closing__mark"><ParrotLogo size={132} imgSize={112} rounded="2rem" /></div>
        </section>
      </main>

      <footer className="pl-footer"><div className="pl-container pl-footer__inner"><div><Link to={route.locale === "uz" ? "/" : "/en/"} className="pl-footer__brand"><ParrotLogo size={38} withWordmark /></Link><p>{copy.footer}</p></div><nav aria-label={copy.nav}>{related.map((item) => <Link key={item.path} to={item.path}>{item.heading.split(" ").slice(0, 2).join(" ")}</Link>)}</nav><p className="pl-footer__rights">© {new Date().getFullYear()} EnglishAI.uz</p></div></footer>
    </div>
  );
}
