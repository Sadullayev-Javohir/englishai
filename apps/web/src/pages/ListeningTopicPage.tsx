import { useCallback, useEffect, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api, ApiError } from "@/api/client";
import { ProductEventType } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import type {
  ListeningAnswerCheckDto,
  ListeningExerciseDto,
  ListeningQuizAnswer,
  ListeningQuizResultDto,
} from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { Pill } from "@/components/ui/Pill";
import { Spinner } from "@/components/ui/Spinner";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { TopicImage } from "@/components/TopicImage";
import { publishAssistantContext } from "@/components/assistantContext";
import { LessonFlowAction } from "@/components/LessonFlowAction";
import { ListeningAudioPlayer } from "@/components/listening/ListeningAudioPlayer";
import { markHomeTopicCompleted } from "@/pages/home-concepts/homeRecommendation";
import { ErrorBoundary } from "@/components/ErrorBoundary";
import { useLessonSounds } from "@/components/lesson/useLessonSounds";
import { LessonFeedbackSlot, LessonStageFrame } from "@/components/lesson/LessonStageFrame";
import { LessonProgress } from "@/components/lesson/LessonProgress";
import { LessonAdvanceAction } from "@/components/lesson/LessonAdvanceAction";
import { useAnswerAutoAdvance } from "@/components/lesson/useAnswerAutoAdvance";
import { LessonQuizOption } from "@/components/lesson/LessonQuizOption";
import { cn } from "@/lib/cn";
import { cefrShort } from "@/lib/labels";
import { AppButton, DesignState } from "@/components/design";
import { WrongShake } from "@/components/game";
import "./ListeningTopicPage.css";
import { lessonBackTarget, lessonOriginFrom } from "@/lib/lessonNavigation";

// ─────────────────────────────────────────────────────────────────────────────
// Route entry: loads a listening topic and drives the guided lesson flow, redesigned
// to Pen screens 36–40 with a single viewport-owned lesson frame.
// Stages: hub → audio → practice. One screen at a time; audio-first, then quiz.
// ─────────────────────────────────────────────────────────────────────────────

/** Listening shares Vocabulary's five-heart lesson budget and Pen 37–40 chrome. */
const MAX_HEARTS = 5;
const LISTENING_LESSON_STEPS = [
  { id: "audio", label: "Audio" },
  { id: "practice", label: "Mashq" },
  { id: "result", label: "Yakun" },
] as const;

type Stage = "hub" | "audio" | "practice";
type ListeningLessonStep = typeof LISTENING_LESSON_STEPS[number]["id"];

