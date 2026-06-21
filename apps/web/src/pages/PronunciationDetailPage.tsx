import { useEffect, useRef, useState } from "react";
import { Navigate, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api, ApiError } from "@/api/client";
import type { WordPronunciationCheckDto } from "@/api/types";
import { useAsync } from "@/lib/useAsync";
import {
  playWordAudioBase64,
  playRecordedBlob,
  speakEnglishWord,
  blobToWav16kMono,
  bytesToBase64,
} from "@/lib/audio";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { cn } from "@/lib/cn";

// ── Clean dojo/* 3D component layer (jungle pronunciation dojo) ─────

import { ProgressRing } from "@/components/dojo/ProgressRing";
import { Confetti } from "@/components/dojo/Confetti";
import { DuoButton } from "@/components/game/DuoButton";
import { AppButton, DesignModal } from "@/components/design";
import { VisemeMouth } from "@/components/speaking/VisemeMouth";
import { MicFrequencyBars } from "@/components/speaking/MicFrequencyBars";
import { useMicRecorder } from "@/lib/useMicRecorder";
import "./PronunciationDetailPage.css";

// "Speak slowly" playback factor for the reference clip so the learner hears each sound.
const SLOW_RATE = 0.45;
// Default successful-attempt target before a practice word leaves the queue. The
// authoritative value comes back from the server on each attempt; this is the pre-attempt
// placeholder so the streak indicator renders the right number of slots immediately.
const MASTERY_SUCCESSES = 3;

/**
 * Filled 3D card accents - the same ten-hue "board" palette used across the Jungle
 * Academy (HomePage, modules, daily plan). Every section on this screen gets its own
 * deep, saturated colour so the page reads as a cohesive set of chunky 3D cards over
 * the living background.mp4 - never plain white.
 */
type CardAccent =
  | "board1" | "board2" | "board3" | "board4" | "board5"
  | "board6" | "board7" | "board8" | "board9" | "board10";
const CARD_STYLE: Record<CardAccent, { face: string; lip: string }> = {
  board1: { face: "bg-ea-primary", lip: "" },
  board2: { face: "bg-ea-primary", lip: "" },
  board3: { face: "bg-ea-primary", lip: "" },
  board4: { face: "bg-ea-primary", lip: "" },
  board5: { face: "bg-ea-primary", lip: "" },
  board6: { face: "bg-ea-primary", lip: "" },
  board7: { face: "bg-ea-primary", lip: "" },
  board8: { face: "bg-ea-primary", lip: "" },
  board9: { face: "bg-ea-primary", lip: "" },
  board10: { face: "bg-ea-primary", lip: "" },
};

/** A chunky, deeply-coloured 3D card (the page's building block). Reuses the exact
 *  visual language of the HomePage module/daily-plan cards. */
function ColoredCard({
  accent,
  className,
  children,
}: {
  accent: CardAccent;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div
      data-accent={accent}
      className={cn("pronunciation-home-card", CARD_STYLE[accent].face, CARD_STYLE[accent].lip, className)}
    >
      <div className="pronunciation-home-card__content">{children}</div>
    </div>
  );
}

/** Screen 06 - Pronunciation Detail, rebuilt as a "pronunciation dojo" on the jungle. */
export function LegacyPronunciationRedirect() {
  const { word = "" } = useParams();
  return <Navigate to={`/app/speaking/pronunciation/${encodeURIComponent(word)}`} replace />;
}

export function PronunciationDetailPage() {
  const { word = "" } = useParams();
  const [searchParams] = useSearchParams();
  const practiceWordId = searchParams.get("practiceWordId");
  const returnTo = practiceWordId ? "/app/speaking/practice-words" : "/app/speaking";
  const returnLabel = practiceWordId ? uz.pronunciation.backHome : uz.pronunciation.back;
  return (
    <PronunciationDetailContent
      word={word}
      returnTo={returnTo}
      returnLabel={returnLabel}
      practiceWordId={practiceWordId}
    />
  );
}

