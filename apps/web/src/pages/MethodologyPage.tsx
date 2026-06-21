import { useState } from "react";
import { Link } from "react-router-dom";
import { Icon } from "@/components/ui/Icon";
import { PublicTrustLayout, TrustByline, TrustCta, TrustFaq, TrustRelated, TrustSources } from "@/components/public/PublicTrustLayout";
import type { PublicPage } from "@/content/publicContent";

const skills = [
  { label: "Vocabulary", icon: "style", title: "So‘z bilan tanishing.", text: "Yangi so‘zni ma’nosi bilan bog‘lang, keyin uni eslab, o‘zingiz ayting.", example: "ticket · departure · destination", note: "Bir mavzuning tayanch so‘zlari" },
  { label: "Grammar", icon: "rule", title: "So‘zlarni gapga aylantiring.", text: "Qoidani alohida yodlash bilan cheklanmang. Uni shu mavzuda qo‘llab ko‘ring.", example: "I would like a ticket to London.", note: "Muloyim so‘rovni ifodalash" },
  { label: "Reading", icon: "auto_stories", title: "Tanish so‘zni matnda toping.", text: "Mavzuga oid matnni o‘qing. So‘zlar kontekstda qanday ishlashini kuzating.", example: "Your train departs from platform four.", note: "Sayohat e’lonini tushunish" },
  { label: "Writing", icon: "edit", title: "Fikringizni yozing.", text: "Yangi so‘z va qolip bilan mustaqil matn tuzing, so‘ng qayta ko‘rib chiqing.", example: "Hi! I’m arriving at six. Can you meet me?", note: "Do‘stingizga qisqa xabar" },
  { label: "Speaking", icon: "record_voice_over", title: "Suhbatda sinab ko‘ring.", text: "AI tutor bilan vaziyatni mashq qiling. Xato — yana urinib ko‘rish uchun yo‘l-yo‘riq.", example: "Could you help me find my platform?", note: "Vokzalda yo‘l so‘rash" },
  { label: "Listening", icon: "headphones", title: "Eshiting va tushuning.", text: "Tanish iboralarni tinglashda ajrating, ma’nosini kontekst bilan tekshiring.", example: "The next train to London leaves at nine.", note: "Tinglab, kerakli ma’lumotni ajratish" },
];

