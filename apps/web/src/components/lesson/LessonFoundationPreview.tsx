import { useState } from "react";
import { ExerciseOption } from "@/components/game";
import { LessonAdvanceAction } from "./LessonAdvanceAction";
import { LessonStageFrame } from "./LessonStageFrame";
import { useAnswerAutoAdvance } from "./useAnswerAutoAdvance";

const OPTIONS = ["I am learning English.", "I learning English.", "I learns English."];

export function LessonFoundationPreview() {
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [questionNumber, setQuestionNumber] = useState(1);
  const feedbackReady = selectedIndex !== null;

  function goNext() {
    setQuestionNumber((current) => current + 1);
    setSelectedIndex(null);
  }

  const autoAdvance = useAnswerAutoAdvance({
    enabled: feedbackReady,
    onAdvance: goNext,
    resetKey: questionNumber,
  });

  return (
    <div className="ea-stage ea-stage--focus ea-stage--frame ea-stage--md min-h-dvh bg-[var(--ea-bg)]">
      <LessonStageFrame
        mode="practice"
        width="md"
        aria-label="Shared lesson foundation preview"
        header={(
          <div className="flex w-full items-center gap-4 rounded-[20px] border border-[var(--ea-border)] bg-[var(--ea-surface)] px-4 py-3 ">
            <span className="font-duo text-sm font-extrabold uppercase tracking-wide text-[var(--ea-muted)]">
              EA-LESSON-00
            </span>
            <div className="h-3 flex-1 overflow-hidden rounded-full bg-[var(--ea-border)]" aria-label="Lesson progress">
              <div className="h-full w-2/3 rounded-full bg-ea-primary" />
            </div>
            <span className="font-duo font-extrabold text-[var(--ea-text)]">{questionNumber}</span>
          </div>
        )}
        footer={(
          <LessonAdvanceAction
            onAdvance={autoAdvance.advance}
            disabled={!feedbackReady}
            timerActive={autoAdvance.active}
            remainingSeconds={autoAdvance.remainingSeconds}
          />
        )}
      >
        <section className="grid gap-6 py-4" aria-labelledby="foundation-preview-title">
          <div className="text-center">
            <p className="font-caption text-sm font-bold uppercase tracking-[0.16em] text-[var(--ea-muted)]">
              Shared foundation preview
            </p>
            <h1 id="foundation-preview-title" className="mt-2 font-duo text-2xl font-extrabold text-[var(--ea-text)] sm:text-3xl">
              Choose the correct sentence
            </h1>
            <p className="mt-2 font-body text-[var(--ea-muted)]">
              Select an option to preview feedback and the safe 3-second advance.
            </p>
          </div>

          <div className="grid gap-3">
            {OPTIONS.map((option, index) => {
              const state = selectedIndex === null
                ? "idle"
                : index === 0
                  ? "correct"
                  : index === selectedIndex
                    ? "wrong"
                    : "dim";
              return (
                <ExerciseOption
                  key={option}
                  label={option}
                  state={state}
                  selected={selectedIndex === index}
                  disabled={feedbackReady}
                  feedbackIcon={selectedIndex === null ? undefined : index === 0 ? "check" : index === selectedIndex ? "close" : undefined}
                  onSelect={() => setSelectedIndex(index)}
                />
              );
            })}
          </div>

          <div className="min-h-24 rounded-[20px] border border-[var(--ea-border)] bg-[var(--ea-surface)] p-4 text-[var(--ea-text)]">
            {feedbackReady ? (
              <>
                <strong className="font-duo text-lg">{selectedIndex === 0 ? "Correct" : "Try this pattern"}</strong>
                <p className="mt-1 text-[var(--ea-muted)]">The feedback is ready. Continue manually or wait for the countdown.</p>
              </>
            ) : (
              <p className="text-[var(--ea-muted)]">All idle options use one neutral visual state.</p>
            )}
          </div>
        </section>
      </LessonStageFrame>
    </div>
  );
}
