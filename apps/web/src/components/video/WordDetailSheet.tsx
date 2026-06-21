import { useEffect, useRef, useState } from "react";
import { uz } from "@/content/uz";
import { api, ApiError } from "@/api/client";
import type { WordPronunciationCheckDto } from "@/api/types";
import { useAsync } from "@/lib/useAsync";
import {
  blobToWav16kMono,
  bytesToBase64,
  playPronunciationFeedback,
  playRecordedBlob,
  playWordVoice,
  unlockFeedbackAudio,
} from "@/lib/audio";
import { useMicRecorder } from "@/lib/useMicRecorder";
import { Icon } from "@/components/ui/Icon";
import { Spinner } from "@/components/ui/Spinner";
import { VisemeMouth } from "@/components/speaking/VisemeMouth";
import { cn } from "@/lib/cn";
import { DesignSheet } from "@/components/design";
import "./wordOverlays.css";

const PRONUNCIATION_TIMEOUT_MS = 20_000;

interface WordDetailSheetProps {
  /** The clicked word (already stripped of punctuation/casing for lookup). */
  word: string;
  /** The transcript line the word came from - shown as a real in-video example sentence. */
  exampleSentence: string;
  /** A vetted Uzbek meaning from the lesson glossary, if the word is in it (rule 11). */
  translation?: string | null;
  /**
   * Heading over the example sentence. Defaults to the in-video label ("Bu videoda:"); text-based
   * skills (vocabulary, reading, …) pass the in-text label ("Bu matnda:") since the sentence comes
   * from a passage, not a video.
   */
  exampleLabel?: string;
  /** Opens the video's "AI'dan so'rash" explain-chat panel, prefilled with a question about this
   * word - omitted where the sheet is used outside the video player (that panel is video-only). */
  onAskAi?: (question: string) => void;
  /** Shows the "O'zingiz ayting" record-and-score block. Used by the video player where learners
   * practice pronunciation; the vocabulary/reading word sheet (old design) keeps to meaning +
   * listening only, so it is off by default elsewhere. */
  enableRecording?: boolean;
  onClose: () => void;
}

/**
 * A bottom sheet shown when a learner taps a word in the video transcript (PROJECT-SPEC B.3,
 * Bosqich 3): IPA, an animated 2D mouth (visemes), per-phoneme difficulty, a vetted Uzbek tip,
 * native audio, and the word in its real in-video context. Reuses the Speaking word-detail
 * endpoint (CMU-backed, so any English word resolves) and the VisemeMouth component.
 */
