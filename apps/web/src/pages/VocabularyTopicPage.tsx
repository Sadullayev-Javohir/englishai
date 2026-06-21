import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useLocation, useParams } from "react-router-dom";
import { BookOpen, CheckCircle2, ChevronDown, Languages, Sparkles, Volume2, XCircle } from "lucide-react";
import { api } from "@/api/client";
import { ProductEventType, SkillType, type RecordTopicModuleScoreResult, type TopicWordDto, type VocabularyTopicDetailDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import { uz } from "@/content/uz";
import { cefrShort } from "@/lib/labels";
import { lessonBackTarget, lessonOriginFrom } from "@/lib/lessonNavigation";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { publishAssistantContext } from "@/components/assistantContext";
import { HighlightedPassage } from "@/components/HighlightedPassage";
import { useLessonSounds } from "@/components/lesson/useLessonSounds";
import { useAnswerAutoAdvance } from "@/components/lesson/useAnswerAutoAdvance";
import { VocabularyQuizOption } from "@/components/vocabulary/VocabularyQuizOption";
import { VocabularyAction, VocabularyHeading, VocabularyProgress } from "@/components/vocabulary/VocabularyChrome";
import { VocabularyPhoto } from "@/components/vocabulary/VocabularyPhoto";
import { WordsStage, type VocabularyReturnState } from "@/components/vocabulary/VocabularyFlashcards";
import { VocabularyLessonResult } from "@/components/vocabulary/VocabularyLessonResult";
import { buildVocabularyExercises, type VocabularyExercise } from "./vocabularyExercises";
import "./VocabularyTopicPage.css";
import type { VocabularyLessonStep } from "@/components/vocabulary/vocabularyLessonSteps";

export { WordsStage };
type Stage = "hub" | "text" | "words" | "practice";
export function restartVocabularyTopicLesson(_progressScope: string, setStage: (stage: Stage) => void) { setStage("hub"); }
function isValidTopicWord(word: TopicWordDto | null | undefined): word is TopicWordDto { return Boolean(word?.word?.trim() && word.translation?.trim()); }

/** Only the explicit pronunciation round-trip resumes cards; ordinary visits start at the hub. */
function returnState(state: unknown, topicId: string): VocabularyReturnState | undefined {
  const value = (state as { vocabularyReturn?: VocabularyReturnState } | null)?.vocabularyReturn;
  return value?.topicId === topicId && Number.isInteger(value.index) && Array.isArray(value.learned) && value.learned.every(Number.isInteger) ? value : undefined;
}

export function VocabularyTopicPage() {
  const { topicId = "" } = useParams();
  const location = useLocation();
  const resume = returnState(location.state, topicId);
  const [stage, setStage] = useState<Stage>(resume ? "words" : "hub");
  const [cardProgress, setCardProgress] = useState<VocabularyReturnState | undefined>(resume);
  const [pollCount, setPollCount] = useState(0);
  const { data, loading, error, reload } = useAsync(() => api.vocabulary.topic(topicId), [topicId, pollCount]);
  const topic = useMemo(() => data ? { ...data, words: (data.words ?? []).filter(isValidTopicWord) } : null, [data]);
  useLessonSounds(stage);
  useDocumentTitle(topic?.title, uz.nav.vocabulary);
  useEffect(() => {
    if (!data || data.isReady || loading) return;
    const timer = window.setTimeout(() => setPollCount(count => count + 1), 4000);
    return () => window.clearTimeout(timer);
  }, [data, loading]);
  useEffect(() => {
    if (topic?.isReady) void api.analytics.track(getLearnerId(), ProductEventType.TopicOpened, topic.id).catch(() => undefined);
  }, [topic?.id, topic?.isReady]);
  useEffect(() => {
    if (!topic?.isReady) return;
    return publishAssistantContext({ area: "vocabulary", resourceId: topic.id, title: `${topic.title} — ${topic.titleUz}`,
      context: `${topic.passage}\n${topic.words.map(word => `${word.word} — ${word.translation}`).join("\n")}`,
      focusText: "", route: `/app/vocabulary/topic/${topic.id}`, stage });
  }, [stage, topic]);
  if (loading) return <ModulePageLoader icon="bookmarks" accent="green" />;
  if (error || !topic) return <section className="vocabulary-state" role="alert"><h1>Ma’lumotni yuklab bo‘lmadi</h1><VocabularyAction onClick={reload}>Qayta urinish</VocabularyAction></section>;
  if (!topic.isReady) return <section className="vocabulary-state" role="status"><h1>{uz.vocabularyTopics.pending}</h1><VocabularyAction onClick={reload}>Qayta tekshirish</VocabularyAction></section>;
  const backTarget = lessonBackTarget("vocabulary", lessonOriginFrom(location));
  return <div className={`vocabulary-topic vocabulary-topic--${stage}`} data-module="vocabulary">
    {stage === "hub" && <HubStage topic={topic} backTarget={backTarget} onStart={() => setStage("text")} />}
    {stage === "text" && <TextStage topic={topic} onBack={() => setStage("hub")} onToWords={() => setStage("words")} />}
    {stage === "words" && <WordsStage key={topic.id} topic={topic} initialState={cardProgress} onProgress={setCardProgress} onPrevWord={() => setStage("text")} onFinished={() => setStage("practice")} />}
    {stage === "practice" && <PracticeStage key={topic.id} topic={topic} onBack={() => setStage("words")} onRestart={() => { setCardProgress(undefined); setStage("hub"); }} />}
  </div>;
}

export function VocabularySlideShell({ title, showTitle = true, step, hearts = 5, backTarget, onBack, mode = "focus", footer, children }: {
  title: string; showTitle?: boolean; step: VocabularyLessonStep; hearts?: number; xp?: number; backTarget?: string; onBack?: () => void;
  mode?: "focus" | "flow"; footer: ReactNode; children: ReactNode;
}) {
  return <section className={`vocabulary-slide vocabulary-slide--${mode}`}>
    <VocabularyProgress step={step} hearts={hearts} backTarget={backTarget} onBack={onBack} />
    {showTitle && <h1>{title}</h1>}
    {children}
    <footer className="vocabulary-slide__actions">{footer}</footer>
  </section>;
}

function HubStage({ topic, backTarget, onStart }: { topic: VocabularyTopicDetailDto; backTarget: string; onStart: () => void }) {
  const knownMother = topic.title.toLowerCase() === "my mother";
  return <div data-pen-screen="12"><VocabularySlideShell title={topic.title} showTitle={false} step="intro" backTarget={backTarget} footer={<VocabularyAction onClick={onStart}>Darsni boshlash</VocabularyAction>}>
    <div className="vocabulary-intro">
      <VocabularyPhoto topic={topic} />
      <div className="vocabulary-intro__promise"><span className="vocabulary-tag">{cefrShort(topic.level)} · {knownMother ? "OILA" : topic.category.replace(/_/g, " ").toLocaleUpperCase("uz")}</span>
        <h1>{topic.title}</h1><p>{knownMother ? "Sizni qo‘llab-quvvatlaydigan inson haqida gapiring." : topic.titleUz}</p>
        <div className="vocabulary-intro__facts"><span className="vocabulary-tag vocabulary-tag--green">{topic.words.length} so‘z</span><span className="vocabulary-tag" title="Taxminiy dars davomiyligi">{Math.max(1, Math.ceil(topic.words.length * .3))} daqiqa</span></div>
      </div>
    </div>
    <ol className="vocabulary-intro__steps"><li><BookOpen size={22} />Matnda ko‘ring</li><li><Volume2 size={22} />Tinglang va ayting</li><li><Sparkles size={22} />Xotiradan toping</li></ol>
  </VocabularySlideShell></div>;
}

function TextStage({ topic, onBack, onToWords }: { topic: VocabularyTopicDetailDto; onBack: () => void; onToWords: () => void }) {
  const [translated, setTranslated] = useState(false);
  const translation = useAsync(() => api.vocabulary.passageTranslation(topic.id), [topic.id], translated);
  return <div data-pen-screen="13"><VocabularySlideShell title="Avval, kichik hikoya." showTitle={false} step="text" onBack={onBack} mode="flow" footer={<VocabularyAction onClick={onToWords}>So‘zlarga o‘tamiz</VocabularyAction>}>
    <VocabularyHeading title="Avval, kichik hikoya." subtitle={`Ajratilgan ${topic.words.length} ta so‘zga e’tibor bering.`} />
    <div className="vocabulary-reading">
      <div className="vocabulary-reading__title"><span>{topic.title.toLocaleUpperCase("en")}</span><span className="vocabulary-tag">{topic.words.length} ta so‘z</span></div>
      <div className="vocabulary-reading__paragraphs">{topic.passage.split(/\n\s*\n/).map((paragraph, index) => <HighlightedPassage key={index} passage={paragraph} words={topic.words} className="vocabulary-reading__passage" />)}</div>
      <button type="button" className="vocabulary-reading__translate" aria-expanded={translated} aria-controls="vocabulary-translation" onClick={() => setTranslated(!translated)}><Languages size={20} />O‘zbekcha ma’nosi<ChevronDown size={18} /></button>
      {translated && <div id="vocabulary-translation" className="vocabulary-reading__translation" aria-live="polite">
        {translation.loading ? "Tarjima tayyorlanmoqda…" : translation.error ? <><p>Tarjimani yuklab bo‘lmadi.</p><button type="button" onClick={translation.reload}>Qayta urinish</button></> : translation.data?.sentences.map((sentence, index) => <p key={index}>{sentence.uzbek || sentence.english}</p>)}
      </div>}
    </div>
  </VocabularySlideShell></div>;
}

function PracticeStage({ topic, onBack, onRestart }: { topic: VocabularyTopicDetailDto; onBack: () => void; onRestart: () => void }) {
  const learnerId = getLearnerId();
  const [hearts, setHearts] = useState(5);
  const [correctCount, setCorrectCount] = useState(0);
  const [index, setIndex] = useState(0);
  const [finished, setFinished] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState(false);
  const [result, setResult] = useState<RecordTopicModuleScoreResult | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const startedAt = useRef(Date.now());
  const exercises = useMemo(() => buildVocabularyExercises(topic.words), [topic.words]);
  const total = exercises.length;
  const submitStarted = useRef(false);
  const { playAnswer, playComplete } = useLessonSounds(finished ? "practice-result" : `practice-${index}`);
  const submitResult = useCallback(async () => {
    if (submitStarted.current || total === 0) return;
    submitStarted.current = true;
    setSubmitting(true); setSubmitError(false);
    try { setResult(await api.vocabulary.recordTopicModuleScore(learnerId, topic.id, SkillType.Vocabulary, Math.round(correctCount / total * 100))); }
    catch { setSubmitError(true); submitStarted.current = false; }
    finally { setSubmitting(false); }
  }, [correctCount, learnerId, topic.id, total]);
  useEffect(() => { if (finished && !result && !submitError) void submitResult(); }, [finished, result, submitError, submitResult]);
  useEffect(() => { if (result) playComplete(); }, [result, playComplete]);
  const next = useCallback(() => {
    if (index === total - 1) { setElapsedSeconds(Math.max(1, Math.round((Date.now() - startedAt.current) / 1000))); setFinished(true); }
    else setIndex(i => i + 1);
  }, [index, total]);
  const onResult = useCallback((correct: boolean) => {
    playAnswer(correct);
    if (correct) setCorrectCount(count => count + 1);
    else setHearts(count => Math.max(0, count - 1));
  }, [playAnswer]);
  if (!total) return <section className="vocabulary-state"><h1>Hozircha so‘zlar yo‘q</h1><VocabularyAction onClick={onBack}>Kartalarga qaytish</VocabularyAction></section>;
  if (hearts === 0) return <section className="vocabulary-state" role="status"><h1>{uz.vocabularyTopics.heartsEmpty}</h1><VocabularyAction onClick={onRestart}>{uz.vocabularyTopics.retryLesson}</VocabularyAction></section>;
  if (finished) {
    if (submitting || (!result && !submitError)) return <ModulePageLoader icon="bookmarks" accent="green" />;
    if (submitError && !result) return <section className="vocabulary-state" role="alert"><h1>{uz.vocabularyTopics.rewardSaveError}</h1><VocabularyAction onClick={() => void submitResult()} disabled={submitting}>Qayta urinish</VocabularyAction></section>;
    if (result) return <VocabularyLessonResult topic={topic} correctCount={correctCount} total={total} elapsedSeconds={elapsedSeconds} result={result} onRetry={onRestart} />;
  }
  const exercise = exercises[index];
  return <section className="vocabulary-practice">
    <VocabularyProgress step="test" hearts={hearts} onBack={onBack} />
    <ChoiceExercise key={index} exercise={exercise} accent={index} word={topic.words.find(word => word.word === exercise.word)} onResult={onResult} onNext={next} />
  </section>;
}

/** A wrong typed answer remains visible until the learner retries; first-attempt score is immutable. */
export function ChoiceExercise({ exercise, accent, word, onResult, onNext }: {
  exercise: VocabularyExercise; accent: number; word?: TopicWordDto; onResult: (correct: boolean) => void; onNext: () => void;
}) {
  void accent;
  const isMc = exercise.kind === "mc";
  const [chosen, setChosen] = useState<number | null>(null);
  const [typed, setTyped] = useState("");
  const [revealed, setRevealed] = useState(false);
  const [correct, setCorrect] = useState(false);
  const [retrying, setRetrying] = useState(false);
  const gradedRef = useRef(false);
  const autoAdvance = useAnswerAutoAdvance({ enabled: revealed && (isMc || correct), onAdvance: onNext, resetKey: exercise });
  useEffect(() => { setChosen(null); setTyped(""); setRevealed(false); setCorrect(false); setRetrying(false); gradedRef.current = false; }, [exercise]);
  function grade(answer: string) {
    if (revealed) return;
    const ok = answer.trim().toLocaleLowerCase("en") === exercise.answer.trim().toLocaleLowerCase("en");
    setCorrect(ok); setRevealed(true);
    if (!gradedRef.current) { gradedRef.current = true; onResult(ok); }
  }
  const wrongTyped = !isMc && revealed && !correct;
  const translation = isMc && exercise.dir === "en-uz" ? exercise.answer : exercise.prompt;
  return <section className={`vocabulary-exercise vocabulary-exercise--${isMc ? "mc" : "typing"}`} data-answered={revealed} data-pen-screen={wrongTyped ? "18" : "17"}>
    <VocabularyHeading title={wrongTyped ? "Deyarli topdingiz!" : "Xotiradan toping."} subtitle={wrongTyped ? undefined : isMc ? "Bu so‘zning tarjimasini toping." : "Bu so‘z inglizchada qanday yoziladi?"} />
    {wrongTyped ? <>
      <div className="vocabulary-correction" role="status"><p>{exercise.prompt} →</p><del>{typed || "Eslay olmadim"}</del><strong>{exercise.answer}</strong>{word?.ipa && <span>{word.ipa}</span>}</div>
      <div className="vocabulary-feedback vocabulary-feedback--wrong"><h2><Sparkles size={24} />{typed && typed.length === exercise.answer.length && [...typed].filter((letter, index) => letter !== exercise.answer[index]).length === 1 ? "Bitta harfga e’tibor bering." : "Yozilishiga e’tibor bering."}</h2><p>“{typed || "…"}” emas, “{exercise.answer}” deb yoziladi.</p></div>
      {word?.exampleSentence && <p className="vocabulary-correction__example">“{word.exampleSentence}”</p>}
      <VocabularyAction onClick={() => { setRevealed(false); setTyped(""); setRetrying(true); }}>Qayta urinib ko‘rish</VocabularyAction>
    </> : <>
      <div className="vocabulary-recall"><span className="vocabulary-tag">{isMc ? "ENGLISH → O‘ZBEKCHA" : "O‘ZBEKCHA → ENGLISH"}</span><h2>{exercise.prompt}</h2></div>
      {isMc ? <div className="vocabulary-topic__test-options lesson-quiz-options">{exercise.options.map((option, index) => {
        const state = !revealed ? "idle" : option === exercise.answer ? "correct" : chosen === index ? "wrong" : "dim";
        return <VocabularyQuizOption key={index} index={index} label={option} state={state} selected={chosen === index} onSelect={() => { setChosen(index); grade(option); }} disabled={revealed} />;
      })}</div> : <form className="vocabulary-typing" onSubmit={event => { event.preventDefault(); if (typed.trim()) grade(typed); }}>
        <label htmlFor="vocabulary-answer">Javobingiz</label>
        <div className="vocabulary-answer-field" data-answer-state={revealed && correct ? "correct" : "idle"}>
          <input id="vocabulary-answer" aria-describedby="vocabulary-answer-hint" autoFocus value={typed} onChange={event => setTyped(event.target.value)} disabled={revealed} autoComplete="off" autoCapitalize="none" spellCheck={false} placeholder={`${exercise.answer.slice(0, 3)}…`} />
          {revealed && correct && <CheckCircle2 size={24} aria-hidden="true" />}
        </div>
        <p id="vocabulary-answer-hint">Yordam: {exercise.answer.length} ta harf{retrying ? " · Yozib mustahkamlang" : ""}</p>
        {!revealed && <VocabularyAction type="submit" disabled={!typed.trim()}>Tekshirish</VocabularyAction>}
      </form>}
      {revealed ? <div className="vocabulary-exercise__feedback" role="status"><div className={`vocabulary-feedback${correct ? "" : " vocabulary-feedback--wrong"}`}><h2>{correct ? <CheckCircle2 size={24} /> : <XCircle size={24} />}{correct ? "To‘ppa-to‘g‘ri!" : "Birga eslab qolamiz."}</h2><p>{exercise.word} — {translation}</p></div><VocabularyAction onClick={autoAdvance.advance}>Keyingi</VocabularyAction></div> : !isMc && <button type="button" className="vocabulary-secondary" onClick={() => grade("")}>Eslay olmadim</button>}
    </>}
  </section>;
}
