# Shared catalog components contract (READ BEFORE EDITING ANY *CatalogPage)

All six content catalogs must share ONE 3D "shelf" card language (spec 11-catalogs.md). A clean
shared layer lives at `src/components/catalog/` - USE THESE, do not reinvent card styling, do not
copy `src/components/duo/*` (deprecated), do not use the old `SkillTopicCard`/`BookCover` plain
`border rounded-xl` look for the grid cards.

## Import
```ts
import { ShelfCard, CatalogHeader, LevelFilterRow } from "@/components/catalog";
import type { LevelFilter } from "@/components/catalog"; // = CefrLevel | "all" | null
import { usePaywall } from "@/components/PaywallProvider";
import { TopicImage } from "@/components/TopicImage";
import { BookCover } from "@/components/BookCover";
import { cefrShort } from "@/lib/labels";
import { uz } from "@/content/uz";
import { CefrLevel } from "@/api/types";
```

## ShelfCard props (the one card for every catalog)
```ts
<ShelfCard
  module="video" | "reading" | "books" | "grammar" | "writing" | "listening" // REQUIRED - sets accent
  cover={<TopicImage topicId={t.topicId} title={t.title} level={t.level} hideLevelBadge className="aspect-[4/3] w-full" />}
  level={t.level}                         // REQUIRED - corner badge
  title={t.title}                         // English-first title
  subtitle={t.titleUz ?? t.author}        // short line (Uzbek label / author / category)
  xp={10}                                  // optional XP reward chip
  locked={gate.isLocked}                  // optional - dims + disables nav + shake
  pro={!gate.isLocked && gate.requiresPro}// optional - ⭐ paywall affordance, stays tappable
  done={gate.passed}                       // optional - green "Tugatildi" footer
  onOpen={() => ...}                       // tap handler; ignored when locked
/>
```
- The card already renders: corner CEFR badge, hover-lift + chunky lip, tap-sink, locked overlay,
  pro ⭐ badge, and a CTA footer ("Ko'rish"/"O'qish"/"Boshlash"/"Yozish"/"Tinglash"). You only
  choose `module`, `cover`, `level`, `title`, `subtitle`, `xp`, state flags, `onOpen`.
- For BOOKS: cover = `<BookCover title=.. level=.. coverImageUrl={b.coverImageUrl} hideLevelBadge className="aspect-[3/4] w-full" />` (hideLevelBadge avoids a double badge; ShelfCard shows the level).
- For VIDEO: cover = the YouTube thumbnail `<img>` (aspect-video) exactly like the current page's
  VideoCard, but WITHOUT the outer PopCard wrapper - ShelfCard provides the card. Keep the
  duration chip + play overlay if you like; ShelfCard's `onOpen` runs the existing `openItem`.
- XP: choose a small fixed number per module (e.g. video 10, reading 10, books 15, grammar 12,
  writing 12, listening 12). It is decorative reward copy; do NOT invent a dynamic XP from the API.

## CatalogHeader (title + parrot greeter + progress pill)
```ts
<CatalogHeader module="reading" title={uz.reading.title} subtitle={uz.reading.subtitle} learned={done} total={all} />
```
- `greeting` is automatic (uz.catalog.greeting[module]). Pass `learned`/`total` ONLY when the
  catalog has a real completion count (books: sectionsRead/sectionCount doesn't map to "learned
  topics"; reading/grammar/writing/listening: count `gate.passed` vs total when filter is the
  learner's own level / "all"). If no meaningful count, omit learned/total - it shows the module
  accent pill instead.

## LevelFilterRow (3D pill filter)
```ts
<LevelFilterRow value={filter} label={uz.reading.levelLabel} allLabel={uz.reading.allLevels}
  learnerLevel={learnerLevel} onChange={setFilter} />
```
- Replace the page's existing hand-rolled level pills entirely with this.
- `LevelFilter` type = `CefrLevel | "all" | null` (same as the pages already use).

## Gating / paywall (reading, grammar, writing, listening only)
- Keep `useSkillTopicGates(learnerId, filter==="all"||filter===null ? undefined : filter, "<Module>")`
  and `gateOf(topicId)`. Pass `locked={gate.isLocked}` `pro={!locked && gate.requiresPro}` `done={gate.passed}`.
- `onOpen` must call `openPaywall()` when `pro`, else navigate/open the lesson. Example:
  ```ts
  onOpen={() => {
    const g = gateOf(t.topicId);
    if (g.isLocked) return;
    if (g.requiresPro) { openPaywall(); return; }
    navigate(`/grammar/topic/${t.topicId}`);
  }}
  ```
- Video has NO gates/paywall - just `onOpen={() => void openItem(video)}`.

## Must preserve (do not break)
- Video: the infinite scroll feed + paste-URL panel + `openItem`/`openByUrl` logic. FIX the existing
  duplicate `export function VideoCatalogPage()` bug (the file currently defines it twice; keep ONE).
- Reading/Listening: the in-page `Reader`/`Player` flow opened from a card (keep `openId` state and
  rendering the reader/player when set). The grid cards now call `setOpenId(topicId)` via `onOpen`.
- Books: `navigate(`/books/${b.id}`)`. Progress: `b.isCompleted ? done : show progress` via ShelfCard
  `done` + ShelfCard already shows subtitle; keep the `b.sectionsRead/b.sectionCount` as subtitle.
- Grammar: pass `grammarFocusLabel(t.grammarFocusCode)` as subtitle.
- Writing: pass `topicState?.topicTitle` handling as before.

## Visual / tokens
- Grid: `grid grid-cols-2 md:grid-cols-3 gap-sm md:gap-lg` (books: add `lg:grid-cols-4`).
- Cards sit directly on the jungle (AppShell already mounts JungleBackground + top GlobalHud + no
  sidebar). Do NOT wrap the page in a cream card; the ShelfCard frosted white is the surface.
- Keep `max-w-[900px]` (books `max-w-[1000px]`, video keeps its own layout) wrapper.
- Font: cards use Nunito (font-duo) labels already; body uses existing font tokens.

## Acceptance for your page
- Shares ShelfCard look (cover + level + xp + CTA) with the other five.
- Locked shows paywall affordance WITHOUT breaking navigation (locked = no nav; pro = openPaywall).
- Filtering works; cards route to the correct lesson page.
- No sidebar (already true - do not add one).
- `npx tsc --noEmit` must pass for the frontend (no type errors introduced).
