import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { api, ApiError } from "@/api/client";
import { getLearnerId, setStoredLevel } from "@/app/session";
import { returnTargetOr } from "@/app/returnTarget";
import { CefrLevel, PlacementItemKind, PlacementSpeakingOutcome, TestStage } from "@/api/types";
import type { PlacementItemDto } from "@/api/types";
import { uz } from "@/content/uz";
import { blobToWav16kMono, bytesToBase64 } from "@/lib/audio";
import { AssessmentAudioPlayer, formatAudioTime, PlacementAudioPlayer } from "@/components/PlacementAudioPlayer";
import { Icon } from "@/components/ui/Icon";
import { DesignConfirm, DesignModal } from "@/components/design";
import { enterSecureAssessment, isIntegrityApiError, supportsFullscreenAssessment, useSecureAssessment } from "@/lib/secureAssessment";
import type { SecureAssessmentMode } from "@/lib/secureAssessment";
import { BookOpen, Circle, CircleCheck, Lightbulb, LoaderCircle, Mic, ShieldCheck, Square } from "lucide-react";
import { OnboardingChrome, PlayButton, PlayChip, StepProgress } from "./onboarding/OnboardingChrome";
import { PLACEMENT_SKILLS } from "./onboarding/placementSkills";
import { isPlacementResult, placementStorage } from "./onboarding/placementStorage";
import "./PlacementTestPage.css";

export { PLACEMENT_RESULT_KEY } from "./onboarding/placementStorage";
type AdvanceResult = { isTestCompleted: boolean; nextItem: PlacementItemDto | null };

