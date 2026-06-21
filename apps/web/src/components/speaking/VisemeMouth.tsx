import { VisemeFace, type VisemeFrameLike } from "./VisemeFace";

export type { VisemeFrameLike };

interface VisemeMouthProps {
  /** Viseme id/offset frames for the whole word (works for any word - see `VisemeFace`). */
  frames?: VisemeFrameLike[] | null;
  /** Changes on every new utterance to (re)start the animation; falsy = idle/neutral. */
  playKey?: string | number | null;
  /** Playback speed: 1 = native; < 1 plays the mouth slower (for "speak slowly"). */
  rate?: number;
  size?: number;
}

/** Animated mouth synced to a word's viseme track. Thin re-export of `VisemeFace` so existing
 * call sites keep the familiar `VisemeMouth` name. */
export function VisemeMouth(props: VisemeMouthProps) {
  return <VisemeFace {...props} />;
}
