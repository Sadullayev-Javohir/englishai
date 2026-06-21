import { useEffect, useRef, useState, type CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowLeft, Check, ChevronLeft, ChevronRight, LockKeyhole, RotateCw, Volume2 } from "lucide-react";
import type { VocabularyTopicDetailDto } from "@/api/types";
import { api } from "@/api/client";
import { useLessonSounds } from "@/components/lesson/useLessonSounds";
import { VocabularyAction, VocabularyProgress } from "./VocabularyChrome";
import { VocabularyWordPhoto } from "./VocabularyPhoto";
import { useVocabularyVoice } from "./useVocabularyVoice";
import "./VocabularyCardStack.css";

export interface VocabularyReturnState {
  topicId: string;
  index: number;
  learned: number[];
}

export function WordsStage({ topic, onPrevWord, onFinished, initialState, onProgress }: {
  topic: VocabularyTopicDetailDto; onPrevWord: () => void; onFinished: () => void; initialState?: VocabularyReturnState;
  onProgress?: (state: VocabularyReturnState) => void;
}) {
  const navigate = useNavigate();
  const words = topic.words;
  const [index, setIndex] = useState(Math.max(0, Math.min(initialState?.index ?? 0, words.length - 1)));
  const [learned, setLearned] = useState<Set<number>>(() => new Set(initialState?.learned.filter(i => i >= 0 && i < words.length) ?? []));
  const [flipped, setFlipped] = useState(false);
  const [hasTurned, setHasTurned] = useState(false);
  const [showReady, setShowReady] = useState(false);
  const [audioLoading, setAudioLoading] = useState(false);
  const activeRef = useRef(true);
  const playRequest = useRef(0);
  const word = words[index];
  const voice = useVocabularyVoice(word?.word ?? "");
  useLessonSounds(`${index}:${flipped}`);
  useEffect(() => {
    onProgress?.({ topicId: topic.id, index, learned: [...learned] });
  }, [index, learned, onProgress, topic.id]);
  useEffect(() => {
    activeRef.current = true;
    return () => { activeRef.current = false; playRequest.current += 1; };
  }, []);
  const allLearned = words.length > 0 && learned.size === words.length;
  function selectCard(next: number) {
    playRequest.current += 1;
    voice.stop();
    setIndex(next);
    setFlipped(false);
    setHasTurned(false);
    setAudioLoading(false);
    setShowReady(false);
  }
  async function listen() {
    if (!word) return;
    const request = ++playRequest.current;
    voice.stop();
    setAudioLoading(true);
    const detail = await api.speaking.wordDetail(word.word).catch(() => null);
    if (!activeRef.current || request !== playRequest.current) return;
    setAudioLoading(false);
    voice.play(detail?.audioBase64);
  }
  function flipCard() {
    playRequest.current += 1;
    setAudioLoading(false);
    voice.stop();
    setHasTurned(true);
    setFlipped(value => !value);
  }
  function markLearned() {
    const nextLearned = new Set(learned).add(index);
    playRequest.current += 1;
    voice.stop();
    setLearned(nextLearned);
    if (nextLearned.size === words.length) setShowReady(true);
    else selectCard(words.findIndex((_, i) => !nextLearned.has(i)));
  }
  if (!word) return <section className="vocabulary-state"><h1>Hozircha so‘zlar yo‘q</h1><VocabularyAction onClick={onPrevWord}>Matnga qaytish</VocabularyAction></section>;
  const rows = Math.ceil(words.length / 2);
  return (
    <section className="vocabulary-flashcards" data-pen-screen={showReady ? "16C" : flipped ? "16B" : "16"}>
      <header className="vocabulary-flashcards__heading">
        <VocabularyProgress step="flashcards" onBack={onPrevWord} />
        <div><h1>{showReady ? "Testga tayyorsiz!" : "Flashcardlar"}</h1><span className={`vocabulary-tag${allLearned ? " vocabulary-tag--green" : ""}`} aria-live="polite">{learned.size} / {words.length}</span></div>
      </header>
      <div className="vocabulary-flashcards__workspace">
        <aside className="vocabulary-collection" aria-label="Sizning to‘plamingiz">
          <div className="vocabulary-collection__heading"><h2>SIZNING TO‘PLAMINGIZ</h2><span>{learned.size} / {words.length}</span></div>
          <div className="vocabulary-collection__rows">
            {Array.from({ length: rows }, (_, row) => (
              <div className="vocabulary-collection__row" key={row}>
                {[row, row + rows].filter(i => i < words.length).map(i => (
                  <button type="button" key={i} onClick={() => selectCard(i)}
                    className={`vocabulary-collection__word vocabulary-collection__word--${i % 3}${learned.has(i) ? " is-learned" : ""}${index === i && !showReady ? " is-active" : ""}`}
                    aria-label={`${i + 1}. ${words[i].translation}${learned.has(i) ? " — o‘rganildi" : ""}`} aria-current={index === i && !showReady ? "step" : undefined}>
                    <span>{String(i + 1).padStart(2, "0")}</span><strong>{words[i].translation}</strong>{learned.has(i) && <Check size={13} />}
                  </button>
                ))}
              </div>
            ))}
          </div>
          <p>Istalgan kartani oching.<br />Test uchun barchasini o‘rganing.</p>
          <button type="button" className="vocabulary-collection__back" onClick={onPrevWord}><ArrowLeft size={14} />Matnga qaytish</button>
        </aside>
        <div className="vocabulary-reader">
          {showReady ? (
            <>
              <div className="vocabulary-reader__ready">
                <img src="/assets/play/parrot.svg" alt="" width={216} height={180} />
                <h2>{words.length} ta karta<br />o‘rganildi!</h2><p>Endi bilimingizni sinab ko‘ring.</p>
              </div>
              <VocabularyAction onClick={onFinished}>Testga o‘tish</VocabularyAction>
              <p className="vocabulary-reader__ready-note">{learned.size}/{words.length} karta o‘rganildi. Test ochiq.</p>
              <button type="button" className="vocabulary-reader__review" onClick={() => selectCard(0)}>Kartalarni yana ko‘rish</button>
            </>
          ) : (
            <>
              <div className="vocabulary-card-stack vocabulary-card-stack--deck"
                data-remaining-cards={words.length - index - 1}
                style={{ "--v-deck-count": words.length - index - 1 } as CSSProperties}>
                <div className="vocabulary-card-stack__layers" aria-hidden="true">
                  {words.slice(index + 1).map((stackedWord, offset) => (
                    <div key={index + offset + 1} className="vocabulary-card-stack__card"
                      data-stacked-index={index + offset + 1}
                      style={{ "--v-card-depth": offset + 1, zIndex: words.length - offset } as CSSProperties}>
                      {offset === 0 && <div className="vocabulary-flashcard__side vocabulary-card-stack__preview" data-preview-word={stackedWord.word}>
                        <div className="vocabulary-flashcard__top">
                          <span className="vocabulary-tag">{String(index + 2).padStart(2, "0")} / {words.length}</span>
                          <RotateCw size={18} />
                        </div>
                        <div className="vocabulary-flashcard__face">
                          <span className="vocabulary-flashcard__picture"><VocabularyWordPhoto word={stackedWord} topic={topic} loading="eager" /></span>
                          <span className="vocabulary-flashcard__hint"><RotateCw size={16} />Bosib aylantiring</span>
                        </div>
                      </div>}
                    </div>
                  ))}
                </div>
                <div key={index} className={`vocabulary-flashcard${flipped ? " is-back" : ""}`} data-has-turned={hasTurned}>
                  <div className="vocabulary-flashcard__rotor">
                    <div className="vocabulary-flashcard__side vocabulary-flashcard__side--front" aria-hidden={flipped}>
                      <div className="vocabulary-flashcard__top"><span className="vocabulary-tag">{String(index + 1).padStart(2, "0")} / {words.length}</span><RotateCw size={18} /></div>
                      <button type="button" tabIndex={flipped ? -1 : 0} className="vocabulary-flashcard__face" aria-label="Kartani aylantirish" onClick={flipCard} data-card-face="front">
                        <span className="vocabulary-flashcard__picture"><VocabularyWordPhoto word={word} topic={topic} loading="eager" /></span>
                        <span className="vocabulary-flashcard__hint"><RotateCw size={16} />Bosib aylantiring</span>
                      </button>
                    </div>
                    <div className="vocabulary-flashcard__side vocabulary-flashcard__side--back" aria-hidden={!flipped}>
                      <div className="vocabulary-flashcard__top"><span className="vocabulary-tag">{String(index + 1).padStart(2, "0")} / {words.length}</span>
                        <button type="button" tabIndex={flipped ? 0 : -1} className={`vocabulary-flashcard__sound${voice.state === "playing" ? " is-playing" : ""}`} onClick={() => void listen()} aria-label={`${word.word} — tinglash`} aria-busy={audioLoading || voice.state === "loading"}><Volume2 size={24} /></button>
                      </div>
                      <button type="button" tabIndex={flipped ? 0 : -1} className="vocabulary-flashcard__face" aria-label="Kartani aylantirish" onClick={flipCard} data-card-face="back">
                      <span className="vocabulary-flashcard__word">{word.word}</span>
                      <span className="vocabulary-flashcard__translation">{word.translation}</span>
                      <span className="vocabulary-flashcard__ipa">{word.ipa || "Talaffuz mavjud emas"}</span>
                      <span className="vocabulary-flashcard__example"><small>MISOL GAP</small><span>{word.exampleSentence || "Bu so‘z uchun misol gap hali tayyor emas."}</span></span>
                      <span className="vocabulary-flashcard__hint">Bosib rasmga qayting</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
              <nav className="vocabulary-card-nav" aria-label="Flashcard navigatsiyasi">
                <button type="button" disabled={index === 0} onClick={() => selectCard(index - 1)} aria-label="Oldingi karta"><ChevronLeft size={20} /></button>
                <button type="button" className="vocabulary-card-nav__primary" onClick={() => flipped ? markLearned() : flipCard()}>{flipped ? "O‘rgandim →" : "Aylantirish"}</button>
                <button type="button" disabled={index === words.length - 1} onClick={() => selectCard(index + 1)} aria-label="Keyingi karta"><ChevronRight size={20} /></button>
              </nav>
              {allLearned ? <VocabularyAction onClick={() => setShowReady(true)}>Testga tayyorsiz!</VocabularyAction> : <button type="button" className="vocabulary-reader__locked" disabled><LockKeyhole size={20} /><span><strong>Test hozircha yopiq</strong><small>{words.length}/{words.length} karta o‘rganilgach ochiladi</small></span></button>}
              <button type="button" className="vocabulary-reader__pronunciation" onClick={() => navigate(`/app/vocabulary/topic/${topic.id}/pronunciation/${index}`, { state: { vocabularyReturn: { topicId: topic.id, index, learned: [...learned] } } })}>Talaffuzni mashq qilish</button>
              {voice.state === "error" && <p className="vocabulary-reader__audio-error" role="status">Ovozni ijro qilib bo‘lmadi. Qayta tinglang.</p>}
            </>
          )}
        </div>
      </div>
    </section>
  );
}