/** All six skills share the WEB 08 Pen board; the server owns progress and scoring. */
export function PlacementTestPage() {
  const navigate = useNavigate();
  const learnerId = getLearnerId();
  const storage = useMemo(() => placementStorage(learnerId), [learnerId]);
  const sessionIdRef = useRef<string | null>(null);
  const requestInFlight = useRef(false);
  const mounted = useRef(true);
  const [item, setItem] = useState<PlacementItemDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sessionExpired, setSessionExpired] = useState(false);
  const [finishing, setFinishing] = useState(false);
  const [finalizePending, setFinalizePending] = useState(false);
  const [exitConfirmOpen, setExitConfirmOpen] = useState(false);
  const [speakingError, setSpeakingError] = useState<string | null>(null);
  const [started, setStarted] = useState(false);
  const [secureError, setSecureError] = useState(false);
  const [secureMode, setSecureMode] = useState<SecureAssessmentMode>("fullscreen");
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [fallbackStarting, setFallbackStarting] = useState(false);
  const [fallbackError, setFallbackError] = useState(false);
  const [selected, setSelected] = useState<number | null>(null);

  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);
  const handleIntegrityInvalidated = useCallback(() => storage.saveSession(null), [storage]);
  const secureAssessment = useSecureAssessment(activeSessionId, started && Boolean(item) && !finishing, handleIntegrityInvalidated, secureMode);

  function handleSessionFailure(reason: unknown): boolean {
    if (isIntegrityApiError(reason)) { secureAssessment.markInvalidated(); return true; }
    if (reason instanceof ApiError && (reason.status === 410 || reason.status === 403)) {
      storage.saveSession(null);
      setSessionExpired(true);
      return true;
    }
    return false;
  }

  async function finalizeSession(id: string) {
    setFinalizePending(true);
    setFinishing(true);
    setItem(null);
    setError(null);
    try {
      const result = await api.placement.finalize(id);
      if (!isPlacementResult(result) || result.stageResults.length !== PLACEMENT_SKILLS.length) {
        throw new Error("The placement result must contain all six skills.");
      }
      storage.saveResult(result);
      storage.saveSession(null);
      await secureAssessment.leaveSecureAssessment();
      if (mounted.current) navigate("/placement/result", { state: { result, learnerId }, replace: true });
    } catch (reason) {
      if (!handleSessionFailure(reason)) setError("Natijani olib bo‘lmadi. Javoblaringiz saqlangan. Natijani qayta olishga urinib ko‘ring.");
    } finally {
      setFinishing(false);
    }
  }

  async function acceptProgress(progress: AdvanceResult) {
    if (progress.isTestCompleted) {
      await finalizeSession(sessionIdRef.current!);
    } else if (progress.nextItem) {
      setItem(progress.nextItem);
      setSelected(null);
      setSpeakingError(null);
      setError(null);
    } else {
      // A missing item is NOT completion (e.g. a retryable speaking-provider failure).
      throw new Error("The server did not return the next question.");
    }
  }

  async function resumeSession(id: string, failedItemId?: string) {
    const resumed = await api.placement.resume(id);
    sessionIdRef.current = resumed.sessionId;
    setActiveSessionId(resumed.sessionId);
    storage.saveSession(resumed.sessionId);
    if (resumed.isCompleted) {
      await finalizeSession(resumed.sessionId);
      return;
    }
    if (!resumed.currentItem) throw new Error("The active session has no question.");
    setItem(resumed.currentItem);
    if (failedItemId && resumed.currentItem.id === failedItemId) {
      // Keep the mounted input/recorder and its data. The user, not an automatic retry,
      // decides whether to submit again after an ambiguous network failure.
      setError("Javob yuborilmadi. Yozganingiz va yozuvingiz shu sahifada saqlanib turibdi. Qayta yuboring.");
    } else {
      setSelected(null);
      setSpeakingError(null);
      setError(null);
      if (failedItemId) storage.saveDraft(id, failedItemId, null);
    }
  }

  async function bootstrap() {
    setError(null);
    try {
      const id = sessionIdRef.current ?? storage.readSession();
      if (id) {
        // Transient resume failures must never clear the session or start a new test.
        await resumeSession(id);
      } else {
        const response = await api.placement.start(learnerId, true);
        sessionIdRef.current = response.sessionId;
        storage.saveSession(response.sessionId);
        if (!mounted.current) return;
        setActiveSessionId(response.sessionId);
        if (!response.firstItem) throw new Error("The test has no first question.");
        setItem(response.firstItem);
      }
    } catch (reason) {
      if (!handleSessionFailure(reason)) setError("Testga ulanib bo‘lmadi. Internetni tekshirib, qayta urinib ko‘ring. Mavjud sessiya saqlandi.");
    }
  }

  async function beginSecureTest() {
    if (requestInFlight.current) return;
    requestInFlight.current = true;
    setBusy(true);
    setSecureError(false);
    try {
      setSecureMode(await enterSecureAssessment());
      setStarted(true);
      await bootstrap();
    } catch { setSecureError(true); }
    finally { requestInFlight.current = false; setBusy(false); }
  }

  async function retrySession() {
    if (requestInFlight.current) return;
    requestInFlight.current = true;
    setBusy(true);
    setError(null);
    try {
      if (finalizePending && sessionIdRef.current) await finalizeSession(sessionIdRef.current);
      else await bootstrap();
    } finally { requestInFlight.current = false; setBusy(false); }
  }

  async function submitAnswer(send: () => Promise<AdvanceResult & { retryable?: boolean; outcome?: PlacementSpeakingOutcome }>) {
    if (requestInFlight.current || !sessionIdRef.current || !item) return;
    requestInFlight.current = true;
    setBusy(true);
    setError(null);
    setSpeakingError(null);
    const previousItem = item.id;
    try {
      const response = await send();
      if (response.retryable) {
        const messages: Partial<Record<PlacementSpeakingOutcome, string>> = {
          [PlacementSpeakingOutcome.InvalidAudio]: uz.placement.speak.invalidAudio,
          [PlacementSpeakingOutcome.NoSpeech]: uz.placement.speak.noSpeech,
          [PlacementSpeakingOutcome.LowConfidence]: uz.placement.speak.lowConfidence,
        };
        setSpeakingError(messages[response.outcome!] ?? uz.placement.speak.serviceUnavailable);
      } else {
        await acceptProgress(response);
        storage.saveDraft(sessionIdRef.current, previousItem, null);
      }
    } catch (reason) {
      if (!handleSessionFailure(reason)) {
        try { await resumeSession(sessionIdRef.current, previousItem); }
        catch (resumeError) {
          if (!handleSessionFailure(resumeError)) setError("Ulanish uzildi. Javobingizni o‘chirmang — ulanishni tekshirib, davom ettiring.");
        }
      }
    } finally { requestInFlight.current = false; setBusy(false); }
  }

  const handleMcqAnswer = (optionIndex: number) => submitAnswer(() => api.placement.answer(sessionIdRef.current!, item!.id, optionIndex));
  const handleWritingSubmit = (text: string) => submitAnswer(() => api.placement.answerWriting(sessionIdRef.current!, item!.id, text.trim()));
  const handleSpeakingSubmit = (audio: string) => submitAnswer(() => api.placement.answerSpeaking(sessionIdRef.current!, item!.id, audio));

  async function startFromA1() {
    if (fallbackStarting) return;
    setFallbackStarting(true);
    setFallbackError(false);
    try {
      await api.learning.start(learnerId, CefrLevel.A1);
      try { setStoredLevel(CefrLevel.A1); } catch { /* server already persisted */ }
      storage.saveSession(null);
      await secureAssessment.leaveSecureAssessment();
      navigate(returnTargetOr("/home"), { replace: true });
    } catch { setFallbackError(true); setFallbackStarting(false); }
  }

  // Supporting states use the same typography, controls and chrome as the six boards.
  const leaveTest = async () => {
    await secureAssessment.leaveSecureAssessment();
    navigate("/assessment");
  };

  if (!started) {
    return (
      <PlacementState title={uz.placement.secure.gateTitle} onBack={leaveTest}>
        <PlayChip><ShieldCheck size={16} aria-hidden />6 ko‘nikma · Adaptiv test</PlayChip>
        <p className="onboarding-play__lede">{uz.placement.secure.notice}</p>
        {!supportsFullscreenAssessment() && <p className="onboarding-play__note">{uz.placement.secure.kioskHint}</p>}
        {secureError && <p role="alert" className="onboarding-play__alert">{uz.placement.secure.startError}</p>}
        <PlayButton disabled={busy} onClick={beginSecureTest}>{storage.readSession() ? uz.placement.secure.continueCta : uz.placement.secure.startCta}</PlayButton>
      </PlacementState>
    );
  }
  if (secureAssessment.invalidated) {
    return (
      <PlacementState title={uz.placement.secure.invalidatedTitle} onBack={leaveTest}>
        <p role="alert" className="onboarding-play__alert">{uz.placement.secure.invalidatedBody}</p>
        <PlayButton onClick={async () => {
          storage.saveSession(null);
          await secureAssessment.leaveSecureAssessment();
          window.location.reload();
        }}>{uz.placement.restartTest}</PlayButton>
        <button type="button" className="onboarding-play__quiet" disabled={fallbackStarting} onClick={startFromA1}>
          {fallbackStarting ? uz.common.loading : uz.placement.secure.invalidatedA1Cta}
        </button>
        {fallbackError && <p role="alert" className="onboarding-play__alert">{uz.assessmentIntro.startError}</p>}
      </PlacementState>
    );
  }
  if (sessionExpired) {
    return (
      <PlacementState title={uz.placement.sessionExpiredTitle} onBack={leaveTest}>
        <p className="onboarding-play__lede">Saqlangan test muddati tugagan. Darajangizni aniqlash uchun yangi test boshlang.</p>
        <PlayButton onClick={() => {
          storage.saveSession(null);
          setSessionExpired(false);
          window.location.reload();
        }}>{uz.placement.restartTest}</PlayButton>
      </PlacementState>
    );
  }
  if (error && !item) {
    return (
      <PlacementState title={finalizePending ? "Natija saqlanmoqda" : "Testga ulanib bo‘lmadi"} onBack={leaveTest}>
        <p role="alert" className="onboarding-play__alert">{error}</p>
        <PlayButton disabled={busy} onClick={retrySession}>{busy ? uz.common.loading : finalizePending ? "Natijani qayta olish" : uz.common.retry}</PlayButton>
      </PlacementState>
    );
  }
  if (finishing || !item) {
    return (
      <PlacementState title={finishing ? uz.placement.finishing : uz.common.loading} onBack={leaveTest}>
        <LoaderCircle className="placement-audio__spinner" size={32} aria-hidden />
        <p className="onboarding-play__lede" role="status">{finishing ? "Olti ko‘nikma bo‘yicha javoblaringiz baholanmoqda." : "Test savollari tayyorlanmoqda."}</p>
      </PlacementState>
    );
  }

  // ── Main render ──────────────────────────────────────────────────────────

  return (
    <OnboardingChrome className="placement-test-page" onBack={() => setExitConfirmOpen(true)} secureAssessment>
      <div>
        <PlacementBoard item={item}>
          <AnimatePresence mode="wait">
            <motion.div key={item.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
              {item.kind === PlacementItemKind.Writing ? (
                <WritingItem item={item} busy={busy} onSubmit={handleWritingSubmit} initialText={storage.readDraft(sessionIdRef.current!, item.id)} onTextChange={text => storage.saveDraft(sessionIdRef.current!, item.id, text)} />
              ) : item.kind === PlacementItemKind.Speaking ? (
                <SpeakingItem item={item} busy={busy} onSubmit={handleSpeakingSubmit} submitError={speakingError} onRecordingStart={() => setSpeakingError(null)} withoutFullscreenWatch={secureAssessment.withoutFullscreenWatch} />
              ) : (
                <McqContent item={item} selected={selected} busy={busy} onSelect={setSelected} onSubmit={() => selected !== null && handleMcqAnswer(selected)} />
              )}
            </motion.div>
          </AnimatePresence>
          {error && <div className="play-placement__recovery"><p role="alert" className="onboarding-play__alert">{error}</p><button type="button" className="onboarding-play__quiet" disabled={busy} onClick={retrySession}>Ulanishni tekshirish</button></div>}
        </PlacementBoard>
      </div>
      <DesignModal
        open={secureAssessment.warningOpen}
        title={uz.placement.secure.warningTitle}
        description={uz.placement.secure.warningBody}
        onClose={() => void secureAssessment.returnToTest()}
        closeOnBackdrop={false}
        closeOnEscape={false}
        showClose={false}
        className="play-placement__modal"
        footer={<PlayButton disabled={secureAssessment.reporting} onClick={secureAssessment.returnToTest}>{uz.placement.secure.warningCta}</PlayButton>}
      >
        <div className="flex flex-col items-center gap-4">
            <Icon name="warning" className="text-[48px] text-[var(--ea-warning)]" />
        </div>
      </DesignModal>
      <DesignConfirm
        open={exitConfirmOpen}
        className="play-placement__modal"
        title="Testdan chiqasizmi?"
        description="Joriy sessiya saqlanadi va keyin davom ettirishingiz mumkin."
        message="Placement testni hozir tark etishni tasdiqlang."
        confirmLabel="Chiqish"
        cancelLabel="Testda qolish"
        destructive
        onClose={() => setExitConfirmOpen(false)}
        onConfirm={async () => {
          await secureAssessment.leaveSecureAssessment();
          navigate("/welcome");
        }}
      />
    </OnboardingChrome>
  );
}

