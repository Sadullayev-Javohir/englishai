import { useEffect, useState } from "react";
import type React from "react";
import { NavLink, useLocation, useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { EnglishAiLogo } from "@/components/EnglishAiLogo";
import { Icon } from "@/components/ui/Icon";
import { DuoButton } from "@/components/game/DuoButton";
import { cn } from "@/lib/cn";
import { useAuth } from "@/app/auth";

/**
 * Shared top Navbar - used on the landing page and every in-app screen so the
 * whole product reads as one brand. Mirrors the /home (SideNav) 3D language:
 * the EnglishAI logo + wordmark sit top-left, the primary sections are centred
 * links (active gets the chunky green "push-down" lip), and a CTA sits top-right.
 *
 * On the landing page the links are hash anchors (#muammo, #qanday, …); a
 * scroll-spy marks the section currently in view as active (sariq) so the
 * navbar tracks the scroll position across ALL sections, not just the hero.
 * When signed in the links route into the app and active follows the pathname.
 */
interface NavItem {
  to: string;
  anchor?: string; // when set, the link is a hash anchor on the landing page
  icon: string;
  label: string;
  match: (pathname: string) => boolean;
}

const ITEMS: NavItem[] = [
  { to: "/#muammo", anchor: "#muammo", icon: "help", label: uz.marketing.nav.problem, match: (p) => p === "/" },
  { to: "/#qanday", anchor: "#qanday", icon: "auto_stories", label: uz.marketing.nav.how, match: (p) => p === "/" },
  { to: "/#modullar", anchor: "#modullar", icon: "widgets", label: uz.marketing.nav.modules, match: (p) => p === "/" },
  { to: "/#biznes", anchor: "#biznes", icon: "payments", label: uz.marketing.nav.pricing, match: (p) => p === "/" },
  { to: "/#savol", anchor: "#savol", icon: "forum", label: uz.marketing.nav.faq, match: (p) => p === "/" },
  // In-app routes (only meaningful once signed in)
  { to: "/home", icon: "home", label: uz.nav.home, match: (p) => p === "/home" },
  { to: "/levels", icon: "map", label: uz.nav.levels, match: (p) => p === "/levels" || p.startsWith("/levels/") },
  { to: "/leaderboard", icon: "trophy", label: uz.nav.leaderboard, match: (p) => p === "/leaderboard" },
];

/** Scroll-spy: returns the id of the section currently most in view. */
function useActiveSection(enabled: boolean): string | null {
  const [active, setActive] = useState<string | null>(null);
  useEffect(() => {
    if (!enabled) return;
    const ids = ITEMS.filter((i) => i.anchor).map((i) => i.anchor!.slice(1));
    const sections = ids
      .map((id) => document.getElementById(id))
      .filter((el): el is HTMLElement => !!el);
    if (sections.length === 0) return;

    const visible = new Map<string, number>();
    const io = new IntersectionObserver(
      (entries) => {
        for (const e of entries) {
          if (e.isIntersecting) visible.set(e.target.id, e.intersectionRatio);
          else visible.delete(e.target.id);
        }
        let best: string | null = null;
        let bestRatio = 0;
        for (const [id, ratio] of visible) {
          if (ratio > bestRatio) {
            bestRatio = ratio;
            best = id;
          }
        }
        if (best) setActive(best);
      },
      { rootMargin: "-45% 0px -45% 0px", threshold: [0, 0.25, 0.5, 0.75, 1] },
    );
    sections.forEach((s) => io.observe(s));
    return () => io.disconnect();
  }, [enabled]);
  return active;
}

export function Navbar() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const { status } = useAuth();
  const signedIn = status === "authenticated";
  const enterApp = () => navigate(signedIn ? "/home" : "/login");
  const onLanding = pathname === "/" && !signedIn;
  const activeSection = useActiveSection(onLanding);

  const items = signedIn ? ITEMS.filter((i) => !i.anchor) : ITEMS.filter((i) => i.anchor);

  const isActive = (item: NavItem) =>
    item.match(pathname) ||
    (onLanding && item.anchor?.slice(1) === activeSection);

  const renderLink = (item: NavItem, compact: boolean) => {
    const active = isActive(item);
    const cls = cn(
      "flex shrink-0 items-center gap-2 rounded-full font-label-md font-bold transition-all",
      compact ? "px-md py-1.5 gap-1.5 text-[16px]" : "px-md py-2",
      active
        ? "bg-ea-green-600 text-white "
        : "text-white/75 hover:bg-white/10 hover:text-white",
    );

    const handleAnchorClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
      const id = item.anchor!.slice(1);
      const target = document.getElementById(id);
      if (!target) return;
      e.preventDefault();
      const startY = window.scrollY;
      const endY =
        target.getBoundingClientRect().top + window.scrollY - 64; // offset for sticky header
      const distance = endY - startY;
      // Slow, eased scroll so the page glides down gently on each header click.
      const duration = 2200;
      const startTime = performance.now();
      const easeInOutCubic = (t: number) =>
        t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
      const step = (now: number) => {
        const t = Math.min(1, (now - startTime) / duration);
        window.scrollTo(0, startY + distance * easeInOutCubic(t));
        if (t < 1) requestAnimationFrame(step);
      };
      requestAnimationFrame(step);
      // Keep the hash in the URL without the native jump.
      history.replaceState(null, "", item.anchor!);
    };

    return item.anchor ? (
      <a
        key={item.to}
        href={item.anchor}
        className={cls}
        onClick={handleAnchorClick}
      >
        <Icon name={item.icon} className={compact ? "text-[16px]" : "text-[18px]"} />
        {item.label}
      </a>
    ) : (
      <NavLink key={item.to} to={item.to} className={cls}>
        <Icon name={item.icon} className={compact ? "text-[16px]" : "text-[18px]"} />
        {item.label}
      </NavLink>
    );
  };

  return (
    <header className="sticky top-0 z-50 w-full border-b border-white/10 bg-ea-text/80 ">
      <div className="mx-auto flex h-16 max-w-[1140px] items-center justify-between gap-md px-margin-mobile md:px-8">
        {/* Brand - logo tile + wordmark, links to landing */}
        <motion.button
          type="button"
          onClick={() => navigate("/")}
          whileHover={{ scale: 1.04 }}
          whileTap={{ scale: 0.96 }}
          className="flex shrink-0 items-center gap-2"
          aria-label={uz.brand}
        >
          <EnglishAiLogo size={36} withWordmark rounded="8px" tileStyle={{ background: "#F0EAFF" }} wordmarkClassName="!text-white" />
        </motion.button>

        {/* Centre nav links */}
        <nav className="hidden items-center gap-1 lg:flex">
          {items.map((item) => renderLink(item, false))}
        </nav>

        {/* CTA */}
        <DuoButton
          color="green"
          size="sm"
          icon="rocket_launch"
          iconTrailing
          onClick={enterApp}
          className="shrink-0 font-duo"
        >
          {signedIn ? uz.marketing.ctaReturning : uz.marketing.nav.cta}
        </DuoButton>
      </div>

      {/* Mobile links row (compact, horizontal scroll) */}
      <nav className="flex gap-2 overflow-x-auto px-margin-mobile pb-2 lg:hidden">
        {items.map((item) => renderLink(item, true))}
      </nav>
    </header>
  );
}
