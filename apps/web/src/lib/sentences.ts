// Splits an English teaching text into sentences for per-sentence translation. Keeps each
// sentence's trailing punctuation and the whitespace that follows, so re-joining the pieces
// reproduces the original text exactly (the renderer relies on that to preserve spacing).
//
// This is deliberately lightweight (no NLP): it breaks on ., ! and ? followed by whitespace,
// while avoiding the most common false splits - a digit-dot-digit decimal (3.5) and a few
// frequent abbreviations (Mr., Mrs., etc.). Good enough for short lesson texts.
const ABBREVIATIONS = new Set([
  "mr", "mrs", "ms", "dr", "prof", "sr", "jr", "st", "vs", "etc", "e.g", "i.e",
]);

export interface SentencePiece {
  /** The sentence text including its trailing punctuation (trimmed of surrounding spaces). */
  text: string;
  /** Trailing whitespace that followed the sentence in the source (preserved on re-render). */
  trailing: string;
}

export function splitSentences(text: string): SentencePiece[] {
  if (!text) return [];

  const pieces: SentencePiece[] = [];
  let start = 0;

  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (ch !== "." && ch !== "!" && ch !== "?") continue;

    const next = text[i + 1];
    // Must be at end of string or followed by whitespace to count as a sentence boundary.
    if (next !== undefined && !/\s/.test(next)) continue;

    // Skip decimals like "3.5".
    if (ch === "." && /\d/.test(text[i - 1] ?? "") && /\d/.test(next ?? "")) continue;

    // Skip common abbreviations (look at the word immediately before the dot).
    if (ch === ".") {
      const word = text.slice(start, i).split(/[\s(]/).pop()?.toLowerCase() ?? "";
      if (ABBREVIATIONS.has(word)) continue;
    }

    let end = i + 1;
    // Absorb any run of consecutive closing punctuation/quotes (e.g. ?!, ."  ).
    while (end < text.length && /["')\].!?]/.test(text[end])) end++;

    const raw = text.slice(start, end);
    const trimmed = raw.trim();
    if (trimmed.length > 0) {
      // Capture the whitespace that follows so re-joining reproduces the original spacing.
      let ws = end;
      while (ws < text.length && /\s/.test(text[ws])) ws++;
      pieces.push({ text: trimmed, trailing: text.slice(end, ws) });
      start = ws;
      i = ws - 1;
    }
  }

  const tail = text.slice(start).trim();
  if (tail.length > 0) pieces.push({ text: tail, trailing: "" });

  return pieces;
}
