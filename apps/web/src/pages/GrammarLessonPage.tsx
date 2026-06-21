import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { ArrowRight, CheckCircle2, Heart, LoaderCircle, PencilLine, XCircle } from "lucide-react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { api } from "@/api/client";
import { GrammarExerciseType, SkillType } from "@/api/types";
import type { GrammarExerciseAnswer, GrammarExerciseCheckDto, GrammarExerciseDto, GrammarExerciseResultDto, GrammarLessonDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import { lessonBackTarget, lessonOriginFrom, lessonState } from "@/lib/lessonNavigation";
import { TopicImage } from "@/components/TopicImage";
import { publishAssistantContext } from "@/components/assistantContext";
import { LessonStopControl } from "@/components/lesson/LessonStopControl";
import { useLessonSounds } from "@/components/lesson/useLessonSounds";
import { useAnswerAutoAdvance } from "@/components/lesson/useAnswerAutoAdvance";
import { grammarPhoto, grammarFocusTitle } from "@/components/grammar/grammarContent";
import { grammarLessonProgress, type GrammarStage } from "@/components/grammar/grammarLessonSteps";
import { LessonProgress } from "@/components/lesson/LessonProgress";
import { LessonQuizOption } from "@/components/lesson/LessonQuizOption";
import "./GrammarLessonPage.css";

type CheckedAnswer = { answer: GrammarExerciseAnswer; outcome: GrammarExerciseCheckDto };

export function GrammarLessonPage() {
  const { topicId = "" } = useParams();
  const { data: lesson, loading, error, reload } = useAsync(() => api.grammar.lesson(topicId), [topicId]);
  useDocumentTitle(lesson?.topic, "Grammar");
  if (loading || error || !lesson || !lesson.isReady) return (
    <section className="grammar-lesson grammar-lesson--status">
      <div className="grammar-lesson__inner">
        <LessonProgress {...grammarLessonProgress("context", lesson?.exercises ?? [], 0)} classPrefix="grammar-progress" backTarget="/app/grammar" />
        <div className="grammar-lesson-state" role="status">
          {loading ? <><LoaderCircle className="animate-spin" />Dars yuklanmoqda…</> : <><h1>{error ? "Darsni yuklab bo‘lmadi." : "Dars tayyorlanmoqda."}</h1><Action onClick={reload}>Qayta urinish</Action></>}
        </div>
      </div>
    </section>
  );
  return <GrammarLesson key={topicId} lesson={{ ...lesson, exercises: [...lesson.exercises].sort((left, right) => left.type - right.type) }} />;
}

function GrammarLesson({ lesson }: { lesson: GrammarLessonDto }) {
  const navigate = useNavigate();
  const location = useLocation();
  const origin = lessonOriginFrom(location);
  const backTarget = lessonBackTarget("grammar", origin);
  const [stage, setStage] = useState<GrammarStage>("context");
  const [index, setIndex] = useState(0);
  const [chosen, setChosen] = useState<number>();
  const [draft, setDraft] = useState("");
  const [outcome, setOutcome] = useState<GrammarExerciseCheckDto | null>(null);
  const [checking, setChecking] = useState(false);
  const [requestError, setRequestError] = useState("");
  const [answers, setAnswers] = useState<Record<string, CheckedAnswer>>({});
  const [result, setResult] = useState<GrammarExerciseResultDto | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [review, setReview] = useState(false);
  const [startedAt, setStartedAt] = useState(() => Date.now());
  const [elapsed, setElapsed] = useState(0);
  const pending = useRef(false);
  const submission = useRef<Promise<void> | null>(null);
  const current = lesson.exercises[index];
  const correctCount = Object.values(answers).filter(answer => answer.outcome.isCorrect).length;
  const hearts = Math.max(0, 5 - Object.values(answers).filter(answer => !answer.outcome.isCorrect).length);
  const { playAnswer, playComplete } = useLessonSounds(`${stage}-${index}`);
  const focusCode = lesson.grammarFocusCode;
  const title = focusCode ? grammarFocusTitle({ grammarFocusCode: focusCode, title: lesson.topic })
    : (location.state as { grammarFocusTitle?: string } | null)?.grammarFocusTitle ?? lesson.topic;
  const firstExample = lesson.curated?.examples[0];
  const photo = grammarPhoto(focusCode, lesson.topic);

  useEffect(() => publishAssistantContext({
    area: "grammar", resourceId: lesson.topicId, title,
    context: [lesson.contextIntro, lesson.explanation, lesson.curated?.summaryUz, ...(lesson.curated?.formulas ?? [])].filter(Boolean).join("\n"),
    focusText: stage === "quiz" ? current?.prompt ?? "" : "", route: location.pathname, stage,
  }), [current?.prompt, lesson, location.pathname, stage, title]);
  useEffect(() => { if (stage === "done") playComplete(); }, [stage, playComplete]);

  const save = useCallback(() => {
    if (submission.current) return submission.current;
    setSubmitting(true);
    setRequestError("");
    const promise = api.grammar.submitExercises(lesson.topicId, getLearnerId(), Object.values(answers).map(item => item.answer))
      .then(saved => { setResult(saved); setElapsed(Math.max(1, Math.round((Date.now() - startedAt) / 1000))); setStage("done"); })
      .catch(() => { submission.current = null; setRequestError("Natijani saqlab bo‘lmadi. Qayta urinib ko‘ring."); })
      .finally(() => setSubmitting(false));
    submission.current = promise;
    return promise;
  }, [answers, lesson.topicId, startedAt]);

  const nextQuestion = useCallback(() => {
    if (!outcome || submitting) return;
    if (index === lesson.exercises.length - 1) { void save(); return; }
    setIndex(value => value + 1); setChosen(undefined); setDraft(""); setOutcome(null); setRequestError("");
  }, [index, lesson.exercises.length, outcome, save, submitting]);
  const autoAdvance = useAnswerAutoAdvance({ enabled: stage === "quiz" && !!outcome?.isCorrect && !submitting && !requestError, onAdvance: nextQuestion, resetKey: current?.id });

  async function check() {
    if (!current || pending.current || outcome || submitting) return;
    const typed = current.type !== GrammarExerciseType.Recognition;
    if (typed ? !draft.trim() : chosen === undefined) return;
    pending.current = true; setChecking(true); setRequestError("");
    const answer: GrammarExerciseAnswer = { exerciseId: current.id, selectedOptionIndex: typed ? -1 : chosen!, ...(typed ? { textAnswer: draft.trim() } : {}) };
    try {
      const checked = await api.grammar.checkExercise(lesson.topicId, current.id, answer.selectedOptionIndex, answer.textAnswer);
      setOutcome(checked);
      // Corrections are practice: the first graded answer remains the score and
      // hearts/XP cannot be farmed by revisiting or retrying the same question.
      setAnswers(previous => previous[current.id] ? previous : { ...previous, [current.id]: { answer, outcome: checked } });
      playAnswer(checked.isCorrect);
    } catch { setRequestError("Javobni tekshirib bo‘lmadi. Qayta urinib ko‘ring."); }
    finally { pending.current = false; setChecking(false); }
  }
  function retryQuestion() { autoAdvance.cancel(); setOutcome(null); setChosen(undefined); setDraft(""); setRequestError(""); }
  function restart() {
    autoAdvance.cancel(); submission.current = null; setAnswers({}); setResult(null); setIndex(0);
    setChosen(undefined); setDraft(""); setOutcome(null); setRequestError(""); setReview(false); setStartedAt(Date.now()); setStage("context");
  }
  function back() {
    autoAdvance.cancel();
    if (stage === "context") navigate(backTarget);
    else if (stage === "rule") setStage("context");
    else if (stage === "examples") setStage("rule");
    else if (stage === "quiz") { setStage("examples"); setChosen(undefined); setDraft(""); setOutcome(null); }
  }
  async function stop() {
    autoAdvance.cancel();
    await api.vocabulary.resetTopicModuleScore(getLearnerId(), lesson.topicId, SkillType.Grammar);
    navigate("/home", { replace: true });
  }
  const progress = grammarLessonProgress(stage, lesson.exercises, index);
  const screen = stage === "context" ? "22" : stage === "rule" ? "23" : stage === "examples" ? "24" : stage === "done" ? "30" : outcome && !outcome.isCorrect ? "28" : current?.type === GrammarExerciseType.Rephrase ? "29" : current?.type === GrammarExerciseType.FillInBlank ? "27" : outcome ? "26" : "25";

  if (stage === "done" && result) {
    const readingUnlocked = result.completion?.modules.some(module => module.module === "Reading" && module.unlocked) ?? false;
    const mistakes = result.outcomes.filter(item => !item.isCorrect);
    return <section className="grammar-lesson grammar-lesson--result" data-pen-screen="30" data-grammar-stage="done">
      <div className="grammar-lesson__inner">
      <LessonProgress {...progress} classPrefix="grammar-progress" backTarget={backTarget} />
      <div className="grammar-lesson__body grammar-lesson__body--result">
      <div className="grammar-result">
        <img className="grammar-result__mascot" src="/assets/play/parrot.svg" alt="" width={230} height={192} />
        <span className="grammar-tag grammar-tag--green">{result.passed ? "BIR QADAM OLDINGA" : "YANA BIR BOR URINIB KO‘RING"}</span>
        <h1>{result.passed ? "Qoida endi gapga aylandi!" : "Birga yana mashq qilamiz."}</h1>
        <dl className="grammar-result__metrics">
          <div><dd>{result.correctCount}/{result.totalExercises}</dd><dt>to‘g‘ri</dt></div>
          <div><dd>+{correctCount * 10}</dd><dt>mashq XP</dt></div>
          <div><dd>{elapsed < 60 ? `${elapsed} sek` : `${Math.round(elapsed / 60)} min`}</dd><dt>mashq</dt></div>
        </dl>
        <Feedback title="Natijangiz saqlandi." text={result.passed ? "Bugun o‘rganganingizni ertaga ham ishlating." : "Xatolarni ko‘rib chiqib, natijangizni yaxshilang."} />
        {lesson.curated?.formulas.length ? <div className="grammar-result__formula">{lesson.curated.formulas[0]}</div> : null}
        <Action onClick={result.passed && readingUnlocked ? () => navigate(`/reading/topic/${lesson.topicId}`, { state: lessonState(origin) }) : restart}>{result.passed && readingUnlocked ? "Reading’ga o‘tish" : "Qayta mashq qilish"}</Action>
        <button className="grammar-secondary" onClick={() => setReview(value => !value)}>{review ? "Xatolarni yopish" : "Xatolarimni ko‘rish"}</button>
        {review && <section className="grammar-result__review" aria-label="Xatolarim">
          {mistakes.length === 0 ? <p>Xato javob yo‘q. Ajoyib!</p> : mistakes.map(item => {
            const question = lesson.exercises.find(exercise => exercise.id === item.exerciseId);
            return <article key={item.exerciseId}><h2>{question?.prompt}</h2><p>To‘g‘ri javob: <strong>{question?.options[item.correctOptionIndex]}</strong></p>{item.explanation && <p>{item.explanation}</p>}</article>;
          })}
          <button className="grammar-secondary" onClick={restart}>Qayta mashq qilish</button>
        </section>}
      </div>
      </div>
      </div>
    </section>;
  }

  return <section className={`grammar-lesson grammar-lesson--${stage}`} data-pen-screen={screen} data-grammar-stage={stage} data-answered={!!outcome}>
    <div className="grammar-lesson__inner">
      <LessonProgress {...progress} classPrefix="grammar-progress" onBack={back}
        backLabel="Oldingi bosqich" backDisabled={checking || submitting} hearts={hearts} heartLabel={`${hearts} yurak`}
        action={stage === "quiz" && <div className="grammar-progress__stop"><LessonStopControl onConfirm={stop} /></div>} />
      <div className="grammar-lesson__body" key={`${stage}-${index}-${screen}`}>
        {stage === "context" && <>
          <Heading title={title} subtitle={lesson.curated?.titleUz} />
          <article className="grammar-context">
            <div className="grammar-context__visual">
              <div className="grammar-context__photo">{photo ? <img src={photo} alt={lesson.topic} /> : <TopicImage topicId={lesson.topicId} title={lesson.topic} level={lesson.level} hideLevelBadge hideTitle className="h-full w-full" />}</div>
              <div className="grammar-context__text"><p>{lesson.contextIntro}</p></div>
            </div>
            {firstExample && <p className="grammar-context__translation">{firstExample.english}<br />{firstExample.uzbek}</p>}
          </article>
          <Feedback title="Gapdagi qoidani toping." text={lesson.curated?.summaryUz || "Matnni o‘qing va grammatik shaklning qanday ishlatilganiga e’tibor bering."} />
        </>}
        {stage === "rule" && <>
          <Heading title="Bitta sodda qolip." />
          {lesson.curated ? <>
            <div className="grammar-formula"><span className="grammar-tag">{title.toUpperCase()}</span>{lesson.curated.formulas.map(formula => <strong key={formula}>{formula}</strong>)}<p>{lesson.curated.summaryUz}</p></div>
            <div className="grammar-rules">{lesson.curated.rules.map((rule, i) => <article key={i}><h2>{rule.headingUz}</h2><p>{rule.bodyUz}</p></article>)}</div>
            {lesson.curated.commonMistakesUz.length > 0 && <div className="grammar-rule-notes">{lesson.curated.commonMistakesUz.map(note => <p key={note}>{note}</p>)}</div>}
          </> : <div className="grammar-rules"><p>{lesson.explanation}</p></div>}
        </>}
        {stage === "examples" && <>
          <Heading title="Uch xil gap. Bir qoida." />
          <div className="grammar-examples">{(lesson.curated?.examples ?? []).map((example, i) => <article key={i} className={`grammar-example grammar-example--${i % 3}`}><span>{example.english.includes("?") ? "SO‘ROQ" : /\b(?:not|never|no)\b|n't/i.test(example.english) ? "INKOR" : "TASDIQ"}</span><h2>{example.english}</h2><p>{example.uzbek}</p></article>)}{!lesson.curated?.examples.length && <article className="grammar-example"><p>{lesson.explanation}</p></article>}</div>
        </>}
        {stage === "quiz" && (hearts === 0 ? <div className="grammar-lesson-state"><Heart size={40} /><h1>Jonlar tugadi.</h1><p>Qoidani takrorlab, yana sinab ko‘ring.</p><Action onClick={restart}>Qayta urinish</Action></div> : !current ? <div className="grammar-lesson-state"><h1>Mashqlar tayyorlanmoqda.</h1><p>Hozircha bu dars uchun test yo‘q.</p></div> : <>
          <Heading title={outcome && !outcome.isCorrect ? "Bitta kichik tuzatish." : current.type === GrammarExerciseType.Recognition ? "Bo‘sh joyni to‘ldiring." : current.type === GrammarExerciseType.FillInBlank ? "Endi o‘zingiz yozing." : "Fikrni bitta gapga jamlang."} subtitle={current.type === GrammarExerciseType.Rephrase ? `${title}’dan foydalaning.` : undefined} />
          <Exercise exercise={current} chosen={chosen} draft={draft} outcome={outcome} checking={checking} onChoose={setChosen} onDraft={setDraft} onCheck={() => void check()} />
          {outcome && <Feedback wrong={!outcome.isCorrect} title={outcome.isCorrect ? current.type === GrammarExerciseType.Rephrase ? "Ma’no saqlandi." : "Ajoyib!" : "Bitta kichik tuzatish."} text={outcome.explanation || (outcome.isCorrect ? "Qolipni to‘g‘ri ishlatdingiz." : `To‘g‘ri javob: ${current.options[outcome.correctOptionIndex]}`)} />}
        </>)}
        {requestError && <p className="grammar-error" role="alert">{requestError}</p>}
      </div>
      <footer className="grammar-lesson__actions">
        {stage === "context" && <Action onClick={() => setStage("rule")}>Qoidani ko‘rish</Action>}
        {stage === "rule" && <Action onClick={() => setStage("examples")}>Misollarda ko‘rish</Action>}
        {stage === "examples" && <Action onClick={() => setStage("quiz")}>Endi o‘zim sinayman</Action>}
        {stage === "quiz" && current && hearts > 0 && <>
          <Action onClick={outcome ? outcome.isCorrect ? requestError ? nextQuestion : autoAdvance.advance : retryQuestion : () => void check()} disabled={checking || submitting || (!outcome && (current.type === GrammarExerciseType.Recognition ? chosen === undefined : !draft.trim()))} loading={checking || submitting}>
            {outcome ? outcome.isCorrect ? index === lesson.exercises.length - 1 ? "Natijamni ko‘rish" : "Keyingi mashq" : "Tushundim, yana sinayman" : "Tekshirish"}
          </Action>
          {outcome && !outcome.isCorrect && <button className="grammar-secondary" onClick={nextQuestion}>Keyingi mashqqa o‘tish</button>}
        </>}
      </footer>
    </div>
  </section>;
}

function Heading({ title, subtitle }: { title:string; subtitle?:string }) {
  return <div className="grammar-heading"><span className="grammar-tag">GRAMMAR</span><h1>{title}</h1>{subtitle && <p>{subtitle}</p>}</div>;
}
function Action({ children, onClick, disabled, loading }: { children:ReactNode; onClick:()=>void; disabled?:boolean; loading?:boolean }) {
  return <button type="button" className="grammar-primary" disabled={disabled} onClick={onClick}>{children}{loading ? <LoaderCircle size={20} className="animate-spin" /> : <ArrowRight size={20} />}</button>;
}
function Feedback({ title, text, wrong = false }: { title:string; text:string; wrong?:boolean }) {
  return <aside className={`grammar-feedback${wrong ? " grammar-feedback--wrong" : ""}`} role="status"><h2>{wrong ? <XCircle size={24} /> : <CheckCircle2 size={24} />}{title}</h2><p>{text}</p></aside>;
}
function Exercise({ exercise, chosen, draft, outcome, checking, onChoose, onDraft, onCheck }: {
  exercise:GrammarExerciseDto; chosen:number|undefined; draft:string; outcome:GrammarExerciseCheckDto|null; checking:boolean;
  onChoose:(index:number)=>void; onDraft:(value:string)=>void; onCheck:()=>void;
}) {
  const typed = exercise.type !== GrammarExerciseType.Recognition;
  const wrong = outcome && !outcome.isCorrect;
  const correctAnswer = outcome ? exercise.options[outcome.correctOptionIndex] : "";
  const gap = exercise.prompt.match(/_{2,}/);
  const prefix = gap?.index !== undefined ? exercise.prompt.slice(0, gap.index).trim() : "";
  const wholeSentence = prefix && correctAnswer.toLowerCase().startsWith(`${prefix.toLowerCase()} `);
  const shownPrompt = outcome?.isCorrect && exercise.type !== GrammarExerciseType.Rephrase && gap
    ? wholeSentence ? correctAnswer : exercise.prompt.replace(/_{2,}/, correctAnswer) : exercise.prompt;
  if (wrong && typed) return <div className="grammar-correction"><p>{draft}</p><strong>{correctAnswer}</strong></div>;
  return <>
    <div className="grammar-question"><h2>{shownPrompt}</h2></div>
    {typed ? <form className="grammar-answer-form" onSubmit={event => { event.preventDefault(); if (!outcome && !checking) onCheck(); }}>
      <label htmlFor="grammar-answer">{exercise.type === GrammarExerciseType.Rephrase ? "SIZNING GAPINGIZ" : "Fe’lning mos shakli"}</label>
      <div className="grammar-answer-field" data-answer-state={outcome?.isCorrect ? "correct" : "idle"}>
        {exercise.type === GrammarExerciseType.Rephrase ? <textarea id="grammar-answer" value={draft} onChange={event => onDraft(event.target.value)} disabled={checking || !!outcome} maxLength={2000} placeholder="Gapingizni yozing…" rows={2} /> : <input id="grammar-answer" autoComplete="off" value={draft} onChange={event => onDraft(event.target.value)} disabled={checking || !!outcome} maxLength={2000} placeholder="Javobingiz…" />}
        {outcome?.isCorrect ? <CheckCircle2 size={24} aria-hidden="true" /> : <PencilLine size={18} aria-hidden="true" />}
      </div>
      {exercise.type === GrammarExerciseType.Rephrase && <span className="grammar-answer-count">{draft.trim() ? draft.trim().split(/\s+/).length : 0} so‘z</span>}
    </form> : <div className="grammar-options lesson-quiz-options" role="group" aria-label="Javob variantlari">{exercise.options.map((option, i) => (
      <LessonQuizOption key={i} index={i} label={option} classPrefix="grammar-option"
        state={!outcome ? "idle" : outcome.correctOptionIndex === i ? "correct" : chosen === i ? "wrong" : "dim"}
        selected={chosen === i} disabled={checking || !!outcome} onSelect={() => onChoose(i)} />
    ))}</div>}
  </>;
}