function PlacementState({ title, children, onBack }: { title: string; children: React.ReactNode; onBack: () => void }) {
  return (
    <OnboardingChrome className="placement-test-page" onBack={onBack}>
      <section className="play-placement__state">
        <img src="/assets/play/parrot.svg" alt="" width={200} height={168} />
        <h1 className="onboarding-play__title">{title}</h1>
        {children}
      </section>
    </OnboardingChrome>
  );
}

// ── MCQ Content ─────────────────────────────────────────────────────────────

interface McqContentProps {
  item: PlacementItemDto;
  selected: number | null;
  busy: boolean;
  onSelect: (index: number) => void;
  onSubmit: () => void;
}

// The live session and DEV-only preview render these same components.
export function McqContent({
  item,
  selected,
  busy,
  onSelect,
  onSubmit,
}: McqContentProps) {
  const options = item.options ?? [];

  return (
    <div className="play-placement__mcq">
      {item.hasAudio && <PlacementAudioPlayer questionId={item.id} disabled={busy} />}
      {item.passageText && !item.hasAudio && (
        <section className="play-placement__passage" aria-label={uz.placement.read.passageLabel}>
          <h2><BookOpen size={20} aria-hidden />{uz.placement.read.passageLabel}</h2>
          <p lang="en">{item.passageText}</p>
        </section>
      )}
      <PlacementQuestion item={item} />
      <div className="play-placement__answers" role="group" aria-label="Javob variantlari">
        {options.map((option, index) => <button key={index} type="button" disabled={busy} onClick={() => onSelect(index)} aria-pressed={selected === index} className={`play-placement__answer ${selected === index ? "is-selected" : ""}`}>
          <span className="play-placement__letter">{String.fromCharCode(65 + index)}</span><span lang="en">{option}</span>{selected === index ? <CircleCheck size={20} aria-hidden /> : <Circle size={20} aria-hidden />}
        </button>)}
      </div>
      <div className="play-placement__actions">
        <PlayButton disabled={selected === null || busy} onClick={onSubmit}>{busy ? uz.common.loading : uz.placement.submitAnswer}</PlayButton>
        {/* The API requires an answer index: never invent a skip or submit a random answer. */}
        <button type="button" className="onboarding-play__quiet" disabled title="Bu testda savollarni o‘tkazib yuborish hozircha qo‘llab-quvvatlanmaydi.">O‘tkazib yuborish</button>
      </div>
    </div>
  );
}

