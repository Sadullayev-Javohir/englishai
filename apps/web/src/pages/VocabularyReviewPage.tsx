import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { MiniTestType, WordUsageReasonCode, type DueReviewDto } from "@/api/types";
import { uz } from "@/content/uz";

import { Flashcard3D } from "@/components/review/Flashcard3D";
import { RatingButton3D, type RatingGrade } from "@/components/review/RatingButton3D";
import { ProgressDuo } from "@/components/review/ProgressDuo";
import { ReviewStreak } from "@/components/review/ReviewStreak";
import { RewardPop, Confetti, DuoButton } from "@/components/game";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { LessonAdvanceAction } from "@/components/lesson/LessonAdvanceAction";
import { LessonGuidance } from "@/components/lesson/LessonStageFrame";
import { useLessonProgress } from "@/lib/lessonProgress";
import "./VocabularyReviewPage.css";

/**
 * Vocabulary SRS review - one due word at a time, verify, advance.
 *
 * The route keeps the learner shell intact and applies the protected Home design
 * language through a page-local namespace: soft multi-tone surfaces, strong ink,
 * lime actions, lavender results, generous radii and responsive touch targets.
 *
 * The mini-test presented per card is server-decided (DueReviewDto.miniTestType,
 * Domain.Vocabulary.VocabularyItem.NextMiniTestType): ClozeChoice and WrittenUsage are
 * checked server-side (SubmitReviewCommand.SubmittedAnswer) - this page never decides
 * "passed" for those itself. SpokenUsage/ListeningRecognition fall back to the flip-card
 * self-rating (SubmitReviewCommand.SelfRatedPassed) - spoken/listening review needs Azure
 * Speech pronunciation assessment, out of scope for this pass; self-rated recall is a
 * legitimate SRS mechanic on its own, not a stand-in bug.
 */

const T = uz.vocabulary.review.mandatory;

/** Server-side XP award for a verified pass, per mini-test type. Self-rated flip-card grades keep
 *  their own hard/good/easy scale (see XP_BY_GRADE below). */
const XP_BY_MINI_TEST: Partial<Record<MiniTestType, number>> = {
  [MiniTestType.ClozeChoice]: 10,
  [MiniTestType.WrittenUsage]: 15,
};

const XP_BY_GRADE: Record<RatingGrade, number> = { hard: 5, good: 10, easy: 15 };

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

/** Replaces the target word inside its example sentence with a blank, for the cloze prompt.
 *  Falls back to null (prompt shows the translation alone) when no match is found. */
function blankSentence(sentence: string | null, word: string): string | null {
  if (!sentence) return null;
  const pattern = new RegExp(`\\b${word.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\b`, "i");
  if (!pattern.test(sentence)) return null;
  return sentence.replace(pattern, "_____");
}

type AnswerState = "idle" | "checking" | "correct" | "incorrect";

