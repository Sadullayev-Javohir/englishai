import { useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api } from "@/api/client";
import { useAuth } from "@/app/auth";
import { AppButton, DesignInput, DesignSelect, DesignTextarea } from "@/components/design";
import { Icon } from "@/components/ui/Icon";
import { LinkedInIcon } from "@/components/LinkedInIcon";
import { PublicTrustLayout, TrustFaq, TrustRelated } from "@/components/public/PublicTrustLayout";
import { officialContacts } from "@/content/officialContacts";
import type { PublicPage } from "@/content/publicContent";

const topics = [
  { value: "product", label: "Texnik yordam", hint: "Sahifa manzili, qurilma, kutilgan natija va xatoni takrorlash qadamlarini yozing." },
  { value: "content", label: "Kontentdagi xato", hint: "Sahifa manzili, xato jumla va taklif qilayotgan tuzatishingizni yozing. Manba bo‘lsa, qo‘shing." },
  { value: "billing", label: "Obuna va to‘lov", hint: "Reja nomi va savolingizni yozing. Karta raqami, CVV yoki parolni yubormang." },
  { value: "partnership", label: "Hamkorlik taklifi", hint: "Taklifingiz, tashkilotingiz va hamkorlik maqsadini qisqacha yozing." },
  { value: "other", label: "Boshqa savol", hint: "Nima haqida bilmoqchisiz? Savolingizni tushunarli va aniq yozing." },
];

function ContactForm() {
  const { status } = useAuth();
  const [params] = useSearchParams();
  const requestedTopic = params.get("topic");
  const [topic, setTopic] = useState(topics.find((item) => item.value === requestedTopic)?.value ?? "product");
  const [subject, setSubject] = useState("");
  const [message, setMessage] = useState("");
  const [sending, setSending] = useState(false);
  const [sent, setSent] = useState(false);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState("");
  const authenticated = status === "authenticated";
  const selectedTopic = topics.find((item) => item.value === topic)!;
  const draft = `[${selectedTopic.label}] ${subject.trim()}\n\n${message.trim()}`;

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (sending) return;
    setError("");
    setCopied(false);
    if (!subject.trim() || !message.trim()) {
      setError("Mavzu va xabar matnini kiriting.");
      return;
    }
    if (!authenticated) {
      try {
        await navigator.clipboard.writeText(draft);
        setCopied(true);
      } catch {
        setError("Nusxalab bo‘lmadi. Matnni qo‘lda nusxalang va yordam chatiga joylang.");
      }
      return;
    }
    setSending(true);
    try {
      await api.support.send(draft, []);
      setSent(true);
    } catch {
      setError("Xabar yuborilmadi. Matningiz saqlandi. Qayta urinib ko‘ring yoki yordam chatini oching.");
    } finally {
      setSending(false);
    }
  }

  return (
    <section className="cp-form-card" id="form" aria-labelledby="cp-form-title">
      {sent ? (
        <div className="cp-sent" role="status">
          <span className="pt-icon pt-icon--green"><Icon name="check_circle" /></span>
          <h2 id="cp-form-title">Xabaringiz yuborildi.</h2>
          <p>Javobni ilova ichidagi yordam chatida kuzatishingiz mumkin.</p>
          <Link to="/support" className="ea-button ea-button--md ea-button--primary">Yordam chatini ochish<Icon name="arrow_forward" className="text-[18px]" /></Link>
          <AppButton tone="standard" onClick={() => { setSent(false); setSubject(""); setMessage(""); }}>Yangi murojaat</AppButton>
        </div>
      ) : (
        <>
          <div className="cp-form-heading"><span className="pt-icon pt-icon--lavender"><Icon name="edit_note" /></span><div><h2 id="cp-form-title">Keling, gaplashamiz.</h2><p>{authenticated ? "Xabaringiz yordam chatiga yuboriladi." : "Xabaringizni tayyorlang, so‘ng yordam chatiga o‘ting."}</p></div></div>
          <form onSubmit={(event) => void submit(event)}>
            <label htmlFor="contact-topic">Murojaat turi</label>
            <DesignSelect id="contact-topic" value={topic} disabled={sending} onChange={(event) => { setTopic(event.target.value); setCopied(false); }}>
              {topics.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </DesignSelect>
            <label htmlFor="contact-subject">Mavzu <span aria-hidden="true">*</span></label>
            <DesignInput id="contact-subject" required maxLength={120} placeholder="Masalan, mashq natijasi saqlanmadi" value={subject} disabled={sending} onChange={(event) => { setSubject(event.target.value); setCopied(false); }} />
            <label htmlFor="contact-message">Xabaringiz <span aria-hidden="true">*</span></label>
            <DesignTextarea id="contact-message" required maxLength={2000} rows={5} placeholder="Batafsil yozing. Sizni tushunishimiz osonroq bo‘lsin." value={message} disabled={sending} aria-describedby="contact-hint contact-length" onChange={(event) => { setMessage(event.target.value); setCopied(false); }} />
            <div className="cp-message-meta"><p id="contact-hint">{selectedTopic.hint}</p><span id="contact-length">{message.length} / 2000</span></div>
            <div className="cp-privacy"><Icon name="lock" className="text-[16px]" /><p>Parol, karta rekvizitlari yoki boshqa maxfiy ma’lumotlarni yozmang.</p></div>
            {error && <p className="cp-error" role="alert">{error}</p>}
            {copied && <p className="cp-copy-status" role="status">Matn nusxalandi. Hali yuborilmadi — tizimga kirib, yordam chatiga joylang.</p>}
            <AppButton type="submit" fullWidth loading={sending} leadingIcon={authenticated ? "send" : copied ? "check" : "content_copy"}>{sending ? "Yuborilmoqda…" : authenticated ? "Murojaatni yuborish" : copied ? "Yana nusxalash" : "Xabar matnini nusxalash"}</AppButton>
            {!authenticated && <Link to="/support" className="cp-login-link">Kirish va yordam chatini ochish<Icon name="arrow_forward" className="text-[16px]" /></Link>}
            <p className="cp-form-note">{authenticated ? "Javob muddati murojaat murakkabligi va navbatga bog‘liq." : "Yuborish uchun EnglishAI hisobingizga kirish kerak. Avval matnni nusxalab oling."}</p>
          </form>
        </>
      )}
    </section>
  );
}

