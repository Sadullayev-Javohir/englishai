import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { api } from "@/api/client";
import {
  MiniTestType,
  WordUsageReasonCode,
  type DueReviewDto,
  type VocabularyItemDto,
  type VocabularyTopicDetailDto,
} from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { useWordVoice } from "@/lib/useWordVoice";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { Spinner } from "@/components/ui/Spinner";
import { AppButton } from "@/components/design";
import { TopicImage } from "@/components/TopicImage";
import { MilestoneTrack } from "@/components/vocabulary/MilestoneTrack";
import { stageMilestones, isMastered } from "@/lib/srsStage";
import { cn } from "@/lib/cn";
import "./SavedVocabularyTopicPracticePage.css";

/**
 * Topic-scoped vocabulary practice with two modes:
 *
 * - **Review (SRS) mode** — runs the topic's *due* words through their server-decided mini-tests
 *   (DueReviewDto.miniTestType) and submits each answer via `api.vocabulary.review`, which advances
 *   the 3/7/21 ladder on the server. This is what fills the milestone ticks. ClozeChoice/WrittenUsage
 *   are verified server-side; SpokenUsage/ListeningRecognition fall back to a self-rating (the same
 *   contract VocabularyReviewPage uses).
 * - **Free practice mode** — a local write/listen drill over all the topic's saved words. It never
 *   touches the schedule; it's just extra reinforcement when nothing is due (or on demand).
 */

type Mode = "review" | "free";

/** Resolves a server WordUsageReasonCode to its vetted Uzbek explanation (docs/development-guide.md rule 11). */
function reasonMessage(code: WordUsageReasonCode | null): string | null {
  switch (code) {
    case WordUsageReasonCode.WordNotUsed:
      return uz.vocabulary.review.session.reasonWordNotUsed;
    case WordUsageReasonCode.WrongMeaning:
      return uz.vocabulary.review.session.reasonWrongMeaning;
    case WordUsageReasonCode.TooShort:
      return uz.vocabulary.review.session.reasonTooShort;
    case WordUsageReasonCode.NotEnglish:
      return uz.vocabulary.review.session.reasonNotEnglish;
    default:
      return null;
  }
}

export function SavedVocabularyTopicPracticePage() {
  const { topicId = "" } = useParams();
  const learnerId = getLearnerId();
  const navigate = useNavigate();

  const vocabularyState = useAsync(() => api.vocabulary.list(learnerId), [learnerId]);
  const topicState = useAsync(() => api.vocabulary.topic(topicId), [topicId], Boolean(topicId));
  const dueState = useAsync(() => api.vocabulary.due(learnerId), [learnerId]);

  const words = useMemo(
    () => (vocabularyState.data ?? []).filter((word) => word.sourceTopicId === topicId),
    [topicId, vocabularyState.data],
  );
  const dueItems = useMemo(
    () => (dueState.data ?? []).filter((item) => item.sourceTopicId === topicId),
    [topicId, dueState.data],
  );

  // Mode is chosen once the data lands: review when something is due, otherwise free practice.
  const [mode, setMode] = useState<Mode | null>(null);
  useEffect(() => {
    if (mode !== null || vocabularyState.loading || dueState.loading) return;
    setMode(dueItems.length > 0 ? "review" : "free");
  }, [mode, vocabularyState.loading, dueState.loading, dueItems.length]);

  const loading = vocabularyState.loading || topicState.loading || dueState.loading;
  const error = vocabularyState.error || topicState.error;

  if (loading || mode === null) {
    return <Spinner className="saved-practice-spinner" />;
  }

  if (error || !topicState.data) {
    return (
      <PracticeMessage
        icon="cloud_off"
        title={uz.mySavedWords.practiceLoadError}
        hint={uz.mySavedWords.practiceLoadErrorHint}
        actionLabel={uz.common.retry}
        onAction={() => {
          vocabularyState.reload();
          topicState.reload();
          dueState.reload();
        }}
        onBack={() => navigate("/app/vocabulary/saved")}
      />
    );
  }

  if (words.length === 0) {
    return (
      <PracticeMessage
        icon="bookmark_border"
        title={uz.mySavedWords.practiceEmpty}
        hint={uz.mySavedWords.practiceEmptyHint}
        actionLabel={uz.mySavedWords.browseTopics}
        onAction={() => navigate("/app/vocabulary/topics")}
        onBack={() => navigate("/app/vocabulary/saved")}
      />
    );
  }

  const topic = topicState.data;
  const backToSaved = () => navigate("/app/vocabulary/saved");

  return (
    <main className="saved-practice">
      <button type="button" className="saved-practice__back" onClick={backToSaved}>
        <Icon name="arrow_back" />
        {uz.mySavedWords.backToSaved}
      </button>

      <PracticeHero
        topic={topic}
        topicId={topicId}
        mode={mode}
        dueCount={dueItems.length}
        onSwitch={setMode}
      />

      {mode === "review" && dueItems.length > 0 ? (
        <ReviewFlow items={dueItems} onExit={backToSaved} onFree={() => setMode("free")} />
      ) : (
        <FreePracticeFlow words={words} onExit={backToSaved} />
      )}
    </main>
  );
}