export function VocabularyReviewPage() {
  const learnerId = getLearnerId();
  const navigate = useNavigate();

  const { data, loading, error, reload } = useAsync(
    () => api.vocabulary.due(learnerId),
    [learnerId],
  );

  const cards = data ?? [];

  const [index, setIndex] = useLessonProgress("vocabulary-review.index", 0);
  const [flipped, setFlipped] = useState(false);
  const [gradeStreak, setGradeStreak] = useState(0);
  const [xp, setXp] = useState(0);
  const [reward, setReward] = useState<{ show: boolean; amount: number }>({ show: false, amount: 0 });
  const [phase, setPhase] = useLessonProgress<"running" | "empty" | "done">("vocabulary-review.phase", "running");

  // Mini-test answer state, shared by the ClozeChoice/WrittenUsage cards.
  const [answerState, setAnswerState] = useState<AnswerState>("idle");
  const [selectedOption, setSelectedOption] = useState<string | null>(null);
  const [writtenText, setWrittenText] = useState("");
  const [reasonText, setReasonText] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [pendingResult, setPendingResult] = useState<{ passed: boolean; amount: number } | null>(null);

  useEffect(() => {
    if (!data || error) return;
    setPhase(data.length === 0 ? "empty" : "running");
    setIndex(0);
  }, [data, error, setIndex, setPhase]);

  /** Resets the per-card answer UI when moving to a new card. */
  function resetCardState() {
    setFlipped(false);
    setAnswerState("idle");
    setSelectedOption(null);
    setWrittenText("");
    setReasonText(null);
    setSubmitError(null);
    setPendingResult(null);
  }

  /** Stores the checked result so the learner can review feedback before advancing. */
  function finishCard(passed: boolean, amount: number) {
    setGradeStreak((p) => (passed ? p + 1 : 0));
    if (passed) {
      setXp((p) => p + amount);
      setReward({ show: true, amount });
    }
    setPendingResult({ passed, amount });
  }

  function handleAdvance() {
    if (!pendingResult) return;
    setReward({ show: false, amount: 0 });
    resetCardState();
    if (index + 1 >= cards.length) {
      void api.vocabulary.reviewStatus(learnerId).then((status) => {
        if (status.isRequired) {
          setIndex(0);
          reload();
        } else {
          setPhase("done");
        }
      }).catch(() => {
        setSubmitError(T.statusError);
        reload();
      });
    } else {
      setIndex((i) => i + 1);
    }
  }

  /** Self-rated flip-card fallback (SpokenUsage/ListeningRecognition - see file header). */
  async function handleRate(grade: RatingGrade) {
    const card = cards[index];
    if (!card || answerState !== "idle") return;
    const passed = grade !== "hard";
    setAnswerState("checking");
    setSubmitError(null);
    try {
      const result = await api.vocabulary.review(card.id, { selfRatedPassed: passed });
      setAnswerState(result.passed ? "correct" : "incorrect");
      finishCard(result.passed, XP_BY_GRADE[grade]);
    } catch {
      setAnswerState("idle");
      setSubmitError(T.submitError);
    }
  }

  /** ClozeChoice: pick an option; the server verifies it against the item's real word. */
  async function handleClozeSelect(option: string) {
    const card = cards[index];
    if (!card || answerState !== "idle") return;
    setSelectedOption(option);
    setAnswerState("checking");

    setSubmitError(null);
    try {
      const result = await api.vocabulary.review(card.id, { submittedAnswer: option });
      setAnswerState(result.passed ? "correct" : "incorrect");
      finishCard(result.passed, XP_BY_MINI_TEST[MiniTestType.ClozeChoice] ?? 10);
    } catch {
      setSelectedOption(null);
      setAnswerState("idle");
      setSubmitError(T.submitError);
    }
  }

  /** WrittenUsage: submit a sentence; the server (AI) grades whether it uses the word. */
  async function handleWrittenSubmit() {
    const card = cards[index];
    const text = writtenText.trim();
    if (!card || !text || answerState !== "idle") return;
    setAnswerState("checking");

    setSubmitError(null);
    try {
      const result = await api.vocabulary.review(card.id, { submittedAnswer: text });
      setAnswerState(result.passed ? "correct" : "incorrect");
      setReasonText(result.passed ? null : reasonMessage(result.reasonCode ?? null));
      finishCard(result.passed, XP_BY_MINI_TEST[MiniTestType.WrittenUsage] ?? 15);
    } catch {
      setAnswerState("idle");
      setSubmitError(T.submitError);
    }
  }

  if (error) {
    return (
      <ReviewPageFrame onBack={() => navigate(-1)} backLabel={T.back}>
        <StateCard tone="error" icon="cloud_off" title={T.loadErrorTitle} text={T.loadErrorText}>
          <button type="button" className="vr-primary-button" onClick={reload}>{T.retry}</button>
        </StateCard>
      </ReviewPageFrame>
    );
  }

  if (phase === "empty") {
    return (
      <ReviewPageFrame onBack={() => navigate(-1)} backLabel={T.back}>
        <StateCard tone="empty" icon="task_alt" title={T.nothingDueTitle} text={T.nothingDueText}>
          <button type="button" className="vr-primary-button" onClick={() => navigate("/app/vocabulary/saved")}>
            <Icon name="bookmarks" />
            {T.emptyCta}
          </button>
        </StateCard>
      </ReviewPageFrame>
    );
  }

  if (loading) {
    return <ModulePageLoader icon="repeat" accent="purple" />;
  }

  if (phase === "done") {
    return (
      <ReviewPageFrame onBack={() => navigate(-1)} backLabel={T.back}>
        <Confetti show={true} />
        <StateCard
          tone="done"
          icon="emoji_events"
          title={T.sessionDoneTitle}
          text={`${cards.length} ta so'z · ${xp} XP ishlab topildi`}
        >
          <button type="button" className="vr-primary-button" onClick={() => navigate("/home")}>
            <Icon name="home" />
            {T.sessionDoneContinue}
          </button>
        </StateCard>
      </ReviewPageFrame>
    );
  }

  const current = cards[index];
  if (!current) return null;

  return (
    <ReviewPageFrame onBack={() => navigate(-1)} backLabel={T.later} streak={gradeStreak} xp={xp}>
      <motion.header
        initial={{ opacity: 0, y: -12 }}
        animate={{ opacity: 1, y: 0 }}
        className="vr-review-hero"
      >
        <div className="vr-review-hero__copy">
          <span className="vr-eyebrow">{T.eyebrow}</span>
          <h1>{T.pageTitle}</h1>
          <p>{T.pageSubtitle}</p>
        </div>
        <div className="vr-review-hero__status">
          <ReviewStreak count={gradeStreak} className="vr-streak" />
          <span className="vr-due-chip">{T.dueToday(cards.length)}</span>
        </div>
        <ProgressDuo done={index} due={cards.length} className="vr-progress" />
      </motion.header>

      <LessonGuidance
        className="vr-purpose-note"
        title={T.purposeTitle}
        items={[T.purposeText]}
        icon="schedule"
      />

      <section className="vr-exercise-card" aria-live="polite">
        <div className="vr-exercise-card__meta">
          <span>{index + 1} / {cards.length}</span>
          <span>{current.partOfSpeech ?? "Review"}</span>
        </div>

        {current.miniTestType === MiniTestType.ClozeChoice ? (
          <ClozeChoiceCard
            card={current}
            answerState={answerState}
            selectedOption={selectedOption}
            onSelect={handleClozeSelect}
          />
        ) : current.miniTestType === MiniTestType.WrittenUsage ? (
          <WrittenUsageCard
            card={current}
            answerState={answerState}
            text={writtenText}
            reasonText={reasonText}
            onChangeText={setWrittenText}
            onSubmit={handleWrittenSubmit}
          />
        ) : (
          <>
            <Flashcard3D
              word={current.word}
              translation={current.translation}
              exampleSentence={current.exampleSentence}
              partOfSpeech={current.partOfSpeech}
              sourceTopicId={current.sourceTopicId}
              flipped={flipped}
              onReveal={() => setFlipped(true)}
            />
            <p className="vr-swipe-hint">
              <Icon name="swipe" />
              Kartani bosing yoki mobil qurilmada suring
            </p>
          </>
        )}
      </section>

      {submitError && (
        <div className="vr-feedback vr-feedback--error">
          <Icon name="error" />
          <span>{submitError}</span>
        </div>
      )}

      {pendingResult && (
        <section className={`vr-result-panel vr-result-panel--${pendingResult.passed ? "correct" : "incorrect"}`} aria-live="polite">
          <div className="vr-result-panel__copy">
            <span className="vr-result-panel__icon"><Icon name={pendingResult.passed ? "check_circle" : "cancel"} filled /></span>
            <div>
              <strong>{pendingResult.passed ? uz.vocabulary.review.correct : uz.vocabulary.review.incorrect}</strong>
              <p>{pendingResult.passed ? `+${pendingResult.amount} XP` : "Javobni ko'rib chiqing va keyingi savolga o'ting."}</p>
            </div>
          </div>
          <LessonAdvanceAction
            onAdvance={handleAdvance}
            label={index + 1 >= cards.length ? "Natijani ko'rish" : "Keyingi savol"}
            className="vr-next-button"
          />
        </section>
      )}

      {current.miniTestType !== MiniTestType.ClozeChoice &&
        current.miniTestType !== MiniTestType.WrittenUsage && (
          <section className="vr-rating-panel">
            <div>
              <span className="vr-eyebrow">{T.ratingEyebrow}</span>
              <h2>{T.ratingTitle}</h2>
              <p>{T.ratingHint}</p>
            </div>
            <div className="vr-rating-grid">
              <div><RatingButton3D grade="hard" label={T.rateHard} onRate={handleRate} disabled={answerState !== "idle"} /><small>{T.rateHardHint}</small></div>
              <div><RatingButton3D grade="good" label={T.rateGood} onRate={handleRate} disabled={answerState !== "idle"} /><small>{T.rateGoodHint}</small></div>
              <div><RatingButton3D grade="easy" label={T.rateEasy} onRate={handleRate} disabled={answerState !== "idle"} /><small>{T.rateEasyHint}</small></div>
            </div>
          </section>
        )}

      {reward.show && <RewardPop label={`+${reward.amount} XP`} className="vr-reward-pop" />}
    </ReviewPageFrame>
  );
}