export function ListeningTopicPage() {
  const { topicId = "" } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const backTarget = lessonBackTarget("listening", lessonOriginFrom(location));
  const [stage, setStage] = useState<Stage>("hub");
  const [attempt, setAttempt] = useState(0);
  useLessonSounds(stage);
  const [pollCount, setPollCount] = useState(0);
  const { data, loading, error, reload } = useAsync(
    () => api.listening.exercise(topicId),
    [topicId, pollCount],
  );
  useDocumentTitle(data?.title, uz.nav.listening);

  useEffect(() => {
    if (!data || data.isReady || loading) return;
    const timer = window.setInterval(() => setPollCount((count) => count + 1), 4000);
    return () => window.clearInterval(timer);
  }, [data, loading]);

  useEffect(() => {
    if (!data?.isReady) return;
    void api.analytics
      .track(getLearnerId(), ProductEventType.TopicOpened, data.topicId)
      .catch(() => undefined);
  }, [data?.topicId, data?.isReady]);

  useEffect(() => {
    if (!data?.isReady) return;
    return publishAssistantContext({
      area: "listening",
      resourceId: data.topicId,
      title: data.title,
      context: [
        `Topic: ${data.topic}`,
        `Level: ${data.level}`,
        `Transcript: ${data.transcript}`,
        `Target words: ${data.targetWords.map((word) => `${word.word} — ${word.translation}`).join(" | ")}`,
        `Questions: ${data.questions.map((question) => question.prompt).join(" | ")}`,
      ].join("\n"),
      focusText: "",
      route: `/listening/topic/${data.topicId}`,
    });
  }, [data]);

  if (loading) {
    return <ModulePageLoader icon="headphones" accent="amber" />;
  }

  if (error || !data) {
    const code = error instanceof ApiError && typeof error.body === "object" && error.body !== null
      ? (error.body as { code?: string }).code
      : undefined;
    const locked = code === "topic_locked";
    const subscription = error instanceof ApiError && error.status === 402;
    const message = locked
      ? uz.listeningTopics.locked
      : subscription
        ? uz.listeningTopics.subscriptionRequired
        : uz.listeningTopics.loadError;

    return (
      <div className="listening-topic listening-topic--centered" data-module="listening" data-anim>
        <DesignState
          className={cn("listening-topic__state", "listening-topic__state--danger")}
          icon={locked ? "lock" : "cloud_off"}
          title={message}
          action={(
            <div className="listening-topic__state-actions">
              {!locked && !subscription && (
                <AppButton tone="primary" leadingIcon="refresh" onClick={() => reload()}>{uz.listeningTopics.retry}</AppButton>
              )}
              <AppButton tone="standard" leadingIcon="arrow_back" onClick={() => navigate(backTarget)}>{uz.listeningTopics.back}</AppButton>
            </div>
          )}
        />
      </div>
    );
  }

  if (!data.isReady) {
    return (
      <div className="listening-topic listening-topic--centered" data-module="listening" data-anim>
        <DesignState
          className="listening-topic__state"
          icon="hourglass_empty"
          title={uz.listeningTopics.pending}
          action={<AppButton tone="primary" leadingIcon="refresh" onClick={() => reload()}>{uz.listeningTopics.retry}</AppButton>}
        />
      </div>
    );
  }

  return (
    <div className={`listening-topic listening-topic--${stage}`} data-module="listening" data-anim>
      <div className="listening-topic__inner">
        <ErrorBoundary>
          <AnimatePresence mode="wait">
            {stage === "hub" && (
              <HubStage key="hub" topic={data} backTarget={backTarget} onStart={() => setStage("audio")} />
            )}
            {stage === "audio" && (
              <AudioStage
                key={`audio-${attempt}`}
                topic={data}
                onBack={() => setStage("hub")}
                onStartQuiz={() => setStage("practice")}
             />
            )}
            {stage === "practice" && (
              <PracticeStage
                key={`practice-${attempt}`}
                topic={data}
                onExit={() => setStage("audio")}
                onRetry={() => {
                  setAttempt((value) => value + 1);
                  setStage("audio");
                }}
             />
            )}
          </AnimatePresence>
        </ErrorBoundary>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Vocabulary and Listening use the same labelled lesson progress presentation.
// Questions remain a substep of "Mashq" instead of creating decorative segments.
// ─────────────────────────────────────────────────────────────────────────────

function ListeningProgress({
  step,
  substep,
  hearts = MAX_HEARTS,
  backTarget,
  onBack,
  complete = false,
}: {
  step: ListeningLessonStep;
  substep?: string;
  hearts?: number;
  backTarget?: string;
  onBack?: () => void;
  complete?: boolean;
}) {
  return (
    <LessonProgress
      steps={LISTENING_LESSON_STEPS}
      step={step}
      substep={substep}
      hearts={hearts}
      heartLabel={`${hearts} / ${MAX_HEARTS} yurak`}
      backTarget={backTarget}
      onBack={onBack}
      complete={complete}
      classPrefix="listening-progress"
    />
  );
}

function ListeningSlideShell({
  step,
  substep,
  hearts = MAX_HEARTS,
  complete = false,
  backTarget,
  onBack,
  mode = "focus",
  className,
  footer,
  children,
}: {
  step: ListeningLessonStep;
  substep?: string;
  hearts?: number;
  complete?: boolean;
  backTarget?: string;
  onBack?: () => void;
  mode?: "focus" | "flow";
  className?: string;
  footer: React.ReactNode;
  children: React.ReactNode;
}) {
  const header = <ListeningProgress step={step} substep={substep} hearts={hearts} complete={complete} backTarget={backTarget} onBack={onBack} />;

  return (
    <motion.div
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -14 }}
      className={cn("listening-topic__stage", `listening-topic__stage--${mode}`)}
    >
      <LessonStageFrame
        mode={mode}
        width="md"
        className={cn("listening-topic__slide", className)}
        header={header}
        footer={<div className="listening-topic__slide-action">{footer}</div>}
      >
        {children}
      </LessonStageFrame>
    </motion.div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Stage 0 - Hub: the topic hero (image + title) and the two-step preview, then
// the launch button.
// ─────────────────────────────────────────────────────────────────────────────

const HUB_STEPS = [
  { icon: "headphones", label: uz.listeningTopics.audioTitle },
  { icon: "quiz", label: uz.listeningTopics.toQuiz },
];

function HubStage({ topic, backTarget, onStart }: { topic: ListeningExerciseDto; backTarget: string; onStart: () => void }) {
  return (
    <ListeningSlideShell
      step="audio"
      backTarget={backTarget}
      footer={
        <AppButton tone="primary" size="lg" fullWidth leadingIcon="play_arrow" onClick={onStart}>
          {uz.listeningTopics.hubStart}
        </AppButton>
      }
    >
      <ListeningHeading title={topic.title} subtitle="Avval tinglang. Keyin savollarga javob bering." />
      <motion.section
        data-pen-screen="37-introduction"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="listening-topic__hero"
      >
        <div className="listening-topic__hero-media">
          <TopicImage topicId={topic.topicId} title={topic.title} level={topic.level} category={topic.topic} hideLevelBadge hideTitle className="h-full w-full" />
        </div>
        <Pill tone="neutral" className="listening-topic__hero-level">{cefrShort(topic.level)}</Pill>
        <h2 className="listening-topic__hero-title">Bir audio. Yangi imkoniyat.</h2>
        <p className="listening-topic__hero-subtitle">{topic.topic}</p>

        <ol className="listening-topic__steps" aria-label={uz.lessonGuidance.title}>
          {HUB_STEPS.map((step, i) => (
            <li key={step.icon} className="listening-topic__step">
              <span className="listening-topic__step-icon"><Icon name={step.icon} filled /></span>
              <span className="listening-topic__step-label">{step.label}</span>
              {i < HUB_STEPS.length - 1 && <Icon name="chevron_right" className="listening-topic__step-arrow" />}
            </li>
          ))}
        </ol>
      </motion.section>
    </ListeningSlideShell>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Stage 1 - Audio. A clean player card (hero icon + hint), transport controls
// (progress + play/replay), then "to quiz" once the clip has been heard.
// ─────────────────────────────────────────────────────────────────────────────

function ListeningHeading({ title, subtitle }: { title: string; subtitle?: string }) {
  return <div className="listening-topic__heading"><span className="listening-tag">LISTENING</span><h1>{title}</h1>{subtitle && <p>{subtitle}</p>}</div>;
}

function transcriptEvidence(transcript: string) {
  const normalized = transcript.trim().replace(/\s+/g, " ");
  const [firstSentence] = normalized.match(/[^.!?]+[.!?]?/) ?? [];
  return firstSentence?.trim() || normalized || uz.listeningTopics.noTranscript;
}

function AudioStage({ topic, onBack, onStartQuiz }: {
  topic: ListeningExerciseDto; onBack: () => void; onStartQuiz: () => void;
}) {
  const [hasListened, setHasListened] = useState(false);
  return (
    <ListeningSlideShell step="audio" onBack={onBack} className="listening-topic__slide--audio"
      footer={<div className="listening-topic__footer-action">
        <AppButton tone="primary" size="lg" fullWidth aria-label={uz.listeningTopics.toQuiz} leadingIcon="quiz" onClick={onStartQuiz} disabled={!hasListened}>Mashqqa o‘tish</AppButton>
        {!hasListened && <p className="listening-topic__footer-hint">Avval audioni tinglang.</p>}
      </div>}>
      <ListeningHeading title={topic.title} subtitle="Avval tinglang. Keyin savollarga javob bering." />
      <div className="listening-topic__audio-card" data-pen-screen="37" data-testid="listening-audio-controls-card">
        <div className="listening-topic__audio-content">
          <div className="listening-topic__episode-photo"><TopicImage topicId={topic.topicId} title={topic.title} level={topic.level} hideLevelBadge hideTitle className="h-full w-full" /></div>
          <span className="listening-tag">{cefrShort(topic.level)} · {topic.topic}</span>
          <h2>Bir audio.<br />Bir kichik hikoya.</h2>
        </div>
        <ListeningAudioPlayer topicId={topic.topicId} title={topic.title} onListened={() => setHasListened(true)} onPlaybackError={() => setHasListened(false)} />
      </div>
      <p className="listening-topic__audio-hint"><Icon name="lightbulb" />{uz.listeningTopics.audioHint}</p>
      <details className="listening-topic__disclosure"><summary>Transkriptni ko‘rish</summary><p>{topic.transcript || uz.listeningTopics.noTranscript}</p></details>
    </ListeningSlideShell>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Stage 2 - Practice. Comprehension quiz, one question per screen, instant
// right/wrong feedback, hearts, then the transcript review + final result.
// ─────────────────────────────────────────────────────────────────────────────

function PracticeStage({
  topic,
  onExit,
  onRetry,
}: {
  topic: ListeningExerciseDto;
  onExit: () => void;
  onRetry: () => void;
}) {
  const learnerId = getLearnerId();
  const [index, setIndex] = useState(0);
  const [selectedOptionIndex, setSelectedOptionIndex] = useState<number | null>(null);
  const [feedback, setFeedback] = useState<ListeningAnswerCheckDto | null>(null);
  const [checking, setChecking] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ListeningQuizResultDto | null>(null);
  const [hearts, setHearts] = useState(MAX_HEARTS);
  const submitStartedRef = useRef(false);
  const answersRef = useRef<ListeningQuizAnswer[]>([]);
  const { playAnswer, playComplete } = useLessonSounds(`listening-question-${index}`);
  const questions = topic.questions;
  const question = questions[index];

  useEffect(() => {
    if (result) playComplete();
  }, [playComplete, result]);

  const checkSelected = useCallback(async () => {
    if (!question || selectedOptionIndex === null || checking || feedback) return;
    setChecking(true);
    setError(null);
    try {
      const checked = await api.listening.checkAnswer(topic.topicId, question.id, selectedOptionIndex);
      setFeedback(checked);
      const nextAnswers = [
        ...answersRef.current.filter((answer) => answer.questionId !== question.id),
        { questionId: question.id, selectedOptionIndex },
      ];
      answersRef.current = nextAnswers;
      if (!checked.isCorrect) {
        setHearts((count) => Math.max(0, count - 1));
      }
      playAnswer(checked.isCorrect);
    } catch {
      setSelectedOptionIndex(null);
      setError(uz.listeningTopics.checkError);
    } finally {
      setChecking(false);
    }
  }, [checking, feedback, playAnswer, question, selectedOptionIndex, topic.topicId]);

  const submit = useCallback(async (finalAnswers: ListeningQuizAnswer[]) => {
    if (submitStartedRef.current) return;
    const answerIds = new Set(finalAnswers.map((answer) => answer.questionId));
    const hasEveryAnswer = finalAnswers.length === questions.length
      && questions.every((item) => answerIds.has(item.id));
    if (!hasEveryAnswer) {
      return;
    }

    submitStartedRef.current = true;
    setSubmitting(true);
    setError(null);
    try {
      setResult(await api.listening.submit(topic.topicId, learnerId, finalAnswers));
    } catch {
      submitStartedRef.current = false;
      setError(uz.listeningTopics.submitError);
    } finally {
      setSubmitting(false);
    }
  }, [learnerId, questions, topic.topicId]);

  const next = useCallback(() => {
    if (!feedback) return;
    if (index >= questions.length - 1) {
      void submit(answersRef.current);
      return;
    }
    setIndex((value) => value + 1);
    setSelectedOptionIndex(null);
    setFeedback(null);
    setError(null);
  }, [feedback, index, questions.length, setIndex, submit]);

  const autoAdvance = useAnswerAutoAdvance({
    enabled: feedback?.isCorrect === true && !checking && !submitting && error === null,
    onAdvance: next,
    resetKey: question?.id,
  });

  const cancelAutoAdvance = autoAdvance.cancel;
  useEffect(() => {
    cancelAutoAdvance();
    setSelectedOptionIndex(null);
  }, [cancelAutoAdvance, question?.id]);

  if (submitting) {
    return <div className="listening-topic__loading" data-anim><Spinner /></div>;
  }

  if (result) {
    return <ResultStage topic={topic} result={result} onRetry={onRetry} />;
  }

  if (hearts === 0) {
    return (
      <motion.div
        initial={{ opacity: 0, scale: 0.94, y: 16 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        className="listening-topic__empty-wrap"
      >
        <DesignState
          className="listening-topic__state listening-topic__state--danger"
          icon="heart_broken"
          title={uz.listeningTopics.heartsEmpty}
          action={<AppButton tone="danger" leadingIcon="refresh" onClick={onRetry}>{uz.listeningTopics.retryLesson}</AppButton>}
        />
      </motion.div>
    );
  }

  if (!question) {
    return (
      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        className="listening-topic__empty-wrap"
      >
        <DesignState
          className="listening-topic__state"
          icon="quiz"
          title={uz.listeningTopics.noQuestions}
          action={<AppButton tone="primary" leadingIcon="arrow_back" onClick={onExit}>{uz.listening.back}</AppButton>}
        />
      </motion.div>
    );
  }

  return (
    <LessonStageFrame
      mode="practice"
      width="md"
      className="listening-topic__practice"
      data-pen-screen={feedback?.isCorrect === false ? "39" : "38"}
      headerClassName="listening-topic__practice-header"
      header={(
        <ListeningProgress
          step="practice"
          substep={`Savol ${index + 1} / ${questions.length}`}
          hearts={hearts}
          onBack={onExit}
        />
      )}
      bodyClassName="listening-topic__practice-body"
      footer={(
        <div className="listening-topic__footer-action listening-topic__footer-action--reserved" data-testid={autoAdvance.active ? "answer-auto-advance" : "listening-footer-slot"}>
          {feedback ? (
            <LessonAdvanceAction onAdvance={feedback.isCorrect ? autoAdvance.advance : next} disabled={submitting} loading={submitting} timerActive={autoAdvance.active} remainingSeconds={autoAdvance.remainingSeconds} label={index >= questions.length - 1 ? uz.listening.check : uz.listeningTopics.nextExercise} />
          ) : (
            <AppButton
              tone="primary"
              size="lg"
              fullWidth
              trailingIcon="arrow_forward"
              disabled={selectedOptionIndex === null}
              loading={checking}
              onClick={() => void checkSelected()}
            >
              {uz.listening.check}
            </AppButton>
          )}
        </div>
      )}
    >
      <ListeningHeading title={feedback?.isCorrect === false ? "Muhim joyini yana tinglang." : "Nimani eshitdingiz?"} />
      {feedback?.isCorrect !== false && <ListeningAudioPlayer topicId={topic.topicId} title={topic.title} />}
      <motion.section
        key={question.id}
        initial={{ opacity: 0, y: 18 }}
        animate={{ opacity: 1, y: 0 }}
        className="listening-topic__exercise"
      >
        {feedback?.isCorrect !== false && <h2 className="listening-topic__question">{question.prompt}</h2>}

        <WrongShake shake={feedback?.isCorrect === false}>
          <div className="lesson-quiz-options listening-topic__options-grid">
            {question.options.map((option, optionIndex) => {
              const isSelected = (feedback?.selectedOptionIndex ?? selectedOptionIndex) === optionIndex;
              const isCorrect = feedback?.correctOptionIndex === optionIndex;
              // Pen 39 compares the learner's answer with the correct one.
              if (feedback?.isCorrect === false && !isSelected && !isCorrect) return null;
              const state = !feedback
                ? "idle"
                : isCorrect
                  ? "correct"
                  : isSelected
                    ? "wrong"
                    : "dim";
              return (
                <LessonQuizOption
                  key={optionIndex}
                  index={optionIndex}
                  label={option}
                  state={state}
                  selected={isSelected}
                  disabled={checking || feedback !== null}
                  onSelect={() => setSelectedOptionIndex(optionIndex)}
                  classPrefix="listening-topic__option"
                />
              );
            })}
          </div>
        </WrongShake>

        {(checking || feedback || error) && (
          <div className="listening-topic__answer-status">
            <div className="listening-topic__checking-slot">
              {checking && <div role="status" className="listening-topic__checking"><Spinner /><span>{uz.common.loading}</span></div>}
            </div>

            <LessonFeedbackSlot className="listening-topic__feedback-slot">
              <AnimatePresence initial={false}>
                {feedback && (
                  <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="listening-topic__feedback-content">
                    <div className={cn("listening-topic__feedback", feedback.isCorrect ? "is-correct" : "is-wrong")}>
                      <Icon name={feedback.isCorrect ? "check_circle" : "cancel"} filled className="listening-topic__feedback-icon" />
                      <div className="listening-topic__feedback-copy">
                        <p className="listening-topic__feedback-title">
                          {feedback.isCorrect ? uz.listening.correct : uz.listening.incorrect}
                        </p>
                        {!feedback.isCorrect && (
                          <p className="listening-topic__feedback-note">
                            {uz.listening.correctAnswer} {question.options[feedback.correctOptionIndex]}
                          </p>
                        )}
                        {feedback.hint && <p className="listening-topic__feedback-note">{feedback.hint}</p>}
                      </div>
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>

              {error && (
                <div className="listening-topic__feedback is-wrong" role="alert">
                  <Icon name="error" filled className="listening-topic__feedback-icon" />
                  <div className="listening-topic__feedback-copy"><p className="listening-topic__feedback-title">{error}</p></div>
                </div>
              )}
            </LessonFeedbackSlot>
          </div>
        )}
      </motion.section>
      {feedback?.isCorrect === false && (
        <section className="listening-topic__transcript-evidence" aria-label="Audio parchasi">
          <span className="listening-tag">AUDIO PARCHASI</span>
          <p>“{transcriptEvidence(topic.transcript)}”</p>
          <ListeningAudioPlayer topicId={topic.topicId} title={topic.title} />
        </section>
      )}
    </LessonStageFrame>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Result - transcript review first (so learners can reread), then the shared
// TopicLessonResult with score + reward.
// ─────────────────────────────────────────────────────────────────────────────

function ResultStage({
  topic,
  result,
  onRetry,
}: {
  topic: ListeningExerciseDto;
  result: ListeningQuizResultDto;
  onRetry: () => void;
}) {
  const mastered = result.passed && result.completion?.isMastered === true;
  const navigate = useNavigate();
  useEffect(() => { if (mastered) markHomeTopicCompleted(getLearnerId(), topic.topicId); }, [mastered, topic.topicId]);
  const [showResult, setShowResult] = useState(false);

  if (!showResult) {
    return (
      <ListeningSlideShell
        mode="flow"
        step="result"
        onBack={() => setShowResult(true)}
        className="listening-topic__slide--transcript"
        footer={(
          <AppButton tone="primary" size="lg" fullWidth leadingIcon="visibility" onClick={() => setShowResult(true)}>
            Natijani ko'rish
          </AppButton>
        )}
      >
        <div className="listening-topic__transcript-head">
          <Pill tone="neutral" className="listening-topic__hero-level">{uz.listeningTopics.transcriptTitle}</Pill>
        </div>
        <div className="listening-topic__transcript-card">
          <p>{topic.transcript || uz.listeningTopics.noTranscript}</p>
        </div>
      </ListeningSlideShell>
    );
  }

  return (
    <LessonStageFrame mode="focus" className="listening-topic__result" data-pen-screen="40"
      header={<ListeningProgress step="result" complete hearts={MAX_HEARTS} onBack={onRetry} />}
      footer={<div className="listening-topic__footer-action">
        {result.passed && result.completion
          ? <LessonFlowAction current="listening" topicId={topic.topicId} topicTitle={topic.title} completion={result.completion} passed={result.passed} />
          : <AppButton tone="primary" fullWidth onClick={() => navigate("/home")}>Bugungi rejaga qaytish</AppButton>}
        <button type="button" className="listening-topic__retry" onClick={onRetry}>{uz.listeningTopics.retryLesson}</button>
      </div>}>
      <div className="listening-topic__result-content">
        <img className="listening-topic__parrot" src="/assets/play/parrot.svg" alt="" width={180} height={150} />
        <span className="listening-tag">{result.passed ? "BIR QADAM OLDINGA" : "YANA BIRGA MASHQ QILAMIZ"}</span>
        <h1>{result.passed ? "Endi yanada aniq eshitasiz." : uz.listening.notPassed}</h1>
        <dl className="listening-topic__metrics">
          <div><dd>{result.correctCount}/{result.totalQuestions}</dd><dt>to‘g‘ri</dt></div>
          <div><dd>{result.scorePercent}%</dd><dt>natija</dt></div>
        </dl>
        <div className="listening-topic__saved" role="status"><h2><Icon name="check_circle" />Natijangiz saqlandi.</h2><p>{result.passed ? "Bugun o‘rganganingizni ertaga ham ishlating." : uz.listeningTopics.passHint}</p></div>
        {topic.targetWords[0] && <div className="listening-topic__remember"><span>ESLAB QOLING</span><h2>{topic.targetWords[0].word}</h2><p>{topic.targetWords[0].translation}</p></div>}
      </div>
    </LessonStageFrame>
  );
}