export function MethodologyPage({ page }: { page: PublicPage }) {
  const [selectedSkill, setSelectedSkill] = useState(0);
  const skill = skills[selectedSkill];
  return (
    <PublicTrustLayout page={page}>
      <header className="pt-hero mp-hero">
        <div>
          <p className="pt-eyebrow">ENGLISH AI · METODOLOGIYA</p>
          <h1>Yodlashdan<br /><em>qo‘llashgacha.</em></h1>
          <p className="pt-lead">{page.summary}</p>
          <Link className="ea-button ea-button--lg ea-button--primary" to={page.cta.href}>{page.cta.label}<Icon name="arrow_forward" className="text-[20px]" /></Link>
          <span className="mp-hero-note">O‘z darajangiz. O‘z sur’atingiz. Har kuni bir qadam.</span>
        </div>
        <div className="mp-hero-art">
          <img src="/assets/play/words-world.svg" width="360" height="400" alt="Kitobdan ochilayotgan so‘zlar va yangi dunyo" />
          <span className="mp-art-label"><Icon name="auto_awesome" className="text-[18px]" />Bir mavzu. Bir-biriga bog‘langan bilim.</span>
        </div>
      </header>
      <div className="mp-principles" aria-label="Metodologiya asoslari">
        <div><span className="pt-icon pt-icon--lavender"><Icon name="route" /></span><div><strong>A1 dan C2 gacha</strong><p>Darajangizga mos boshlanish</p></div></div>
        <div><span className="pt-icon pt-icon--peach"><Icon name="hub" /></span><div><strong>6 ta bog‘langan ko‘nikma</strong><p>Bir mavzuni turli yo‘llarda qo‘llash</p></div></div>
        <div><span className="pt-icon pt-icon--green"><Icon name="event_repeat" /></span><div><strong>3 / 7 / 21 takrorlash</strong><p>Bilimni faol eslab mustahkamlash</p></div></div>
      </div>
      <section className="mp-path pt-section" aria-labelledby="mp-path-title">
        <div className="pt-section-heading">
          <div><p className="pt-eyebrow">O‘QUV JARAYONI</p><h2 id="mp-path-title">{page.sections[0].heading}</h2></div>
          <p>Har bir mashq — keyingi suhbatga tayyorgarlik.</p>
        </div>
        <ol className="mp-process">
          {page.sections[0].steps?.map((step, index) => <li key={step}><span>{String(index + 1).padStart(2, "0")}</span><strong>{step === "Feedback" ? "Fikr va tuzatish" : step}</strong>{index < 4 && <Icon name="arrow_forward" className="text-[18px]" />}</li>)}
        </ol>
        <div className="mp-prose-grid">{page.sections[0].paragraphs.map((text) => <p key={text}>{text}</p>)}</div>
      </section>
      <section className="mp-skills" aria-labelledby="mp-skills-title">
        <div className="pt-section-heading">
          <div><p className="pt-eyebrow">BILIMNI BIRLASHTIRAMIZ</p><h2 id="mp-skills-title">{page.sections[1].heading}</h2></div>
          <span className="pt-tag">Misol mavzu: sayohat</span>
        </div>
        <p className="mp-skills-intro">{page.sections[1].paragraphs[0]}</p>
        <div className="mp-skill-buttons" aria-label="Ko‘nikma namunasini tanlang">
          {skills.map((item, index) => <button type="button" key={item.label} aria-pressed={selectedSkill === index} aria-controls="mp-skill-example" onClick={() => setSelectedSkill(index)}><Icon name={item.icon} className="text-[20px]" />{item.label}</button>)}
        </div>
        <div className="mp-skill-example" id="mp-skill-example" aria-live="polite">
          <div><span className="pt-eyebrow">{skill.label}</span><h3>{skill.title}</h3><p>{skill.text}</p></div>
          <div className="mp-example-quote"><span>AMALIY NAMUNA</span><p lang="en">{skill.example}</p><small>{skill.note}</small></div>
        </div>
        <div className="pt-note"><Icon name="info" className="text-[20px]" /><p>{page.sections[1].paragraphs[1]}</p></div>
      </section>
      <section className="mp-repetition pt-section" aria-labelledby="mp-repeat-title">
        <div><p className="pt-eyebrow">ESLASH — O‘RGANISHNING BIR QISMI</p><h2 id="mp-repeat-title">{page.sections[2].heading}</h2>{page.sections[2].paragraphs.map((text) => <p key={text}>{text}</p>)}</div>
        <div className="mp-review-card">
          <span className="pt-tag"><Icon name="event_repeat" className="text-[16px]" />Faol takrorlash</span>
          <div className="mp-review-days">{[3, 7, 21].map((day) => <div key={day}><strong>{day}</strong><span>kun</span></div>)}</div>
          <p>O‘qib chiqish emas.<br /><strong>Javobni mustaqil eslash.</strong></p>
          <span className="mp-review-caption">So‘z o‘rganilgan kundan boshlab</span>
        </div>
      </section>
      <TrustByline page={page} />
      <TrustFaq page={page} />
      <TrustSources page={page} />
      <TrustCta eyebrow="BIRINCHI QADAM SIZDAN" title={<>Usul tushunarli.<br />Endi sinab ko‘ring.</>} text="Darajangizni aniqlang va o‘zingizga mos mashqdan boshlang." href={page.cta.href} label={page.cta.label} />
      <TrustRelated page={page} />
    </PublicTrustLayout>
  );
}