function ReviewPageFrame({
  children,
  onBack,
  backLabel,
  streak,
  xp,
}: {
  children: React.ReactNode;
  onBack: () => void;
  backLabel: string;
  streak?: number;
  xp?: number;
}) {
  return (
    <main className="vocabulary-review-page">
      <div className="vocabulary-review-page__glow vocabulary-review-page__glow--one" />
      <div className="vocabulary-review-page__glow vocabulary-review-page__glow--two" />
      <div className="vocabulary-review-page__content">
        <div className="vr-topbar">
          <button type="button" className="vr-back-button" onClick={onBack}>
            <Icon name="arrow_back" />
            {backLabel}
          </button>
          <PageHeader streak={streak} xp={xp} />
        </div>
        {children}
      </div>
    </main>
  );
}

function StateCard({
  tone,
  icon,
  title,
  text,
  children,
}: {
  tone: "error" | "empty" | "done";
  icon: string;
  title: string;
  text: string;
  children: React.ReactNode;
}) {
  return (
    <motion.section
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      className={`vr-state-card vr-state-card--${tone}`}
    >
      <span className="vr-state-card__icon"><Icon name={icon} filled /></span>
      <span className="vr-eyebrow">{T.eyebrow}</span>
      <h1>{title}</h1>
      <p>{text}</p>
      <div className="vr-state-card__action">{children}</div>
    </motion.section>
  );
}