function PronunciationDetailContent({
  word,
  returnTo,
  returnLabel,
  practiceWordId = null,
  eyebrow = uz.pronunciation.dojoTitle,
}: {
  word: string;
  returnTo: string;
  returnLabel: string;
  practiceWordId?: string | null;
  eyebrow?: string;
}) {
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.speaking.wordDetail(word), [word]);

  // Bumped on every native-audio tap to (re)start the mouth/parrot animation.
  const [playKey, setPlayKey] = useState(0);
  const [playing, setPlaying] = useState(false);
  const audioRef = useRef<HTMLAudioElement | null>(null);

  // Track whether the (re)play finished so the parrot stops "talking".
  useEffect(() => {
    if (playKey === 0) return;
    const t = window.setTimeout(() => setPlaying(false), 2600);
    return () => window.clearTimeout(t);
  }, [playKey]);

  function playNative(rate = 1) {
    if (!data) return;
    if (!audioRef.current) audioRef.current = new Audio();
    const played =
      data.audioBase64 != null &&
      playWordAudioBase64(data.audioBase64, audioRef.current, rate < 1 ? SLOW_RATE : 1);
    if (!played) speakEnglishWord(data.spokenForm ?? data.word, rate < 1 ? { rate: SLOW_RATE } : undefined);
    setPlaying(true);
    setPlayKey((k) => k + 1);
  }

  const notFound = error instanceof ApiError && error.status === 404;

  return (
    <main className="pronunciation-home-page" data-pen-screen="49">
      <div className="pronunciation-home-shell">
      <motion.header
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 240, damping: 24 }}
        className="pronunciation-home-header"
      >
        <div className="pronunciation-home-header__copy">
          <p>{eyebrow === uz.pronunciation.dojoTitle ? "SPEAKING · TALAFFUZ" : eyebrow}</p>
          <strong>Tinglang. Takrorlang. Ishonch hosil qiling.</strong>
        </div>
      </motion.header>

      {loading ? (
        <ModulePageLoader icon="record_voice_over" accent="red" />
      ) : error || !data ? (
        <ColoredCard accent="board4" className="pronunciation-home-error text-center py-xl">
          <div className="w-14 h-14 rounded-full bg-white/20 flex items-center justify-center mx-auto">
            <Icon name={notFound ? "search_off" : "error"} className="text-white text-3xl" />
          </div>
          <div className="pronunciation-home-error__copy">
            <h2 className="font-duo font-extrabold text-headline-md">
              {notFound ? uz.pronunciation.notFoundTitle : uz.pronunciation.errorTitle}
            </h2>
            <p className="font-body-md text-body-md">
              {notFound ? uz.pronunciation.notFoundBody : uz.pronunciation.errorBody}
            </p>
          </div>
          <div className="pronunciation-home-error__actions flex flex-col gap-sm">
            {!notFound && (
              <DuoButton color="blue" fullWidth onClick={reload}>
                {uz.pronunciation.retry}
              </DuoButton>
            )}
            <DuoButton color="green" fullWidth onClick={() => navigate(returnTo)}>
              {returnLabel}
            </DuoButton>
          </div>
        </ColoredCard>
      ) : (
        <div className="pronunciation-home-layout">
          <div className="pronunciation-home-main">
            <ColoredCard accent="board2" className="pronunciation-home-hero">
              <div className="grid items-center gap-md p-md sm:grid-cols-[minmax(0,1fr)_150px] sm:gap-lg sm:p-lg md:grid-cols-[minmax(0,1fr)_170px] md:p-xl [@media(max-height:600px)]:gap-sm [@media(max-height:600px)]:p-sm">
                <div className="text-center sm:text-left">
                  <span className="inline-flex rounded-full bg-white/15 px-3 py-1 font-duo text-[12px] font-extrabold uppercase tracking-wider ring-1 ring-white/25">
                    Bugungi so'z
                  </span>
                  <h1 className="mt-md break-words font-duo text-[clamp(2.25rem,12vw,3rem)] font-extrabold capitalize leading-none text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.3)] md:text-[64px] [@media(max-height:600px)]:mt-sm [@media(max-height:600px)]:text-[2.25rem]">
                    {data.spokenForm ? `${data.word} — ${data.spokenForm}` : data.word}
                  </h1>
                  <p className="mt-sm font-body-lg text-body-lg text-white/80">/{data.ipa}/</p>
                </div>
                <div className="mx-auto rounded-[24px] bg-ea-surface/95 p-2 ring-2 ring-white/40">
                  <VisemeMouth frames={data.visemes} playKey={playKey || null} rate={1} size={145} />
                </div>
              </div>
              <div className="grid gap-sm border-t border-white/15 bg-black/10 p-md sm:grid-cols-2">
                <DuoButton color="blue" icon="volume_up" fullWidth onClick={() => playNative(1)}>
                  {playing ? "Eshitilmoqda..." : uz.pronunciation.nativePronunciation}
                </DuoButton>
                <DuoButton color="yellow" icon="slow_motion_video" fullWidth onClick={() => playNative(SLOW_RATE)}>
                  {uz.pronunciation.slow}
                </DuoButton>
              </div>
            </ColoredCard>

            <ColoredCard accent="board6" className="pronunciation-home-panel">
              <div className="mb-md flex items-center gap-sm">
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-white/20 font-duo font-extrabold ring-1 ring-white/30">1</span>
                <h2 className="font-duo font-extrabold text-headline-md text-white">{uz.pronunciation.mouthPosition}</h2>
              </div>
              <p className="mb-md font-body-md text-body-md text-white/80">
                Tovushlarni tartib bilan ko'ring. Sariq tovushlarga ko'proq e'tibor bering.
              </p>
              <div className="flex flex-wrap gap-sm">
                {data.phonemes.map((p, i) => (
                  <PhonemeChip
                    key={`${p.phoneme}-${i}`}
                    symbol={p.phoneme}
                    hard={p.isHardForUzbek}
                    onHear={() => playNative(SLOW_RATE)}
                  />
                ))}
              </div>
              {data.tipUz && (
                <div className="mt-lg flex items-start gap-sm rounded-2xl bg-white/15 p-md ring-1 ring-white/20">
                  <Icon name="lightbulb" filled className="shrink-0 text-[22px] text-ea-orange-600" />
                  <div>
                    <h3 className="font-duo font-extrabold text-white">{uz.pronunciation.tip}</h3>
                    <p className="mt-xs font-body-md text-body-md text-white/90">{data.tipUz}</p>
                  </div>
                </div>
              )}
            </ColoredCard>

            {(data.keyWord || data.exampleSentenceUz) && (
              <ColoredCard accent="board1" className="pronunciation-home-panel pronunciation-home-context">
                <div className="mb-md flex items-center gap-sm">
                  <span className="flex h-8 w-8 items-center justify-center rounded-full bg-white/20 font-duo font-extrabold ring-1 ring-white/30">2</span>
                  <h2 className="font-duo font-extrabold text-headline-md text-white">Kontekstda mustahkamlang</h2>
                </div>
                {data.keyWord && (
                  <p className="font-duo font-extrabold text-headline-md text-white capitalize">{data.keyWord}</p>
                )}
                {data.exampleSentenceUz && (
                  <p className="mt-sm border-l-4 border-white/50 pl-md font-body-lg text-body-lg text-white/90">
                    {data.exampleSentenceUz}
                  </p>
                )}
              </ColoredCard>
            )}
          </div>

          <div className="pronunciation-home-aside">
            <PronunciationAttempt
              word={data.spokenForm ?? data.word}
              practiceWordId={practiceWordId}
              onMastered={() => navigate("/app/speaking/practice-words", { replace: true })}
            />
          </div>
        </div>
      )}
      </div>
    </main>
  );
}

