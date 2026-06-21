import { existsSync, readFileSync, readdirSync } from "node:fs";
import { extname, join, relative } from "node:path";
import { describe, expect, it } from "vitest";

const SOURCE_ROOT = "src";
const SOURCE_EXTENSIONS = new Set([".css", ".ts", ".tsx"]);

function productionFiles(directory = SOURCE_ROOT): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return productionFiles(path);
    if (!SOURCE_EXTENSIONS.has(extname(entry.name))) return [];
    if (/\.(?:test|spec)\.[^.]+$/.test(entry.name)) return [];
    return [path];
  });
}

const files = productionFiles();
const sources = files.map((path) => ({ path, source: readFileSync(path, "utf8") }));
const liveVoiceSources = [
  "pages/LiveAccentTutorPage.css",
  "pages/LiveAccentTutorResult.css",
  "pages/SpeakingLiveTemplate.css",
];
const projectSources = [
  ...sources,
  { path: "tailwind.config.js", source: readFileSync("tailwind.config.js", "utf8") },
];

function displayPath(path: string) {
  return path.startsWith(`${SOURCE_ROOT}/`) ? relative(SOURCE_ROOT, path) : path;
}

function matches(pattern: RegExp, allow: string[] = [], inputs = sources) {
  return inputs.flatMap(({ path, source }) => {
    const displayed = displayPath(path);
    if (allow.includes(displayed)) return [];
    return pattern.test(source) ? [displayed] : [];
  });
}

function customShadowViolations() {
  const allowed = /^(?:none\b|var\(--ea-shadow-[a-z-]+\)(?:\s*!important)?$|0\s+0\s+0\b)/;
  const allowedFiles = new Set([
    "components/video/wordOverlays.css",
    "index.css",
    "pages/DemographicsSetupPage.css",
    "pages/FounderDashboardPage.css",
    "pages/LiveAccentTutorPage.css",
    "pages/LiveAccentTutorResult.css",
    "pages/NotFoundPage.css",
    "pages/ProgressPage.css",
    "pages/ReadingTopicPage.css",
    "pages/SpeakingLiveTemplate.css",
    "pages/SupportChat.css",
    "pages/VideoCatalogPage.css",
    "pages/VideoPlayerPage.css",
    "pages/home-concepts/homeConceptLab.css",
  ]);
  return sources.flatMap(({ path, source }) => {
    if (extname(path) !== ".css") return [];
    if (allowedFiles.has(displayPath(path))) return [];
    return Array.from(source.matchAll(/box-shadow\s*:\s*([^;}]+)/g))
      .filter((match) => !allowed.test(match[1].trim()))
      .map((match) => `${displayPath(path)}: ${match[1].trim()}`);
  });
}

