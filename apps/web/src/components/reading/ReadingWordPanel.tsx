import { useEffect, useRef } from "react";
import { ChevronLeft, ChevronRight, Volume2 } from "lucide-react";
import { api } from "@/api/client";
import type { TargetWordDto } from "@/api/types";
import { playWordVoice } from "@/lib/audio";
import { useAsync } from "@/lib/useAsync";

export function ReadingWordPanel({ words, index, onSelect }: { words: TargetWordDto[]; index: number; onSelect: (index: number) => void }) {
  const entry = words[index];
  const word = entry?.word ?? "";
  const { data } = useAsync(() => api.speaking.wordDetail(word), [word], Boolean(word));
  const audio = useRef<HTMLAudioElement | null>(null);
  useEffect(() => () => { audio.current?.pause(); }, [word]);
  if (!entry) return <aside className="reading-word"><p>Bu matn uchun lug‘at hali qo‘shilmagan.</p></aside>;
  return (
    <aside className="reading-word" aria-label="Tanlangan so‘z" aria-live="polite">
      <span className="reading-word__position">TANLANGAN SO‘Z · {String(index + 1).padStart(2, "0")} / {words.length}</span>
      <h2>{word}</h2>
      {data?.ipa && <p className="reading-word__pronunciation">{data.ipa}</p>}
      <strong>{entry.translation}</strong>
      <button type="button" className="reading-word__audio" aria-label={`${word} talaffuzini tinglash`} onClick={() => { audio.current ??= new Audio(); playWordVoice(data?.audioBase64, word, audio.current); }}><Volume2 size={24} /></button>
      {entry.exampleSentence && <p className="reading-word__example">{entry.exampleSentence}</p>}
      {words.length > 1 && <nav className="reading-word__paging" aria-label="Lug‘at so‘zlari"><button type="button" aria-label="Oldingi so‘z" disabled={index === 0} onClick={() => onSelect(index - 1)}><ChevronLeft size={18} /></button><span>{index + 1} / {words.length}</span><button type="button" aria-label="Keyingi so‘z" disabled={index === words.length - 1} onClick={() => onSelect(index + 1)}><ChevronRight size={18} /></button></nav>}
    </aside>
  );
}