/** MiniTestType.ClozeChoice: pick the correct English word from the server-supplied options. */
function ClozeChoiceCard({
  card,
  answerState,
  selectedOption,
  onSelect,
}: {
  card: DueReviewDto;
  answerState: AnswerState;
  selectedOption: string | null;
  onSelect: (option: string) => void;
}) {
  const prompt = blankSentence(card.exampleSentence, card.word);
  const options = card.options ?? [card.word];
  const answered = answerState !== "idle";

  return (
    <div className="vr-question-card vr-question-card--cloze">
      <div className="vr-question-card__header">
        <span className="vr-question-card__word">
          {uz.vocabulary.review.session.clozePrompt}
        </span>
        {card.partOfSpeech && (
          <span className="vr-question-card__part">
            {card.partOfSpeech}
          </span>
        )}
      </div>

      <p className="vr-question-card__cloze-translation">
        {card.translation}
      </p>

      {prompt && (
        <p className="vr-question-card__example">
          “{prompt}”
        </p>
      )}

      <div className="vr-choice-grid">
        {options.map((option) => {
          const isSelected = selectedOption === option;
          const isCorrectOption = answered && option.toLowerCase() === card.word.toLowerCase();
          const isWrongSelected = answered && isSelected && !isCorrectOption;

          return (
            <button
              key={option}
              type="button"
              disabled={answered}
              onClick={() => onSelect(option)}
              className={[
                "vr-choice-button",
                isCorrectOption ? "is-correct" : "",
                isWrongSelected ? "is-incorrect" : "",
              ].filter(Boolean).join(" ")}
            >
              {option}
            </button>
          );
        })}
      </div>

      {answerState === "correct" && (
        <p className="vr-answer-message">
          {uz.vocabulary.review.correct}
        </p>
      )}
      {answerState === "incorrect" && (
        <p className="vr-answer-message">
          {uz.vocabulary.review.incorrect} - {uz.vocabulary.review.correctAnswerWas(card.word)}
        </p>
      )}
    </div>
  );
}

