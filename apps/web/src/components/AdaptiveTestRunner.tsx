import { useCallback, useEffect, useRef, useState } from "react";
import type { MutableRefObject } from "react";
import { uz } from "@/content/uz";
import { api, ApiError } from "@/api/client";
import { PlacementItemKind, PlacementSpeakingOutcome } from "@/api/types";
import type { PlacementItemDto, ResumePlacementTestResult } from "@/api/types";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { PlacementAudioPlayer } from "@/components/PlacementAudioPlayer";
import { ProgressBar } from "@/components/ui/ProgressBar";
import { Spinner } from "@/components/ui/Spinner";
import { DesignModal } from "@/components/design";
import { cn } from "@/lib/cn";
import { blobToWav16kMono, bytesToBase64 } from "@/lib/audio";
import { optionLetter } from "@/lib/labels";
import { isIntegrityApiError, useSecureAssessment } from "@/lib/secureAssessment";
import type { SecureAssessmentMode } from "@/lib/secureAssessment";

// The backend runs an adaptive CAT, so the total length is not known up front. We show a
// soft progress estimate against a typical full-length run (12+6+5+1+1 = 25 items across
// the five skill stages; Writing and Speaking are single productive tasks).
const ESTIMATED_TOTAL = 25;

function SecureAssessmentWarning({ reporting, onReturn }: { reporting: boolean; onReturn: () => Promise<void> }) {
  return (
    <DesignModal
      open
      title={uz.placement.secure.warningTitle}
      description={uz.placement.secure.warningBody}
      onClose={() => void onReturn()}
      closeOnBackdrop={false}
      closeOnEscape={false}
      showClose={false}
      className="max-w-md text-center"
      footer={<Button disabled={reporting} onClick={onReturn}>{uz.placement.secure.warningCta}</Button>}
    >
      <div className="flex flex-col items-center gap-4">
        <Icon name="warning" className="text-[48px] text-[var(--ea-warning)]" />
      </div>
    </DesignModal>
  );
}

type Advance = { isTestCompleted: boolean; nextItem: PlacementItemDto | null };

type RunnerProps = {
  /** Begins the session; returns the session id and the first item to present. */
  start: () => Promise<{ sessionId: string; firstItem: PlacementItemDto | null }>;
  /** Returns a previously persisted session id when the caller has one. */
  getStoredSessionId?: () => string | null;
  /** Persists or clears the active session id for reload-safe recovery. */
  persistSessionId?: (sessionId: string | null) => void;
  /** Called once the test is finished, with the session id, to finalize and navigate away. */
  onComplete: (sessionId: string) => Promise<void>;
  /** Target of the header close button. */
  onClose: () => void;
  variant?: "default" | "exit-test";
  secure?: boolean;
  /** Which secure surface the caller established - decides which integrity signals are watched. */
  secureMode?: SecureAssessmentMode;
};

/**
 * Drives an adaptive, multi-skill test (placement G.1 or level exit test M.5) over the API.
 * The per-item answer/audio endpoints are shared (`api.placement.*`) because both flows store
 * their session in the same place; only `start`/`onComplete` differ between callers.
 */