function PracticeHero({
  topic,
  topicId,
  mode,
  dueCount,
  onSwitch,
}: {
  topic: VocabularyTopicDetailDto;
  topicId: string;
  mode: Mode;
  dueCount: number;
  onSwitch: (mode: Mode) => void;
}) {
  const title = topic.titleUz || topic.title;
  const isReview = mode === "review" && dueCount > 0;
  return (
    <section className="saved-practice-hero">
      <div className="saved-practice-hero__copy">
        <span className="saved-practice__eyebrow">
          <Icon name={isReview ? "bolt" : "fitness_center"} filled />
          {isReview ? uz.mySavedWords.practiceReviewEyebrow : uz.mySavedWords.practiceFreeEyebrow}
        </span>
        <h1>{title}</h1>
        <p>{isReview ? uz.mySavedWords.practiceReviewNote : uz.mySavedWords.practiceFreeNote}</p>
        {dueCount > 0 && (
          <div className="saved-practice-hero__modes" role="group" aria-label="Mashq turi">
            <button
              type="button"
              className={cn("saved-practice-mode", isReview && "is-active")}
              onClick={() => onSwitch("review")}
              aria-pressed={isReview}
            >
              <Icon name="bolt" filled />
              {uz.mySavedWords.practiceReviewCta(dueCount)}
            </button>
            <button
              type="button"
              className={cn("saved-practice-mode", !isReview && "is-active")}
              onClick={() => onSwitch("free")}
              aria-pressed={!isReview}
            >
              <Icon name="fitness_center" filled />
              {uz.mySavedWords.practiceFreeCta}
            </button>
          </div>
        )}
      </div>
      <div className="saved-practice-hero__visual" aria-hidden="true">
        <TopicImage
          topicId={topicId}
          title={title}
          level={topic.level}
          category={topic.category}
          hideLevelBadge
          hideTitle
          className="saved-practice-hero__image"
        />
      </div>
    </section>
  );
}

// ── Review (SRS) mode ──────────────────────────────────────────────────────

type ReviewAnswerState = "idle" | "checking" | "correct" | "incorrect";

