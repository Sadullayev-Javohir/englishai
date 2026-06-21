import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import { getPublicPage, type PublicPage } from "@/content/publicContent";
import { ContactPage } from "./ContactPage";
import { EditorialPolicyPage } from "./EditorialPolicyPage";
import { MethodologyPage } from "./MethodologyPage";
import "./PublicContentPage.css";

function PublicHeader() {
  return (
    <header className="pc-header">
      <Link className="pc-brand" to="/" aria-label="EnglishAI.uz bosh sahifasi">
        <EnglishAiLogo size={42} withWordmark />
      </Link>
      <nav aria-label="Ommaviy sahifalar">
        <Link to="/learn">Qo‘llanmalar</Link>
        <Link to="/methodology">Metodika</Link>
        <Link to="/product">Mahsulot</Link>
        <Link to="/pricing">Narxlar</Link>
      </nav>
      <Link className="pc-header-cta" to="/login">Bepul boshlash</Link>
    </header>
  );
}

function PageCard({ page }: { page: PublicPage }) {
  return (
    <article className="pc-card">
      <p className="pc-eyebrow">{page.intentCluster}</p>
      <h2><Link to={page.path}>{page.title}</Link></h2>
      <p>{page.summary}</p>
      <Link className="pc-text-link" to={page.path}>Batafsil o‘qish <span aria-hidden="true">→</span></Link>
    </article>
  );
}

export function PublicContentPage() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const page = getPublicPage(pathname.replace(/\/$/, "") || "/");

  if (!page) return <Navigate to="/not-found" replace />;
  if (page.path === "/editorial-policy") return <EditorialPolicyPage page={page} />;
  if (page.path === "/methodology") return <MethodologyPage page={page} />;
  if (page.path === "/contact") return <ContactPage page={page} />;

  const related = page.relatedPaths
    .map((path) => getPublicPage(path))
    .filter((item): item is PublicPage => Boolean(item));

  return (
    <div className="pc-page">
      <PublicHeader />
      <main>
        <nav className="pc-breadcrumb" aria-label="Breadcrumb">
          <Link to="/">EnglishAI.uz</Link><span aria-hidden="true">/</span>
          {page.path !== "/learn" && <><Link to="/learn">Qo‘llanmalar</Link><span aria-hidden="true">/</span></>}
          <span aria-current="page">{page.kind === "hub" ? "Qo‘llanmalar" : page.title}</span>
        </nav>

        <article className="pc-article">
          <header className="pc-hero">
            <p className="pc-eyebrow">{page.intentCluster}</p>
            <h1>{page.title}</h1>
            <p className="pc-lead">{page.summary}</p>
            <div className="pc-byline">
              <span>Muallif: {page.author}</span>
              <span>Tekshiruvchi: {page.reviewer}</span>
              <time dateTime={page.reviewedAt}>Tekshirildi: {page.reviewedAt}</time>
            </div>
            <button type="button" className="pc-primary-cta" onClick={() => navigate(page.cta.href)}>
              {page.cta.label}
            </button>
          </header>

          <aside className="pc-direct-answer" aria-label="Qisqa javob">
            <strong>Qisqa javob</strong>
            <p>{page.description}</p>
          </aside>

          {page.kind === "hub" ? (
            <section className="pc-grid" aria-label="EnglishAI qo‘llanmalari">
              {related.map((item) => <PageCard key={item.path} page={item} />)}
            </section>
          ) : (
            <div className="pc-body">
              {page.sections.map((section) => (
                <section key={section.heading}>
                  <h2>{section.heading}</h2>
                  {section.paragraphs.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}
                  {section.steps?.length ? <ol>{section.steps.map((step) => <li key={step}>{step}</li>)}</ol> : null}
                  {section.example ? <div className="pc-example"><strong>Amaliy namuna</strong><p>{section.example}</p></div> : null}
                </section>
              ))}
            </div>
          )}

          {page.faq.length > 0 && (
            <section className="pc-faq" aria-labelledby="pc-faq-title">
              <p className="pc-eyebrow">Savol-javob</p>
              <h2 id="pc-faq-title">Ko‘p beriladigan savollar</h2>
              {page.faq.map((item) => (
                <details key={item.question}>
                  <summary>{item.question}</summary>
                  <p>{item.answer}</p>
                </details>
              ))}
            </section>
          )}

          <section className="pc-sources" aria-labelledby="pc-sources-title">
            <h2 id="pc-sources-title">Manbalar</h2>
            <ul>{page.sources.map((source) => <li key={source.url}><a href={source.url} rel="noreferrer">{source.label}</a></li>)}</ul>
          </section>

          {page.kind !== "hub" && related.length > 0 && (
            <section aria-labelledby="pc-related-title">
              <p className="pc-eyebrow">Keyingi qadam</p>
              <h2 id="pc-related-title">Tegishli qo‘llanmalar</h2>
              <div className="pc-grid">{related.map((item) => <PageCard key={item.path} page={item} />)}</div>
            </section>
          )}
        </article>
      </main>
      <footer className="pc-footer">
        <div><EnglishAiLogo size={32} withWordmark /> — o‘zbeklar uchun ingliz tili platformasi.</div>
        <nav aria-label="Tashkilot ma’lumotlari">
          <Link to="/about">Biz haqimizda</Link><Link to="/editorial-policy">Tahrir siyosati</Link><Link to="/contact">Aloqa</Link>
        </nav>
      </footer>
    </div>
  );
}