const SKILL_INSTRUCTIONS: Record<TestStage, string> = {
  [TestStage.Vocabulary]: "So‘z ma’nosini tanlang",
  [TestStage.Grammar]: "To‘g‘ri gapni tanlang",
  [TestStage.Listening]: "Tinglang va javobni tanlang",
  [TestStage.Reading]: "Matn asosida javob bering",
  [TestStage.Writing]: "Ingliz tilida yozing",
  [TestStage.Speaking]: "Ingliz tilida gapiring",
};

function PlacementQuestion({ item }: { item: PlacementItemDto }) {
  return (
    <div className="play-placement__question">
      <p className="play-placement__eyebrow">{SKILL_INSTRUCTIONS[item.stage]}</p>
      <h1 className="onboarding-play__title" lang="en">{item.prompt}</h1>
    </div>
  );
}

/** The preview and the live secure session share exactly the same board. */
export function PlacementBoard({ item, children }: { item: PlacementItemDto; children: React.ReactNode }) {
  const stage = PLACEMENT_SKILLS.find(skill => skill.stage === item.stage);
  return (
    <section className="play-placement" data-skill={stage?.label}>
      <div className="play-placement__status"><PlayChip>{stage?.label ?? "Test"} · {item.stageNumber} / {item.stageCount}</PlayChip><span>{item.itemNumberInStage} / {item.itemsInStage}</span></div>
      <StepProgress value={item.stageNumber} total={item.stageCount} label="Test jarayoni" />
      {children}
      <p className="play-placement__reassurance"><Lightbulb size={18} aria-hidden />Shoshilmang. Bu o‘rganish yo‘lingiz.</p>
    </section>
  );
}