export function WordDetailSheet({
  word,
  exampleSentence,
  translation,
  exampleLabel = uz.videoWord.inThisVideo,
  onAskAi,
  enableRecording = true,
  onClose,
}: WordDetailSheetProps) {
  const { data, loading, error } = useAsync(() => api.speaking.wordDetail(word), [word]);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  // Each tap bumps `key` so the mouth replays; `rate` slows both the voice and the mouth track
  // together for "speak slowly". Null until the first tap, so the mouth starts in its neutral pose.
  const [play, setPlay] = useState<{ key: number; rate: number } | null>(null);

  function playNative(rate: number) {
    audioRef.current ??= new Audio();
    // Prefer the Azure reference clip (reliable where the browser has no TTS voices); fall back
    // to the browser voice. Only restart the mouth when playback was actually accepted.
    if (playWordVoice(data?.audioBase64, word, audioRef.current, rate)) {
      const effectiveRate = data?.audioBase64 ? Math.max(0.5, rate) : rate;
      setPlay((p) => ({ key: (p?.key ?? 0) + 1, rate: effectiveRate }));
    }
  }

  // "Siz ham sinab ko'ring": record the learner's own attempt, score it against this word, and
  // let them hear their own recording back (unlike the reference-clip playback above).
  const [checking, setChecking] = useState(false);
  const [checkResult, setCheckResult] = useState<WordPronunciationCheckDto | null>(null);
  const [checkError, setCheckError] = useState<"timeout" | "failed" | null>(null);
  const assessmentControllerRef = useRef<AbortController | null>(null);
  const recordedBlobRef = useRef<Blob | null>(null);
  const myAudioRef = useRef<HTMLAudioElement | null>(null);
  const [myRecordingUrl, setMyRecordingUrl] = useState<string | null>(null);
  const [portalTarget, setPortalTarget] = useState<Element>(() => document.fullscreenElement ?? document.body);

  const { status: micStatus, micError, start, stop } = useMicRecorder(async (blob) => {
    recordedBlobRef.current = blob;
    setChecking(true);
    setCheckError(null);
    const controller = new AbortController();
    assessmentControllerRef.current = controller;
    const timeout = window.setTimeout(() => controller.abort(), PRONUNCIATION_TIMEOUT_MS);
    try {
      const wav = await blobToWav16kMono(blob);
      const result = await api.vocabulary.pronounce(word, bytesToBase64(wav), controller.signal);
      setCheckResult(result);
      if (!result.isAuthentic) return;
      const passed = result.recognized && result.overallScore >= 85;
      playPronunciationFeedback(passed);
      if (!passed) window.setTimeout(() => playNative(0.75), 450);
    } catch {
      setCheckResult(null);
      setCheckError(controller.signal.aborted ? "timeout" : "failed");
    } finally {
      window.clearTimeout(timeout);
      if (assessmentControllerRef.current === controller) assessmentControllerRef.current = null;
      setChecking(false);
    }
  });

  useEffect(() => {
    const updatePortalTarget = () => setPortalTarget(document.fullscreenElement ?? document.body);
    document.addEventListener("fullscreenchange", updatePortalTarget);
    return () => document.removeEventListener("fullscreenchange", updatePortalTarget);
  }, []);

  useEffect(() => {
    // Revoke the last object URL on unmount so it doesn't leak.
    return () => {
      assessmentControllerRef.current?.abort();
      if (myRecordingUrl) URL.revokeObjectURL(myRecordingUrl);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  function toggleRecording() {
    if (micStatus === "recording") {
      stop();
      return;
    }
    setCheckResult(null);
    setCheckError(null);
    unlockFeedbackAudio();
    void start();
  }

  function playMyRecording() {
    if (!recordedBlobRef.current) return;
    myAudioRef.current ??= new Audio();
    setMyRecordingUrl((prev) => playRecordedBlob(recordedBlobRef.current!, myAudioRef.current!, prev));
  }

  const notFound = error instanceof ApiError && error.status === 404;

  return (
    <DesignSheet
      open
      onClose={onClose}
      title={word}
      description="Word power"
      closeLabel={uz.videoWord.close}
      className="word-detail-sheet"
      portalTarget={portalTarget}
    >
        <div className="p-lg">
          {loading ? (
            <div className="py-xl flex justify-center">
              <Spinner />
            </div>
          ) : error || !data ? (
            <div className="py-lg text-center space-y-sm">
              <Icon name={notFound ? "search_off" : "error"} className="text-ea-muted" />
              <p className="font-body-md text-body-md text-ea-muted">
                {notFound ? uz.videoWord.notFound : uz.videoWord.error}
              </p>
            </div>
          ) : (
            <div className="space-y-lg">
              <div className="word-detail-sheet__meaning space-y-sm p-md text-center">
                <p className="word-detail-sheet__ipa font-body-lg text-body-lg">{data.ipa}</p>
                {translation && (
                  <p className="font-duo text-headline-md font-extrabold text-ea-ink-deep">{translation}</p>
                )}
                <div className="flex flex-wrap items-center justify-center gap-sm pt-xs">
                  <button
                    onClick={() => playNative(0.75)}
                    className="word-detail-sheet__listen"
                  >
                    <Icon name="volume_up" className="text-[20px]" />
                    {uz.videoWord.listen}
                  </button>
                  <button
                    onClick={() => playNative(0.45)}
                    className="word-detail-sheet__listen word-detail-sheet__listen--slow"
                  >
                    <Icon name="slow_motion_video" className="text-[20px]" />
                    {uz.videoWord.listenSlow}
                  </button>
                  {onAskAi && (
                    <button
                      onClick={() => onAskAi(`"${word}" so'zi nima uchun ishlatilgan?`)}
                      className="inline-flex items-center gap-xs rounded-xl border border-ea-border bg-ea-surface px-3 py-2 font-duo text-label-md font-extrabold text-ea-primary transition-colors hover:border-ea-primary"
                    >
                      <Icon name="smart_toy" className="text-[20px]" />
                      {uz.videoWord.askAi}
                    </button>
                  )}
                </div>
              </div>

              <div className="word-detail-sheet__mouth flex flex-col items-center gap-md p-md">
                <h3 className="flex items-center gap-xs font-duo text-label-md font-extrabold text-ea-text">
                  <Icon name="face" className="text-ea-primary text-[18px]" />
                  {uz.videoWord.mouthPosition}
                </h3>
                <VisemeMouth
                  frames={data.visemes}
                  playKey={play?.key ?? null}
                  rate={play?.rate ?? 1}
                  size={128}
                />
                <div className="flex flex-wrap gap-xs justify-center">
                  {data.phonemes.map((p, i) => (
                    <span
                      key={i}
                      className={cn(
                        "flex items-center justify-center min-w-10 h-10 px-2 rounded-lg border font-headline-md text-headline-md",
                        p.isHardForUzbek
                          ? "border-ea-brand-yellow bg-ea-yellow-50 text-ea-yellow-700"
                          : "border-ea-border bg-ea-surface text-ea-primary",
                      )}
                      title={p.isHardForUzbek ? "O'zbek tilida qiyin tovush" : undefined}
                    >
                      {p.phoneme}
                    </span>
                  ))}
                </div>
              </div>

              {data.tipUz && (
                <div className="flex items-start gap-sm rounded-[20px] border border-ea-brand-yellow bg-ea-yellow-50 p-md text-ea-yellow-700">
                  <Icon name="lightbulb" filled className="shrink-0 text-ea-yellow-700" />
                  <div>
                    <p className="mb-xs font-duo text-label-md font-extrabold">{uz.videoWord.tip}</p>
                    <p className="font-body-md text-body-md">{data.tipUz}</p>
                  </div>
                </div>
              )}

              <div className="word-detail-sheet__example p-md">
                <p className="mb-xs font-duo text-label-md font-extrabold text-ea-muted">{exampleLabel}</p>
                <p className="font-body-md text-body-md italic text-ea-text">"{exampleSentence}"</p>
              </div>

              {/* "O'zingiz ayting" - record, score, and hear yourself back. Only in contexts that
                  ask for pronunciation practice (the video player); the plain meaning/listen word
                  sheet (vocabulary/reading) omits it. */}
              {enableRecording && (
                <div className="flex flex-col items-center gap-sm rounded-[24px] border border-ea-border bg-ea-primary-soft p-md text-center text-ea-text">
                  <h3 className="flex items-center gap-xs font-duo text-label-md font-extrabold text-ea-text">
                    <Icon name="mic" filled className="text-ea-primary text-[18px]" />
                    {uz.pronunciation.practiceTitle}
                  </h3>
                  <p className="font-caption text-caption text-ea-muted">{uz.pronunciation.practiceHint}</p>

                  <button
                    type="button"
                    onClick={toggleRecording}
                    disabled={checking}
                    aria-pressed={micStatus === "recording"}
                    aria-busy={checking || undefined}
                    aria-describedby="word-pronunciation-status"
                    aria-label={
                      micStatus === "recording"
                        ? uz.pronunciation.recording
                        : checking
                          ? uz.pronunciation.checking
                          : uz.pronunciation.recordStart
                    }
                    className={cn(
                      "flex h-14 w-14 items-center justify-center rounded-full text-white transition-transform",
                      micStatus === "recording" ? "bg-error animate-pulse" : "bg-ea-primary",
                      checking && "opacity-60",
                    )}
                  >
                    <Icon name={micStatus === "recording" ? "stop" : "mic"} filled className="text-[24px]" />
                  </button>

                  <p
                    id="word-pronunciation-status"
                    className="min-h-[1.2em] font-caption text-caption font-bold text-ea-text"
                    role="status"
                    aria-live="polite"
                    aria-atomic="true"
                  >
                    {micError
                      ? uz.pronunciation.micError
                      : micStatus === "recording"
                        ? uz.pronunciation.recording
                        : checking
                          ? uz.pronunciation.checking
                          : uz.pronunciation.recordStart}
                  </p>

                  {(checkResult || checkError) && (
                    <div className="w-full space-y-xs border-t border-ea-border pt-xs text-ea-text">
                      {checkError ? (
                        <p className="font-caption text-caption font-bold text-ea-text">
                          {checkError === "timeout"
                            ? uz.pronunciation.assessmentTimeout
                            : uz.pronunciation.assessmentError}
                        </p>
                      ) : checkResult?.isAuthentic === false ? (
                        <p className="font-caption text-caption font-bold text-ea-text">
                          {uz.pronunciation.authenticScoreUnavailable}
                        </p>
                      ) : checkResult?.recognized ? (
                        <>
                          <p className="font-label-md text-label-md font-extrabold text-ea-text">
                            {checkResult.overallScore >= 85 ? uz.pronunciation.correct : uz.pronunciation.incorrect}
                            {" - "}
                            {Math.round(checkResult.overallScore)} {uz.pronunciation.scoreLabel}
                          </p>
                          {checkResult.feedbackUz && (
                            <p className="font-caption text-caption text-ea-muted">{checkResult.feedbackUz}</p>
                          )}
                        </>
                      ) : (
                        <p className="font-caption text-caption font-bold text-ea-text">
                          {uz.pronunciation.notRecognizedAttempt}
                        </p>
                      )}
                      {recordedBlobRef.current && (
                        <button
                          type="button"
                          onClick={playMyRecording}
                          className="mx-auto flex items-center gap-xs font-label-md text-label-md font-bold text-ea-primary hover:underline"
                        >
                          <Icon name="play_arrow" filled className="text-[18px] text-ea-primary" />
                          {uz.pronunciation.listenMine}
                        </button>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
    </DesignSheet>
  );
}