export function AdaptiveTestRunner({
  start,
  getStoredSessionId,
  persistSessionId,
  onComplete,
  onClose,
  variant = "default",
  secure = false,
  secureMode = "fullscreen",
}: RunnerProps) {
  const sessionIdRef = useRef<string | null>(null);
  const [item, setItem] = useState<PlacementItemDto | null>(null);
  const [answered, setAnswered] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);
  const [finalizePending, setFinalizePending] = useState(false);
  const [sessionExpired, setSessionExpired] = useState(false);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);

  const handleIntegrityInvalidated = useCallback(() => {
    persistSessionId?.(null);
  }, [persistSessionId]);
  const secureAssessment = useSecureAssessment(
    activeSessionId,
    secure && Boolean(item) && !finalizePending && !error,
    handleIntegrityInvalidated,
    secureMode,
  );

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const storedSessionId = getStoredSessionId?.();
      if (storedSessionId) {
        try {
          const resumed = await api.placement.resume(storedSessionId);
          if (cancelled) return;
          sessionIdRef.current = resumed.sessionId;
          setActiveSessionId(resumed.sessionId);
          if (resumed.isCompleted || !resumed.currentItem) {
            await finalizeSession(resumed.sessionId);
          } else {
            setItem(resumed.currentItem);
          }
          return;
        } catch (resumeError) {
          if (cancelled) return;
          if (resumeError instanceof ApiError && resumeError.status === 410) {
            persistSessionId?.(null);
            setSessionExpired(true);
            return;
          }
          setError(true);
          return;
        }
      }

      try {
        const res = await start();
        if (cancelled) return;
        sessionIdRef.current = res.sessionId;
        setActiveSessionId(res.sessionId);
        persistSessionId?.(res.sessionId);
        setItem(res.firstItem);
      } catch {
        if (!cancelled) setError(true);
      }
    })();
    return () => {
      cancelled = true;
    };
    // `start` is provided fresh per render by thin wrappers; we intentionally run once.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function finalizeSession(sessionId: string) {
    setFinalizePending(true);
    setItem(null);
    try {
      await secureAssessment.leaveSecureAssessment();
      await onComplete(sessionId);
      persistSessionId?.(null);
    } catch (finalizeError) {
      if (isIntegrityApiError(finalizeError)) secureAssessment.markInvalidated();
      else setError(true);
    } finally {
      setFinalizePending(false);
    }
  }

  async function recoverSession() {
    const sessionId = sessionIdRef.current;
    if (!sessionId) {
      setError(true);
      return;
    }

    try {
      const resumed: ResumePlacementTestResult = await api.placement.resume(sessionId);
      if (resumed.isCompleted || !resumed.currentItem) {
        await finalizeSession(sessionId);
        return;
      }
      setItem(resumed.currentItem);
      setError(false);
    } catch (resumeError) {
      if (isIntegrityApiError(resumeError)) {
        secureAssessment.markInvalidated();
        return;
      }
      if (resumeError instanceof ApiError && resumeError.status === 410) {
        persistSessionId?.(null);
        setSessionExpired(true);
        return;
      }
      setError(true);
    }
  }

  // Shared "go to the next item or finish" step used by every stage.
  async function advance(res: Advance) {
    setAnswered((n) => n + 1);
    if (res.isTestCompleted || !res.nextItem) {
      await finalizeSession(sessionIdRef.current!);
      return;
    }
    setItem(res.nextItem);
  }

  async function retry() {
    if (!finalizePending && sessionIdRef.current && !item) {
      setError(false);
      await finalizeSession(sessionIdRef.current);
      return;
    }
    setError(false);
    await recoverSession();
  }

  async function closeRunner() {
    await secureAssessment.leaveSecureAssessment();
    onClose();
  }

  if (secureAssessment.invalidated) {
    return (
      <div className="exit-test-runner__state" role="alert">
        <div className="exit-test-runner__state-card">
          <Icon name="gpp_bad" className="exit-test-runner__error-text text-[42px]" />
          <h1>Test bekor qilindi</h1>
          <p>Xavfsiz test rejimi ikki marta buzildi. Testni yangidan boshlashingiz kerak.</p>
          <Button onClick={closeRunner}>Ortga qaytish</Button>
        </div>
      </div>
    );
  }

  if (sessionExpired) {
    return (
      <div className="exit-test-runner__state" role="alert">
        <div className="exit-test-runner__state-card">
          <Icon name="history" className="exit-test-runner__error-text text-[42px]" />
          <h1>{uz.placement.sessionExpiredTitle}</h1>
          <p>{uz.placement.sessionExpiredMessage}</p>
          <Button onClick={() => location.reload()}>{uz.common.retry}</Button>
        </div>
      </div>
    );
  }

  if (error) {
    if (variant === "exit-test") {
      return (
        <div className="exit-test-runner__state" role="alert">
          <div className="exit-test-runner__state-card">
            <Icon name="error" className="exit-test-runner__error-text text-[42px]" />
            <h1>{uz.common.error}</h1>
            <p>{uz.common.retry}</p>
            <Button onClick={retry}>{uz.common.retry}</Button>
          </div>
        </div>
      );
    }
    return (
      <div className="flex min-h-[100dvh] flex-col items-center justify-center gap-md overflow-x-hidden px-4 pb-[max(1.5rem,env(safe-area-inset-bottom))] pt-[max(1.5rem,env(safe-area-inset-top))] text-center md:px-8">
        <Icon name="error" className="text-error text-[40px]" />
        <p className="font-body-md text-text-secondary">{uz.common.error}</p>
        <Button onClick={retry}>{uz.common.retry}</Button>
      </div>
    );
  }

  if (!item) {
    if (variant === "exit-test") {
      return (
        <div className="exit-test-runner__state" aria-live="polite">
          <div className="exit-test-runner__state-card">
            <Spinner />
            <p>{uz.placement.finishing}</p>
          </div>
        </div>
      );
    }
    return (
      <div className="flex min-h-[100dvh] items-center justify-center px-4 py-[max(1.5rem,env(safe-area-inset-top))]">
        <Spinner />
      </div>
    );
  }

  const progress = Math.min(answered / ESTIMATED_TOTAL, 0.95);

  if (variant === "exit-test") {
    const exactTotal = item.totalItems || ESTIMATED_TOTAL;
    const exactCompleted = Math.max(item.completedItems, answered);
    const exactProgress = Math.min(exactCompleted / exactTotal, 0.95);
    return (
      <div className="exit-test-runner" data-secure-assessment-surface={secure ? "" : undefined}>
        <header className="exit-test-runner__header">
          <div className="exit-test-runner__header-inner">
            <button aria-label={uz.common.close} onClick={closeRunner} className="exit-test-runner__close"><Icon name="close" /></button>
            <div className="exit-test-runner__progress">
              <div className="exit-test-runner__progress-row">
                <span>{uz.placement.stageProgress(item.stageNumber, item.stageCount)}</span>
                <span>{uz.placement.questionProgress(exactCompleted + 1, exactTotal)}</span>
              </div>
              <ProgressBar value={exactProgress} label={uz.levelMap.exitTitle} barClassName="bg-primary-container" />
            </div>
            <div className="exit-test-runner__score"><Icon name="bolt" filled /><span>{exactCompleted}</span></div>
          </div>
        </header>
        {item.kind === PlacementItemKind.Writing ? (
          <WritingItem key={item.id} item={item} busy={busy} setBusy={setBusy} onError={recoverSession} onIntegrityInvalidated={secureAssessment.markInvalidated} advance={advance} sessionId={sessionIdRef} exitTest />
        ) : item.kind === PlacementItemKind.Speaking ? (
          <SpeakingItem key={item.id} item={item} busy={busy} setBusy={setBusy} onError={recoverSession} onIntegrityInvalidated={secureAssessment.markInvalidated} advance={advance} sessionId={sessionIdRef} exitTest />
        ) : (
          <McqItem key={item.id} item={item} busy={busy} setBusy={setBusy} onError={recoverSession} onIntegrityInvalidated={secureAssessment.markInvalidated} advance={advance} sessionId={sessionIdRef} exitTest />
        )}
        {secureAssessment.warningOpen ? <SecureAssessmentWarning reporting={secureAssessment.reporting} onReturn={secureAssessment.returnToTest} /> : null}
      </div>
    );
  }

  return (
    <div className="flex min-h-[100dvh] flex-col overflow-x-hidden" data-secure-assessment-surface={secure ? "" : undefined}>
      <header className="sticky top-0 z-10 border-b border-border bg-surface pt-[env(safe-area-inset-top)]">
        <div className="mx-auto flex w-full max-w-[700px] items-center justify-between gap-3 px-4 py-3 md:gap-4 md:px-8 md:py-md min-[1200px]:max-w-[820px]">
          <button aria-label={uz.common.close} onClick={closeRunner} className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full hover:opacity-80">
            <Icon name="close" className="text-primary" />
          </button>
          <div className="flex flex-col items-center flex-1">
            <ProgressBar value={progress} />
            <span className="font-label-md text-label-md text-text-secondary mt-2">
              {uz.placement.questionProgress(answered + 1, ESTIMATED_TOTAL)}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <Icon name="bolt" filled className="text-streak-active" />
            <span className="font-label-md text-label-md font-bold text-text-primary">{answered}</span>
          </div>
        </div>
      </header>

      {item.kind === PlacementItemKind.Writing ? (
        <WritingItem key={item.id} item={item} busy={busy} setBusy={setBusy} onError={() => setError(true)} advance={advance} sessionId={sessionIdRef} />
      ) : item.kind === PlacementItemKind.Speaking ? (
        <SpeakingItem key={item.id} item={item} busy={busy} setBusy={setBusy} onError={() => setError(true)} advance={advance} sessionId={sessionIdRef} />
      ) : (
        <McqItem key={item.id} item={item} busy={busy} setBusy={setBusy} onError={() => setError(true)} advance={advance} sessionId={sessionIdRef} />
      )}
    </div>
  );
}

