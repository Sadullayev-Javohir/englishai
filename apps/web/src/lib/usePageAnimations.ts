import { useEffect } from "react";
import type { DependencyList } from "react";
import { useLocation } from "react-router-dom";

/**
 * Global animation driver. Once mounted it:
 *   1. Adds `.anim-on` to <html> so the CSS system activates (the prerendered /
 *      no-JS HTML still shows everything - SEO-safe).
 *   2. Watches every `[data-reveal]` and `[data-stagger]` element and adds
 *      `.is-visible` as they scroll into view (IntersectionObserver).
 *   3. Re-scans on every route change so freshly mounted pages animate too.
 *
 * To use on any page, simply add markup like:
 *   <section data-reveal="up">…</section>
 *   <div data-anim="float" />
 *   <ul data-stagger>{items.map((x,i)=><li style={{'--i':i}} key={i}>…</li>)}</ul>
 */
export function usePageAnimations(deps: DependencyList = []): void {
  const { pathname } = useLocation();
  const dependencyKey = JSON.stringify(deps);

  // Activate the system once on first mount.
  useEffect(() => {
    if (typeof document !== "undefined") {
      document.documentElement.classList.add("anim-on");
    }
  }, []);

  // (Re)observe reveal/stagger targets whenever the route changes OR the caller's
  // content deps change (e.g. async data has finished loading and new reveal nodes
  // were just mounted). Without this, sections rendered after a fetch would stay at
  // opacity:0 forever because the initial observer pass never saw them.
  useEffect(() => {
    if (typeof document === "undefined") return;
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      // Skip observers; CSS already forces everything visible.
      return;
    }

    // Auto-tag top-level blocks inside the routed <main> so EVERY page gets a
    // strong scroll-reveal without hand-editing each one. Skips nodes that are
    // already animated (have data-reveal / data-anim / motion classes) so we
    // never double-animate a framer-motion page.
    const main = document.getElementById("main");
    if (main) {
      const blocks = Array.from(
        main.querySelectorAll<HTMLElement>(":scope > *"),
      );
      let autoIndex = 0;
      for (const block of blocks) {
        const already =
          block.hasAttribute("data-reveal") ||
          block.hasAttribute("data-anim") ||
          block.querySelector("[data-reveal], [data-anim], [data-stagger]") ||
          block.className.includes("animate-") ||
          block.getAttribute("class")?.includes("motion");
        if (!already) {
          block.setAttribute("data-reveal", "up");
          block.style.setProperty("--auto-i", String(autoIndex++));
        }
      }
    }

    const targets = Array.from(
      document.querySelectorAll<HTMLElement>(
        "[data-reveal], [data-stagger]",
      ),
    );

    const io = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.classList.add("is-visible");
            io.unobserve(entry.target);
          }
        }
      },
      { threshold: 0.12, rootMargin: "0px 0px -8% 0px" },
    );

    // Reveal elements already in view immediately; observe the rest.
    for (const t of targets) {
      const rect = t.getBoundingClientRect();
      if (rect.top < window.innerHeight && rect.bottom > 0) {
        t.classList.add("is-visible");
      } else {
        io.observe(t);
      }
    }

    return () => io.disconnect();
  }, [dependencyKey, pathname]);
}
