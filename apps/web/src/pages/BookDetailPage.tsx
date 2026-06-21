import { useEffect, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { api, ApiError, apiErrorDetails } from "@/api/client";
import type {
  BookDetailDto,
  BookQuestionDto,
  BookQuestionOutcomeDto,
  BookQuizResultDto,
  BookSectionDto,
} from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { BookCover } from "@/components/BookCover";
import { publishAssistantContext } from "@/components/assistantContext";
import { LessonAdvanceAction } from "@/components/lesson/LessonAdvanceAction";
import { LessonStopControl } from "@/components/lesson/LessonStopControl";
import { LessonGuidance, LessonStageFrame } from "@/components/lesson/LessonStageFrame";
import { useAnswerAutoAdvance } from "@/components/lesson/useAnswerAutoAdvance";
import { useLessonSounds } from "@/components/lesson/useLessonSounds";
import { Confetti, DuoButton } from "@/components/game";
import { ExerciseOption } from "@/components/game/ExerciseOption";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { Pill } from "@/components/ui/Pill";
import { ProgressRing } from "@/components/ui/ProgressRing";
import { uz } from "@/content/uz";
import { tapLight } from "@/lib/haptics";
import { cefrShort, optionLetter } from "@/lib/labels";
import { useAsync } from "@/lib/useAsync";
import "./BookDetailPage.css";

type Accent = "green" | "red" | "orange" | "amber";
/** Books uses one consistent module accent (orange); semantic green/red/amber stay for state. */
const BOOKS_ACCENT: Accent = "orange";
type RunnerStage = "intro" | "read" | "question" | "result" | "complete";
type BookSubmitError = { message: string; correlationId?: string };

/** Canonical continuous book runner: intro → section → one question per slide → authoritative
 * result → next section. Only the active section is loaded, so generated book content stays lazy. */
export function BookDetailPage() {
  const { bookId = "" } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const { data: detail, loading, error } = useAsync(
    () => api.books.detail(bookId, learnerId),
    [bookId, learnerId],
  );
  useDocumentTitle(detail?.title, "Kitob");
  const requestedSectionId = searchParams.get("section");

  useEffect(() => {
    if (!detail || !requestedSectionId) return;
    const requestedSection = detail.sections.find((section) => section.id === requestedSectionId);
    if (requestedSection?.isLocked !== true) return;
    const fallbackSection = detail.sections.find((section) => !section.isRead && !section.isLocked)
      ?? detail.sections.find((section) => !section.isLocked);
    if (fallbackSection) setSearchParams({ section: fallbackSection.id }, { replace: true });
    else setSearchParams({}, { replace: true });
  }, [detail, requestedSectionId, setSearchParams]);

  if (loading) return <ModulePageLoader icon="auto_stories" accent="orange" />;
  if (error || !detail) return <PageState icon="error" text={uz.common.error} accent="red" />;

  const requestedSection = detail.sections.find((section) => section.id === requestedSectionId);
  const initialSectionId = requestedSection && !requestedSection.isLocked
    ? requestedSection.id
    : null;

  return (
    <BookRunner
      key={bookId}
      detail={detail}
      initialSectionId={initialSectionId}
      learnerId={learnerId}
      onExit={() => navigate("/books")}
      onCanonicalSection={(sectionId) => setSearchParams({ section: sectionId }, { replace: true })}
   />
  );
}

function BookRunner({
  detail,
  initialSectionId,
  learnerId,
  onExit,
  onCanonicalSection,
}: {
  detail: BookDetailDto;
  initialSectionId: string | null;
  learnerId: string;
  onExit: () => void;
  onCanonicalSection: (sectionId: string) => void;
}) {
  const [stage, setStage] = useState<RunnerStage>("intro");
  const [sectionId, setSectionId] = useState<string | null>(initialSectionId);
  const [questionIndex, setQuestionIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, number>>({});
  const [outcomes, setOutcomes] = useState<Record<string, BookQuestionOutcomeDto>>({});
  const [result, setResult] = useState<BookQuizResultDto | null>(null);
  const [checking, setChecking] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<BookSubmitError | null>(null);
  const [sectionReload, setSectionReload] = useState(0);
  const submitInFlightRef = useRef(false);
  const { playAnswer, playComplete } = useLessonSounds(
    stage === "question"
      ? `book-${sectionId}-question-${questionIndex}`
      : `book-${sectionId ?? detail.id}-${stage}`,
  );

  useEffect(() => {
    if (!initialSectionId) return;
    setSectionId(initialSectionId);
    setQuestionIndex(0);
    setAnswers({});
    setOutcomes({});
    setResult(null);
    setSubmitError(null);
    submitInFlightRef.current = false;
    setStage("read");
  }, [initialSectionId]);

  const { data: section, loading: sectionLoading, error: sectionError } = useAsync(
    () => stage !== "intro" && sectionId
      ? api.books.section(detail.id, sectionId, learnerId)
      : Promise.resolve(null),
    [detail.id, sectionId, learnerId, sectionReload, stage],
  );
  const sectionTimedOut = sectionError instanceof ApiError && sectionError.status === 408;
  useEffect(() => {
    if ((stage === "result" || stage === "complete") && result) playComplete();
  }, [playComplete, result, stage]);

  const orderedSections = useMemo(
    () => [...detail.sections].sort((a, b) => a.order - b.order),
    [detail.sections],
  );
  const currentSummaryIndex = orderedSections.findIndex((item) => item.id === sectionId);
  const nextSection = currentSummaryIndex >= 0 ? orderedSections[currentSummaryIndex + 1] : undefined;
  const total = detail.sections.length;
  const read = result?.sectionsRead ?? detail.sectionsRead;

  useEffect(() => publishAssistantContext({
    area: "books",
    resourceId: section ? `${detail.id}:${section.sectionId}` : detail.id,
    title: section ? `${detail.title} — ${section.title}` : detail.title,
    context: [
      `Author: ${detail.author}`,
      `Topic: ${detail.topic}`,
      `Level: ${detail.level}`,
      `Synopsis: ${detail.synopsis}`,
      section?.isReady ? `Current section: ${section.body}` : "",
      section?.isReady ? `Questions: ${section.questions.map((question) => question.prompt).join(" | ")}` : "",
    ].filter(Boolean).join("\n"),
    focusText: "",
    route: section ? `/books/${detail.id}/sections/${section.sectionId}` : `/books/${detail.id}`,
    stage,
  }), [detail, section, stage]);

  function startSection(targetId: string) {
    onCanonicalSection(targetId);
    setSectionId(targetId);
    setQuestionIndex(0);
    setAnswers({});
    setOutcomes({});
    setResult(null);
    setSubmitError(null);
    submitInFlightRef.current = false;
    setStage("read");
  }

  async function checkAnswer(activeSection: BookSectionDto, question: BookQuestionDto, option: number) {
    if (checking || outcomes[question.id]) return;

    setChecking(true);
    setSubmitError(null);
    setAnswers((current) => ({ ...current, [question.id]: option }));
    try {
      const outcome = await api.books.checkAnswer(
        activeSection.bookId,
        activeSection.sectionId,
        question.id,
        option,
      );
      setOutcomes((current) => ({ ...current, [question.id]: outcome }));
      playAnswer(outcome.isCorrect);
    } catch {
      setAnswers((current) => {
        const next = { ...current };
        delete next[question.id];
        return next;
      });
      setSubmitError({ message: uz.books.answerCheckError });
    } finally {
      setChecking(false);
    }
  }

  async function submitSection(activeSection: BookSectionDto) {
    if (submitInFlightRef.current) return;
    submitInFlightRef.current = true;
    setSubmitting(true);
    setSubmitError(null);
    try {
      const payload = activeSection.questions.map((question) => ({
        questionId: question.id,
        selectedOptionIndex: answers[question.id],
      }));
      const authoritativeResult = await api.books.submit(
        activeSection.bookId,
        activeSection.sectionId,
        learnerId,
        payload,
      );
      setResult(authoritativeResult);
      setStage("result");
    } catch (error) {
      const details = apiErrorDetails(error);
      setSubmitError({
        message: error instanceof ApiError && error.status === 408
          ? uz.books.submitTimeout
          : uz.books.submitError,
        correlationId: details?.correlationId,
      });
      submitInFlightRef.current = false;
    } finally {
      setSubmitting(false);
    }
  }

  function retrySection() {
    setAnswers({});
    setOutcomes({});
    setResult(null);
    setQuestionIndex(0);
    setSubmitError(null);
    submitInFlightRef.current = false;
    setStage("read");
  }

  function continueAfterResult() {
    if (!result?.passed) {
      retrySection();
      return;
    }
    if (result.bookCompleted || !nextSection) {
      setStage("complete");
      return;
    }
    startSection(nextSection.id);
  }

  function stopQuiz() {
    onExit();
  }

  return (
    <div className="book-detail book-detail--centered" data-module="books">
      <AnimatePresence mode="wait">
        {stage === "intro" && (
          <IntroSlide
            key="intro"
            detail={detail}
            startSectionId={initialSectionId}
            onStart={startSection}
            onExit={onExit}
         />
        )}

        {stage !== "intro" && stage !== "complete" && (sectionLoading || !section) && (
          <ModulePageLoader key="section-loading" icon="auto_stories" accent="orange" embedded />
        )}

        {stage !== "intro" && stage !== "complete" && Boolean(sectionError) && (
          <Slide key="section-error" accent="red">
            <div className="text-center">
              <Icon name="cloud_off" filled className="text-[42px]" />
              <p className="mt-3 font-duo text-lg font-extrabold">
                {sectionTimedOut ? uz.books.preparingTimeout : uz.common.error}
              </p>
              <DuoButton className="mt-5" color="yellow" icon="refresh" onClick={() => setSectionReload((value) => value + 1)}>
                {uz.books.retry}
              </DuoButton>
            </div>
          </Slide>
        )}

        {stage !== "intro" && stage !== "complete" && section && !section.isReady && (
          <Slide key="section-preparing" accent="amber">
            <div className="text-center">
              <Icon name="auto_stories" className="animate-ea-float text-[42px]" />
              <p className="mt-3 font-duo text-lg font-extrabold">{uz.books.preparing}</p>
              <DuoButton className="mt-5" color="yellow" icon="refresh" onClick={() => setSectionReload((value) => value + 1)}>
                {uz.books.retry}
              </DuoButton>
            </div>
          </Slide>
        )}

        {stage === "read" && section?.isReady && (
          <Slide
            key={`read-${section.sectionId}`}
            accent={BOOKS_ACCENT}
            header={<RunnerHeader title={section.title} label={`${section.order}/${total}`} onBack={() => setStage("intro")} progress={(currentSummaryIndex + 1) / Math.max(total, 1)} />}
            footer={<DuoButton color="green" fullWidth size="lg" onClick={() => setStage("question")}>{uz.books.quizTitle}</DuoButton>}
            bodyClassName="book-detail__reading-main"
          >
            <LessonGuidance title={uz.lessonGuidance.title} items={uz.lessonGuidance.books.read} />
            <div className="book-detail__reading-title"><p className="book-detail__eyebrow">{section.order}-bo‘lim</p><h1>{section.title}</h1></div>
            <article className="book-detail__reading-card"><p>{section.body}</p></article>
          </Slide>
        )}

        {stage === "question" && section?.isReady && section.questions[questionIndex] && (
            <QuestionSlide
              key={`${section.sectionId}-question-${questionIndex}`}
              question={section.questions[questionIndex]}
              index={questionIndex}
              total={section.questions.length}
              selected={answers[section.questions[questionIndex].id]}
              outcome={outcomes[section.questions[questionIndex].id]}
              checking={checking}
              submitting={submitting}
              submitError={submitError}
              topRightAction={<LessonStopControl resetScore={false} onConfirm={stopQuiz} />}
              onSelect={(option) => void checkAnswer(section, section.questions[questionIndex], option)}
              onBack={() => questionIndex === 0 ? setStage("read") : setQuestionIndex((index) => index - 1)}
              onNext={() => {
                if (questionIndex < section.questions.length - 1) setQuestionIndex((index) => index + 1);
                else void submitSection(section);
              }}
           />
        )}

        {stage === "result" && section?.isReady && result && (
          <ResultSlide
            key={`result-${section.sectionId}`}
            result={result}
            nextSectionExists={!!nextSection}
            onContinue={continueAfterResult}
            onRetry={retrySection}
         />
        )}

        {stage === "complete" && (
          <CompleteSlide key="complete" detail={detail} read={read} onExit={onExit} />
        )}
      </AnimatePresence>
    </div>
  );
}

function IntroSlide({
  detail,
  startSectionId,
  onStart,
  onExit,
}: {
  detail: BookDetailDto;
  startSectionId: string | null;
  onStart: (sectionId: string) => void;
  onExit: () => void;
}) {
  const total = detail.sections.length;
  const ratio = total ? detail.sectionsRead / total : 0;
  return (
    <Slide
      accent={BOOKS_ACCENT}
      header={<RunnerHeader title={detail.title} label={`${detail.sectionsRead}/${total}`} onBack={onExit} progress={ratio} />}
      footer={startSectionId ? (
        <DuoButton color="yellow" fullWidth size="lg" onClick={() => onStart(startSectionId)}>
          {detail.sectionsRead > 0 ? uz.books.continueReading : uz.books.startReading}
        </DuoButton>
      ) : undefined}
    >
      <LessonGuidance title={uz.lessonGuidance.title} items={uz.lessonGuidance.books.intro} />
      <div className="book-detail__intro-grid">
        <div className="book-detail__hero">
          <BookCover
            title={detail.title}
            level={detail.level}
            coverImageUrl={detail.coverImageUrl}
            className="book-detail__cover"
          />
          <div className="book-detail__hero-copy">
            <div className="book-detail__badges">
              <span className="book-detail__badge">{cefrShort(detail.level)}</span>
              <span className="book-detail__badge book-detail__badge--topic">{detail.topic}</span>
            </div>
            <p className="book-detail__eyebrow">EnglishAi kutubxonasi</p>
            <h1>{detail.title}</h1>
            <p className="book-detail__title-uz">{detail.titleUz}</p>
            <p className="book-detail__author">{uz.books.by(detail.author)}</p>
          </div>
        </div>
        <div className="book-detail__overview">
          <div className="book-detail__progress-card">
            <ProgressRing value={ratio} size={58} strokeWidth={6} trackClassName="text-ea-border" arcClassName="text-ea-mod-books">
              <span>{detail.sectionsRead}/{total}</span>
            </ProgressRing>
            <div>
              <small>O‘qish jarayoni</small>
              <strong>{uz.books.progress(detail.sectionsRead, total)}</strong>
            </div>
          </div>
          <div className="book-detail__synopsis">
            <span className="book-detail__section-icon"><Icon name="menu_book" filled /></span>
            <div>
              <p className="book-detail__eyebrow">Kitob haqida</p>
              <h2>{uz.books.synopsisTitle}</h2>
              <p>{detail.synopsis}</p>
            </div>
          </div>
        </div>
      </div>
      <div className="book-detail__section-heading">
        <div>
          <p className="book-detail__eyebrow">O‘qish rejasi</p>
          <h2>Kitob bo‘limlari</h2>
        </div>
        <span>{detail.sectionsRead}/{total} tugallangan</span>
      </div>
      <div className="book-detail__chapter-list">
        {detail.sections.map((section) => (
          <button
            key={section.id}
            type="button"
            onClick={() => { if (!section.isLocked) { tapLight(); onStart(section.id); } }}
            disabled={section.isLocked}
            title={section.isLocked ? uz.books.sectionLockedHint : undefined}
            className={`book-detail__chapter ${section.isRead ? "is-read" : ""} ${section.isLocked ? "is-locked" : ""}`}
          >
            <span className="book-detail__chapter-number">
              {section.isRead
                ? <Icon name="check" filled />
                : section.isLocked
                  ? <Icon name="lock" filled />
                  : String(section.order).padStart(2, "0")}
            </span>
            <span className="book-detail__chapter-copy">
              <small>{section.isRead ? "O‘qilgan" : section.isLocked ? uz.books.sectionLocked : `${section.order}-bo‘lim`}</small>
              <strong>{section.title}</strong>
              {section.isLocked && <span>{uz.books.sectionLockedHint}</span>}
            </span>
            <span className="book-detail__chapter-arrow"><Icon name={section.isLocked ? "lock" : "arrow_forward"} /></span>
          </button>
        ))}
      </div>
    </Slide>
  );
}

function QuestionSlide({
  question,
  index,
  total,
  selected,
  outcome,
  checking,
  submitting,
  submitError,
  topRightAction,
  onSelect,
  onBack,
  onNext,
}: {
  question: BookQuestionDto;
  index: number;
  total: number;
  selected?: number;
  outcome?: BookQuestionOutcomeDto;
  checking: boolean;
  submitting: boolean;
  submitError: BookSubmitError | null;
  topRightAction?: ReactNode;
  onSelect: (option: number) => void;
  onBack: () => void;
  onNext: () => void;
}) {
  const autoAdvance = useAnswerAutoAdvance({
    enabled: Boolean(outcome) && !checking && !submitting && !submitError,
    onAdvance: onNext,
    resetKey: question.id,
  });

  return (
    <Slide
      accent={outcome ? (outcome.isCorrect ? "green" : "red") : BOOKS_ACCENT}
      topRightAction={topRightAction}
      header={<RunnerHeader title={question.prompt} label={`${index + 1}/${total}`} onBack={onBack} progress={(index + 1) / total} />}
      stageClassName="book-detail__stage--quiz"
      footer={outcome ? (
        <LessonAdvanceAction
          onAdvance={autoAdvance.advance}
          disabled={submitting}
          loading={submitting}
          timerActive={autoAdvance.active}
          remainingSeconds={autoAdvance.remainingSeconds}
          label={index === total - 1 ? uz.books.check : uz.common.next}
       />
      ) : undefined}
    >
      <LessonGuidance title={uz.lessonGuidance.title} items={uz.lessonGuidance.books.quiz} />
      <div className="book-detail__question-title"><p className="book-detail__eyebrow">Bilimingizni tekshiring</p><h2>{question.prompt}</h2></div>
      <div className="book-detail__answers">
        {question.options.map((option, optionIndex) => {
          const isSelected = selected === optionIndex;
          const isCorrectOption = outcome?.correctOptionIndex === optionIndex;
          const answerClass = outcome
            ? isCorrectOption
              ? "is-correct"
              : isSelected
                ? "is-wrong"
                : "is-muted"
            : isSelected
              ? "is-selected"
              : "";

          return (
            <ExerciseOption
              key={optionIndex}
              disabled={checking || Boolean(outcome)}
              onSelect={() => onSelect(optionIndex)}
              selected={isSelected && !outcome}
              state={outcome ? (isCorrectOption ? "correct" : isSelected ? "wrong" : "dim") : "idle"}
              feedbackIcon={outcome && (isCorrectOption || isSelected) ? (isCorrectOption ? "check" : "close") : undefined}
              className={`book-detail__answer ${answerClass}`}
              label={<span className="book-detail__answer-content"><span className="book-detail__answer-letter">
                {checking && isSelected ? <Icon name="progress_activity" className="animate-spin" /> : optionLetter(optionIndex)}
              </span>
              <span className="min-w-0 break-words">{option}</span></span>}
           />
          );
        })}
      </div>
      {outcome && (
        <div className={`book-detail__feedback ${outcome.isCorrect ? "is-correct" : "is-wrong"}`}>
          <span className="book-detail__feedback-icon"><Icon name={outcome.isCorrect ? "check_circle" : "lightbulb"} filled /></span>
          <div><h3>{outcome.isCorrect ? uz.books.correct : uz.books.incorrect}</h3>
          {!outcome.isCorrect && (
            <>
              <p>
                {uz.books.correctAnswer} {optionLetter(outcome.correctOptionIndex)}. {question.options[outcome.correctOptionIndex]}
              </p>
              {outcome.explanation && <p>{outcome.explanation}</p>}
            </>
          )}
          </div></div>
      )}
      {submitError && (
        <div className="book-detail__error" role="alert">
          <p>{submitError.message}</p>
          {submitError.correlationId && <small>{uz.books.errorCode(submitError.correlationId)}</small>}
          {outcome && (
            <button type="button" onClick={onNext}>{uz.books.retrySubmit}</button>
          )}
        </div>
      )}
    </Slide>
  );
}

function ResultSlide({
  result,
  nextSectionExists,
  onContinue,
  onRetry,
}: {
  result: BookQuizResultDto;
  nextSectionExists: boolean;
  onContinue: () => void;
  onRetry: () => void;
}) {
  return (
    <Slide
      accent={result.passed ? "green" : "amber"}
      header={<RunnerHeader title="Bo‘lim natijasi" label={`${result.correctCount}/${result.totalQuestions}`} onBack={result.passed ? onContinue : onRetry} progress={result.scorePercent / 100} />}
      footer={result.passed ? (
        <DuoButton color="yellow" fullWidth size="lg" onClick={onContinue}>{result.bookCompleted || !nextSectionExists ? uz.common.continue : uz.books.nextSection}</DuoButton>
      ) : (
        <DuoButton color="orange" fullWidth size="lg" icon="refresh" onClick={onRetry}>{uz.books.again}</DuoButton>
      )}
    >
      {result.passed && <Confetti show count={80} />}
      <div className="book-detail__result">
        <span className={`book-detail__result-icon ${result.passed ? "is-passed" : "is-retry"}`}>
          <Icon name={result.passed ? "celebration" : "lightbulb"} filled />
        </span>
        <p className="book-detail__eyebrow">Bo‘lim natijasi</p>
        <h2>{uz.books.score(result.correctCount, result.totalQuestions)}</h2>
        <p>{result.passed ? uz.books.sectionPassed : uz.books.sectionFailed}</p>
        <div className="book-detail__score-grid">
          <div><strong>{result.correctCount}</strong><span>To‘g‘ri javob</span></div>
          <div><strong>{result.totalQuestions}</strong><span>Jami savol</span></div>
          <div><strong>{result.scorePercent}%</strong><span>Natija</span></div>
        </div>
      </div>
    </Slide>
  );
}

function CompleteSlide({ detail, read, onExit }: { detail: BookDetailDto; read: number; onExit: () => void }) {
  return (
    <Slide
      accent="amber"
      header={<RunnerHeader title={detail.title} label={`${read}/${detail.sections.length}`} onBack={onExit} progress={1} />}
      footer={<DuoButton color="blue" size="lg" fullWidth icon="auto_stories" onClick={onExit}>{uz.books.backToBooks}</DuoButton>}
    >
      <Confetti show count={100} />
      <div className="book-detail__result">
        <span className="book-detail__result-icon is-complete"><Icon name="workspace_premium" filled /></span>
        <p className="book-detail__eyebrow">Kitob yakunlandi</p>
        <h1>{detail.title}</h1>
        <p>{uz.books.bookComplete}</p>
        <div className="book-detail__completion-card">
          <Icon name="verified" filled />
          <div><strong>{read}/{detail.sections.length}</strong><span>{uz.books.progress(read, detail.sections.length)}</span></div>
        </div>
      </div>
    </Slide>
  );
}

function RunnerHeader({ title, label, progress, onBack }: { title: string; label: string; progress: number; onBack: () => void }) {
  return (
    <div className="book-detail__runner-header">
      <button type="button" onClick={onBack} aria-label={uz.common.back} className="book-detail__runner-back">
        <Icon name="arrow_back" />
      </button>
      <div className="book-detail__runner-track">
        <div className="book-detail__runner-progress" style={{ width: `${Math.round(progress * 100)}%` }} />
      </div>
      <strong className="book-detail__runner-title">{title}</strong>
      <Pill tone="neutral" className="book-detail__runner-pill">{label}</Pill>
    </div>
  );
}

function Slide({ accent, header, footer, bodyClassName, stageClassName, topRightAction, children }: {
  accent: Accent;
  header?: ReactNode;
  footer?: ReactNode;
  bodyClassName?: string;
  stageClassName?: string;
  topRightAction?: ReactNode;
  children: ReactNode;
}) {
  return (
    <motion.section
      initial={{ opacity: 0, x: 42, rotateY: 10 }}
      animate={{ opacity: 1, x: 0, rotateY: 0 }}
      exit={{ opacity: 0, x: -42, rotateY: -10 }}
      transition={{ type: "spring", stiffness: 220, damping: 24 }}
      className={`book-detail__slide book-detail__slide--${accent}`}
      style={{ transformStyle: "preserve-3d" }}
    >
      <LessonStageFrame
        mode="practice"
        width="lg"
        className={`book-detail__stage${stageClassName ? ` ${stageClassName}` : ""}`}
        topRightAction={topRightAction}
        header={header}
        footer={footer}
        bodyClassName={bodyClassName}
      >
        {children}
      </LessonStageFrame>
    </motion.section>
  );
}

function PageState({ icon, text, accent = "green" }: { icon: string; text: string; accent?: Accent }) {
  return (
    <div className="book-detail book-detail__state-wrap">
      <div className={`book-detail__state book-detail__state--${accent}`}>
        <Icon name={icon} filled className="animate-ea-float text-[36px]" />
        <p>{text}</p>
      </div>
    </div>
  );
}
