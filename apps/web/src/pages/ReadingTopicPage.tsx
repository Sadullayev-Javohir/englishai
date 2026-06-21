import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ArrowRight, Check, Circle, CircleCheck, Heart, XCircle } from "lucide-react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { api } from "@/api/client";
import { ProductEventType } from "@/api/types";
import type { ReadingAnswerCheckDto, ReadingPassageDto, ReadingQuizAnswer, ReadingQuizResultDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import { cefrShort } from "@/lib/labels";
import { lessonBackTarget, lessonOriginFrom } from "@/lib/lessonNavigation";
import { useLessonProgress } from "@/lib/lessonProgress";
import { publishAssistantContext } from "@/components/assistantContext";
import { HighlightedPassage } from "@/components/HighlightedPassage";
import { ReadingWordPanel } from "@/components/reading/ReadingWordPanel";
import { ReadingResult } from "@/components/reading/ReadingResult";
import { READING_LESSON_STEPS } from "@/components/reading/readingLessonSteps";
import { LessonProgress } from "@/components/lesson/LessonProgress";
import { useLessonSounds } from "@/components/lesson/useLessonSounds";
import "./ReadingTopicPage.css";

// Preserve the two-heart product rule; Pen's five-heart example is not a rule change.
const MAX_HEARTS = 2;
type Stage = "text" | "practice";
type GradedAnswer = ReadingQuizAnswer & { correct: boolean };

export function ReadingTopicPage() {
  const { topicId = "" } = useParams();
  return <ReadingLesson key={topicId} topicId={topicId} />;
}

function ReadingLesson({ topicId }: { topicId: string }) {
  const navigate = useNavigate();
  const location = useLocation();
  const backTarget = lessonBackTarget("reading", lessonOriginFrom(location));
  const [stage, setStage] = useState<Stage>("text");
  const [attempt, setAttempt] = useLessonProgress<GradedAnswer[]>(`reading-pen.${topicId}.answers`, []);
  const [index, setIndex] = useState(0);
  const [chosen, setChosen] = useState<number | null>(null);
  const [feedback, setFeedback] = useState<ReadingAnswerCheckDto | null>(null);
  const [result, setResult] = useState<ReadingQuizResultDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<"check" | "submit" | null>(null);
  const [reviewing, setReviewing] = useState(false);
  const [poll, setPoll] = useState(0);
  const request = useRef(false);
  const started = useRef(Date.now());
  const { data: topic, loading, error: loadError, reload } = useAsync(() => api.reading.passage(topicId), [topicId, poll]);
  const bodyRef = useRef<HTMLDivElement>(null);
  const { playAnswer, playComplete } = useLessonSounds(result ? "result" : `${stage}-${index}-${Boolean(feedback)}`);
  useDocumentTitle(topic?.title, "Reading");
  const answers = useMemo(() => attempt.filter(answer => topic?.questions.some(question => question.id === answer.questionId)), [attempt, topic]);
  const hearts = Math.max(0, MAX_HEARTS - answers.filter(answer => !answer.correct).length);
  const xp = answers.filter(answer => answer.correct).length * 10;
  const question = topic?.questions[index];
  useEffect(() => { if (topic && !topic.isReady) { const id = window.setTimeout(() => setPoll(value => value + 1), 4000); return () => window.clearTimeout(id); } }, [topic, poll]);
  useEffect(() => { if (topic?.isReady) void api.analytics.track(getLearnerId(), ProductEventType.TopicOpened, topic.topicId).catch(() => undefined); }, [topic?.isReady, topic?.topicId]);
  useEffect(() => topic?.isReady ? publishAssistantContext({ area:"reading", resourceId:topic.topicId, title:topic.title, context:`${topic.body}\n${topic.targetWords.map(word => `${word.word} — ${word.translation}`).join(" | ")}`, focusText:question?.prompt ?? "", route:`/reading/topic/${topicId}`, stage:result ? "result" : feedback ? "feedback" : stage }) : undefined, [topic, topicId, stage, feedback, result, question?.prompt]);
  useEffect(() => { bodyRef.current?.scrollTo?.({ top:0 }); }, [stage, index, feedback, result]);
  const resetQuestion = useCallback(() => { setChosen(null); setFeedback(null); setError(null); }, []);
  const restart = () => { setAttempt([]); setIndex(0); setStage("text"); setResult(null); setReviewing(false); resetQuestion(); started.current = Date.now(); };
  function startPractice() {
    if (!reviewing) {
      const next = topic?.questions.findIndex(item => !answers.some(answer => answer.questionId === item.id)) ?? 0;
      // A finished persisted attempt is not a partially resumed quiz.
      if (next < 0) { setAttempt([]); started.current = Date.now(); }
      setIndex(Math.max(0, next)); resetQuestion();
    }
    setStage("practice"); setReviewing(false);
  }
  async function check() {
    if (!question || chosen === null || request.current || feedback) return;
    request.current = true; setBusy(true); setError(null);
    try {
      const checked = await api.reading.checkAnswer(topicId, question.id, chosen);
      setFeedback(checked);
      setAttempt(current => [...current.filter(answer => answer.questionId !== question.id), { questionId:question.id, selectedOptionIndex:chosen, correct:checked.isCorrect }]);
      playAnswer(checked.isCorrect);
    } catch { setError("check"); }
    finally { request.current = false; setBusy(false); }
  }
  async function next() {
    if (!topic || request.current) return;
    if (index < topic.questions.length - 1 && hearts > 0) { setIndex(value => value + 1); resetQuestion(); return; }
    if (hearts === 0) { setFeedback(null); return; }
    request.current = true; setBusy(true); setError(null);
    try {
      const submitted = await api.reading.submit(topicId, getLearnerId(), answers.map(({ questionId, selectedOptionIndex }) => ({ questionId, selectedOptionIndex })));
      setResult(submitted); playComplete();
    } catch { setError("submit"); }
    finally { request.current = false; setBusy(false); }
  }

  if (loading) return <div className="reading-state" role="status">Matn yuklanmoqda…</div>;
  if (loadError || !topic) return <div className="reading-state" role="alert"><h1>Matnni yuklab bo‘lmadi.</h1><p>Ulanishingizni tekshirib, qayta urinib ko‘ring.</p><button type="button" className="reading-primary" onClick={reload}>Qayta urinish</button></div>;
  if (!topic.isReady) return <div className="reading-state" role="status"><h1>Reading darsi tayyorlanmoqda…</h1><button className="reading-secondary" onClick={reload}>Yangilash</button></div>;
  const exhausted = hearts === 0 && !feedback;
  const screen = result ? "35" : feedback && stage === "practice" ? "34" : stage === "practice" ? "33" : "32";
  const progressStep = result ? "result" : stage;
  return (
    <div className={`reading-lesson reading-lesson--${result ? "result" : stage}`} data-module="reading" data-pen-screen={screen}>
      <LessonProgress
        steps={READING_LESSON_STEPS}
        step={progressStep}
        complete={Boolean(result)}
        hearts={result ? undefined : hearts}
        heartLabel={`${hearts} / ${MAX_HEARTS} yurak`}
        onBack={() => navigate(backTarget)}
        backLabel="Mavzularga qaytish"
        backDisabled={busy}
        classPrefix="reading-progress"
      />
      <div className="reading-lesson__body" data-lesson-stage-body ref={bodyRef}>
        {result ? <ReadingResult topic={topic} result={result} xp={xp} elapsedSeconds={Math.round((Date.now() - started.current) / 1000)} onRetry={restart} />
          : stage === "text" ? <ReadingText topic={topic} />
          : exhausted ? <section className="reading-state"><Heart size={48} /><h1>Yuraklar tugadi</h1><p>Matnni yana bir bor o‘qib, qayta urinib ko‘ring.</p></section>
          : !question ? <section className="reading-state"><h1>Bu matn uchun savollar hali tayyor emas.</h1></section>
          : feedback ? <ReadingFeedback feedback={feedback} topic={topic} chosen={chosen} index={index} />
          : <section className="reading-question" data-question-index={index + 1}>
            <header className="reading-heading"><span className="reading-tag">READING</span><h1>Asosiy fikrni toping.</h1></header>
            <h2 className="reading-question__prompt">{question.prompt}</h2>
            <div className="reading-options" aria-label={`${index + 1} / ${topic.questions.length} savol`}>
              {question.options.map((option, optionIndex) => <button type="button" key={optionIndex} className={`reading-option${chosen === optionIndex ? " is-selected" : ""}`} aria-pressed={chosen === optionIndex} disabled={busy} onClick={() => setChosen(optionIndex)}><span className="reading-option__key">{String.fromCharCode(65 + optionIndex)}</span><span>{option}</span><Circle size={20} /></button>)}
            </div>
            <button type="button" className="reading-text-link" onClick={() => { setReviewing(true); setStage("text"); }}>Matnga qaytish ↗</button>
          </section>}
        {error && <p className="reading-error" role="alert">{error === "check" ? "Javobni tekshirib bo‘lmadi. Qayta urinib ko‘ring." : "Natijani saqlab bo‘lmadi. Davom etishni qayta bosing."}</p>}
      </div>
      {!result && <footer className="reading-lesson__actions" data-lesson-stage-footer>
        <button type="button" className="reading-primary" disabled={busy || (stage === "practice" && Boolean(question) && !feedback && !exhausted && chosen === null)} onClick={() => stage === "text" ? startPractice() : exhausted ? restart() : !question ? setStage("text") : feedback ? void next() : void check()}>
          {busy ? "Kutilmoqda…" : stage === "text" ? reviewing ? "Savolga qaytish" : "Savollarga o‘tamiz" : exhausted ? "Qayta boshlash" : !question ? "Matnga qaytish" : feedback ? index === topic.questions.length - 1 ? "Natijani ko‘rish" : "Keyingi savol" : "Tekshirish"}<ArrowRight size={20} />
        </button>
      </footer>}
    </div>
  );
}

function ReadingText({ topic }: { topic: ReadingPassageDto }) {
  const words = useMemo(() => (topic.targetWords.length ? topic.targetWords : topic.glossary).filter(word => word.word?.trim() && word.translation?.trim()), [topic]);
  const [selected, setSelected] = useState(0);
  const dictionary = useRef<HTMLElement>(null);
  function selectWord(word: string) {
    const index = words.findIndex(item => item.word.toLowerCase() === word);
    if (index >= 0) setSelected(index);
    // Pen stacks the dictionary under the passage on phones. The passage is a
    // document-flow surface, so bring the definition into the outer viewport.
    if (window.matchMedia("(max-width: 700px)").matches) {
      const panel = dictionary.current;
      const scroller = panel?.closest<HTMLElement>("[data-lesson-stage-body]");
      if (panel && scroller) {
        const offset = panel.getBoundingClientRect().top - scroller.getBoundingClientRect().top;
        if (getComputedStyle(scroller).overflowY === "visible") window.scrollTo({ top:window.scrollY + offset, behavior:"auto" });
        else scroller.scrollTo({ top:scroller.scrollTop + offset, behavior:"auto" });
      }
    }
  }
  return <section className="reading-text">
    <header className="reading-heading"><span className="reading-tag">READING</span><h1>{topic.title}</h1><p>Vocabularydagi {words.length} ta so‘z ajratilgan. So‘zni bosing — tarjimasini ko‘ring.</p></header>
    <div className="reading-text__columns">
      <article className="reading-passage">
        <div className="reading-passage__controls"><span className="reading-tag">{cefrShort(topic.level)} · {topic.topic.toLocaleUpperCase()}</span><span className="reading-tag">{words.length} ta so‘z</span></div>
        <HighlightedPassage passage={topic.body} words={words} className="reading-passage__copy" selectedWord={words[selected]?.word} onWordSelect={selectWord} />
      </article>
      <section className="reading-text__dictionary" ref={dictionary}><ReadingWordPanel words={words} index={selected} onSelect={setSelected} /></section>
    </div>
  </section>;
}

function ReadingFeedback({ feedback, topic, chosen, index }: { feedback: ReadingAnswerCheckDto; topic: ReadingPassageDto; chosen: number | null; index: number }) {
  const question = topic.questions[index];
  const answer = question.options[feedback.correctOptionIndex];
  // Quote only text that really occurs in this passage. An API explanation is
  // labeled as an explanation, never manufactured as a verbatim passage quote.
  const phrase = answer?.replace(/^to\s+/i, "").replace(/[.!?]+$/, "").toLowerCase();
  const excerpt = phrase && phrase.length > 4 ? topic.body.split(/(?<=[.!?])\s+/).find(sentence => sentence.toLowerCase().includes(phrase)) : undefined;
  return <section className="reading-feedback-page">
    <header className="reading-heading"><span className="reading-tag">READING</span><h1>Javob matnning o‘zida.</h1></header>
    <div className={`reading-option ${feedback.isCorrect ? "is-correct" : "is-wrong"}`}><span className="reading-option__key">{String.fromCharCode(65 + (chosen ?? 0))}</span><span>{question.options[chosen ?? 0]}</span>{feedback.isCorrect ? <CircleCheck size={20} /> : <XCircle size={20} />}</div>
    <div className={`reading-feedback${feedback.isCorrect ? "" : " reading-feedback--wrong"}`}><h2>{feedback.isCorrect ? <CircleCheck size={24} /> : <XCircle size={24} />}{feedback.isCorrect ? "Aniq topdingiz!" : "Yana bir bor o‘ylab ko‘ring."}</h2><p>{feedback.isCorrect ? "Javobni matndan topdingiz. Shu tarzda davom eting." : <>To‘g‘ri javob: <strong>{answer}</strong></>}</p></div>
    <div className="reading-evidence"><span className="reading-tag">{excerpt ? "MATNDAN DALIL" : feedback.explanation ? "JAVOB IZOHI" : "MATN BILAN SOLISHTIRING"}</span><blockquote>{excerpt ? `“${excerpt}”` : feedback.explanation ?? "To‘g‘ri javobni matndagi ma’lumot bilan solishtiring."}</blockquote></div>
    {!excerpt && !feedback.explanation && <span className="reading-answer-note"><Check size={16} />{answer}</span>}
  </section>;
}
