import { Link } from "react-router-dom";
import { Icon } from "@/components/ui/Icon";
import { PublicTrustLayout, TrustByline, TrustCta, TrustFaq, TrustRelated, TrustSources } from "@/components/public/PublicTrustLayout";
import type { PublicPage } from "@/content/publicContent";

const sectionPresentation = [
  { id: "yozish", icon: "edit_note", tone: "lavender" },
  { id: "manba-va-ai", icon: "fact_check", tone: "green" },
  { id: "tuzatish", icon: "history", tone: "peach" },
  { id: "tijoriy-aniqlik", icon: "verified_user", tone: "lavender" },
];

export function EditorialPolicyPage({ page }: { page: PublicPage }) {
  return (
    <PublicTrustLayout page={page}>
      <header className="pt-hero">
        <div>
          <p className="pt-eyebrow">ENGLISH AI · TAHRIRIY SIYOSAT</p>
          <h1>Ishonchli bilim.<br /><em>Ochiq tamoyillar.</em></h1>
          <p className="pt-lead">{page.summary}</p>
          <a className="pt-text-link" href="#yozish">Tamoyillarimiz bilan tanishing<Icon name="arrow_downward" className="text-[18px]" /></a>
        </div>
        <div className="ep-promise">
          <div className="ep-promise-top"><span className="pt-tag"><Icon name="verified" className="text-[16px]" />Ishonch, avvalo.</span><img src="/assets/play/mascot.svg" width="94" height="94" alt="" /></div>
          <h2>Har bir material<br />uchun bir mezon.</h2>
          <ul>
            <li><Icon name="check_circle" className="text-[20px]" />Tushunarli va amaliy</li>
            <li><Icon name="check_circle" className="text-[20px]" />Ishonchli manbaga tayangan</li>
            <li><Icon name="check_circle" className="text-[20px]" />Inson ko‘rib chiqqan</li>
          </ul>
          <p>AI yordam beradi. Mas’uliyat bizda.</p>
        </div>
      </header>
      <TrustByline page={page} />
      <div className="ep-reading-layout">
        <aside className="ep-sidebar">
          <nav aria-label="Sahifa bo‘limlari">
            <p className="pt-eyebrow">USHBU SAHIFADA</p>
            {page.sections.map((section, index) => <a key={section.heading} href={`#${sectionPresentation[index].id}`}><Icon name={sectionPresentation[index].icon} className="text-[18px]" />{section.heading}</a>)}
            <a href="#savollar"><Icon name="help" className="text-[18px]" />Savol-javob</a>
            <a href="#manbalar"><Icon name="menu_book" className="text-[18px]" />Manbalar</a>
          </nav>
          <div className="ep-sidebar-note"><Icon name="chat_bubble" className="text-[22px]" /><strong>Xatoni payqadingizmi?</strong><p>Birga yanada yaxshiroq qilamiz.</p><Link className="pt-text-link" to="/contact?topic=content#form">Bizga yozing<Icon name="arrow_forward" className="text-[16px]" /></Link></div>
        </aside>
        <article className="ep-article" aria-label={page.title}>
          {page.sections.map((section, index) => (
            <section key={section.heading} id={sectionPresentation[index].id} className="ep-section">
              <span className={`pt-icon pt-icon--${sectionPresentation[index].tone}`}><Icon name={sectionPresentation[index].icon} /></span>
              <h2>{section.heading}</h2>
              {section.paragraphs.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}
              {index === 1 && <div className="pt-note"><Icon name="lightbulb" className="text-[20px]" /><p>AI — yordamchi. Dalil va manbaning o‘rnini bosmaydi.</p></div>}
            </section>
          ))}
        </article>
      </div>
      <TrustFaq page={page} />
      <TrustSources page={page} />
      <TrustCta eyebrow="SIZNING FIKRINGIZ MUHIM" title={<>Yaxshi kontent.<br />Birgalikdagi mas’uliyat.</>} text="Xato jumla, sahifa manzili va asoslovchi manbani yuboring." href="/contact?topic=content#form" label={page.cta.label} />
      <TrustRelated page={page} />
    </PublicTrustLayout>
  );
}