function opaqueWhiteSurfaceViolations() {
  const opaqueWhite = /background(?:-color)?\s*:\s*(?:white\b|#fff(?:fff)?\b|rgba?\(\s*255(?:[, ]+255){2}\s*(?:[,/]\s*(?:0?\.(?:8\d|9\d)|1(?:\.0+)?|(?:8\d|9\d|100)%))?\s*\)|rgb\(\s*255(?:[, ]+255){2}\s*\/\s*(?:8\d|9\d|100)%\s*\))/gi;
  return sources.flatMap(({ path, source }) => {
    if (extname(path) !== ".css") return [];
    return Array.from(source.matchAll(opaqueWhite)).map((match) => `${displayPath(path)}: ${match[0]}`);
  });
}

describe("EnglishAI Play design guard", () => {
  it("publishes the canonical marker without the retired home marker", () => {
    const html = readFileSync("index.html", "utf8");
    expect(html).toContain('data-design-system="englishai-play"');
    expect(html).not.toContain("data-home-design");
  });

  it("keeps retired reference themes deleted and unimported", () => {
    for (const path of [
      "src/styles/reference-catalog-theme.css",
      "src/styles/reference-final-theme.css",
      "src/styles/reference-lesson-theme.css",
      "src/styles/clay.css",
    ]) {
      expect(existsSync(path), path).toBe(false);
    }
    expect(matches(/reference-(?:catalog|final|lesson)-theme|["']\.\/styles\/clay\.css["']/)).toEqual([]);
  });

  it("rejects tactile press, glass and offset-shadow presentation", () => {
    const banned = [
      /active:translate-y|hover:-translate-y|group-active:translate-y/,
      /while(?:Hover|Tap)=.*\by\s*:/,
      /(?<!drop-)shadow-\[|(?:active|hover):shadow-\[/,
      /\bboxShadow\s*:/,
      /backdrop-filter\s*:(?!\s*none\b)|backdrop-blur/,
      /box-shadow\s*:[^;}]*(?:\binset\b|(?:^|,)\s*0\s+[1-9]\d*px\s+0(?:px)?\b)/m,
    ];

    for (const pattern of banned) {
      const patternText = String(pattern);
      const allow = patternText.includes("backdrop-filter") || patternText.includes("box-shadow")
        ? liveVoiceSources
        : [];
      expect(matches(pattern, allow), String(pattern)).toEqual([]);
    }
    expect(customShadowViolations()).toEqual([]);
  });

  it("rejects retired game and clay presentation tokens", () => {
    const legacy = /--duo-|(?:bg|text|border|ring|from|to|rounded|animate|accent|shadow)-duo-|--clay-|(?:bg|text|border|shadow|rounded)-clay-|claymorph|neumorph/i;
    expect(matches(legacy, [], projectSources)).toEqual([]);
  });

  it("keeps opaque surfaces and strong-primary foregrounds theme-aware", () => {
    expect(opaqueWhiteSurfaceViolations()).toEqual([]);
    expect(matches(/\bbg-white\/(?:8\d|9\d|100)\b/)).toEqual([]);

    const strongPrimary = String.raw`bg-ea-(?:primary(?:-end)?|purple-600|blue-600|orange-(?:500|600))(?![-\w])`;
    const wrongForeground = String.raw`(?:text-white|text-ea-(?:ink|ink-deep|text|surface))(?![-\w])`;
    const wrongPrimaryForeground = new RegExp(`(?:${strongPrimary}[^"\\n]{0,180}${wrongForeground}|${wrongForeground}[^"\\n]{0,180}${strongPrimary})`);
    expect(matches(wrongPrimaryForeground)).toEqual([]);
  });

  it("limits gradients to media, live voice, progress and skeleton sources", () => {
    const allowed = [
      "components/game/CleanParallax.tsx",
      "components/game/JungleBackground.tsx",
      "components/game/JungleParallax.tsx",
      "components/game/LessonFrame.tsx",
      "components/speaking/LiveVoiceBackdrop.tsx",
      "components/video/wordOverlays.css",
      "index.css",
      "lesson/JungleBackground.tsx",
      "pages/DemographicsSetupPage.css",
      "pages/FounderDashboardPage.css",
      "pages/LandingStory.css",
      "pages/LeaderboardPage.css",
      "pages/LiveAccentTutorPage.css",
      "pages/ProfilePage.css",
      "pages/ProgressPage.css",
      "pages/ReadingTopicPage.css",
      "pages/SpeakingPage.css",
      "pages/SpeakingLiveTemplate.css",
      "pages/SupportChat.css",
      "pages/VideoCatalogPage.css",
      "pages/VideoPlayerPage.css",
      "pages/VideoPlayerPage.tsx",
      "pages/home-concepts/homeConceptLab.css",
      "pages/home-concepts/professionalHome.css",
      "pages/levelMap/ContinueHero.tsx",
      "pages/levelMap/levelMap.css",
      // Onboarding (login → /home) shares the professional-indigo feature-panel
      // gradient with professionalHome.css above; same system, same allowance.
      "pages/onboarding/onboarding.css",
    ];
    expect(matches(/linear-gradient|radial-gradient|conic-gradient|bg-gradient-to-/, allowed)).toEqual([]);
  });

  it("rejects thick black legacy borders", () => {
    const thickBlack = /border(?:-width)?\s*:\s*[2-9]px[^;}]*(?:#000\b|\bblack\b|var\(--ea-ink\))|border-[2-9][^"\n]*(?:border-black|border-ea-ink)/;
    expect(matches(thickBlack)).toEqual([]);
  });

  it("allows decorative blur only for the semantic live-voice state", () => {
    expect(matches(/blur-(?:2xl|3xl)|filter\s*:\s*blur\(/, ["components/speaking/LiveVoiceBackdrop.tsx", ...liveVoiceSources])).toEqual([]);
  });

  it("keeps modal semantics inside canonical overlay sources", () => {
    expect(matches(/\s(?:role="dialog"|aria-modal="true")/, [
      "components/design/index.tsx",
      "components/LearningAssistant.tsx",
    ])).toEqual([]);
    expect(matches(/fixed inset-0[^"\n]*(?:bg-black|backdrop)/)).toEqual([]);
  });

  it("keeps priority admin flows on canonical components", () => {
    const grammar = readFileSync("src/pages/AdminGrammarPage.tsx", "utf8");
    const users = readFileSync("src/pages/AdminUsersPage.tsx", "utf8");
    const notifications = readFileSync("src/pages/AdminNotificationsPage.tsx", "utf8");

    expect(grammar).toMatch(/DesignModal/);
    expect(grammar).toMatch(/DesignConfirm/);
    expect(grammar).toMatch(/StatCard/);
    expect(users).toMatch(/DesignModal/);
    expect(users).toMatch(/DesignConfirm/);
    expect(existsSync("src/pages/AdminUsersPage.css")).toBe(false);
    expect(notifications).toMatch(/DesignConfirm/);
    expect(notifications).not.toMatch(/window\.confirm/);
  });

  it("keeps all routed admin workspaces on canonical surfaces", () => {
    const routedAdminPages = [
      "AdminPage",
      "AdminCurriculumPage",
      "AdminSectionsPage",
      "AdminUserDetailPage",
      "ServerHealthPage",
    ];

    for (const page of routedAdminPages) {
      const source = readFileSync(`src/pages/${page}.tsx`, "utf8");
      expect(source, page).toMatch(/PageHeader|DesignState|DesignCard/);
      expect(source, page).not.toMatch(/CardTone|toneFor|__tone--|__card--(?:purple|blue|orange|yellow|mint|rose|lavender|sky|pink|cyan|violet)/);
    }

    const design = readFileSync("src/components/design/index.tsx", "utf8");
    expect(design).toMatch(/export function DesignState/);
  });

  it("keeps learner catalogs on one canonical card surface", () => {
    const catalogs = [
      "GrammarCatalogPage",
      "BooksCatalogPage",
      "ListeningCatalogPage",
      "ReadingCatalogPage",
      "WritingCatalogPage",
      "VocabularyTopicsPage",
      "FreeTalkTopicsPage",
      "RoleTalkScenariosPage",
    ];
    const randomTonePattern = /CARD_TONES|CardTone|catalog__card--(?:blue|teal|orange|purple|yellow|green|pink|mint|red|rose|peach|sky|lilac|lemon)/;

    for (const catalog of catalogs) {
      const source = readFileSync(`src/pages/${catalog}.tsx`, "utf8");
      const css = readFileSync(`src/pages/${catalog}.css`, "utf8");
      expect(`${source}\n${css}`, catalog).not.toMatch(randomTonePattern);
      expect(css, catalog).toContain('@import "../components/catalog/CatalogTheme.css"');


    }

    expect(readFileSync("src/components/catalog/CatalogTheme.css", "utf8")).toContain("border:1px solid var(--ea-border)");
    expect(matches(/\bCARD_TONES\b|\bCardTone\b/)).toEqual([]);
  });

  it("keeps shared compatibility components on canonical primitives", () => {
    expect(existsSync("src/components/admin/AdminButton.tsx")).toBe(false);
    expect(readFileSync("src/components/ui/Button.tsx", "utf8")).toContain("ea-button");
    expect(readFileSync("src/components/ui/Card.tsx", "utf8")).toContain("ea-card");
    expect(readFileSync("src/components/ui/Input.tsx", "utf8")).toContain("ea-form-control");

    const leaderboard = [
      "src/components/leaderboard/LeagueHero.tsx",
      "src/components/leaderboard/Podium.tsx",
      "src/components/leaderboard/RankRow.tsx",
    ].map((path) => readFileSync(path, "utf8")).join("\n");
    expect(leaderboard).not.toMatch(/boxShadow|shadow-inner|shadow-\[/);
  });
});