type ItemProps = {
  item: PlacementItemDto;
  busy: boolean;
  setBusy: (b: boolean) => void;
  onError: () => void | Promise<void>;
  onIntegrityInvalidated?: () => void;
  advance: (res: Advance) => Promise<void>;
  sessionId: MutableRefObject<string | null>;
  exitTest?: boolean;
};

/** Multiple-choice item (Vocabulary/Grammar, Listening, Reading). */
function McqItem({ item, busy, setBusy, onError, onIntegrityInvalidated, advance, sessionId, exitTest }: ItemProps) {
  const [selected, setSelected] = useState<number | null>(null);

  async function submit(optionIndex: number) {
    if (!sessionId.current) return;
    setBusy(true);
    try {
      const res = await api.placement.answer(sessionId.current, item.id, optionIndex);
      await advance(res);
    } catch (submitError) {
      if (isIntegrityApiError(submitError)) onIntegrityInvalidated?.();
      else await onError();
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      {exitTest ? (
        <>
          <main className="exit-test-runner__main">
            <section className="exit-test-runner__card">
              {item.hasAudio && <div className="mb-lg"><p className="exit-test-runner__instruction">{uz.placement.listen.instruction}</p><PlacementAudioPlayer key={item.id} questionId={item.id} /></div>}
              {item.passageText && <div className="exit-test-runner__passage"><span className="exit-test-runner__passage-label">{uz.placement.read.passageLabel}</span><p className="whitespace-pre-line leading-relaxed">{item.passageText}</p></div>}
              <h2 className="exit-test-runner__prompt">{item.prompt}</h2>
              <div className="exit-test-runner__options">
                {(item.options ?? []).map((option, index) => {
                  const isSelected = selected === index;
                  return <button key={index} onClick={() => setSelected(index)} aria-pressed={isSelected} className={cn("exit-test-runner__option", isSelected && "exit-test-runner__option--selected")}><span className="exit-test-runner__letter">{optionLetter(index)}</span><span>{option}</span><Icon name="check_circle" className="exit-test-runner__check" /></button>;
                })}
              </div>
            </section>
          </main>
          <footer className="exit-test-runner__footer"><div className="exit-test-runner__footer-inner"><Button variant="ghost" onClick={() => submit(selected ?? 0)} disabled={busy}>{uz.placement.skipQuestion}</Button><Button onClick={() => submit(selected!)} disabled={selected === null} loading={busy}>{uz.placement.checkAnswer}</Button></div></footer>
        </>
      ) : (
        <>
      <main className="mx-auto flex w-full max-w-[700px] flex-1 flex-col items-center px-4 py-5 md:px-8 md:py-xl min-[1200px]:max-w-[820px]">
        {item.hasAudio && (
          <div className="w-full mb-lg">
            <p className="font-label-md text-label-md text-text-secondary text-center mb-md">
              {uz.placement.listen.instruction}
            </p>
            <PlacementAudioPlayer key={item.id} questionId={item.id} />
          </div>
        )}

        {item.passageText && (
          <div className="w-full mb-lg bg-surface border border-border rounded-xl p-lg">
            <p className="font-label-md text-label-md text-text-secondary mb-sm uppercase tracking-wide">
              {uz.placement.read.passageLabel}
            </p>
            <p className="font-body-md text-body-md text-text-primary whitespace-pre-line leading-relaxed">
              {item.passageText}
            </p>
          </div>
        )}

        <div className="text-center mb-xl">
          <h2 className="font-headline-lg text-headline-lg-mobile text-text-primary mb-sm">{item.prompt}</h2>
        </div>

        <div className="w-full space-y-md">
          {(item.options ?? []).map((option, index) => {
            const isSelected = selected === index;
            return (
              <button
                key={index}
                onClick={() => setSelected(index)}
                className={cn(
                  "group flex min-h-14 w-full items-center justify-between rounded-xl border bg-surface p-4 text-left transition-all duration-200 md:p-lg",
                  isSelected ? "border-primary-container ring-2 ring-primary-container/30" : "border-border hover:border-primary-container",
                )}
              >
                <div className="flex items-center gap-md">
                  <span
                    className={cn(
                      "flex h-11 w-11 shrink-0 items-center justify-center rounded-lg font-bold transition-colors",
                      isSelected ? "bg-primary-container text-on-primary-container" : "bg-background text-text-secondary",
                    )}
                  >
                    {optionLetter(index)}
                  </span>
                  <span className="font-headline-md text-headline-md text-text-primary">{option}</span>
                </div>
                <Icon name="check_circle" className={cn("text-success transition-opacity", isSelected ? "opacity-100" : "opacity-0")} />
              </button>
            );
          })}
        </div>
      </main>

      <footer className="sticky bottom-0 mt-auto border-t border-border bg-surface px-4 pb-[max(1rem,env(safe-area-inset-bottom))] pt-3 md:px-8 md:pt-4">
        <div className="max-w-[700px] mx-auto flex items-center justify-between gap-gutter">
          <Button variant="ghost" className="flex-1 py-md" onClick={() => submit(selected ?? 0)} disabled={busy}>
            {uz.placement.skipQuestion}
          </Button>
          <Button variant="primary" className="flex-[2] py-md" onClick={() => submit(selected!)} disabled={selected === null} loading={busy}>
            {uz.placement.checkAnswer}
          </Button>
        </div>
      </footer>
        </>
      )}
    </>
  );
}

/** Free-text Writing task scored by the writing assessor. */
function WritingItem({ item, busy, setBusy, onError, onIntegrityInvalidated, advance, sessionId, exitTest }: ItemProps) {
  const [text, setText] = useState("");
  const words = text.trim() ? text.trim().split(/\s+/).length : 0;
  const minimumWords = item.minWords ?? 1;
  const maximumWords = item.maxWords ?? Number.POSITIVE_INFINITY;
  const hasValidWordCount = words >= minimumWords && words <= maximumWords;

  async function submit() {
    if (!sessionId.current || !hasValidWordCount) return;
    setBusy(true);
    try {
      const res = await api.placement.answerWriting(sessionId.current, item.id, text.trim());
      await advance(res);
    } catch (submitError) {
      if (isIntegrityApiError(submitError)) onIntegrityInvalidated?.();
      else await onError();
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      {exitTest ? (
        <>
          <main className="exit-test-runner__main">
            <section className="exit-test-runner__card">
              <p className="exit-test-runner__instruction">{uz.placement.write.instruction}</p>
              <h2 className="exit-test-runner__prompt">{item.prompt}</h2>
              <textarea value={text} onChange={(event) => setText(event.target.value)} placeholder={uz.placement.write.placeholder} rows={8} className="exit-test-runner__textarea" />
              <div className="exit-test-runner__meta"><span>{uz.placement.write.wordCount(words)}</span>{item.minWords && item.maxWords ? <span>{uz.placement.write.recommended(item.minWords, item.maxWords)}</span> : null}</div>
              {!hasValidWordCount && words > 0 ? <p className="exit-test-runner__error-text" role="alert">{uz.placement.write.wordLimit(minimumWords, item.maxWords)}</p> : null}
            </section>
          </main>
          <footer className="exit-test-runner__footer"><div className="exit-test-runner__footer-inner"><span /><Button onClick={submit} disabled={!hasValidWordCount} loading={busy}>{busy ? uz.placement.write.checking : uz.placement.write.submit}</Button></div></footer>
        </>
      ) : (
        <>
      <main className="mx-auto flex w-full max-w-[700px] flex-1 flex-col px-4 py-5 md:px-8 md:py-xl min-[1200px]:max-w-[820px]">
        <p className="font-label-md text-label-md text-text-secondary mb-sm">{uz.placement.write.instruction}</p>
        <h2 className="font-headline-md text-headline-md text-text-primary mb-lg">{item.prompt}</h2>

        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={uz.placement.write.placeholder}
          rows={8}
          className="w-full flex-1 min-h-[180px] bg-surface border border-border rounded-xl p-lg font-body-md text-body-md text-text-primary focus:outline-none focus:border-primary-container resize-y"
        />

        <div className="flex items-center justify-between mt-sm">
          <span className="font-caption text-caption text-text-secondary">{uz.placement.write.wordCount(words)}</span>
          {item.minWords && item.maxWords && (
            <span className="font-caption text-caption text-text-secondary">
              {uz.placement.write.recommended(item.minWords, item.maxWords)}
            </span>
          )}
        </div>
      </main>

      <footer className="sticky bottom-0 mt-auto border-t border-border bg-surface px-4 pb-[max(1rem,env(safe-area-inset-bottom))] pt-3 md:px-8 md:pt-4">
        <div className="max-w-[700px] mx-auto">
          <Button variant="primary" fullWidth className="py-md" onClick={submit} disabled={!hasValidWordCount} loading={busy}>
            {busy ? uz.placement.write.checking : uz.placement.write.submit}
          </Button>
        </div>
      </footer>
        </>
      )}
    </>
  );
}

/** Recorded Speaking task scored by the configured speech assessor. */
function SpeakingItem({ item, busy, setBusy, onIntegrityInvalidated, advance, sessionId, exitTest }: ItemProps) {
  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const [recording, setRecording] = useState(false);
  const [micError, setMicError] = useState(false);
  const [speakingError, setSpeakingError] = useState<string | null>(null);

  async function sendAudio(audioBase64: string) {
    if (!sessionId.current) return;
    setBusy(true);
    try {
      const res = await api.placement.answerSpeaking(sessionId.current, item.id, audioBase64);
      if (res.retryable) {
        setSpeakingError(res.outcome === PlacementSpeakingOutcome.InvalidAudio
          ? uz.placement.speak.invalidAudio
          : res.outcome === PlacementSpeakingOutcome.NoSpeech
            ? uz.placement.speak.noSpeech
            : res.outcome === PlacementSpeakingOutcome.LowConfidence
              ? uz.placement.speak.lowConfidence
              : uz.placement.speak.serviceUnavailable);
        return;
      }
      setSpeakingError(null);
      await advance(res);
    } catch (submitError) {
      if (isIntegrityApiError(submitError)) onIntegrityInvalidated?.();
      else setSpeakingError(uz.placement.speak.serviceUnavailable);
    } finally {
      setBusy(false);
    }
  }

  async function toggleRecording() {
    if (recording) {
      recorderRef.current?.stop();
      return;
    }
    try {
      if (!navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === "undefined") {
        setSpeakingError(uz.placement.speak.unsupported);
        return;
      }
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true },
      });
      const recorder = new MediaRecorder(stream);
      recorderRef.current = recorder;
      chunksRef.current = [];
      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) chunksRef.current.push(e.data);
      };
      recorder.onstop = async () => {
        stream.getTracks().forEach((t) => t.stop());
        setRecording(false);
        const blob = new Blob(chunksRef.current, { type: recorder.mimeType });
        if (blob.size === 0) {
          setSpeakingError(uz.placement.speak.emptyAudio);
          return;
        }
        // Transcode MediaRecorder's WebM/Opus to the 16 kHz mono PCM WAV the backend
        // (Azure push-stream) needs; otherwise STT returns NoMatch.
        try {
          const wav = await blobToWav16kMono(blob);
          void sendAudio(bytesToBase64(wav));
        } catch {
          setSpeakingError(uz.placement.speak.invalidAudio);
        }
      };
      recorder.start(250);
      setRecording(true);
    } catch {
      setMicError(true);
    }
  }

  return (
    <>
      {exitTest ? (
        <main className="exit-test-runner__main">
          <section className="exit-test-runner__card exit-test-runner__speaking">
            <p className="exit-test-runner__instruction">{uz.placement.speak.instruction}</p>
            <h2 className="exit-test-runner__prompt">{item.prompt}</h2>
            <button onClick={toggleRecording} disabled={busy} aria-label={uz.placement.speak.record} className={cn("exit-test-runner__mic", recording && "exit-test-runner__mic--recording")}><Icon name="mic" filled className="text-[34px]" /></button>
            <p className="exit-test-runner__status">{busy ? uz.placement.speak.checking : recording ? uz.placement.speak.recording : uz.placement.speak.record}</p>
            {micError && <p className="exit-test-runner__error-text" role="alert">{uz.placement.speak.micPermission}</p>}
            {speakingError && <p className="exit-test-runner__error-text" role="alert">{speakingError}</p>}
          </section>
        </main>
      ) : (
        <>
      <main className="mx-auto flex w-full max-w-[700px] flex-1 flex-col items-center px-4 py-5 md:px-8 md:py-xl min-[1200px]:max-w-[820px]">
        <p className="font-label-md text-label-md text-text-secondary mb-sm text-center">{uz.placement.speak.instruction}</p>
        <h2 className="font-headline-md text-headline-md text-text-primary mb-xl text-center">{item.prompt}</h2>

        <div className="flex flex-col items-center gap-md mt-lg">
          <button
            onClick={toggleRecording}
            disabled={busy}
            aria-label={uz.placement.speak.record}
            className={cn(
              "w-20 h-20 rounded-full flex items-center justify-center text-white transition-all duration-200 disabled:opacity-50",
              recording ? "bg-error animate-pulse scale-110" : "bg-accent hover:brightness-110",
            )}
          >
            <Icon name="mic" filled className="text-[32px]" />
          </button>
          <span className="font-caption text-caption text-text-secondary text-center">
            {busy ? uz.placement.speak.checking : recording ? uz.placement.speak.recording : uz.placement.speak.record}
          </span>
          {micError && <p className="font-caption text-caption text-error text-center">{uz.placement.speak.micPermission}</p>}
          {speakingError && <p className="font-caption text-caption text-error text-center">{speakingError}</p>}
        </div>
      </main>
        </>
      )}
    </>
  );
}