function ReviewFlow({ items, onExit, onFree }: { items: DueReviewDto[]; onExit: () => void; onFree: () => void }) {
  const speak = useWordVoice();
  const [index, setIndex] = useState(0);
  const [answerState, setAnswerState] = useState<ReviewAnswerState>("idle");
  const [selectedOption, setSelectedOption] = useState<string | null>(null);
  const [writtenText, setWrittenText] = useState("");
  const [reasonText, setReasonText] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [correctCount, setCorrectCount] = useState(0);
  const [answered, setAnswered] = useState(false);
  const [finished, setFinished] = useState(false);

  const item = items[index];
  const total = items.length;

  function resetCard() {
    setAnswerState("idle");
    setSelectedOption(null);
    setWrittenText("");
    setReasonText(null);
    setSubmitError(null);
    setAnswered(false);
  }

  function advance() {
    if (index + 1 >= total) {
      setFinished(true);
      return;
    }
    setIndex((value) => value + 1);
    resetCard();
  }

  async function submit(answer: { submittedAnswer?: string; selfRatedPassed?: boolean }) {
    if (!item || answerState !== "idle") return;
    setAnswerState("checking");
    setSubmitError(null);
    try {
      const result = await api.vocabulary.review(item.id, answer);
      setAnswerState(result.passed ? "correct" : "incorrect");
      if (result.passed) {
        setCorrectCount((count) => count + 1);
      } else if (item.miniTestType === MiniTestType.WrittenUsage) {
        setReasonText(reasonMessage(result.reasonCode ?? null));
      }
      setAnswered(true);
    } catch {
      setAnswerState("idle");
      setSelectedOption(null);
      setSubmitError(uz.mySavedWords.submitError);
    }
  }

  if (finished) {
    return <ReviewResult correct={correctCount} total={total} onExit={onExit} onFree={onFree} />;
  }
  if (!item) return null;

  const progress = Math.round(((index + (answered ? 1 : 0)) / total) * 100);
  const isSelfRate =
    item.miniTestType === MiniTestType.SpokenUsage || item.miniTestType === MiniTestType.ListeningRecognition;

  return (
    <AnimatePresence mode="wait">
      <motion.section
        key={item.id}
        initial={{ opacity: 0, y: 18 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -12 }}
        transition={{ duration: 0.2 }}
        className="saved-practice-card"
      >
        <div className="saved-practice-status">
          <div className="saved-practice-progress">
            <motion.span animate={{ width: `${progress}%` }} />
          </div>
          <div className="saved-practice-status__meta">
            <span>{uz.mySavedWords.questionProgress(index + 1, total)}</span>
            <strong><Icon name="task_alt" filled /> {correctCount}</strong>
          </div>
          <div className="saved-practice-status__ladder">
            <MilestoneTrack milestones={stageMilestones(item.stage)} mastered={isMastered(item.stage)} due size="sm" />
          </div>
        </div>

        {item.miniTestType === MiniTestType.ClozeChoice && (
          <>
            <ExerciseHeading icon="quiz" title={uz.mySavedWords.practiceReviewEyebrow} hint={uz.vocabulary.review.session.clozePrompt} />
            <div className="saved-practice-prompt">
              <h2>{item.translation}</h2>
              {item.exampleSentence && <p>“{blankWord(item.exampleSentence, item.word)}”</p>}
            </div>
            <div className="saved-practice-options">
              {(item.options ?? []).map((option) => {
                const isCorrect = answered && normalize(option) === normalize(item.word);
                const isWrong = answered && selectedOption === option && !isCorrect;
                return (
                  <button
                    key={option}
                    type="button"
                    disabled={answerState !== "idle"}
                    onClick={() => {
                      setSelectedOption(option);
                      void submit({ submittedAnswer: option });
                    }}
                    className={cn(isCorrect && "is-correct", isWrong && "is-wrong")}
                  >
                    <span>{option}</span>
                    {isCorrect && <Icon name="check_circle" filled />}
                    {isWrong && <Icon name="cancel" filled />}
                  </button>
                );
              })}
            </div>
          </>
        )}

        {item.miniTestType === MiniTestType.WrittenUsage && (
          <>
            <ExerciseHeading icon="edit" title={uz.mySavedWords.practiceReviewEyebrow} hint={uz.vocabulary.review.session.writeSentencePrompt(item.word)} />
            <div className="saved-practice-prompt saved-practice-prompt--sm">
              <h2>{item.word}</h2>
              <p>{item.translation}</p>
            </div>
            <label className="saved-practice-input-label">
              <span>{uz.vocabulary.review.session.writeSentencePlaceholder}</span>
              <textarea
                value={writtenText}
                onChange={(event) => setWrittenText(event.target.value)}
                disabled={answered || answerState === "checking"}
                rows={3}
                autoComplete="off"
                placeholder={uz.vocabulary.review.session.writeSentencePlaceholder}
              />
            </label>
            {!answered && (
              <AppButton
                type="button"
                fullWidth
                loading={answerState === "checking"}
                disabled={!writtenText.trim()}
                trailingIcon="arrow_forward"
                onClick={() => void submit({ submittedAnswer: writtenText.trim() })}
              >
                {uz.vocabulary.review.session.writeSentenceSubmit}
              </AppButton>
            )}
          </>
        )}

        {isSelfRate && (
          <>
            <ExerciseHeading
              icon={item.miniTestType === MiniTestType.ListeningRecognition ? "headphones" : "record_voice_over"}
              title={uz.mySavedWords.practiceReviewEyebrow}
              hint={uz.mySavedWords.selfRateTitle}
            />
            <div className="saved-practice-listen">
              <motion.button type="button" whileTap={{ scale: 0.96 }} onClick={() => speak(item.word)} aria-label={uz.mySavedWords.listenAgain}>
                <Icon name="volume_up" filled />
              </motion.button>
              <p>{item.translation}</p>
            </div>
            {!answered && (
              <div className="saved-practice-selfrate">
                <AppButton type="button" tone="standard" leadingIcon="close" disabled={answerState === "checking"} onClick={() => void submit({ selfRatedPassed: false })}>
                  {uz.mySavedWords.selfRateNo}
                </AppButton>
                <AppButton type="button" tone="success" leadingIcon="check" loading={answerState === "checking"} onClick={() => void submit({ selfRatedPassed: true })}>
                  {uz.mySavedWords.selfRateYes}
                </AppButton>
              </div>
            )}
          </>
        )}

        {submitError && <p className="saved-practice-error" role="alert"><Icon name="error" filled /> {submitError}</p>}

        {answered && (
          <ReviewFeedback
            state={answerState}
            word={item.word}
            reason={reasonText}
            onNext={advance}
            isLast={index + 1 >= total}
          />
        )}
      </motion.section>
    </AnimatePresence>
  );
}

