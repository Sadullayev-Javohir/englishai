import { Link, useLocation } from "react-router-dom";
import { ParrotLogo } from "@/components/ParrotLogo";
import { Icon } from "@/components/ui/Icon";
import "./PublicSiteChrome.css";

export type PublicLocale = "uz" | "en";

function publicLocalePaths(pathname: string, authenticated = false) {
  const landingPath = pathname.match(/^(\/landing)(?:\/eng)?\/?$/i)?.[1];
  return landingPath
    ? { uz: landingPath, en: `${landingPath}/eng` }
    : { uz: authenticated ? "/landing" : "/", en: "/en/" };
}

export function PublicSiteHeader({
  locale = "uz",
  authenticated = false,
  landing = false,
}: {
  locale?: PublicLocale;
  authenticated?: boolean;
  landing?: boolean;
}) {
  const { pathname } = useLocation();
  const english = locale === "en";
  const localePaths = publicLocalePaths(pathname, authenticated);
  const home = english ? "/en/" : "/";
  const links = landing
    ? [
        { href: "#nega", label: english ? "Why EnglishAI?" : "Nega EnglishAI?" },
        { href: "#qanday", label: english ? "How it works" : "Qanday ishlaydi?" },
        { href: english ? "/en/learn-english" : "/learn", label: english ? "Learning guides" : "Qo‘llanmalar" },
        { href: "/pricing", label: english ? "Pricing" : "Narxlar" },
      ]
    : [
        { href: "/learn", label: "Qo‘llanmalar" },
        { href: "/methodology", label: "Metodologiya" },
        { href: "/pricing", label: "Narxlar" },
        { href: "/contact", label: "Aloqa" },
      ];

  return (
    <header className="ps-header">
      <Link to={home} className="ps-brand" aria-label={english ? "EnglishAI home" : "EnglishAI bosh sahifasi"}>
        <ParrotLogo size={38} withWordmark />
      </Link>
      <nav className="ps-navigation" aria-label={english ? "Main navigation" : "Asosiy navigatsiya"}>
        {links.map(({ href, label }) => href.startsWith("#") ? (
          <a key={href} href={href}>{label}</a>
        ) : (
          <Link key={href} to={href} aria-current={pathname.replace(/\/$/, "") === href ? "page" : undefined}>{label}</Link>
        ))}
      </nav>
      <div className="ps-header-actions">
        <Link className="ps-language" to={english ? localePaths.uz : localePaths.en} hrefLang={english ? "uz" : "en"} aria-label={english ? "Switch to Uzbek" : "Switch to English"}>
          <Icon name="translate" className="text-[18px]" />{english ? "UZ" : "EN"}
        </Link>
        <Link className="ea-button ea-button--md ea-button--standard" to={authenticated ? "/home" : "/login"}>
          {authenticated ? (english ? "Continue learning" : "Davom etish") : (english ? "Log in" : "Kirish")}
        </Link>
      </div>
    </header>
  );
}

export function PublicSiteFooter({ locale = "uz" }: { locale?: PublicLocale }) {
  const { pathname } = useLocation();
  const english = locale === "en";
  const localePaths = publicLocalePaths(pathname);
  return (
    <footer className="ps-footer">
      <div>
        <Link className="ps-brand" to={english ? "/en/" : "/"} aria-label="EnglishAI"><ParrotLogo size={34} withWordmark /></Link>
        <p>© 2026 EnglishAI. {english ? "One small step, every day." : "Har kuni bir qadam."}</p>
      </div>
      <nav aria-label={english ? "More about EnglishAI" : "EnglishAI haqida"}>
        <Link to="/methodology">{english ? "Our methodology" : "Metodologiya"}</Link>
        <Link to="/contact">{english ? "Contact" : "Aloqa"}</Link>
        <Link to="/editorial-policy">{english ? "Editorial policy" : "Tahririy siyosat"}</Link>
        <Link to={english ? localePaths.uz : localePaths.en} hrefLang={english ? "uz" : "en"}>{english ? "O‘zbekcha" : "English"}</Link>
      </nav>
    </footer>
  );
}
