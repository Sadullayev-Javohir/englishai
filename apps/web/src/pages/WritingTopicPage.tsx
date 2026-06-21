import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import {
  ProductEventType,
  WritingDimension,
  WritingAssessmentDto,
  WritingTaskDto,
} from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import { Icon } from "@/components/ui/Icon";
import { Pill } from "@/components/ui/Pill";
import { Spinner } from "@/components/ui/Spinner";
import { ErrorBoundary } from "@/components/ErrorBoundary";
import { cn } from "@/lib/cn";
import { cefrShort } from "@/lib/labels";
import { TopicImage } from "@/components/TopicImage";
import { publishAssistantContext } from "@/components/assistantContext";
import { Confetti, RewardPop, Friend } from "@/components/game";
import { LessonStageFrame, LessonGuidance } from "@/components/lesson/LessonStageFrame";
import {
  AppButton,
  AppIconButton,
  DesignConfirm,
  DesignState,
  DesignTextarea,
} from "@/components/design";
import "./WritingTopicPage.css";

const MAX_HEARTS = 5;
const TOPIC_STAGES = ["hub", "prompt", "task", "practice", "done"] as const;

const DIMENSION_LABEL: Record<WritingDimension, string> = {
  [WritingDimension.TaskAchievement]: uz.writing.dimensions.taskAchievement,
  [WritingDimension.Coherence]: uz.writing.dimensions.coherence,
  [WritingDimension.LexicalResource]: uz.writing.dimensions.lexicalResource,
  [WritingDimension.GrammaticalAccuracy]: uz.writing.dimensions.grammaticalAccuracy,
};

// ─────────────────────────────────────────────────────────────────────────────
// Route entry: loads a topic's writing task and drives the guided lesson flow,
// redesigned to the /home Professional-Indigo language (coral writing accent,
// clean token-only cards). Stages: hub → prompt → task → practice → done.
// One screen at a time; the AI 4-dimension assessment (rule §17 Writing) is kept.
// ─────────────────────────────────────────────────────────────────────────────

type Stage = "hub" | "prompt" | "task" | "practice" | "done";

function topicStageProgress(stage: Stage) {
  return `${TOPIC_STAGES.indexOf(stage) + 1} / ${TOPIC_STAGES.length}`;
}

function keepTopicEditorVisible(element: HTMLTextAreaElement | null) {
  element?.scrollIntoView({ block: "nearest", inline: "nearest" });
}