function ReviewFeedback({
  state,
  word,
  reason,
  onNext,
  isLast,
}: {
  state: ReviewAnswerState;
  word: string;
  reason: string | null;
  onNext: () => void;
  isLast: boolean;
}) {
  const correct = state === "correct";
  return (
    <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} className="saved-practice-feedback">
      <div className={correct ? "is-correct" : "is-wrong"} role="status">
        <Icon name={correct ? "check_circle" : "cancel"} filled />
        <span>{correct ? uz.mySavedWords.correct : reason ?? uz.mySavedWords.correctAnswer(word)}</span>
      </div>
      <button type="button" className="saved-practice-button saved-practice-button--primary" onClick={onNext}>
        {isLast ? uz.vocabulary.review.finish : uz.mySavedWords.nextQuestion}
        <Icon name="arrow_forward" />
      </button>
    </motion.div>
  );
}

function ReviewResult({ correct, total, onExit, onFree }: { correct: number; total: number; onExit: () => void; onFree: () => void }) {
  const percentage = total === 0 ? 0 : Math.round((correct / total) * 100);
  return (
    <motion.section
      initial={{ opacity: 0, y: 18, scale: 0.96 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      className="saved-practice-result"
    >
      <span className="saved-practice-result__icon"><Icon name="verified" filled /></span>
      <span className="saved-practice__eyebrow">{uz.mySavedWords.practiceReviewEyebrow}</span>
      <div>
        <h1>{uz.mySavedWords.reviewDoneTitle}</h1>
        <p>{uz.mySavedWords.reviewDoneNote}</p>
      </div>
      <div className="saved-practice-result__score">
        <div>
          <span>{uz.mySavedWords.reviewPassLabel(correct, total)}</span>
          <strong>{percentage}%</strong>
        </div>
        <div className="saved-practice-progress" aria-label={`${uz.mySavedWords.resultLabel}: ${percentage}%`}>
          <motion.span initial={{ width: 0 }} animate={{ width: `${percentage}%` }} />
        </div>
      </div>
      <div className="saved-practice-actions">
        <button type="button" className="saved-practice-button saved-practice-button--secondary" onClick={onFree}>
          <Icon name="fitness_center" filled />
          {uz.mySavedWords.practiceFreeCta}
        </button>
        <button type="button" className="saved-practice-button saved-practice-button--primary" onClick={onExit}>
          <Icon name="bookmarks" filled />
          {uz.mySavedWords.backToSaved}
        </button>
      </div>
    </motion.section>
  );
}

// ── Free practice mode (local drill, no SRS write) ─────────────────────────

type Exercise =
  | { kind: "write"; word: VocabularyItemDto }
  | { kind: "listen"; word: VocabularyItemDto; options: string[] };

type AnswerState = "idle" | "correct" | "wrong";

function FreePracticeFlow({ words, onExit }: { words: VocabularyItemDto[]; onExit: () => void }) {
  const speak = useWordVoice();
  const exercises = useMemo(() => buildExercises(words), [words]);
  const [index, setIndex] = useState(0);
  const [answerState, setAnswerState] = useState<AnswerState>("idle");
  const [typedAnswer, setTypedAnswer] = useState("");
  const [selectedAnswer, setSelectedAnswer] = useState<string | null>(null);
  const [correctCount, setCorrectCount] = useState(0);
  const [finished, setFinished] = useState(false);

  const exercise = exercises[index];

  function resetAnswer() {
    setAnswerState("idle");
    setTypedAnswer("");
    setSelectedAnswer(null);
  }

  function recordResult(correct: boolean) {
    setAnswerState(correct ? "correct" : "wrong");
    if (correct) setCorrectCount((count) => count + 1);
  }

  function checkWrittenAnswer() {
    if (!exercise || exercise.kind !== "write" || answerState !== "idle") return;
    recordResult(normalize(typedAnswer) === normalize(exercise.word.word));
  }

  function checkListeningAnswer(option: string) {
    if (!exercise || exercise.kind !== "listen" || answerState !== "idle") return;
    setSelectedAnswer(option);
    recordResult(normalize(option) === normalize(exercise.word.word));
  }

  function nextExercise() {
    if (index >= exercises.length - 1) {
      setFinished(true);
      return;
    }
    setIndex((value) => value + 1);
    resetAnswer();
  }

  function restart() {
    setIndex(0);
    setCorrectCount(0);
    setFinished(false);
    resetAnswer();
  }

  if (exercises.length === 0) {
    return (
      <PracticeMessage
        icon="bookmark_border"
        title={uz.mySavedWords.practiceEmpty}
        hint={uz.mySavedWords.practiceEmptyHint}
        actionLabel={uz.mySavedWords.backToSaved}
        onAction={onExit}
        onBack={onExit}
        embedded
      />
    );
  }

  if (finished) {
    const percentage = Math.round((correctCount / exercises.length) * 100);
    return (
      <motion.section initial={{ opacity: 0, y: 18, scale: 0.96 }} animate={{ opacity: 1, y: 0, scale: 1 }} className="saved-practice-result">
        <span className="saved-practice-result__icon"><Icon name="emoji_events" filled /></span>
        <span className="saved-practice__eyebrow">{uz.mySavedWords.practiceFreeEyebrow}</span>
        <div>
          <h1>{uz.mySavedWords.practiceDone}</h1>
          <p>{uz.mySavedWords.practiceResult(correctCount, exercises.length)}</p>
        </div>
        <div className="saved-practice-result__score">
          <div>
            <span>{uz.mySavedWords.resultLabel}</span>
            <strong>{percentage}%</strong>
          </div>
          <div className="saved-practice-progress" aria-label={`${uz.mySavedWords.resultLabel}: ${percentage}%`}>
            <motion.span initial={{ width: 0 }} animate={{ width: `${percentage}%` }} />
          </div>
        </div>
        <div className="saved-practice-actions">
          <button type="button" className="saved-practice-button saved-practice-button--secondary" onClick={restart}>
            <Icon name="replay" />
            {uz.mySavedWords.practiceAgain}
          </button>
          <button type="button" className="saved-practice-button saved-practice-button--primary" onClick={onExit}>
            <Icon name="bookmarks" filled />
            {uz.mySavedWords.backToSaved}
          </button>
        </div>
      </motion.section>
    );
  }

  if (!exercise) return null;

  const progress = Math.round(((index + 1) / exercises.length) * 100);
  return (
    <AnimatePresence mode="wait">
      <motion.section
        key={`${exercise.kind}-${exercise.word.id}`}
        initial={{ opacity: 0, y: 18 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -12 }}
        transition={{ duration: 0.2 }}
        className={cn("saved-practice-card", exercise.kind === "listen" && "saved-practice-card--listen")}
      >
        <div className="saved-practice-status">
          <div className="saved-practice-progress">
            <motion.span animate={{ width: `${progress}%` }} />
          </div>
          <div className="saved-practice-status__meta">
            <span>{uz.mySavedWords.questionProgress(index + 1, exercises.length)}</span>
            <strong><Icon name="task_alt" filled /> {correctCount}</strong>
          </div>
        </div>
        {exercise.kind === "write" ? (
          <WriteExercise
            exercise={exercise}
            typedAnswer={typedAnswer}
            answerState={answerState}
            onChange={setTypedAnswer}
            onCheck={checkWrittenAnswer}
            onNext={nextExercise}
          />
        ) : (
          <ListeningExercise
            exercise={exercise}
            answerState={answerState}
            selectedAnswer={selectedAnswer}
            onListen={() => speak(exercise.word.word)}
            onSelect={checkListeningAnswer}
            onNext={nextExercise}
          />
        )}
      </motion.section>
    </AnimatePresence>
  );
}

function WriteExercise({
  exercise,
  typedAnswer,
  answerState,
  onChange,
  onCheck,
  onNext,
}: {
  exercise: Extract<Exercise, { kind: "write" }>;
  typedAnswer: string;
  answerState: AnswerState;
  onChange: (value: string) => void;
  onCheck: () => void;
  onNext: () => void;
}) {
  const answered = answerState !== "idle";

  return (
    <>
      <ExerciseHeading icon="edit" title={uz.mySavedWords.writeSection} hint={uz.mySavedWords.writePrompt} />
      <div className="saved-practice-prompt">
        <h2>{exercise.word.translation}</h2>
        {exercise.word.exampleSentence && <p>“{blankWord(exercise.word.exampleSentence, exercise.word.word)}”</p>}
      </div>
      <label className="saved-practice-input-label">
        <span>Javobingiz</span>
        <input
          type="text"
          value={typedAnswer}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter" && !answered && typedAnswer.trim()) onCheck();
          }}
          disabled={answered}
          autoComplete="off"
          autoCapitalize="none"
          placeholder={uz.mySavedWords.writePlaceholder}
        />
      </label>
      {answered ? (
        <AnswerFeedback state={answerState} answer={exercise.word.word} onNext={onNext} />
      ) : (
        <button type="button" className="saved-practice-button saved-practice-button--primary" disabled={!typedAnswer.trim()} onClick={onCheck}>
          {uz.mySavedWords.checkAnswer}
          <Icon name="arrow_forward" />
        </button>
      )}
    </>
  );
}

