import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { cefrShort } from "@/lib/labels";
import { api, ApiError } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import { WritingDimension } from "@/api/types";
import type { WritingAssessmentDto, WritingTaskDto } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { Friend } from "@/components/game";
import { LessonProgress, type LessonStep } from "@/components/lesson/LessonProgress";
import { lessonBackTarget, lessonOriginFrom } from "@/lib/lessonNavigation";
import { uz } from "@/content/uz";
import "./WritingTaskPage.css";

const WRITING_STEPS: readonly LessonStep[] = [
  { id: "intro", label: "Topshiriq" },
  { id: "editor", label: "Matn yozish" },
  { id: "review", label: "AI izohi" },
  { id: "result", label: "Yakuniy natija" },
];
type Stage = (typeof WRITING_STEPS)[number]["id"];

const DIMENSION_LABEL: Record<WritingDimension, string> = {
  [WritingDimension.TaskAchievement]: uz.writing.dimensions.taskAchievement,
  [WritingDimension.Coherence]: uz.writing.dimensions.coherence,
  [WritingDimension.LexicalResource]: uz.writing.dimensions.lexicalResource,
  [WritingDimension.GrammaticalAccuracy]: uz.writing.dimensions.grammaticalAccuracy,
};

function countWords(text: string): number {
  const trimmed = text.trim();
  return trimmed === "" ? 0 : trimmed.split(/\s+/).length;
}

function assessmentErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError) || typeof error.body !== "object" || error.body === null) {
    return uz.writing.assessmentError;
  }
  const body = error.body as { code?: unknown; correlationId?: unknown };
  const correlationId = typeof body.correlationId === "string" ? body.correlationId : null;
  const base = body.code === "timeout" ? uz.writing.assessmentTimeout : uz.writing.assessmentUnavailable;
  return correlationId ? `${base} ${uz.writing.correlationId(correlationId)}` : base;
}

function sampleOpening(task: WritingTaskDto) {
  const word = task.targetWords[0]?.word;
  return word ? `Hi,\n\nI want to tell you about ${word}.` : "Hi,\n\nI want to tell you about someone who inspires me.";
}

/** Pen screens 42–45. Short stages occupy the visible frame; the body receives
 * an inner scroll only when learner content or browser zoom needs it. */
export function WritingTaskPage() {
  const { topicId = "" } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const backTarget = lessonBackTarget("writing", lessonOriginFrom(location));
  const [reloadKey, setReloadKey] = useState(0);
  const { data: task, loading, error } = useAsync(() => api.writing.task(topicId), [topicId, reloadKey]);
  useDocumentTitle(task?.title, uz.nav.writing);
  const taskNotFound = error instanceof ApiError && error.status === 404;

  useEffect(() => {
    if (!task || task.isReady || loading || error) return;
    const timer = window.setTimeout(() => setReloadKey((value) => value + 1), 2500);
    return () => window.clearTimeout(timer);
  }, [error, loading, task]);

  return (
    <div className="writing-task-page" data-anim>
      {loading ? <ModulePageLoader icon="edit_note" accent="orange" /> : null}
      {!loading && (error || !task) ? (
        <StateCard
          icon="cloud_off"
          title={taskNotFound ? uz.writing.taskNotFound : uz.writing.taskLoadError}
          description={taskNotFound ? uz.writing.taskNotFoundHint : undefined}
          actionLabel={taskNotFound ? uz.writing.back : uz.common.retry}
          actionIcon={taskNotFound ? "arrow_back" : "refresh"}
          onAction={() => taskNotFound ? navigate(backTarget, { replace: true }) : setReloadKey((value) => value + 1)}
        />
      ) : null}
      {!loading && task && !task.isReady ? (
        <StateCard icon="auto_edit" title={uz.writing.preparing} description={task.title} actionLabel={uz.writing.retry} actionIcon="refresh" onAction={() => setReloadKey((value) => value + 1)} />
      ) : null}
      {!loading && task?.isReady ? <WritingTaskFlow task={task} topicId={topicId} onExit={() => navigate(backTarget)} /> : null}
    </div>
  );
}

function StateCard({ icon, title, description, actionLabel, actionIcon, onAction }: {
  icon: string; title: string; description?: string; actionLabel: string; actionIcon: string; onAction: () => void;
}) {
  return (
    <section className="writing-task-state" role="status">
      <span className="writing-task-state__icon"><Icon name={icon} filled /></span>
      <h1>{title}</h1>
      {description ? <p>{description}</p> : null}
      <button type="button" className="writing-task-primary" onClick={onAction}>{actionLabel}<Icon name={actionIcon} /></button>
    </section>
  );
}