/** MiniTestType.WrittenUsage: write one sentence using the target word; the server (AI) grades it. */
function WrittenUsageCard({
  card,
  answerState,
  text,
  reasonText,
  onChangeText,
  onSubmit,
}: {
  card: DueReviewDto;
  answerState: AnswerState;
  text: string;
  reasonText: string | null;
  onChangeText: (value: string) => void;
  onSubmit: () => void;
}) {
  const answered = answerState === "correct" || answerState === "incorrect";
  const checking = answerState === "checking";

  return (
    <div className="vr-question-card vr-question-card--written">
      <div className="vr-question-card__header">
        <span className="vr-question-card__word">
          {card.word}
        </span>
        {card.partOfSpeech && (
          <span className="vr-question-card__part">
            {card.partOfSpeech}
          </span>
        )}
      </div>

      <p className="vr-question-card__translation">{card.translation}</p>

      <p className="vr-question-card__prompt">
        {uz.vocabulary.review.session.writeSentencePrompt(card.word)}
      </p>

      <textarea
        value={text}
        onChange={(e) => onChangeText(e.target.value)}
        disabled={answered || checking}
        placeholder={uz.vocabulary.review.session.writeSentencePlaceholder}
        rows={3}
        className="vr-writing-input"
      />

      <DuoButton
        color="blue"
        fullWidth
        className="vr-submit-button"
        onClick={onSubmit}
        disabled={!text.trim() || answered || checking}
        loading={checking}
      >
        {checking
          ? uz.vocabulary.review.session.grading
          : uz.vocabulary.review.session.writeSentenceSubmit}
      </DuoButton>

      {answerState === "correct" && (
        <p className="vr-answer-message">
          {uz.vocabulary.review.correct}
        </p>
      )}
      {answerState === "incorrect" && (
        <p className="vr-answer-message">
          {reasonText ?? uz.vocabulary.review.incorrect}
        </p>
      )}
    </div>
  );
}

/** Branded header: EnglishAI logo + wordmark, with a replay control. */
function PageHeader({ streak, xp }: { streak?: number; xp?: number }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: -10 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex items-center justify-between gap-3"
    >
      <div className="flex items-center gap-2">
        {typeof streak === "number" && (
          <span className="flex items-center gap-1 rounded-full bg-ea-orange-500 px-3 py-1.5 font-duo text-label-md font-extrabold text-ea-on-primary ">
            <Icon name="local_fire_department" filled className="text-[18px]" />
            {streak}
          </span>
        )}
        {typeof xp === "number" && (
          <span className="flex items-center gap-1 rounded-full bg-ea-orange-500 px-3 py-1.5 font-duo text-label-md font-extrabold text-ea-on-primary ">
            <Icon name="bolt" filled className="text-[18px]" />
            {xp}
          </span>
        )}
      </div>
    </motion.div>
  );
}