export function WritingTopicPage() {
  const { topicId = "" } = useParams();
  const id = topicId;
  const navigate = useNavigate();
  const [stage, setStage] = useState<Stage>("hub");
  const [pollCount, setPollCount] = useState(0);
  const { data, loading, error, reload } = useAsync(
    () => api.writing.task(id),
    [id, pollCount]
  );
  useDocumentTitle(data?.title, uz.nav.writing);

  // The AI assessment produced when the learner submits the writing task; passed into the
  // practice (result) stage so we never re-submit empty text.
  const [assessment, setAssessment] = useState<WritingAssessmentDto | null>(null);

  useEffect(() => {
    if (!data?.isReady) return;
    return publishAssistantContext({
      area: "writing",
      resourceId: data.topicId,
      title: data.title,
      context: [
        `Prompt: ${data.prompt}`,
        `Word target: ${data.minWords}-${data.maxWords}`,
        `Guidance: ${data.guidance.join(" | ")}`,
        `Target words: ${data.targetWords.map((word) => word.word).join(", ")}`,
      ].join("\n"),
      focusText: "",
      route: `/writing/topic/${data.topicId}`,
    });
  }, [data]);

  useEffect(() => {
    if (!data || data.isReady || loading) return;
    const t = setInterval(() => setPollCount((n) => n + 1), 4000);
    return () => clearInterval(t);
  }, [data, loading]);

  useEffect(() => {
    if (!data?.isReady) return;
    void api.analytics
      .track(getLearnerId(), ProductEventType.TopicOpened, data.topicId)
      .catch(() => undefined);
  }, [data?.topicId, data?.isReady]);

  if (loading) {
    return (
      <div className="writing-topic writing-topic--centered" data-module="writing" data-anim>
        <Spinner />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="writing-topic writing-topic--centered" data-module="writing" data-anim>
        <DesignState
          className="writing-topic__state"
          icon="cloud_off"
          title={uz.common.error}
          action={<AppButton tone="primary" leadingIcon="refresh" onClick={() => reload()}>{uz.writingTopics.retry}</AppButton>}
        />
      </div>
    );
  }

  if (!data.isReady) {
    return (
      <div className="writing-topic writing-topic--centered" data-module="writing" data-anim>
        <DesignState
          className="writing-topic__state"
          icon="hourglass_empty"
          title={uz.writingTopics.pending}
          action={<AppButton tone="primary" leadingIcon="refresh" onClick={() => reload()}>{uz.writingTopics.retry}</AppButton>}
        />
      </div>
    );
  }

  return (
    <div className={`writing-topic writing-topic--${stage}`} data-module="writing" data-anim>
      <div className="writing-topic__inner">
        <ErrorBoundary>
          <AnimatePresence mode="wait">
            {stage === "hub" && (
              <HubStage
                key="hub"
                topic={data}
                onExit={() => navigate("/writing")}
                onStart={() => setStage("prompt")}
              />
            )}
            {stage === "prompt" && (
              <PromptStage
                key="prompt"
                topic={data}
                onBack={() => setStage("hub")}
                onToTask={() => setStage("task")}
              />
            )}
            {stage === "task" && (
              <TaskStage
                key="task"
                topic={data}
                onBack={() => setStage("prompt")}
                onSubmitted={(a) => {
                  setAssessment(a);
                  setStage("practice");
                }}
              />
            )}
            {stage === "practice" && (
              <PracticeStage
                key="practice"
                assessment={assessment}
                title={data.title}
                onBack={() => setStage("task")}
                onFinished={() => setStage("done")}
              />
            )}
            {stage === "done" && (
              <DoneStage key="done" onExit={() => navigate("/writing")} />
            )}
          </AnimatePresence>
        </ErrorBoundary>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Shared clean HUD pieces (progress track + hearts/xp chips), reused by every
// slide header so the whole lesson shares one calm chrome.
// ─────────────────────────────────────────────────────────────────────────────

function ProgressTrack({ progress, total }: { progress: number; total: number }) {
  return (
    <div
      className="writing-topic__progress"
      role="progressbar"
      aria-label={`${progress} / ${total}`}
      aria-valuemin={0}
      aria-valuemax={total}
      aria-valuenow={progress}
    >
      <span style={{ width: `${total > 0 ? (progress / total) * 100 : 0}%` }} />
    </div>
  );
}

function HeartChip({ hearts }: { hearts: number }) {
  return (
    <span className="writing-topic__chip writing-topic__chip--heart">
      <Icon name="favorite" filled />
      {hearts}
    </span>
  );
}

function XpChip({ xp }: { xp: number }) {
  return (
    <span className="writing-topic__chip writing-topic__chip--xp">
      <Icon name="bolt" filled />
      {xp}
    </span>
  );
}

function WritingSlideShell({
  stage,
  title,
  status,
  xp = 0,
  onExit,
  mode = "focus",
  bodyClassName,
  footer,
  children,
}: {
  stage: Stage;
  title: string;
  status: string;
  xp?: number;
  onExit: () => void;
  mode?: "focus" | "flow";
  bodyClassName?: string;
  footer: React.ReactNode;
  children: React.ReactNode;
}) {
  const [stopOpen, setStopOpen] = useState(false);
  const index = TOPIC_STAGES.indexOf(stage);

  const header = (
    <div className="writing-topic__slide-header">
      <div className="writing-topic__hud">
        <AppIconButton
          icon={stage === "hub" ? "close" : "arrow_back"}
          label={uz.writingTopics.back}
          tone="standard"
          onClick={onExit}
          className="writing-topic__back-btn"
        />
        <ProgressTrack progress={index + 1} total={TOPIC_STAGES.length} />
        <HeartChip hearts={MAX_HEARTS} />
        <XpChip xp={xp} />
        <span className="writing-topic__counter" aria-hidden="true">{topicStageProgress(stage)}</span>
        <button type="button" className="writing-topic__stop" onClick={() => setStopOpen(true)}>
          <Icon name="stop_circle" filled />
          <span>To‘xtatish</span>
        </button>
      </div>
      <div className="writing-topic__heading">
        <p className="writing-topic__title" title={title}>{title}</p>
        <span className="writing-topic__status">{status}</span>
      </div>
      <DesignConfirm
        open={stopOpen}
        onClose={() => setStopOpen(false)}
        title="Darsni to‘xtatish"
        message="Joriy bosqich saqlanadi va yozish mavzulariga qaytiladi. Davom etasizmi?"
        confirmLabel="Ha, to‘xtatish"
        destructive
        onConfirm={onExit}
      />
    </div>
  );

  return (
    <motion.div
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -14 }}
      transition={{ type: "spring", stiffness: 220, damping: 24 }}
      className={cn("writing-topic__stage", `writing-topic__stage--${mode}`)}
    >
      <LessonStageFrame
        mode={mode}
        width="lg"
        className={cn("writing-topic__slide", mode === "flow" && "writing-topic__slide--scroll")}
        header={header}
        bodyClassName={bodyClassName}
        footer={<div className="writing-topic__slide-action">{footer}</div>}
      >
        {children}
      </LessonStageFrame>
    </motion.div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Stage 0 - Hub: topic overview (hero image + title + prompt) and the start action.
// ─────────────────────────────────────────────────────────────────────────────

function HubStage({
  topic,
  onExit,
  onStart,
}: {
  topic: WritingTaskDto;
  onExit: () => void;
  onStart: () => void;
}) {
  return (
    <WritingSlideShell
      stage="hub"
      title={topic.title}
      status={uz.writingTopics.hubStart}
      onExit={onExit}
      footer={
        <AppButton tone="primary" size="lg" fullWidth leadingIcon="play_arrow" onClick={onStart}>
          {uz.writingTopics.hubStart}
        </AppButton>
      }
    >
      <motion.section
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="writing-topic__hero"
      >
        <div className="writing-topic__hero-media">
          <TopicImage
            topicId={topic.topicId}
            title={topic.title}
            level={topic.level}
            category={topic.title}
            hideLevelBadge
            className="h-full w-full"
          />
        </div>
        <Pill tone="neutral" className="writing-topic__hero-level">{cefrShort(topic.level)}</Pill>
        <h1 className="writing-topic__hero-title">{topic.title}</h1>
        <p className="writing-topic__hero-subtitle">{topic.prompt}</p>
      </motion.section>
    </WritingSlideShell>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Stage 1 - Namuna va ko'rsatma. Shows the prompt + guidance, then "Yozish".
// ─────────────────────────────────────────────────────────────────────────────

function PromptStage({
  topic,
  onBack,
  onToTask,
}: {
  topic: WritingTaskDto;
  onBack: () => void;
  onToTask: () => void;
}) {
  return (
    <WritingSlideShell
      stage="prompt"
      title={topic.title}
      status={uz.writingTopics.sampleTitle}
      onExit={onBack}
      mode="flow"
      bodyClassName="writing-topic__slide-body--scroll"
      footer={
        <AppButton tone="primary" size="lg" fullWidth leadingIcon="edit_note" onClick={onToTask}>
          {uz.writingTopics.toTask}
        </AppButton>
      }
    >
      <LessonGuidance title={uz.lessonGuidance.title} items={uz.lessonGuidance.writing.guidance} />
      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        className="writing-topic__card writing-topic__card--prompt"
      >
        <Pill tone="neutral" className="writing-topic__card-pill">{uz.writingTopics.sampleTitle}</Pill>
        <h2 className="writing-topic__prompt-text">{topic.prompt}</h2>

        {topic.guidance.length > 0 && (
          <div className="writing-topic__guidance">
            <p className="writing-topic__guidance-title">
              <Icon name="lightbulb" filled />
              {uz.writingTopics.guidanceTitle}
            </p>
            <ul className="writing-topic__guidance-list">
              {topic.guidance.map((g, i) => (
                <li key={i}>{g}</li>
              ))}
            </ul>
          </div>
        )}

        <p className="writing-topic__word-target">
          {uz.writingTopics.wordTarget(topic.minWords, topic.maxWords)}
        </p>
      </motion.section>
    </WritingSlideShell>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Stage 2 - Yozish topshirig'i. Free-text editor + submit → AI assessment.
// ─────────────────────────────────────────────────────────────────────────────

function TaskStage({
  topic,
  onBack,
  onSubmitted,
}: {
  topic: WritingTaskDto;
  onBack: () => void;
  onSubmitted: (assessment: WritingAssessmentDto) => void;
}) {
  const learnerId = getLearnerId();
  const [text, setText] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState(false);
  const editorRef = useRef<HTMLTextAreaElement>(null);

  const wordCount = text.trim() === "" ? 0 : text.trim().split(/\s+/).length;
  const overLimit = wordCount > topic.maxWords;

  useEffect(() => {
    const keepVisible = () => {
      if (document.activeElement !== editorRef.current) return;
      window.requestAnimationFrame(() => keepTopicEditorVisible(editorRef.current));
    };
    const viewport = window.visualViewport;
    viewport?.addEventListener("resize", keepVisible);
    viewport?.addEventListener("scroll", keepVisible);
    window.addEventListener("resize", keepVisible);
    return () => {
      viewport?.removeEventListener("resize", keepVisible);
      viewport?.removeEventListener("scroll", keepVisible);
      window.removeEventListener("resize", keepVisible);
    };
  }, []);

  useEffect(() => publishAssistantContext({
    area: "writing",
    resourceId: topic.topicId,
    title: topic.title,
    context: [
      `Prompt: ${topic.prompt}`,
      `Word target: ${topic.minWords}-${topic.maxWords}`,
      `Guidance: ${topic.guidance.join(" | ")}`,
      `Target words: ${topic.targetWords.map((word) => word.word).join(", ")}`,
    ].join("\n"),
    focusText: text,
  }), [text, topic]);

  async function submit() {
    if (submitting || wordCount === 0 || overLimit) return;
    setSubmitting(true);
    setSubmitError(false);
    try {
      const result = await api.writing.submit(learnerId, topic.topicId, text);
      onSubmitted(result);
    } catch {
      setSubmitError(true);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <WritingSlideShell
      stage="task"
      title={topic.prompt}
      status={submitting ? uz.writingTopics.submitting : uz.writingTopics.taskTitle}
      onExit={onBack}
      mode="flow"
      bodyClassName="writing-topic__slide-body--scroll"
      footer={
        <AppButton
          tone="primary"
          size="lg"
          fullWidth
          leadingIcon="spellcheck"
          onClick={submit}
          disabled={wordCount === 0 || overLimit || submitting}
          loading={submitting}
        >
          {submitting ? uz.writingTopics.submitting : uz.writingTopics.submit}
        </AppButton>
      }
    >
      <LessonGuidance title={uz.lessonGuidance.title} items={uz.lessonGuidance.writing.editor} />
      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        className="writing-topic__card writing-topic__card--editor"
      >
        <Pill tone="neutral" className="writing-topic__card-pill">{uz.writingTopics.taskTitle}</Pill>
        <p className="writing-topic__editor-label">{uz.writingTopics.yourAnswer}</p>
        <DesignTextarea
          ref={editorRef}
          value={text}
          onChange={(e) => setText(e.target.value)}
          onFocus={() => window.requestAnimationFrame(() => keepTopicEditorVisible(editorRef.current))}
          placeholder={uz.writingTopics.placeholder}
          disabled={submitting}
          className="writing-topic__editor"
        />
        <div className="writing-topic__editor-meta">
          <span
            className={cn(
              "writing-topic__editor-count",
              overLimit && "writing-topic__editor-count--invalid"
            )}
          >
            {uz.writingTopics.wordCount(wordCount)}
            {overLimit && ` · ${uz.writingTopics.overLimit(topic.maxWords)}`}
          </span>
          <span>{uz.writingTopics.wordTarget(topic.minWords, topic.maxWords)}</span>
        </div>
        <div className="writing-topic__feedback-slot" aria-live="polite" aria-busy={submitting || undefined}>
          {submitting ? (
            <div className="writing-topic__feedback-loading" role="status">
              <Icon name="progress_activity" className="animate-spin" />
              <span>{uz.writingTopics.submitting}</span>
            </div>
          ) : submitError ? (
            <p role="alert" className="writing-topic__submit-error">{uz.common.error}</p>
          ) : null}
        </div>
      </motion.section>
    </WritingSlideShell>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Stage 3 - Baholash natijasi (practice). Shows the AI score + 4 dimension bars.
// ─────────────────────────────────────────────────────────────────────────────

function DimensionBar({ label, score }: { label: string; score: number }) {
  return (
    <div className="writing-topic__dim">
      <div className="writing-topic__dim-head">
        <span className="writing-topic__dim-label">{label}</span>
        <span className="writing-topic__dim-score">{score.toFixed(1)}/5</span>
      </div>
      <div
        className="writing-topic__dim-track"
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={5}
        aria-valuenow={score}
      >
        <span style={{ width: `${Math.max(0, Math.min(100, (score / 5) * 100))}%` }} />
      </div>
    </div>
  );
}

function PracticeStage({
  assessment,
  title,
  onBack,
  onFinished,
}: {
  assessment: WritingAssessmentDto | null;
  title: string;
  onBack: () => void;
  onFinished: () => void;
}) {
  const xp = assessment ? Math.round(assessment.overallPercent / 10) : 0;

  return (
    <WritingSlideShell
      stage="practice"
      title={title}
      status={uz.writingTopics.assessmentTitle}
      xp={xp}
      onExit={onBack}
      mode="flow"
      bodyClassName="writing-topic__slide-body--scroll"
      footer={
        <AppButton tone="primary" size="lg" fullWidth leadingIcon="arrow_forward" onClick={onFinished}>
          {uz.writingTopics.nextExercise}
        </AppButton>
      }
    >
      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        className="writing-topic__card writing-topic__card--assessment"
      >
        <Pill tone="neutral" className="writing-topic__card-pill">{uz.writingTopics.assessmentTitle}</Pill>
        {assessment && (
          <>
            <h2 className="writing-topic__score">{uz.writing.overallScore(assessment.overallPercent)}</h2>
            <p className="writing-topic__band">{uz.writing.bandLabel(assessment.overallBand)}</p>

            <div className="writing-topic__dims">
              {assessment.dimensionScores.map((d) => (
                <DimensionBar key={d.dimension} label={DIMENSION_LABEL[d.dimension]} score={d.score} />
              ))}
            </div>

            <p className="writing-topic__level">
              {uz.writing.estimatedLevel} <strong>{cefrShort(assessment.estimatedLevel)}</strong>
            </p>
          </>
        )}

        <Confetti show count={60} />
        <RewardPop label={`+${xp} XP`} />
        <div className="writing-topic__mascot">
          <motion.div
            initial={{ scale: 0.5, rotate: -8 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={{ type: "spring", stiffness: 300, damping: 14 }}
          >
            <Friend skill="writing" size={96} />
          </motion.div>
        </div>
      </motion.section>
    </WritingSlideShell>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Stage 4 - Done / celebration.
// ─────────────────────────────────────────────────────────────────────────────

function DoneStage({ onExit }: { onExit: () => void }) {
  return (
    <WritingSlideShell
      stage="done"
      title={uz.writingTopics.topicMastered}
      status={uz.writingTopics.topicMastered}
      xp={10}
      onExit={onExit}
      footer={
        <AppButton tone="primary" size="lg" fullWidth leadingIcon="arrow_back" onClick={onExit}>
          {uz.writingTopics.back}
        </AppButton>
      }
    >
      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        className="writing-topic__card writing-topic__result"
      >
        <Confetti show count={80} />
        <RewardPop label="+10 XP" />
        <Friend skill="writing" size={96} />
        <h1 className="writing-topic__result-title">{uz.writingTopics.topicMastered}</h1>
      </motion.section>
    </WritingSlideShell>
  );
}