function WritingTaskFlow({ task, topicId, onExit }: { task: WritingTaskDto; topicId: string; onExit: () => void }) {
  const learnerId = getLearnerId();
  const [stage, setStage] = useState<Stage>("intro");
  const [text, setText] = useState("");
  const [assessment, setAssessment] = useState<WritingAssessmentDto | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [gated, setGated] = useState(false);
  const wordCount = useMemo(() => countWords(text), [text]);
  const overLimit = wordCount > task.maxWords;

  async function submit() {
    if (submitting || wordCount === 0 || overLimit) return;
    setSubmitting(true);
    setSubmitError(null);
    setGated(false);
    try {
      setAssessment(await api.writing.submit(learnerId, topicId, text));
      setStage("review");
    } catch (error) {
      if (error instanceof ApiError && error.status === 402) setGated(true);
      else setSubmitError(assessmentErrorMessage(error));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AnimatePresence mode="wait">
      {stage === "intro" ? <WritingStage key="intro" stage="intro" onBack={onExit} task={task}><IntroStage task={task} onNext={() => setStage("editor")} /></WritingStage> : null}
      {stage === "editor" ? (
        <WritingStage key="editor" stage="editor" onBack={() => setStage("intro")} task={task}>
          <EditorStage task={task} text={text} wordCount={wordCount} overLimit={overLimit} submitting={submitting} gated={gated} submitError={submitError} onText={setText} onSubmit={submit} />
        </WritingStage>
      ) : null}
      {stage === "review" && assessment ? <WritingStage key="review" stage="review" onBack={() => setStage("editor")} task={task}><ReviewStage assessment={assessment} onNext={() => setStage("result")} /></WritingStage> : null}
      {stage === "result" && assessment ? (
        <WritingStage key="result" stage="result" onBack={() => setStage("review")} task={task}>
          <ResultStage assessment={assessment} text={text} onAgain={() => { setAssessment(null); setSubmitError(null); setGated(false); setStage("editor"); }} onExit={onExit} />
        </WritingStage>
      ) : null}
    </AnimatePresence>
  );
}

function WritingStage({ stage, onBack, task, children }: { stage: Stage; onBack: () => void; task: WritingTaskDto; children: React.ReactNode }) {
  return (
    <motion.section initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -20 }} transition={{ type: "spring", stiffness: 260, damping: 28 }} className={`writing-task-stage writing-task-stage--${stage}`}>
      <LessonProgress
        steps={WRITING_STEPS}
        step={stage}
        complete={stage === "result"}
        hearts={5}
        onBack={onBack}
        backLabel={stage === "intro" ? uz.writing.back : "Oldingi bosqich"}
        classPrefix="vocabulary-progress"
      />
      <div className="writing-task-stage__body"><div className="writing-task-stage__body-inner">{children}</div></div>
      <p className="writing-task-stage__meta" aria-live="polite">{stage === "intro" ? `${cefrShort(task.level)} · ${task.minWords}–${task.maxWords} so‘z` : null}</p>
    </motion.section>
  );
}

function StageHeading({ title, subtitle }: { title: string; subtitle?: string }) {
  return <header className="writing-task-heading"><span className="writing-task-tag">WRITING</span><h1>{title}</h1>{subtitle ? <p>{subtitle}</p> : null}</header>;
}

function IntroStage({ task, onNext }: { task: WritingTaskDto; onNext: () => void }) {
  const guidance = task.guidance.slice(0, 3);
  const items = guidance.length > 0 ? guidance : [
    "Kim yoki nima haqida yozayotganingizni ayting.",
    "Asosiy fikringizni bir misol bilan tushuntiring.",
    "Matnni samimiy yakunlang.",
  ];
  return (
    <>
      <StageHeading title={task.title} subtitle="Qisqa, aniq va samimiy yozing." />
      <section className="writing-task-brief">
        <span className="writing-task-brief__type">{cefrShort(task.level)} · YOZUV TOPSHIRIG‘I</span>
        <h2>{task.prompt}</h2>
        <ul>{items.map((item) => <li key={item}><Icon name="check_circle" filled />{item}</li>)}</ul>
      </section>
      <section className="writing-task-sample"><span>BOSHLASH UCHUN NAMUNA</span><p>{sampleOpening(task)}</p><small>{task.minWords}–{task.maxWords} so‘z</small></section>
      <div className="writing-task-actions"><button type="button" className="writing-task-primary" onClick={onNext}>Yozishni boshlash<Icon name="arrow_forward" /></button></div>
    </>
  );
}