// ── Writing Item ─────────────────────────────────────────────────────────────

export function WritingItem({
  item,
  busy,
  onSubmit, initialText = "", onTextChange,
}: {
  item: PlacementItemDto;
  busy: boolean;
  onSubmit: (text: string) => void;
  initialText?: string;
  onTextChange?: (text: string) => void;
}) {
  const [text, setText] = useState(initialText);
  const inputId = useId();
  const words = text.trim() ? text.trim().split(/\s+/).length : 0;
  const minimumWords = item.minWords ?? 1;
  const maximumWords = item.maxWords ?? Number.POSITIVE_INFINITY;
  const canSubmit = words >= minimumWords && words <= maximumWords && !busy;

  return (
    <div className="play-placement__productive placement-test-page__writing" aria-busy={busy}>
      <PlacementQuestion item={item} />
      <p className="onboarding-play__lede">{uz.placement.write.instruction}</p>
      <div className="play-placement__editor">
        <div className="play-placement__editor-label">
          <label htmlFor={inputId}>Javobingiz</label>
          <span>Writing</span>
        </div>
        <textarea
          id={inputId} value={text} onChange={event => { setText(event.target.value); onTextChange?.(event.target.value); }}
          placeholder={uz.placement.write.placeholder} rows={7} disabled={busy}
          spellCheck={false} autoCorrect="off" autoCapitalize="off" lang="en"
          aria-describedby={`${inputId}-limit ${inputId}-count`}
          aria-invalid={words > maximumWords}
          className="play-placement__textarea"
        />
        <div className="play-placement__word-count">
          <span id={`${inputId}-count`} className={words > maximumWords ? "is-over-limit" : undefined} aria-live="polite">{uz.placement.write.wordCount(words)}</span>
          <span id={`${inputId}-limit`}>{uz.placement.write.wordLimit(minimumWords, item.maxWords)}</span>
        </div>
      </div>
      {words > maximumWords && <p role="alert" className="onboarding-play__alert">Matnni {maximumWords} so‘zgacha qisqartiring.</p>}
      <div className="play-placement__actions">
        <PlayButton disabled={!canSubmit} onClick={() => onSubmit(text.trim())}>{busy ? uz.placement.write.checking : uz.placement.write.submit}</PlayButton>
        <p className="onboarding-play__note">O‘z so‘zlaringiz bilan yozing. Javobingiz yuborilgach baholanadi.</p>
      </div>
    </div>
  );
}