function ListeningExercise({
  exercise,
  answerState,
  selectedAnswer,
  onListen,
  onSelect,
  onNext,
}: {
  exercise: Extract<Exercise, { kind: "listen" }>;
  answerState: AnswerState;
  selectedAnswer: string | null;
  onListen: () => void;
  onSelect: (answer: string) => void;
  onNext: () => void;
}) {
  const answered = answerState !== "idle";

  return (
    <>
      <ExerciseHeading icon="headphones" title={uz.mySavedWords.listeningSection} hint={uz.mySavedWords.listeningPrompt} />
      <div className="saved-practice-listen">
        <motion.button type="button" whileTap={{ scale: 0.96 }} onClick={onListen} aria-label={uz.mySavedWords.listenAgain}>
          <Icon name="volume_up" filled />
        </motion.button>
        <p>{uz.mySavedWords.listenAgain}</p>
      </div>
      <div className="saved-practice-options">
        {exercise.options.map((option) => {
          const isCorrect = answered && normalize(option) === normalize(exercise.word.word);
          const isWrong = answered && selectedAnswer === option && !isCorrect;
          return (
            <button
              key={option}
              type="button"
              disabled={answered}
              onClick={() => onSelect(option)}
              className={cn(isCorrect && "is-correct", isWrong && "is-wrong")}
            >
              <span>{option}</span>
              {isCorrect && <Icon name="check_circle" filled />}
              {isWrong && <Icon name="cancel" filled />}
            </button>
          );
        })}
      </div>
      {answered && <AnswerFeedback state={answerState} answer={exercise.word.word} onNext={onNext} />}
    </>
  );
}

