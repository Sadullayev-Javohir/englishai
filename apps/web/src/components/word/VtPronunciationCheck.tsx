import { useEffect, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
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

const PRONUNCIATION_TIMEOUT_MS = 20_000;
const PASS_SCORE = 85;

interface VtPronunciationCheckProps {
  /** The word to practice (already the dictionary/base form). */
  word: string;
  /** CTA label for the toggle button — reuses the vocabulary "pronunciationCta" string. */
  toggleLabel: string;
}

/**
 * Inline pronunciation check that lives directly under the flashcard, replacing the old jump to a
 * separate `/pronunciation/:index` route so the learner never leaves the lesson flow. It reuses the
 * exact record → score → listen machinery from {@link WordDetailSheet} (mic recorder,
 * `api.vocabulary.pronounce`, viseme mouth, native playback) but renders as a calm, collapsible teal
 * panel in the `/home` design language instead of a bottom sheet.
 *
 * The word-detail fetch (IPA, visemes, phonemes, reference audio) is lazy: it only runs once the
 * learner opens the panel, so flipping through many cards costs no extra API calls.
 */
export function VtPronunciationCheck({ word, toggleLabel }: VtPronunciationCheckProps) {
  const [open, setOpen] = useState(false);

  // Reset back to collapsed whenever the card word changes, so each flashcard starts closed.
  useEffect(() => {
    setOpen(false);
  }, [word]);

  return (
    <div className="vt-pron">
      <button
        type="button"
        className={cn("vt-pron__toggle", open && "is-open")}
        aria-expanded={open}
        onClick={() => setOpen((v) => !v)}
      >
        <span className="vt-pron__toggle-icon">
          <Icon name="mic" filled />
        </span>
        <span className="vt-pron__toggle-label">{toggleLabel}</span>
        <Icon name={open ? "expand_less" : "expand_more"} className="vt-pron__toggle-chevron" />
      </button>

      <AnimatePresence initial={false}>
        {open && (
          <motion.div
            key="body"
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: "auto" }}
            exit={{ opacity: 0, height: 0 }}
            transition={{ duration: 0.2 }}
            className="vt-pron__reveal"
          >
            <VtPronunciationBody word={word} />
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

function VtPronunciationBody({ word }: { word: string }) {
  const { data, loading } = useAsync(() => api.speaking.wordDetail(word), [word]);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [play, setPlay] = useState<{ key: number; rate: number } | null>(null);

  function playNative(rate: number) {
    audioRef.current ??= new Audio();
    if (playWordVoice(data?.audioBase64, word, audioRef.current, rate)) {
      const effectiveRate = data?.audioBase64 ? Math.max(0.5, rate) : rate;
      setPlay((p) => ({ key: (p?.key ?? 0) + 1, rate: effectiveRate }));
    }
  }

  const [checking, setChecking] = useState(false);
  const [checkResult, setCheckResult] = useState<WordPronunciationCheckDto | null>(null);
  const [checkError, setCheckError] = useState<"timeout" | "failed" | null>(null);
  const assessmentControllerRef = useRef<AbortController | null>(null);
  const recordedBlobRef = useRef<Blob | null>(null);
  const myAudioRef = useRef<HTMLAudioElement | null>(null);
  const [myRecordingUrl, setMyRecordingUrl] = useState<string | null>(null);

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
      const passed = result.recognized && result.overallScore >= PASS_SCORE;
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

  // Fresh word → drop any previous attempt/score so the panel never shows stale feedback.
  useEffect(() => {
    setCheckResult(null);
    setCheckError(null);
    recordedBlobRef.current = null;
  }, [word]);

  useEffect(() => {
    return () => {
      assessmentControllerRef.current?.abort();
      if (myRecordingUrl) URL.revokeObjectURL(myRecordingUrl);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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

  if (loading) {
    return (
      <div className="vt-pron__panel vt-pron__panel--loading">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="vt-pron__panel">
      <div className="vt-pron__mouth">
        <VisemeMouth frames={data?.visemes} playKey={play?.key ?? null} rate={play?.rate ?? 1} size={96} />
        <div className="vt-pron__meta">
          {data?.ipa && <p className="vt-pron__ipa">{data.ipa}</p>}
          {data?.phonemes?.length ? (
            <div className="vt-pron__phonemes">
              {data.phonemes.map((p, i) => (
                <span
                  key={i}
                  className={cn("vt-pron__phoneme", p.isHardForUzbek && "is-hard")}
                  title={p.isHardForUzbek ? "O'zbek tilida qiyin tovush" : undefined}
                >
                  {p.phoneme}
                </span>
              ))}
            </div>
          ) : null}
        </div>
      </div>

      <div className="vt-pron__listen">
        <button type="button" className="vt-pron__listen-btn" onClick={() => playNative(0.75)}>
          <Icon name="volume_up" className="text-[20px]" />
          {uz.videoWord.listen}
        </button>
        <button type="button" className="vt-pron__listen-btn" onClick={() => playNative(0.45)}>
          <Icon name="slow_motion_video" className="text-[20px]" />
          {uz.videoWord.listenSlow}
        </button>
      </div>

      <div className="vt-pron__record">
        <button
          type="button"
          onClick={toggleRecording}
          disabled={checking}
          aria-pressed={micStatus === "recording"}
          aria-busy={checking || undefined}
          aria-label={
            micStatus === "recording"
              ? uz.pronunciation.recording
              : checking
                ? uz.pronunciation.checking
                : uz.pronunciation.recordStart
          }
          className={cn("vt-pron__mic", micStatus === "recording" && "is-recording", checking && "is-busy")}
        >
          <Icon name={micStatus === "recording" ? "stop" : "mic"} filled className="text-[26px]" />
        </button>
        <p className="vt-pron__status" role="status" aria-live="polite" aria-atomic="true">
          {micError
            ? uz.pronunciation.micError
            : micStatus === "recording"
              ? uz.pronunciation.recording
              : checking
                ? uz.pronunciation.checking
                : uz.pronunciation.practiceHint}
        </p>
      </div>

      {(checkResult || checkError) && (
        <div
          className={cn(
            "vt-pron__result",
            checkResult?.recognized && checkResult.isAuthentic
              ? checkResult.overallScore >= PASS_SCORE
                ? "is-pass"
                : "is-retry"
              : "is-retry",
          )}
        >
          {checkError ? (
            <p className="vt-pron__result-text">
              {checkError === "timeout" ? uz.pronunciation.assessmentTimeout : uz.pronunciation.assessmentError}
            </p>
          ) : checkResult?.isAuthentic === false ? (
            <p className="vt-pron__result-text">{uz.pronunciation.authenticScoreUnavailable}</p>
          ) : checkResult?.recognized ? (
            <>
              <p className="vt-pron__result-score">
                <Icon
                  name={checkResult.overallScore >= PASS_SCORE ? "check_circle" : "refresh"}
                  filled
                />
                {checkResult.overallScore >= PASS_SCORE ? uz.pronunciation.correct : uz.pronunciation.incorrect}
                {" · "}
                {Math.round(checkResult.overallScore)} {uz.pronunciation.scoreLabel}
              </p>
              {checkResult.feedbackUz && <p className="vt-pron__result-text">{checkResult.feedbackUz}</p>}
            </>
          ) : (
            <p className="vt-pron__result-text">{uz.pronunciation.notRecognizedAttempt}</p>
          )}
          {recordedBlobRef.current && (
            <button type="button" className="vt-pron__mine" onClick={playMyRecording}>
              <Icon name="play_arrow" filled className="text-[18px]" />
              {uz.pronunciation.listenMine}
            </button>
          )}
        </div>
      )}
    </div>
  );
}