export function PronunciationDetailExperience({
  word,
  returnTo,
  returnLabel,
  eyebrow,
}: {
  word: string;
  returnTo: string;
  returnLabel: string;
  eyebrow?: string;
}) {
  return (
    <PronunciationDetailContent
      word={word}
      returnTo={returnTo}
      returnLabel={returnLabel}
      eyebrow={eyebrow}
    />
  );
}

/** A single 3D phoneme chip (IPA). Tapping plays the word slowly so the learner hears that
 *  sound in context. Hard-for-Uzbek sounds get a warning tint. */
function PhonemeChip({ symbol, hard, onHear }: { symbol: string; hard: boolean; onHear: () => void }) {
  return (
    <motion.button
      transition={{ type: "spring", stiffness: 600, damping: 24 }}
      onClick={onHear}
      title={hard ? uz.pronunciation.hardHint : uz.pronunciation.tapToHear}
      className={cn(
        "pronunciation-home-phoneme min-w-16 h-16 px-md rounded-xl flex items-center justify-center gap-xs",
        "font-duo font-extrabold text-headline-md text-white",
        hard
          ? "bg-ea-orange-500   transition-transform"
          : "bg-ea-primary   transition-transform",
      )}
    >
      <span>{symbol}</span>
      {hard && <Icon name="priority_high" className="text-[15px] text-white" />}
    </motion.button>
  );
}

/**
 * "Say it yourself" - records the learner, scores it against the target word, and shows a
 * score ring + per-phoneme pass/fail dots. In the practice flow, clearing 85+ the required
 * number of times fills the success streak and opens the mastery modal (which returns to the
 * practice list). Reuses the single-word pronunciation check.
 */
