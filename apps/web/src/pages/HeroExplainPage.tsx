import { Suspense, lazy } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/app/auth";
import { ParrotLogo } from "@/components/ParrotLogo";
import { DuoButton } from "@/components/game/DuoButton";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import "./HeroExplainPage.css";

const ThreeCanvas = lazy(() =>
  import("@/components/hero/ThreeCanvas").then((module) => ({ default: module.ThreeCanvas })),
);

const t = uz.heroCampaign;
type Accent = "green" | "blue" | "purple" | "amber" | "teal";
const PROOF_ACCENTS: Accent[] = ["green", "blue", "purple", "amber"];
const REASON_ACCENTS: Accent[] = ["green", "purple", "teal"];

export function HeroExplainPage() {
  const navigate = useNavigate();
  const { status } = useAuth();
  const reduceMotion = useReducedMotion();
  const signedIn = status === "authenticated";
  const primaryLabel = signedIn ? t.continue : t.start;
  const enterApp = () => navigate(signedIn ? "/home" : "/login");

  return (
    <div className="hero-explain">
      <div className="hero-explain__scene" aria-hidden>
        <Suspense fallback={null}>
          <ThreeCanvas scene="orbit" />
        </Suspense>
      </div>
      <div className="hero-explain__veil" aria-hidden />

      <header className="hero-explain__header">
        <div className="hero-explain__header-inner">
          <button type="button" onClick={() => navigate("/")} aria-label={t.homeAria} className="hero-explain__brand">
            <ParrotLogo size={36} withWordmark />
          </button>
          <button type="button" onClick={enterApp} className="hero-explain__header-cta">
            {primaryLabel}
            <Icon name="arrow_forward" className="hero-explain__button-icon" />
          </button>
        </div>
      </header>

      <main className="hero-explain__main">
        <section className="hero-explain__hero">
          <div className="hero-explain__hero-grid">
            <motion.div
              initial={reduceMotion ? false : { opacity: 0, y: 24 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.55 }}
              className="hero-explain__copy"
            >
              <span className="hero-explain__eyebrow">
                <Icon name="record_voice_over" className="hero-explain__eyebrow-icon" />
                {t.eyebrow}
              </span>
              <h1 className="hero-explain__title">
                {t.titlePrefix} <span>{t.titleAccent}</span> {t.titleSuffix}
              </h1>
              <p className="hero-explain__lead">{t.lead}</p>
              <div className="hero-explain__actions">
                <DuoButton color="green" size="lg" icon="arrow_forward" iconTrailing onClick={enterApp}>
                  {primaryLabel}
                </DuoButton>
                <a href="#why" className="hero-explain__secondary-cta">
                  {t.secondary}
                  <Icon name="arrow_downward" className="hero-explain__button-icon" />
                </a>
              </div>
            </motion.div>

            <motion.div
              initial={reduceMotion ? false : { opacity: 0, y: 28, scale: 0.97 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              transition={{ duration: 0.6, delay: reduceMotion ? 0 : 0.12 }}
              className="hero-explain__demo-card"
            >
              <div className="hero-explain__demo-topline">
                <span className="hero-explain__live-dot" />
                AI tutor bilan jonli mashq
              </div>
              <div className="hero-explain__message hero-explain__message--tutor">
                <span className="hero-explain__avatar"><Icon name="smart_toy" /></span>
                <div><strong>AI tutor</strong><p>Tell me about your plans for this weekend.</p></div>
              </div>
              <div className="hero-explain__message hero-explain__message--learner">
                <div><strong>Siz</strong><p>I am going to visit my friends and practise English.</p></div>
                <span className="hero-explain__avatar hero-explain__avatar--learner"><Icon name="person" /></span>
              </div>
              <div className="hero-explain__feedback">
                <Icon name="verified" />
                <div><strong>Ajoyib javob</strong><p>Gap tuzilishi to'g'ri. Endi “practise” talaffuzini yana bir marta ayting.</p></div>
              </div>
              <div className="hero-explain__mini-stats">
                {[
                  { icon: "graphic_eq", value: "92%", label: "talaffuz aniqligi" },
                  { icon: "trending_up", value: "B1", label: "moslashuvchan daraja" },
                  { icon: "local_fire_department", value: "7 kun", label: "faol mashq seriyasi" },
                ].map((stat, index) => (
                  <MiniStat key={stat.label} {...stat} accent={PROOF_ACCENTS[index]} />
                ))}
              </div>
            </motion.div>
          </div>

          <ul className="hero-explain__proof-grid" aria-label={t.proofAria}>
            {t.proofs.map((proof, index) => (
              <li key={proof.title} className={`hero-explain__proof hero-explain--${PROOF_ACCENTS[index]}`}>
                <Icon name={proof.icon} />
                <h2>{proof.title}</h2>
                <p>{proof.text}</p>
              </li>
            ))}
          </ul>
        </section>

        <section id="why" className="hero-explain__why">
          <div className="hero-explain__container">
            <div className="hero-explain__section-heading">
              <span>{t.whyEyebrow}</span>
              <h2>{t.whyTitle}</h2>
              <p>{t.whyLead}</p>
            </div>
            <div className="hero-explain__reason-grid">
              {t.reasons.map((reason, index) => (
                <ReasonCard key={reason.title} {...reason} accent={REASON_ACCENTS[index]} />
              ))}
            </div>
            <div className="hero-explain__stats">
              {t.stats.map((stat, index) => (
                <div key={stat.label} className={`hero-explain__stat hero-explain--${PROOF_ACCENTS[index]}`}>
                  <strong>{stat.value}</strong>
                  <span>{stat.label}</span>
                </div>
              ))}
            </div>
            <div className="hero-explain__final-card">
              <h2>{t.finalTitle}</h2>
              <p>{t.finalText}</p>
              <DuoButton color="green" size="lg" icon="arrow_forward" iconTrailing onClick={enterApp}>
                {primaryLabel}
              </DuoButton>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}

function MiniStat({ icon, value, label, accent }: { icon: string; value: string; label: string; accent: Accent }) {
  return (
    <div className={`hero-explain__mini-stat hero-explain--${accent}`}>
      <div><Icon name={icon} /><strong>{value}</strong></div>
      <p>{label}</p>
    </div>
  );
}

function ReasonCard({ icon, number, title, text, accent }: { icon: string; number: string; title: string; text: string; accent: Accent }) {
  return (
    <article className={`hero-explain__reason hero-explain--${accent}`}>
      <div className="hero-explain__reason-top"><span><Icon name={icon} /></span><strong>{number}</strong></div>
      <h3>{title}</h3>
      <p>{text}</p>
    </article>
  );
}