function ExerciseHeading({ icon, title, hint }: { icon: string; title: string; hint: string }) {
  return (
    <div className="saved-practice-card__heading">
      <span><Icon name={icon} filled /></span>
      <div>
        <p>{title}</p>
        <h2>{hint}</h2>
      </div>
    </div>
  );
}

function AnswerFeedback({ state, answer, onNext }: { state: AnswerState; answer: string; onNext: () => void }) {
  const correct = state === "correct";
  return (
    <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} className="saved-practice-feedback">
      <div className={correct ? "is-correct" : "is-wrong"} role="status">
        <Icon name={correct ? "check_circle" : "cancel"} filled />
        <span>{correct ? uz.mySavedWords.correct : uz.mySavedWords.correctAnswer(answer)}</span>
      </div>
      <button type="button" className="saved-practice-button saved-practice-button--primary" onClick={onNext}>
        {uz.mySavedWords.nextQuestion}
        <Icon name="arrow_forward" />
      </button>
    </motion.div>
  );
}

function PracticeMessage({
  icon,
  title,
  hint,
  actionLabel,
  onAction,
  onBack,
  embedded = false,
}: {
  icon: string;
  title: string;
  hint: string;
  actionLabel: string;
  onAction: () => void;
  onBack: () => void;
  embedded?: boolean;
}) {
  const body = (
    <section className="saved-practice-state">
      <span className="saved-practice-state__icon"><Icon name={icon} filled /></span>
      <div>
        <h1>{title}</h1>
        <p>{hint}</p>
      </div>
      <div className="saved-practice-actions">
        <button type="button" className="saved-practice-button saved-practice-button--secondary" onClick={onBack}>
          <Icon name="arrow_back" />
          {uz.mySavedWords.backToSaved}
        </button>
        <button type="button" className="saved-practice-button saved-practice-button--primary" onClick={onAction}>
          {actionLabel}
          <Icon name="arrow_forward" />
        </button>
      </div>
    </section>
  );
  if (embedded) return body;
  return <main className="saved-practice saved-practice--centered">{body}</main>;
}