function PronunciationAttempt({
  word,
  practiceWordId,
  onMastered,
}: {
  word: string;
  practiceWordId?: string | null;
  onMastered?: () => void;
}) {
  const [status, setStatus] = useState<"idle" | "recording" | "checking">("idle");
  const [result, setResult] = useState<WordPronunciationCheckDto | null>(null);
  const [best, setBest] = useState<number | null>(null);
  const [celebrate, setCelebrate] = useState(false);
  const [micError, setMicError] = useState(false);
  const [assessmentError, setAssessmentError] = useState(false);
  const [activeStream, setActiveStream] = useState<MediaStream | null>(null);
  const [hasRecording, setHasRecording] = useState(false);
  const [isPlayingRecording, setIsPlayingRecording] = useState(false);
  // Progress toward the 3-success mastery streak (only meaningful in the practice flow).
  const [successCount, setSuccessCount] = useState(0);
  const [requiredSuccesses, setRequiredSuccesses] = useState(MASTERY_SUCCESSES);
  // Set on the attempt that masters the word; drives the "so'zni o'zlashtirdingiz" modal.
  const [masteredScore, setMasteredScore] = useState<number | null>(null);

  const successCountRef = useRef(0);
  const recordedBlobRef = useRef<Blob | null>(null);
  const playbackAudioRef = useRef<HTMLAudioElement | null>(null);
  const playbackUrlRef = useRef<string | null>(null);

  useEffect(() => {
    return () => {
      playbackAudioRef.current?.pause();
      if (playbackUrlRef.current) URL.revokeObjectURL(playbackUrlRef.current);
    };
  }, []);

  async function check(base64: string) {
    setStatus("checking");
    setAssessmentError(false);
    try {
      const attempt = practiceWordId
        ? await api.speaking.practiceAttempt(practiceWordId, base64)
        : null;
      const r = attempt?.assessment ?? await api.vocabulary.pronounce(word, base64);
      setResult(r);

      if (r.recognized && r.isAuthentic) {
        const score = Math.round(r.overallScore);
        setBest((prev) => (prev === null ? score : Math.max(prev, score)));

        if (attempt) {
          // Practice flow: the word leaves the queue only after N successful attempts.
          setRequiredSuccesses(attempt.requiredSuccesses);
          const advanced = attempt.successfulAttempts > successCountRef.current;
          successCountRef.current = attempt.successfulAttempts;
          setSuccessCount(attempt.successfulAttempts);
          if (advanced) {
            setCelebrate(true);
            window.setTimeout(() => setCelebrate(false), 1400);
          }
          if (attempt.mastered) {
            // Word cleared 85+ the required number of times: celebrate, then show the
            // mastery modal. The learner returns to the practice list from the modal.
            setCelebrate(true);
            setMasteredScore(score);
          }
        } else if (best !== null && score > best) {
          // Generic vocab flow: celebrate whenever the learner beats their best.
          setCelebrate(true);
          window.setTimeout(() => setCelebrate(false), 1500);
        }
      }
    } catch {
      setResult(null);
      setAssessmentError(true);
    } finally {
      setStatus("idle");
    }
  }

  const recorder = useMicRecorder(async (blob) => {
    recordedBlobRef.current = blob;
    setHasRecording(true);
    try {
      const wav = await blobToWav16kMono(blob);
      await check(bytesToBase64(wav));
    } catch {
      setAssessmentError(true);
      setStatus("idle");
    }
  });

  useEffect(() => {
    setActiveStream(recorder.stream);
    if (recorder.micError) setMicError(true);
  }, [recorder.micError, recorder.stream]);

  async function toggleRecording() {
    if (status === "recording") {
      recorder.stop();
      return;
    }
    setResult(null);
    setCelebrate(false);
    setMicError(false);
    setAssessmentError(false);
    try {
      if (await recorder.start()) setStatus("recording");
    } catch {
      setActiveStream(null);
      setMicError(true);
    }
  }

  function playMyRecording() {
    if (!recordedBlobRef.current) return;
    playbackAudioRef.current ??= new Audio();
    playbackAudioRef.current.onplay = () => setIsPlayingRecording(true);
    playbackAudioRef.current.onpause = () => setIsPlayingRecording(false);
    playbackAudioRef.current.onended = () => setIsPlayingRecording(false);
    playbackAudioRef.current.onerror = () => setIsPlayingRecording(false);
    playbackUrlRef.current = playRecordedBlob(
      recordedBlobRef.current,
      playbackAudioRef.current,
      playbackUrlRef.current,
    );
  }

  const isRecording = status === "recording";
  const unavailable = result && (!result.recognized || !result.isAuthentic);

  return (
    // position: relative + overflow-visible so the Confetti centers and bursts beyond the card.
    <ColoredCard accent="board4" className="pronunciation-home-practice relative overflow-visible flex flex-col items-center gap-md text-center">
      {/* Confetti burst on every cleared attempt, mounted only while celebrating. */}
      <Confetti show={celebrate} />

      <MasteredModal
        open={masteredScore !== null}
        word={word}
        score={masteredScore ?? 0}
        onClose={() => onMastered?.()}
      />

      <div className="w-full space-y-xs text-left">
        <h3 className="font-duo font-extrabold text-headline-md text-white flex items-center justify-center gap-sm">
          <span className="flex h-8 w-8 items-center justify-center rounded-full bg-white/20 font-duo text-[16px] font-extrabold ring-1 ring-white/30">3</span>
          {uz.pronunciation.practiceTitle}
        </h3>
        <p className="font-caption text-caption text-white/85 text-center">{uz.pronunciation.practiceHint}</p>
        {practiceWordId && (
          <>
            <p className="rounded-xl bg-white/15 px-sm py-xs text-center font-duo text-[13px] font-bold text-white">
              Har safar 85+ ball oling — {requiredSuccesses} marta muvaffaqiyatli talaffuz qilsangiz, so‘z mashq ro‘yxatidan chiqadi.
            </p>
            <div
              className="pron-streak"
              role="status"
              aria-label={`${successCount} / ${requiredSuccesses} muvaffaqiyatli talaffuz`}
            >
              {Array.from({ length: requiredSuccesses }, (_, index) => (
                <span
                  key={index}
                  className={cn("pron-streak__dot", index < successCount && "is-done")}
                >
                  {index < successCount ? <Icon name="check" filled className="text-[18px]" /> : index + 1}
                </span>
              ))}
            </div>
          </>
        )}
      </div>

      {/* Record button: prominent mic with a pulse while recording. */}
      <div className="relative flex items-center justify-center">
        {isRecording && (
          <motion.span
            className="absolute w-24 h-24 rounded-full bg-white/30"
            animate={{ scale: [1, 1.5, 1], opacity: [0.6, 0, 0.6] }}
            transition={{ duration: 1.4, repeat: Infinity, ease: "easeOut" }}
          />
        )}
        <motion.button
          onClick={toggleRecording}
          disabled={status === "checking"}
          transition={{ type: "spring", stiffness: 600, damping: 24 }}
          className={cn(
            "relative z-10 w-20 h-20 rounded-full flex items-center justify-center text-white ",
            "transition-all  disabled:opacity-60 disabled:shadow-none",
            isRecording
              ? "bg-ea-blue-600 scale-110 animate-pulse dark:bg-ea-surface"
              : "bg-ea-blue-600 hover:scale-105 dark:bg-ea-surface",
          )}
          aria-label={uz.pronunciation.recordStart}
        >
          <Icon
            name={isRecording ? "stop" : "mic"}
            filled
            className="pronunciation-home-mic-icon text-[34px]"
          />
        </motion.button>
      </div>

      {isRecording && <MicFrequencyBars stream={activeStream} className="w-full" />}

      <span className="font-duo font-bold text-label-md text-white/90 min-h-4">
        {isRecording
          ? uz.pronunciation.recording
          : status === "checking"
            ? uz.pronunciation.checking
            : uz.pronunciation.recordStart}
      </span>

      {micError && <p className="font-caption text-caption text-white">{uz.pronunciation.micError}</p>}
      {assessmentError && (
        <p className="rounded-xl bg-white/15 p-md font-caption text-caption text-white">
          {uz.pronunciation.assessmentError}
        </p>
      )}

      {hasRecording && (
        <button
          type="button"
          onClick={playMyRecording}
          className="flex items-center justify-center gap-xs rounded-xl bg-white/15 px-md py-sm font-duo text-label-md font-bold text-white transition-colors hover:bg-white/25"
        >
          <Icon
            name={isPlayingRecording ? "graphic_eq" : "play_arrow"}
            filled
            className={cn("text-[20px] text-white", isPlayingRecording && "animate-pulse")}
          />
          {uz.pronunciation.listenMine}
        </button>
      )}

      {result && (
        <div className="w-full flex flex-col items-center gap-md pt-2">
          {unavailable ? (
            <div className="w-full rounded-xl p-md flex items-start gap-sm text-left bg-white/15">
              <Icon name="hearing_disabled" filled className="text-white text-[22px] shrink-0" />
              <p className="font-body-md text-body-md text-white/95">
                {result.isAuthentic
                  ? uz.pronunciation.notRecognizedAttempt
                  : uz.pronunciation.authenticScoreUnavailable}
              </p>
            </div>
          ) : (
            <>
              <ProgressRing value={Math.round(result.overallScore)}>
                <span className="font-duo font-extrabold text-display-sm text-white leading-none">
                  {Math.round(result.overallScore)}
                </span>
                <span className="font-caption text-caption text-white/85">{uz.pronunciation.scoreLabel}</span>
              </ProgressRing>

              {best !== null && (
                <p className="font-label-md text-label-md text-white/90">
                  {uz.pronunciation.bestScore(best)}
                </p>
              )}

              <div className="flex flex-wrap gap-2 justify-center">
                {result.phonemes.map((phoneme, index) => {
                  const ok = phoneme.accuracyScore >= 70;
                  return (
                    <motion.span
                      key={`${phoneme.phoneme}-${index}`}
                      initial={ok ? false : { x: 0 }}
                      animate={ok ? { x: 0 } : { x: [0, -6, 6, -4, 4, 0] }}
                      transition={{ duration: 0.5 }}
                      title={`${ok ? uz.pronunciation.phonemePass : uz.pronunciation.phonemeFail}: ${uz.pronunciation.phonemeScore(Math.round(phoneme.accuracyScore))}`}
                      className={cn(
                        "inline-flex items-center gap-1 rounded-full px-3 py-1",
                        "font-duo font-bold text-[13px] text-white",
                        ok ? "bg-white/25" : "bg-white/10",
                      )}
                    >
                      <Icon
                        name={ok ? "check_circle" : "cancel"}
                        filled
                        className="text-white text-[16px]"
                      />
                      {phoneme.phoneme}
                      <span className="text-white/75">{Math.round(phoneme.accuracyScore)}</span>
                    </motion.span>
                  );
                })}
              </div>

              <div
                className={cn(
                  "w-full rounded-xl p-md flex items-start gap-sm text-left",
                  result.correct ? "bg-white/15" : "bg-white/10",
                )}
              >
                <Icon
                  name={result.correct ? "check_circle" : "lightbulb"}
                  filled
                  className={cn("text-[22px] shrink-0", "text-white")}
                />
                <div className="space-y-xs">
                  <p className={cn("font-duo font-extrabold text-label-md text-white")}>
                    {result.correct ? uz.pronunciation.correct : uz.pronunciation.incorrect}
                  </p>
                  {result.feedbackUz && (
                    <p className="font-body-md text-body-md text-white/95">{result.feedbackUz}</p>
                  )}
                </div>
              </div>
            </>
          )}
        </div>
      )}
    </ColoredCard>
  );
}