// ── Speaking Item ────────────────────────────────────────────────────────────

export function SpeakingItem({
  item, busy, onSubmit, submitError, withoutFullscreenWatch, onRecordingStart,
}: {
  item: PlacementItemDto;
  busy: boolean;
  onSubmit: (audioBase64: string) => void;
  submitError: string | null;
  onRecordingStart?: () => void;
  /** The browser's microphone permission prompt is not an integrity violation. */
  withoutFullscreenWatch: <T>(action: () => Promise<T>) => Promise<T>;
}) {
  const recorderRef = useRef<MediaRecorder | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const disposedRef = useRef(false);
  const requestingRef = useRef(false);
  const startedAt = useRef(0);
  const [recording, setRecording] = useState(false);
  const [requesting, setRequesting] = useState(false);
  const [recordingSeconds, setRecordingSeconds] = useState(0);
  const [preparedAudio, setPreparedAudio] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [preparing, setPreparing] = useState(false);
  const [audioError, setAudioError] = useState<string | null>(null);

  useEffect(() => {
    if (!recording) return;
    const timer = window.setInterval(() => {
      const seconds = Math.floor((performance.now() - startedAt.current) / 1000);
      setRecordingSeconds(Math.min(seconds, 120));
      if (seconds >= 120 && recorderRef.current?.state === "recording") {
        setPreparing(true);
        recorderRef.current.stop();
      }
    }, 250);
    return () => window.clearInterval(timer);
  }, [recording]);

  useEffect(() => {
    disposedRef.current = false;
    return () => {
      disposedRef.current = true;
      const recorder = recorderRef.current;
      if (recorder) {
        recorder.onstop = null;
        recorder.ondataavailable = null;
        recorder.onerror = null;
        if (recorder.state !== "inactive") recorder.stop();
      }
      streamRef.current?.getTracks().forEach(track => track.stop());
    };
  }, []);
  useEffect(() => () => { if (previewUrl) URL.revokeObjectURL(previewUrl); }, [previewUrl]);

  async function toggleRecording() {
    if (busy || preparing || requestingRef.current) return;
    if (recording) {
      if (recorderRef.current?.state === "recording") {
        setPreparing(true);
        recorderRef.current.stop();
      }
      return;
    }
    if (!navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === "undefined") {
      setAudioError(uz.placement.speak.unsupported);
      return;
    }
    requestingRef.current = true;
    setRequesting(true);
    setAudioError(null);
    try {
      const stream = await withoutFullscreenWatch(() => navigator.mediaDevices.getUserMedia({
        audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true },
      }));
      if (disposedRef.current) { stream.getTracks().forEach(track => track.stop()); return; }
      streamRef.current = stream;
      const audioTrack = stream.getAudioTracks()[0];
      if (!audioTrack || audioTrack.readyState === "ended") {
        stream.getTracks().forEach(track => track.stop());
        setAudioError(uz.placement.speak.emptyAudio);
        return;
      }
      const recorder = new MediaRecorder(stream);
      const chunks: Blob[] = [];
      recorderRef.current = recorder;
      setPreparedAudio(null);
      setPreviewUrl(null);
      setRecordingSeconds(0);
      recorder.ondataavailable = event => { if (event.data.size > 0) chunks.push(event.data); };
      recorder.onerror = () => {
        recorder.onstop = null;
        stream.getTracks().forEach(track => track.stop());
        setRecording(false);
        setPreparing(false);
        setAudioError(uz.placement.speak.invalidAudio);
      };
      recorder.onstop = async () => {
        stream.getTracks().forEach(track => track.stop());
        streamRef.current = null;
        if (disposedRef.current) return;
        setRecording(false);
        setRecordingSeconds(Math.floor((performance.now() - startedAt.current) / 1000));
        const blob = new Blob(chunks, { type: recorder.mimeType });
        if (!blob.size) {
          setPreparing(false);
          setAudioError(uz.placement.speak.emptyAudio);
          return;
        }
        setPreviewUrl(URL.createObjectURL(blob));
        setPreparing(true);
        try {
          const wav = await blobToWav16kMono(blob);
          if (!disposedRef.current) setPreparedAudio(bytesToBase64(wav));
        } catch {
          if (!disposedRef.current) setAudioError(uz.placement.speak.invalidAudio);
        } finally {
          if (!disposedRef.current) setPreparing(false);
        }
      };
      recorder.start(250);
      startedAt.current = performance.now();
      onRecordingStart?.();
      setRecording(true);
    } catch {
      streamRef.current?.getTracks().forEach(track => track.stop());
      if (!disposedRef.current) setAudioError(uz.placement.speak.micPermission);
    } finally {
      requestingRef.current = false;
      if (!disposedRef.current) setRequesting(false);
    }
  }

  const status = busy ? uz.placement.speak.checking
    : requesting ? "Mikrofon kutilmoqda…"
      : recording ? "Yozilmoqda. Tugatgach to‘xtating."
        : preparing ? uz.placement.speak.preparing
          : preparedAudio ? uz.placement.speak.ready : "Tayyor bo‘lgach, mikrofonni bosing.";

  return (
    <div className="play-placement__productive play-placement__speaking" aria-busy={busy || preparing}>
      <PlacementQuestion item={item} />
      <p className="onboarding-play__lede">{uz.placement.speak.instruction}</p>
      <div className={`play-placement__recorder ${recording ? "is-recording" : ""}`}>
        <div className="play-placement__recorder-heading"><Mic size={20} aria-hidden /><span>Ovozli javob</span><span>Speaking</span></div>
        {(!previewUrl || recording) && <>
          <button type="button" className="play-placement__record" onClick={() => void toggleRecording()}
            disabled={busy || preparing || requesting} aria-label={recording ? "Yozishni to‘xtatish" : uz.placement.speak.record}>
            {requesting || preparing ? <LoaderCircle size={32} aria-hidden className="placement-audio__spinner" />
              : recording ? <Square size={28} fill="currentColor" aria-hidden /> : <Mic size={32} aria-hidden />}
          </button>
          <time className="play-placement__timer" aria-label="Yozuv davomiyligi">{formatAudioTime(recordingSeconds)}</time>
        </>}
        <p className="play-placement__record-status" role="status">{status}</p>
        {previewUrl && !recording && <AssessmentAudioPlayer key={previewUrl} src={previewUrl} recording durationHint={recordingSeconds} disabled={busy || requesting} />}
        <p className="onboarding-play__note">{item.minWords ? `Taxminan ${item.minWords} yoki undan ko‘proq so‘z ayting. ` : ""}3 soniyadan 2 daqiqagacha gapiring. Yuborishdan oldin yozuvni tinglashingiz mumkin.</p>
      </div>
      {(audioError || submitError) && <p role="alert" className="onboarding-play__alert">{audioError ?? submitError}</p>}
      {previewUrl && !recording && !preparing && recordingSeconds < 3 && <p role="alert" className="onboarding-play__alert">Yozuv juda qisqa. Kamida 3 soniya gapirib, qayta yozing.</p>}
      <div className="play-placement__actions">
        <PlayButton disabled={!preparedAudio || recording || preparing || busy || requesting || recordingSeconds < 3}
          onClick={() => { if (preparedAudio) onSubmit(preparedAudio); }}>
          {busy ? uz.placement.speak.checking : "Ovozli javobni yuborish"}
        </PlayButton>
        {previewUrl && !recording && <button type="button" className="onboarding-play__quiet" disabled={busy || preparing || requesting} onClick={() => void toggleRecording()}>{uz.placement.speak.recordAgain}</button>}
      </div>
    </div>
  );
}
