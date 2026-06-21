/**
 * Responsive audit: drives the running dev server across every canonical band
 * and reports the two defects this refactor was about.
 *
 *   1. horizontal overflow - `scrollWidth > clientWidth` on the document
 *   2. off-centre focus    - a non-scrolling "focus" screen whose gap above the
 *                            content differs from the gap below it. Measuring
 *                            SYMMETRY rather than "how much of the viewport is
 *                            filled" is the point: a centred card legitimately
 *                            fills only 40% of a tall tablet screen, but the
 *                            40% has to sit in the middle. The original bug was
 *                            a gap of 30/470 (page top) reading as 100% dead
 *                            space below.
 *
 * Usage:  npx playwright@1 install chromium   # bir marta
 *         node scripts/responsive-audit.mjs [baseUrl]
 * Requires the dev server to be up (npm run dev). Playwright is intentionally
 * not a package.json dependency - this is an on-demand audit, not a unit test;
 * `responsive-bands.test.ts` is the part that runs in CI.
 */
import { chromium } from "playwright";

const BASE = process.argv[2] ?? "http://localhost:5173";

/** The canonical bands, one representative viewport each. */
const BANDS = [
  { name: "xs", width: 360, height: 780 },
  { name: "mobile", width: 430, height: 932 },
  { name: "tablet", width: 820, height: 1180 },
  { name: "laptop", width: 1024, height: 768 },
  { name: "desktop", width: 1440, height: 900 },
];

/**
 * `focus` = a single self-contained step that should sit in the middle of the
 * frame. `flow` = a catalog/dashboard that legitimately starts at the top.
 */
const ROUTES = [
  { path: "/home", kind: "flow" },
  { path: "/vocabulary/topics", kind: "flow" },
  { path: "/grammar", kind: "flow" },
  { path: "/reading", kind: "flow" },
  { path: "/listening", kind: "flow" },
  { path: "/writing", kind: "flow" },
  { path: "/books", kind: "flow" },
  { path: "/app/speaking", kind: "flow" },
  { path: "/progress", kind: "flow" },
  { path: "/leaderboard", kind: "flow" },
  { path: "/profile", kind: "flow" },
  { path: "/levels", kind: "flow" },
  { path: "/vocabulary/review", kind: "focus" },
  { path: "/vocabulary/topic/40982faf-b26c-49da-ad0f-8b63429e9d79", kind: "focus" },
];

/** Max px a focus screen's top gap may differ from its bottom gap. */
const CENTRE_TOLERANCE = 24;

async function measure(page) {
  return page.evaluate(() => {
    const doc = document.documentElement;
    const vw = doc.clientWidth;
    const vh = window.innerHeight;

    const offenders = [];
    for (const el of document.querySelectorAll("body *")) {
      const r = el.getBoundingClientRect();
      if (r.width === 0 || r.height === 0) continue;
      if (getComputedStyle(el).position === "fixed") continue;
      if (r.right > vw + 1 || r.left < -1) {
        // Ignore anything inside a deliberate horizontal scroller.
        let p = el.parentElement;
        let scoped = false;
        while (p && p !== document.body) {
          const o = getComputedStyle(p).overflowX;
          if (o === "auto" || o === "scroll") { scoped = true; break; }
          p = p.parentElement;
        }
        if (scoped) continue;
        offenders.push(
          `<${el.tagName.toLowerCase()} class="${String(el.className).slice(0, 60)}"> ` +
            `${Math.round(r.left)}..${Math.round(r.right)}`
        );
      }
    }

    // Vertical extent of the real content. Page shells (anything as tall as the
    // viewport) and absolutely-positioned decoration are excluded - otherwise
    // every page trivially spans the full height and nothing is ever off-centre.
    const main = document.querySelector("#main") ?? document.body;
    let top = Infinity;
    let bottom = -Infinity;
    for (const el of main.querySelectorAll("*")) {
      const r = el.getBoundingClientRect();
      if (r.width < 24 || r.height < 12 || r.height >= vh * 0.9) continue;
      const cs = getComputedStyle(el);
      if (cs.visibility === "hidden" || cs.position === "absolute" || cs.position === "fixed")
        continue;
      top = Math.min(top, r.top);
      bottom = Math.max(bottom, r.bottom);
    }

    return {
      overflow: doc.scrollWidth > vw + 1,
      scrollWidth: doc.scrollWidth,
      clientWidth: vw,
      offenders: offenders.slice(0, 5),
      scrolls: doc.scrollHeight > vh + 1,
      gapTop: Number.isFinite(top) ? Math.round(top) : 0,
      gapBottom: Number.isFinite(bottom) ? Math.round(vh - bottom) : 0,
    };
  });
}

const browser = await chromium.launch();
const problems = [];
let checks = 0;

for (const band of BANDS) {
  const context = await browser.newContext({
    viewport: { width: band.width, height: band.height },
  });
  const page = await context.newPage();

  for (const route of ROUTES) {
    try {
      await page.goto(BASE + route.path, { waitUntil: "networkidle", timeout: 20000 });
    } catch {
      await page.waitForTimeout(1200);
    }
    await page.waitForTimeout(450);

    const m = await measure(page);
    checks += 1;
    const where = `${band.name.padEnd(7)} ${String(band.width).padStart(4)}px  ${route.path}`;

    if (m.overflow) {
      problems.push(
        `OVERFLOW  ${where}  scrollW=${m.scrollWidth} > ${m.clientWidth}\n` +
          m.offenders.map((o) => `            ${o}`).join("\n")
      );
    }
    const skew = Math.abs(m.gapTop - m.gapBottom);
    if (route.kind === "focus" && !m.scrolls && skew > CENTRE_TOLERANCE) {
      problems.push(
        `OFF-CENTRE ${where}  tepada ${m.gapTop}px / pastda ${m.gapBottom}px ` +
          `bo'sh joy (farq ${skew}px, ruxsat ${CENTRE_TOLERANCE}px)`
      );
    }
  }
  await context.close();
}

await browser.close();

console.log(`\n${checks} ta tekshiruv (${ROUTES.length} marshrut x ${BANDS.length} band)`);
if (problems.length === 0) {
  console.log("Muammo topilmadi.");
} else {
  console.log(`\n${problems.length} ta muammo:\n`);
  for (const p of problems) console.log(p + "\n");
  process.exitCode = 1;
}