/**
 * Celebration shown the moment a practice word is mastered (cleared 85+ the required number
 * of times). Any dismissal - the button or the backdrop - returns to the practice list.
 */
function MasteredModal({
  open,
  word,
  score,
  onClose,
}: {
  open: boolean;
  word: string;
  score: number;
  onClose: () => void;
}) {
  return (
    <DesignModal
      open={open}
      onClose={onClose}
      title="So‘zni o‘zlashtirdingiz!"
      description={`“${word}” so‘zini ${MASTERY_SUCCESSES} marta 85+ ball bilan to‘g‘ri talaffuz qildingiz.`}
      showClose={false}
      className="pron-mastered-modal"
      footer={
        <AppButton tone="primary" size="lg" leadingIcon="arrow_back" onClick={onClose}>
          Mashqlar ro‘yxatiga qaytish
        </AppButton>
      }
    >
      <div className="pron-mastered">
        <span className="pron-mastered__badge" aria-hidden="true">
          <Icon name="verified" filled />
        </span>
        <div className="pron-mastered__score">
          <b>{score}</b>
          <small>oxirgi ball</small>
        </div>
        <p className="pron-mastered__note">Bu so‘z endi mashq ro‘yxatidan chiqarildi.</p>
      </div>
    </DesignModal>
  );
}