function buildExercises(words: VocabularyItemDto[]): Exercise[] {
  const exercises: Exercise[] = [];
  for (const word of words) {
    exercises.push({ kind: "write", word });
    exercises.push({ kind: "listen", word, options: buildListeningOptions(word, words) });
  }
  return interleaveExercises(exercises);
}

function buildListeningOptions(target: VocabularyItemDto, words: VocabularyItemDto[]): string[] {
  const distractors = words
    .filter((word) => word.id !== target.id && normalize(word.word) !== normalize(target.word))
    .map((word) => word.word)
    .slice(0, 3);
  return stableShuffle([target.word, ...distractors], target.id);
}

function interleaveExercises(exercises: Exercise[]): Exercise[] {
  const writing = exercises.filter((exercise) => exercise.kind === "write");
  const listening = exercises.filter((exercise) => exercise.kind === "listen");
  return [...stableShuffle(writing, "write"), ...stableShuffle(listening, "listen")];
}

function stableShuffle<T>(items: T[], seed: string): T[] {
  let state = Array.from(seed).reduce((sum, character) => sum + character.charCodeAt(0), 0) || 1;
  const copy = [...items];
  for (let index = copy.length - 1; index > 0; index -= 1) {
    state = (state * 9301 + 49297) % 233280;
    const swapIndex = Math.floor((state / 233280) * (index + 1));
    [copy[index], copy[swapIndex]] = [copy[swapIndex], copy[index]];
  }
  return copy;
}

function normalize(value: string): string {
  return value.trim().toLocaleLowerCase("en-US").replace(/\s+/g, " ");
}

function blankWord(sentence: string, word: string): string {
  const escaped = word.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  return sentence.replace(new RegExp(`\\b${escaped}\\b`, "gi"), "_____");
}
