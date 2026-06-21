import { useEffect, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "@/app/auth";
import { useDocumentTitle } from "@/app/documentTitle";
import { Icon } from "@/components/ui/Icon";
import { getPublicPage, type PublicPage } from "@/content/publicContent";
import { PublicSiteFooter, PublicSiteHeader } from "./PublicSiteChrome";
import { usePublicPageScroll } from "./usePublicPageScroll";
import "./PublicTrustLayout.css";

function editorialDate(value: string) {
  const [year, month, day] = value.split("-");
  const months = ["yanvar", "fevral", "mart", "aprel", "may", "iyun", "iyul", "avgust", "sentabr", "oktabr", "noyabr", "dekabr"];
  return `${Number(day)}-${months[Number(month) - 1]}, ${year}`;
}

export function PublicTrustLayout({ page, children }: { page: PublicPage; children: ReactNode }) {
  const { status } = useAuth();
  usePublicPageScroll();
  useDocumentTitle(page.title);

  useEffect(() => {
    document.documentElement.lang = "uz";
    const canonical = `https://englishai.uz${page.path}/`;
    const values: [string, string][] = [
      ['meta[name="description"]', page.description],
      ['meta[property="og:title"]', page.title],
      ['meta[property="og:description"]', page.description],
      ['meta[property="og:url"]', canonical],
      ['meta[property="og:locale"]', "uz_UZ"],
      ['meta[name="twitter:title"]', page.title],
      ['meta[name="twitter:description"]', page.description],
    ];
    values.forEach(([selector, content]) => document.head.querySelector(selector)?.setAttribute("content", content));
    document.head.querySelector('link[rel="canonical"]')?.setAttribute("href", canonical);
    document.head.querySelectorAll('link[rel="alternate"][hreflang]').forEach((node) => node.remove());
    for (const language of ["uz", "x-default"]) {
      const link = document.createElement("link");
      link.rel = "alternate";
      link.hreflang = language;
      link.href = canonical;
      document.head.append(link);
    }
    const schema = document.querySelector("#englishai-structured-data");
    if (schema) schema.textContent = JSON.stringify({
      "@context": "https://schema.org",
      "@graph": [
        { "@type": "WebPage", "@id": `${canonical}#webpage`, url: canonical, name: page.title, description: page.description, inLanguage: "uz", datePublished: page.publishedAt, dateModified: page.reviewedAt },
        { "@type": "FAQPage", "@id": `${canonical}#faq`, mainEntity: page.faq.map(({ question, answer }) => ({ "@type": "Question", name: question, acceptedAnswer: { "@type": "Answer", text: answer } })) },
      ],
    });
  }, [page]);

  return (
    <div className={`pt-page pt-page--${page.slug}`} lang="uz">
      <a className="pt-skip" href="#pt-main">Asosiy mazmunga o‘tish</a>
      <PublicSiteHeader authenticated={status === "authenticated"} />
      <main className="pt-main" id="pt-main">
        <nav className="pt-breadcrumb" aria-label="Sahifa yo‘li">
          <Link to="/">Bosh sahifa</Link><Icon name="chevron_right" className="text-[14px]" />
          <Link to="/learn">Qo‘llanmalar</Link><Icon name="chevron_right" className="text-[14px]" />
          <span aria-current="page">{page.path === "/contact" ? "Aloqa" : page.path === "/methodology" ? "Metodologiya" : "Tahririy siyosat"}</span>
        </nav>
        {children}
      </main>
      <PublicSiteFooter />
    </div>
  );
}

export function TrustByline({ page }: { page: PublicPage }) {
  return (
    <div className="pt-byline">
      <span><Icon name="edit_note" className="text-[18px]" />{page.author}</span>
      <span><Icon name="verified_user" className="text-[18px]" />{page.reviewer}</span>
      <span><Icon name="calendar_month" className="text-[18px]" /><time dateTime={page.reviewedAt}>Ko‘rib chiqildi: {editorialDate(page.reviewedAt)}</time></span>
    </div>
  );
}

export function TrustFaq({ page }: { page: PublicPage }) {
  return (
    <section className="pt-faq" id="savollar" aria-labelledby="pt-faq-title">
      <div>
        <span className="pt-icon pt-icon--lavender"><Icon name="chat_bubble" /></span>
        <p className="pt-eyebrow">SAVOL-JAVOB</p>
        <h2 id="pt-faq-title">Aniqlik kiritamiz.</h2>
        <p>Ko‘p beriladigan savollarga qisqa va ochiq javoblar.</p>
        {page.path !== "/contact" && <Link className="pt-text-link" to="/contact">Boshqa savolingiz bormi? <Icon name="arrow_forward" className="text-[18px]" /></Link>}
      </div>
      <div className="pt-faq-list">
        {page.faq.map(({ question, answer }) => (
          <details key={question}>
            <summary>{question}<Icon name="expand_more" className="text-[20px]" /></summary>
            <p>{answer}</p>
          </details>
        ))}
      </div>
    </section>
  );
}

export function TrustSources({ page }: { page: PublicPage }) {
  return (
    <section className="pt-sources" id="manbalar" aria-labelledby="pt-sources-title">
      <div className="pt-section-heading"><div><p className="pt-eyebrow">TEKSHIRIB KO‘RING</p><h2 id="pt-sources-title">Tayanadigan manbalarimiz</h2></div><span>Ochiq. Birlamchi. Tekshiriladigan.</span></div>
      <div className="pt-source-grid">
        {page.sources.map((source) => (
          <a key={source.url} className="pt-source" href={source.url} target={source.url.startsWith("http") ? "_blank" : undefined} rel="noreferrer">
            <Icon name="menu_book" className="text-[22px]" />
            <div><strong>{source.label}</strong><span>{source.url.startsWith("http") ? new URL(source.url).hostname.replace(/^www\./, "") : "englishai.uz"}</span></div>
            <Icon name="open_in_new" className="text-[16px]" />
          </a>
        ))}
      </div>
    </section>
  );
}

export function TrustRelated({ page }: { page: PublicPage }) {
  return (
    <nav className="pt-related" aria-label="Tegishli qo‘llanmalar">
      <span>Yana tanishing</span>
      {page.relatedPaths.map((path) => {
        const related = getPublicPage(path);
        return related ? <Link key={path} to={path}>{related.title}<Icon name="arrow_forward" className="text-[16px]" /></Link> : null;
      })}
    </nav>
  );
}

export function TrustCta({ eyebrow, title, text, href, label }: { eyebrow: string; title: ReactNode; text: string; href: string; label: string }) {
  return (
    <section className="pt-cta">
      <div><p className="pt-eyebrow">{eyebrow}</p><h2>{title}</h2><p>{text}</p></div>
      <Link to={href} className="ea-button ea-button--lg ea-button--primary">{label}<Icon name="arrow_forward" className="text-[20px]" /></Link>
    </section>
  );
}
