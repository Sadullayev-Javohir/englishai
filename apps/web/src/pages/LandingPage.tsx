import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@/app/auth";
import { useDocumentTitle } from "@/app/documentTitle";
import { Icon } from "@/components/ui/Icon";
import { AppButton } from "@/components/design";
import { LandingProjectAssistant } from "@/components/LandingProjectAssistant";
import { PublicSiteFooter, PublicSiteHeader, type PublicLocale } from "@/components/public/PublicSiteChrome";
import { usePublicPageScroll } from "@/components/public/usePublicPageScroll";
import { landingSkills, playLanding } from "@/content/playLanding";
import seo from "@/content/seo.json";
import { staticAsset } from "@/lib/staticAssets";
import { applyPublicSeoHead, publicSeoRoutes } from "./PublicSeoPage";
import "./LandingPage.css";

export function LandingPage({ locale: explicitLocale }: { locale?: PublicLocale } = {}) {
  const { pathname } = useLocation();
  usePublicPageScroll();
  const locale = explicitLocale ?? (pathname === "/en" || pathname === "/en/" ? "en" : "uz");
  return <LocalizedLanding key={locale} locale={locale} />;
}

function LocalizedLanding({ locale }: { locale: PublicLocale }) {
  const { status } = useAuth();
  const navigate = useNavigate();
  const [answer, setAnswer] = useState<number | null>(null);
  const copy = playLanding[locale];
  const route = publicSeoRoutes.find((item) => item.path === (locale === "en" ? "/en/" : "/"))!;
  const enter = () => navigate(status === "authenticated" ? "/home" : "/login");

  useDocumentTitle(locale === "en" ? "Learn English with an AI tutor" : "AI bilan ingliz tilini o‘rganing");
  useEffect(() => {
    applyPublicSeoHead({ ...route, heading: copy.headline.join(" "), faq: locale === "uz" ? seo.faq : route.faq });
    return () => { document.documentElement.lang = "uz"; };
  }, [copy, locale, route]);

  return (
    <div className="play-landing" lang={locale}>
      <PublicSiteHeader locale={locale} authenticated={status === "authenticated"} landing />
      <main>
        <section className="pl-hero">
          <div>
            <span className="pl-eyebrow">{copy.eyebrow}</span>
            <h1>{copy.headline[0]}<br />{copy.headline[1]}</h1>
            <p>{copy.intro[0]}<br />{copy.intro[1]}</p>
            <AppButton size="lg" trailingIcon="arrow_forward" onClick={enter}>{copy.start}</AppButton>
            <small>{copy.noCard}</small>
            <div className="pl-downloads">
              <button type="button" disabled><Icon name="play_arrow" /><span>Play Market<small>{copy.comingSoon}</small></span></button>
              <a href={staticAsset("/downloads/englishai.apk")} download="englishai.apk"><Icon name="download" /><span>{copy.download}<small>{copy.android}</small></span></a>
            </div>
          </div>
          <div className="pl-hero-art">
            <span className="pl-speech">Hello, world!</span>
            <img src="/assets/play/parrot.svg" alt={copy.parrotAlt} width="480" height="400" />
            <span className="pl-reward"><Icon name="auto_awesome" />{copy.reward}</span>
          </div>
        </section>
        <div className="pl-proof">
          {["route", "record_voice_over", "check_circle"].map((icon, index) => <span key={icon}><Icon name={icon} />{copy.proof[index]}</span>)}
        </div>
        <section className="pl-discover" id="nega">
          <img src="/assets/play/words-world.svg" alt={copy.worldAlt} loading="lazy" width="360" height="400" />
          <div>
            <h2>{copy.discoverTitle[0]}<br />{copy.discoverTitle[1]}</h2>
            <p>{copy.discoverIntro}</p>
            <div className="pl-skills">
              {landingSkills.map(({ label, icon }, index) => (
                <button type="button" key={label} onClick={enter} className={`pl-skill-${index % 3}`}>
                  <Icon name={icon} /><strong>{label}</strong><span>{copy.skills[index]}</span>
                </button>
              ))}
            </div>
          </div>
        </section>
        <section className="pl-demo" id="qanday">
          <div>
            <span className="pl-eyebrow">{copy.demoEyebrow}</span>
            <h2>{copy.demoTitle[0]}<br />{copy.demoTitle[1]}</h2>
            <p>{copy.demoIntro[0]}<br />{copy.demoIntro[1]}</p>
            <span className="pl-demo-day">{copy.demoDay}</span>
          </div>
          <div className="pl-demo-card">
            <span className="pl-eyebrow">{copy.firstWord}<span>01 / 03</span></span>
            <h3>Hello!</h3>
            <p>{copy.question}</p>
            <div className="pl-demo-options">
              {copy.options.map((option, index) => (
                <button type="button" aria-pressed={answer === index} data-correct={answer === index && index === 0} key={option} onClick={() => setAnswer(index)}>
                  {option}{answer === index && <Icon name={index === 0 ? "check_circle" : "close"} />}
                </button>
              ))}
            </div>
            <p className="pl-demo-feedback" role="status">{copy.feedback[answer === null ? 0 : answer === 0 ? 1 : 2]}</p>
          </div>
        </section>
        <section className="pl-steps">
          <h2>{copy.stepsTitle}</h2>
          <div>
            {["route", "event", "chat"].map((icon, index) => (
              <article key={icon}><span><Icon name={icon} /></span><small>0{index + 1}</small><h3>{copy.steps[index]}</h3></article>
            ))}
          </div>
        </section>
        <section className="pl-faq">
          <h2>{copy.faqTitle}</h2>
          {copy.faqs.map((faq) => <details key={faq.q}><summary>{faq.q}</summary><p>{faq.a}</p></details>)}
        </section>
        <section className="pl-final">
          <span className="pl-eyebrow">{copy.finalEyebrow}</span>
          <h2>{copy.finalTitle[0]}<br />{copy.finalTitle[1]}</h2>
          <AppButton onClick={enter} trailingIcon="arrow_forward">{copy.start}</AppButton>
          <p>{copy.finalNote}</p>
        </section>
      </main>
      <PublicSiteFooter locale={locale} />
      <LandingProjectAssistant key={locale} locale={locale} />
    </div>
  );
}
