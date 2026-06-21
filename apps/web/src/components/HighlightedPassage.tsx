import { useMemo } from "react";
import { useWordPopover } from "@/components/word/useWordPopover";
import "./HighlightedPassage.css";

// ─────────────────────────────── Word matching ───────────────────────────────

// Strip surrounding punctuation but keep internal apostrophes/hyphens ("don't", "well-known").
function cleanWord(raw: string): string {
  return raw.replace(/^[^a-zA-Z']+|[^a-zA-Z']+$/g, "").toLowerCase();
}

// A token produced by splitting on /(\s+)/ that is whitespace or empty (not a real word).
const isGap = (token: string): boolean => token === "" || /^\s+$/.test(token);

const isVowel = (c: string): boolean => "aeiou".includes(c);

// Irregular forms (past / past participle / irregular plurals) that the regular suffix rules below
// cannot derive - so a target verb like "give" still highlights when the passage uses "gave", or
// "make a decision" when it says "made a decision". Keyed by base form; only the irregular forms
// are listed (regular -s/-ing are still added by inflectionsOf). Common high-frequency items only.
const IRREGULAR_FORMS: Record<string, string[]> = {
  be: ["am", "is", "are", "was", "were", "been", "being"],
  become: ["became", "become"],
  begin: ["began", "begun"],
  break: ["broke", "broken"],
  bring: ["brought"],
  build: ["built"],
  buy: ["bought"],
  catch: ["caught"],
  choose: ["chose", "chosen"],
  come: ["came"],
  cost: ["cost"],
  cut: ["cut"],
  do: ["does", "did", "done"],
  draw: ["drew", "drawn"],
  drink: ["drank", "drunk"],
  drive: ["drove", "driven"],
  eat: ["ate", "eaten"],
  fall: ["fell", "fallen"],
  feel: ["felt"],
  find: ["found"],
  fly: ["flew", "flown"],
  forget: ["forgot", "forgotten"],
  get: ["got", "gotten"],
  give: ["gave", "given"],
  go: ["goes", "went", "gone"],
  grow: ["grew", "grown"],
  have: ["has", "had"],
  hear: ["heard"],
  hold: ["held"],
  keep: ["kept"],
  know: ["knew", "known"],
  lead: ["led"],
  leave: ["left"],
  lend: ["lent"],
  let: ["let"],
  lose: ["lost"],
  make: ["made"],
  mean: ["meant"],
  meet: ["met"],
  pay: ["paid"],
  put: ["put"],
  read: ["read"],
  ride: ["rode", "ridden"],
  ring: ["rang", "rung"],
  rise: ["rose", "risen"],
  run: ["ran"],
  say: ["said"],
  see: ["saw", "seen"],
  sell: ["sold"],
  send: ["sent"],
  set: ["set"],
  show: ["showed", "shown"],
  sing: ["sang", "sung"],
  sit: ["sat"],
  sleep: ["slept"],
  speak: ["spoke", "spoken"],
  spend: ["spent"],
  stand: ["stood"],
  swim: ["swam", "swum"],
  take: ["took", "taken"],
  teach: ["taught"],
  tell: ["told"],
  think: ["thought"],
  throw: ["threw", "thrown"],
  understand: ["understood"],
  wake: ["woke", "woken"],
  wear: ["wore", "worn"],
  win: ["won"],
  write: ["wrote", "written"],
  // irregular plural nouns
  child: ["children"],
  man: ["men"],
  woman: ["women"],
  person: ["people"],
  foot: ["feet"],
  tooth: ["teeth"],
  // irregular comparatives
  good: ["better", "best"],
  bad: ["worse", "worst"],
  far: ["further", "farther", "furthest", "farthest"],
};

// Common regular English inflections of a base/dictionary word. Target words are taught in their
// base form ("decide", "study", "stop"), but the passage uses inflected forms ("decided",
// "studies", "stopping") - without this they would never highlight. Kept deliberately conservative
// (only well-formed regular patterns) so unrelated words are not highlighted by accident.
function inflectionsOf(base: string): Set<string> {
  const set = new Set<string>([base]);
  const last = base[base.length - 1];

  set.add(base + "s");
  set.add(base + "es");
  set.add(base + "ed");
  set.add(base + "ing");

  // silent-e verbs: decide -> decid(e) + ed/ing/es
  if (base.endsWith("e")) {
    const stem = base.slice(0, -1);
    set.add(stem + "ed");
    set.add(stem + "ing");
    set.add(stem + "es");
  }

  // consonant + y: study -> studies/studied, happy -> happier/happiest
  if (base.length > 1 && base.endsWith("y") && !isVowel(base[base.length - 2])) {
    const stem = base.slice(0, -1);
    set.add(stem + "ies");
    set.add(stem + "ied");
    set.add(stem + "ier");
    set.add(stem + "iest");
  }

  // doubled final consonant (CVC): stop -> stopping/stopped, big -> bigger/biggest
  if (
    base.length >= 3 &&
    !isVowel(last) &&
    isVowel(base[base.length - 2]) &&
    !isVowel(base[base.length - 3])
  ) {
    set.add(base + last + "ing");
    set.add(base + last + "ed");
    set.add(base + last + "er");
    set.add(base + last + "est");
  }

  for (const form of IRREGULAR_FORMS[base] ?? []) set.add(form);

  return set;
}

// True when a passage token is the given target part, allowing for inflection. Short function
// words ("a", "of", "to", "up") must match exactly so chunks like "make a decision" stay precise.
function wordMatchesPart(token: string, part: string): boolean {
  if (token === part) return true;
  if (part.length <= 2) return false;
  return inflectionsOf(part).has(token);
}