function EditorStage({ task, text, wordCount, overLimit, submitting, gated, submitError, onText, onSubmit }: {
  task: WritingTaskDto; text: string; wordCount: number; overLimit: boolean; submitting: boolean; gated: boolean; submitError: string | null; onText: (value: string) => void; onSubmit: () => void;
}) {
  return (
    <>
      <StageHeading title="Endi sizning navbatingiz." />
      <section className="writing-task-editor-card">
        <div className="writing-task-editor-card__top"><span className="writing-task-editor-card__label">YOZISH MAYDONI</span><span className={overLimit ? "is-error" : wordCount >= task.minWords ? "is-ready" : ""}>{wordCount} / {task.maxWords} so‘z</span></div>
        <label className="sr-only" htmlFor="writing-answer">Sizning javobingiz</label>
        <textarea id="writing-answer" value={text} onChange={(event) => onText(event.target.value)} placeholder={uz.writing.placeholder} className="writing-task-editor" disabled={submitting} rows={10} />
        <div className="writing-task-editor-card__footer"><span>{wordCount < task.minWords ? `Kamida ${task.minWords} so‘z yozing.` : "Matningiz avtomatik saqlanadi."}</span><span>{task.minWords}–{task.maxWords} so‘z</span></div>
      </section>
      {task.targetWords.length > 0 ? <div className="writing-task-supports" aria-label="Foydali so‘zlar">{task.targetWords.slice(0, 4).map((word) => <span key={word.word}><Icon name="lightbulb" />{word.word}</span>)}</div> : null}
      {overLimit ? <p className="writing-task-message writing-task-message--error"><Icon name="error" filled />{uz.writing.overLimit(task.maxWords)}</p> : null}
      {gated ? <p className="writing-task-message writing-task-message--error"><Icon name="lock" filled />{uz.writing.gatedText}</p> : null}
      {submitError ? <p className="writing-task-message writing-task-message--error"><Icon name="cloud_off" filled />{submitError}</p> : null}
      <div className="writing-task-actions"><button type="button" className="writing-task-primary" onClick={onSubmit} disabled={wordCount === 0 || overLimit || submitting}>{submitting ? "AI tekshirmoqda…" : "AI bilan tekshirish"}<Icon name={submitting ? "progress_activity" : "arrow_forward"} className={submitting ? "is-spinning" : undefined} /></button><small>Matningiz avtomatik saqlanadi.</small></div>
    </>
  );
}

function ReviewStage({ assessment, onNext }: { assessment: WritingAssessmentDto; onNext: () => void }) {
  const issue = assessment.issues[0];
  return (
    <>
      <StageHeading title={"Fikringiz tushunarli.\nEndi sayqallaymiz."} />
      <section className="writing-task-score">
        <div className="writing-task-score__summary"><strong>{assessment.overallPercent} / 100</strong><span>AI TAHLILI</span></div>
        <div className="writing-task-score__dimensions">{assessment.dimensionScores.map((dimension) => <div key={dimension.dimension}><span>{DIMENSION_LABEL[dimension.dimension]}</span><strong>{dimension.score}/5</strong></div>)}</div>
      </section>
      {issue ? <section className="writing-task-correction"><span>AI IZOHI</span><strong>{DIMENSION_LABEL[issue.dimension]}</strong><p>{issue.explanation}</p></section> : <section className="writing-task-success"><Icon name="check_circle" filled /><div><strong>Bu qismingiz tabiiy chiqqan.</strong><p>Matningiz tushunarli va vazifaga mos yozilgan.</p></div></section>}
      <div className="writing-task-actions"><button type="button" className="writing-task-primary" onClick={onNext}>Natijani yakunlash<Icon name="arrow_forward" /></button></div>
    </>
  );
}

function ResultStage({ assessment, text, onAgain, onExit }: { assessment: WritingAssessmentDto; text: string; onAgain: () => void; onExit: () => void }) {
  return (
    <>
      <section className="writing-task-result-hero"><Friend skill="writing" size={112} /><span>YOZUV NATIJASI SAQLANDI</span><h1>Fikringiz yetib bordi!</h1></section>
      <section className="writing-task-result-score"><strong>{assessment.overallPercent}%</strong><Icon name="check_circle" filled /><strong>{cefrShort(assessment.estimatedLevel)}</strong><small>AI bahosi · yakuniy matn saqlandi</small></section>
      <section className="writing-task-final-copy"><span>YAKUNIY MATNINGIZ</span><p>{text}</p></section>
      <div className="writing-task-actions"><button type="button" className="writing-task-primary" onClick={onExit}>Yozuv mavzulariga qaytish<Icon name="arrow_forward" /></button><button type="button" className="writing-task-secondary" onClick={onAgain}>Matnimni qayta yozish</button></div>
    </>
  );
}

export function AssessmentLoadingOverlay() {
  return <div className="writing-task-assessment" role="status"><Icon name="progress_activity" className="is-spinning animate-spin" /><div><strong>{uz.writing.assessing}</strong><p>{uz.writing.assessingHint}</p></div></div>;
}
