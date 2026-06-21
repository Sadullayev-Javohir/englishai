# EnglishAI monochrome redesign roadmap

Holat: 1-qism discovery natijasi. Production migration hali boshlanmagan.

## Authority ketma-ketligi

1. `docs/design-audit/02-DESIGN.md` — yangi normative yozma tizim.
2. DEV-only `/design-lab/foundation` — barcha komponent/state/theme’larning visual proof’i.
3. Shared token/theme/primitiv runtime.
4. Production page-family migratsiyasi.
5. Legacy cleanup va final drift audit.

## 2-qism — Canonical DESIGN spec

Bitta aniq spec quyidagilarni belgilaydi:

- rangsiz “Quiet Monochrome” falsafasi;
- Light/Dark/System semantic tokenlari;
- background/surface/elevated/inset/border/text/muted/inverse/focus/disabled;
- statuslarning rangsiz birlamchi ifodasi;
- typography, spacing, radii, elevation, motion;
- Button, IconButton, Card, Input, Textarea, Select, Tabs, Stepper;
- Modal, Dialog, Drawer, mobile Sheet, Toast, Tooltip;
- loading, empty, error, locked, completed;
- media ratio/fallback;
- learner/admin density;
- responsive boundary va accessibility contractlari;
- governance va migration definition-of-done.

Gate: yozma yo‘nalish foydalanuvchi tomonidan tasdiqlanadi.

## 3-qism — DEV-only Foundation Design Lab

`/design-lab/foundation` bitta sahifada ko‘rsatadi:

- Light/Dark/System selector;
- neutral palette ramp va typography scale;
- barcha button/card/form holatlari;
- nav/tabs/stepper;
- modal desktopda, sheet mobil’da;
- media/fallback;
- loading/empty/error/locked/complete;
- 44/48/52px control geometries;
- 390, 700/701, 900/901, 1180/1181, 1440 breakpoint proof.

Gate: foydalanuvchi visual approval beradi. Shu paytgacha production UI migratsiyasi yo‘q.

## 4-qism — Shared runtime

- Flash-free persisted Light/Dark/System runtime.
- OS media change va cross-tab sync.
- Root `.dark`, `data-theme`, `data-theme-mode`, `color-scheme`, `theme-color` sync.
- Native status bar mapping.
- Shared semantic token va primitives.
- Focus-visible, reduced-motion, 44px target testlari.

## 5-qism — Production migration tartibi

Har qator alohida bounded migration va browser QA:

1. Public shell: landing, SEO, content, login, 404.
2. Onboarding shell: username, goal, welcome, assessment.
3. Placement + result + exit-test family.
4. Learner shell: sidebar/topbar/mobile nav/overlays.
5. Home + Levels.
6. Catalog family: vocabulary, grammar, listening, reading, writing, books, video, speaking catalogs.
7. Lesson family: vocabulary, grammar, listening, reading, writing.
8. Immersive media/speaking/book/video flows.
9. Progress, leaderboard, profile, saved/review flows.
10. Admin shell va admin pages.
11. Global overlays: assistant, notification, paywall, dialogs/sheets/tooltips.
12. Native-specific surfaces va widgets.

Har slice: source reconciliation → focused test → typecheck/lint → build → visible browser Light/Dark + responsive + interaction + console/network/a11y.

## 6-qism — Legacy cleanup

- `reference-lesson-theme.css`, `reference-catalog-theme.css`, `reference-final-theme.css` olib tashlash.
- Global Clay va Duo production dependencylarini olib tashlash yoki tasdiqlangan scope’ga qisqartirish.
- Raw hex, `!important`, inline style va arbitrary px actionable driftni nolga yaqinlashtirish.
- Orphan pages uchun explicit merge/delete.
- Final route × viewport × theme browser matrix.

## Hozirgi keyingi qadam

`docs/design-audit/02-DESIGN.md` ichida “Quiet Monochrome” Light/Dark/System tizimini to‘liq yozish va approval uchun taqdim etish.