// Tries to match a target entry's word parts against the token stream starting at `start`.
// Whitespace tokens between the parts are skipped so multi-word chunks ("give up", "break the
// ice") match even though the passage is tokenised word-by-word. Returns the index of the last
// matched token (inclusive), or -1 if the entry does not start here.
function matchEntryAt(tokens: string[], start: number, parts: string[]): number {
  let ti = start;
  let end = -1;
  for (let pi = 0; pi < parts.length; pi++) {
    if (pi > 0) {
      while (ti < tokens.length && isGap(tokens[ti])) ti++;
    }
    if (ti >= tokens.length) return -1;
    if (!wordMatchesPart(cleanWord(tokens[ti]), parts[pi])) return -1;
    end = ti;
    ti++;
  }
  return end;
}

// A target word/chunk prepared for matching: its canonical key (for translation/tooltip lookups),
// the cleaned word parts to match, and the example to surface.
interface TargetEntry {
  key: string;
  parts: string[];
  example: string;
}

// One rendered passage piece: plain text, or a highlighted target with its lookup key + example.
type Segment = { text: string; word?: string; example?: string };

// Walks the passage once, greedily highlighting each target word/chunk (longest first so a
// multi-word chunk wins over its individual words). Single words highlight in any inflected form.
function buildSegments(passage: string, entries: TargetEntry[]): Segment[] {
  const tokens = passage.split(/(\s+)/);
  const out: Segment[] = [];
  let i = 0;
  while (i < tokens.length) {
    const token = tokens[i];
    if (isGap(token) || !cleanWord(token)) {
      out.push({ text: token });
      i++;
      continue;
    }
    let matched: { entry: TargetEntry; end: number } | null = null;
    for (const entry of entries) {
      const end = matchEntryAt(tokens, i, entry.parts);
      if (end >= 0) {
        matched = { entry, end };
        break;
      }
    }
    if (matched) {
      out.push({
        text: tokens.slice(i, matched.end + 1).join(""),
        word: matched.entry.key,
        example: matched.entry.example,
      });
      i = matched.end + 1;
    } else {
      out.push({ text: token });
      i++;
    }
  }
  return out;
}

// ─────────────────────────────── Component ───────────────────────────────

/** A topic's target vocabulary word with its vetted Uzbek meaning and example (rule 11). */
export interface HighlightWord {
  word: string;
  translation: string;
  exampleSentence?: string | null;
}

interface HighlightedPassageProps {
  /** The English text to render. */
  passage: string;
  /** The topic's target words to highlight - the SAME canonical set across every skill. */
  words: HighlightWord[];
  /** Class applied to the rendered <p> so each skill keeps its own typography. */
  className?: string;
  /** Keep the highlighted appearance but disable hover/click word details. */
  interactive?: boolean;
  /** Reading's inline dictionary replaces the default tooltip/sheet when supplied. */
  onWordSelect?: (word: string) => void;
  selectedWord?: string;
}

/**
 * Renders an English passage with the topic's target vocabulary words highlighted, exactly like
 * the Vocabulary topic reader - hover for a meaning/pronunciation popover, click for the full word
 * sheet. The same words come from one PostgreSQL source (VocabularyTopic.Words) so Reading,
 * Listening, Grammar and Writing all highlight an identical, consistent set (project mission:
 * interconnected skills around one topic). Matching tolerates inflected forms ("decide" → "decided")
 * and multi-word chunks ("give up", "make a decision").
 */
export function HighlightedPassage({ passage, words, className, interactive = true, onWordSelect, selectedWord }: HighlightedPassageProps) {
  // Lowercased lookups: which tokens are target words, and their meaning/example.
  const targets = useMemo(() => {
    const translation = new Map<string, string>();
    const example = new Map<string, string>();
    for (const w of words) {
      const key = w.word.toLowerCase();
      translation.set(key, w.translation);
      if (w.exampleSentence) example.set(key, w.exampleSentence);
    }
    return { translation, example };
  }, [words]);

  // Prepare the target words/chunks for matching: longest (multi-word) first so a chunk like
  // "give up" wins over the bare word "give". Keyed by the canonical word for tooltip lookups.
  const entries = useMemo<TargetEntry[]>(() => {
    const list: TargetEntry[] = [];
    for (const key of targets.translation.keys()) {
      const parts = key.split(/\s+/).map(cleanWord).filter(Boolean);
      if (parts.length === 0) continue;
      list.push({ key, parts, example: targets.example.get(key) ?? passage });
    }
    return list.sort((a, b) => b.parts.length - a.parts.length);
  }, [targets, passage]);

  // Highlight every target occurrence (inflected forms + multi-word chunks), preserving spacing.
  const segments = useMemo(() => buildSegments(passage, entries), [passage, entries]);

  // Shared hover-tooltip / click-sheet behaviour (identical to the "Yangi so'zlar" word cards).
  const { bind, overlay } = useWordPopover((word) => targets.translation.get(word) ?? null);

  return (
    <>
      <p className={className}>
        {segments.map((seg, i) => {
          if (seg.word === undefined) return <span key={i}>{seg.text}</span>;
          const example = seg.example ?? passage;
          if (!interactive) {
            return (
              <span key={i} className="hp-word">
                {seg.text}
              </span>
            );
          }
          if (onWordSelect) {
            return (
              <button key={i} type="button" className={`hp-word hp-word--interactive${selectedWord?.toLowerCase() === seg.word ? " is-selected" : ""}`} aria-pressed={selectedWord?.toLowerCase() === seg.word} onClick={() => onWordSelect(seg.word!)}>
                {seg.text}
              </button>
            );
          }
          return (
            <span
              key={i}
              {...bind(seg.word, example)}
              className="hp-word hp-word--interactive"
            >
              {seg.text}
            </span>
          );
        })}
      </p>
      {interactive && !onWordSelect ? overlay : null}
    </>
  );
}