export function ContactPage({ page }: { page: PublicPage }) {
  return (
    <PublicTrustLayout page={page}>
      <div className="cp-main-grid">
        <div className="cp-intro">
          <p className="pt-eyebrow">ENGLISH AI · ALOQA</p>
          <h1>Savolingiz bormi?<br /><em>Biz shu yerdamiz.</em></h1>
          <p className="pt-lead">Yordam kerakmi, fikringiz bormi yoki hamkorlik qilmoqchimisiz? Sizni tinglaymiz.</p>
          <div className="cp-channels" aria-label="Rasmiy aloqa kanallari">
            <Link className="cp-channel cp-channel--primary" to="/support">
              <span className="pt-icon pt-icon--lavender"><Icon name="support_agent" /></span>
              <div><strong>Ilova ichidagi yordam</strong><span>Savollar va javoblar bir suhbatda</span></div>
              <Icon name="arrow_forward" className="text-[18px]" />
            </Link>
            <a className="cp-channel" href={officialContacts.telegram} target="_blank" rel="noreferrer">
              <span className="pt-icon pt-icon--green"><Icon name="send" /></span>
              <div><strong>Rasmiy Telegram</strong><span>@englishaiuz · yangiliklar va aloqa</span></div>
              <Icon name="open_in_new" className="text-[18px]" />
            </a>
            <a className="cp-channel" href={officialContacts.companyLinkedIn} target="_blank" rel="noreferrer">
              <span className="pt-icon pt-icon--peach"><LinkedInIcon /></span>
              <div><strong>Hamkorlik uchun</strong><span>EnglishAI.uz · LinkedIn</span></div>
              <Icon name="open_in_new" className="text-[18px]" />
            </a>
          </div>
          <div className="cp-mascot-note"><img src="/assets/play/mascot.svg" width="70" height="70" alt="" /><p>Yaxshi savol —<br /><strong>yaxshi o‘zgarishning boshlanishi.</strong></p></div>
        </div>
        <ContactForm />
      </div>
      <section className="cp-help" aria-labelledby="cp-help-title">
        <div className="pt-section-heading"><div><p className="pt-eyebrow">YOZISHDAN OLDIN</p><h2 id="cp-help-title">Bir oz tafsilot. Aniqroq yordam.</h2></div><p>{page.summary}</p></div>
        <div className="cp-help-grid">
          {[{ icon: "bug_report", title: "Texnik muammo", text: page.sections[0].paragraphs[0] }, { icon: "edit_note", title: "Kontentga tuzatish", text: page.sections[0].paragraphs[1] }, { icon: "shield", title: "Maxfiylikni asrang", text: page.sections[1].paragraphs[0] }].map((item) => (
            <article key={item.title}><Icon name={item.icon} className="text-[24px]" /><h3>{item.title}</h3><p>{item.text}</p></article>
          ))}
        </div>
      </section>
      <TrustFaq page={page} />
      <TrustRelated page={page} />
    </PublicTrustLayout>
  );
}
