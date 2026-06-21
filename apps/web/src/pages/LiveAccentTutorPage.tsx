import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useNavigate, useParams } from "react-router-dom";
import type { AccentTutorEvaluationResult, AccentTutorMessageDto, PronunciationResultDto } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { SpeakingResultShell } from "@/components/speaking/SpeakingResultShell";
import { uz } from "@/content/uz";
import { VoiceLiveSession, VoiceLiveTokenError, type VoiceLiveStatus } from "@/api/voiceLiveSession";
import { isMicrophonePermissionDenied } from "@/lib/microphoneCapture";
import "./LiveAccentTutorPage.css";
import "./LiveAccentTutorResult.css";

const TUTORS = {
  american: { name: "The Casual Slang & Idiom Tutor", accent: "American", code: "US", intro: "Tabiiy slang, idiom va kundalik Amerika ingliz tili", color: "coral", locale: "en-US" },
  british: { name: "The Strict IELTS Examiner", accent: "British", code: "UK", intro: "Aniq grammatika va akademik IELTS nutqi", color: "violet", locale: "en-GB" },
  australian: { name: "The Active Fluency Builder", accent: "Australian", code: "AU", intro: "Tezlik, ravonlik va to‘xtovsiz gapirish", color: "amber", locale: "en-AU" },
  irish: { name: "The Expressive Storyteller", accent: "Irish", code: "IE", intro: "Boy lug‘at, hissiyot va hikoya intonatsiyasi", color: "green", locale: "en-IE" },
} as const;

type TutorId = keyof typeof TUTORS;
type Status = "starting" | "ready" | "listening" | "hearing" | "recognizing" | "thinking" | "speaking" | "idle" | "evaluating";
interface Turn extends AccentTutorMessageDto { id: string; pronunciation?: PronunciationResultDto }
interface LiveCaption { words: string[]; visibleWords: number }

function statusCopy(status: Status, liveConnected: boolean, listeningNotice: string | null) {
  if (status === "ready") return {
    step: "Boshlash",
    title: "Mikrofon o‘chiq",
    description: "Davom etish uchun “Mikrofonni yoqish” tugmasini bosing.",
  };
  if (status === "hearing") return {
    step: "2 / 4",
    title: "Ovozingiz eshitilyapti",
    description: "Gapirishda davom eting. Gapingiz tugagach, qisqa pauza qiling.",
  };
  if (status === "recognizing") return {
    step: "3 / 4",
    title: "Gapingiz yozuvga aylantirilmoqda",
    description: "Iltimos, bir necha soniya kuting.",
  };
  if (status === "thinking") return {
    step: "3 / 4",
    title: "Javob tayyorlanmoqda",
    description: "Tutor gapingizga mos javob tuzyapti.",
  };
  if (status === "speaking") return {
    step: "4 / 4",
    title: "Tutor javob beryapti",
    description: "Javobni tinglang. Xohlasangiz, gapirib javobni to‘xtatishingiz mumkin.",
  };
  if (!liveConnected) return {
    step: "1 / 4",
    title: "Ovoz kanaliga ulanmoqda",
    description: "Ulanish tugaguncha kuting.",
  };
  return {
    step: "1 / 4",
    title: "Mikrofon tayyor",
    description: listeningNotice || "Endi inglizcha gapiring. Ovozingiz eshitilganda ekran yashil rangga o‘tadi.",
  };
}

function reactionFor(text: string): string {
  const normalized = text.toLowerCase();
  if (/music|song|listen/.test(normalized)) return "🎧";
  if (/travel|trip|country|city|flight/.test(normalized)) return "✈️";
  if (/food|eat|cook|breakfast|lunch|dinner/.test(normalized)) return "🍽️";
  if (/book|read|story/.test(normalized)) return "📚";
  if (/work|job|study|school|english|level/.test(normalized)) return "💬";
  if (/great|nice|excellent|well done|brilliant/.test(normalized)) return "✨";
  if (/weekend|today|morning|evening/.test(normalized)) return "☀️";
  if (/feel|happy|love|favourite|favorite/.test(normalized)) return "💛";
  if (text.trim().endsWith("?")) return "💭";
  return "◌";
}

function highlightedWords(turn: Turn) {
  if (turn.role !== "learner") return turn.text;
  return turn.text.split(/(\s+)/).map((token, index) => {
    const clean = token.replace(/[^a-z']/gi, "").toLowerCase();
    const wrong = turn.pronunciation?.words.some((word) => word.needsPractice && word.word.toLowerCase() === clean);
    return wrong ? <mark key={`${token}-${index}`}>{token}</mark> : token;
  });
}

// Realtime engine: Azure Voice Live streams listening + thinking + speaking over one connection,
// so the old per-turn STT/commit/recorder machinery is replaced by this thin adapter that maps
// Voice Live events onto the existing UI states.
// A refused session is not a network failure: the learner has either used up today's live-tutor
// allowance or the platform's daily budget is spent. Both need their own message, otherwise the
// generic "check your connection" copy sends people to debug a connection that is fine.
function startupErrorMessage(error: unknown): string {
  const copy = uz.speaking.liveTutorErrors;
  if (isMicrophonePermissionDenied(error)) return copy.micPermission;
  if (error instanceof VoiceLiveTokenError) {
    if (error.code === "voice_live_daily_limit") return copy.dailyLimit;
    if (error.code === "budget_exhausted") return copy.budgetExhausted;
  }
  return copy.generic;
}

function mapVoiceLiveStatus(status: VoiceLiveStatus): Status {
  switch (status) {
    case "connecting": return "starting";
    case "user_speaking": return "hearing";
    case "tutor_speaking": return "speaking";
    case "error": return "ready";
    default: return status; // listening | thinking
  }
}

export function LiveAccentTutorPage() {
  const { tutorId: routeTutorId } = useParams();
  const navigate = useNavigate();
  const tutorId = routeTutorId && routeTutorId in TUTORS ? routeTutorId as TutorId : null;
  const tutor = tutorId ? TUTORS[tutorId] : null;
  const [status, setStatus] = useState<Status>("starting");
  const [startupError, setStartupError] = useState<string | null>(null);
  const [startupVersion, setStartupVersion] = useState(0);
  const [turns, setTurns] = useState<Turn[]>([]);
  const [liveCaption, setLiveCaption] = useState<LiveCaption>({ words: [], visibleWords: 0 });
  const [listeningNotice] = useState<string | null>(null);
  const [liveConnected, setLiveConnected] = useState(false);
  const [evaluation, setEvaluation] = useState<AccentTutorEvaluationResult | null>(null);
  const sessionRef = useRef<VoiceLiveSession | null>(null);
  const captionRef = useRef<HTMLParagraphElement | null>(null);
  const activeCaptionWordRef = useRef<HTMLSpanElement | null>(null);
  const feedRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    document.body.classList.add("live-room-active");
    return () => document.body.classList.remove("live-room-active");
  }, []);

  const applyStatus = useCallback((next: VoiceLiveStatus) => {
    setLiveConnected(next !== "connecting" && next !== "error");
    setStatus(mapVoiceLiveStatus(next));
  }, []);

  const showCaption = useCallback((text: string) => {
    const trimmed = text.trim();
    if (!trimmed) return;
    const words = trimmed.split(/\s+/);
    setLiveCaption({ words, visibleWords: words.length });
  }, []);

  useEffect(() => {
    if (!tutorId) return;
    setStatus("starting");
    setStartupError(null);
    setLiveConnected(false);
    setLiveCaption({ words: [], visibleWords: 0 });

    const session = new VoiceLiveSession(tutorId, {
      onStatus: applyStatus,
      onUserTranscript: (text, final) => {
        if (final) {
          setLiveCaption({ words: [], visibleWords: 0 });
          if (text.trim()) setTurns((prev) => [...prev, { id: crypto.randomUUID(), role: "learner", text: text.trim() }]);
        } else {
          showCaption(text);
        }
      },
      onTutorTranscript: (text, final) => {
        // Client emits this once per turn (final). The completed line moves to the feed.
        if (final && text.trim()) setTurns((prev) => [...prev, { id: crypto.randomUUID(), role: "tutor", text: text.trim() }]);
      },
      onTutorCaption: (words, spokenCount) => {
        // Karaoke caption synced to audio: full text stays up, spoken words are highlighted.
        setLiveCaption({ words, visibleWords: spokenCount });
      },
      onError: (message, fatal) => {
        if (fatal) {
          setStartupError(message);
          setStatus("ready");
          setLiveConnected(false);
        }
      },
      onIdleTimeout: () => {
        // Auto-closed after silence to avoid billing an idle stream. Show the resume card.
        setLiveConnected(false);
        setLiveCaption({ words: [], visibleWords: 0 });
        setStatus("idle");
      },
    });
    sessionRef.current = session;
    void session.start().catch((error: unknown) => {
      setStartupError(startupErrorMessage(error));
      setStatus("ready");
      setLiveConnected(false);
    });

    return () => {
      session.stop();
      sessionRef.current = null;
    };
  }, [tutorId, startupVersion, applyStatus, showCaption]);

  useEffect(() => {
    const feed = feedRef.current;
    if (feed) feed.scrollTop = feed.scrollHeight;
  }, [turns, liveCaption]);

  // Keep the currently-spoken caption word in view: the caption box is a few lines tall and
  // scrolls, so without this the text freezes at the top while the tutor keeps talking. Adjust
  // only the caption container's scrollTop (never the page), so it works on mobile too.
  useLayoutEffect(() => {
    const container = captionRef.current;
    const active = activeCaptionWordRef.current;
    if (!container || !active) return;
    const containerRect = container.getBoundingClientRect();
    const activeRect = active.getBoundingClientRect();
    const below = activeRect.bottom - containerRect.bottom;
    const above = containerRect.top - activeRect.top;
    if (below > 0) container.scrollTop += below + 8;
    else if (above > 0) container.scrollTop -= above + 8;
  }, [liveCaption]);

  const resumeSession = useCallback(() => {
    // Reconnect a fresh Voice Live session but keep the conversation history on screen so
    // resuming after the idle pause does not look like a page reset.
    setEvaluation(null);
    setStartupError(null);
    setStartupVersion((current) => current + 1);
  }, []);

  const finishSession = useCallback(async () => {
    sessionRef.current?.stop();
    sessionRef.current = null;
    navigate("/app/speaking");
  }, [navigate]);

  const mistakes = useMemo(() => turns.flatMap((turn) => turn.pronunciation?.words.filter((word) => word.needsPractice) ?? []), [turns]);
  const currentStatus = statusCopy(status, liveConnected, listeningNotice);
  if (!tutor) return <main className="live-room-missing"><h1>Tutor topilmadi</h1><button onClick={() => navigate("/app/speaking")}>Speaking sahifasiga qaytish</button></main>;

  if (evaluation) return createPortal(
    <main className={`live-room live-room--result live-room--${tutor.color}`}>
      <SpeakingResultShell
        status={evaluation.evaluable && evaluation.overallScore >= 70 ? "passed" : "neutral"}
        kicker={`${tutor.accent} accent · sessiya natijasi`}
        title={evaluation.evaluable ? "Suhbat yakunlandi" : "Keyingi safar ko‘proq gapiring"}
        subtitle={<p>{evaluation.feedback}</p>}
        score={evaluation.evaluable ? evaluation.overallScore : undefined}
        scoreLabel="Umumiy ball"
        details={evaluation.evaluable ? <div className="accent-result__metrics">
          <span><small>Aniqlik</small><strong>{Math.round(evaluation.accuracyScore)}</strong></span>
          <span><small>Ravonlik</small><strong>{Math.round(evaluation.fluencyScore)}</strong></span>
          <span><small>To‘liqlik</small><strong>{Math.round(evaluation.completenessScore)}</strong></span>
          <p><b>Kuchli tomon:</b> {evaluation.strongestSkill} · <b>Keyingi fokus:</b> {evaluation.focusSkill}</p>
        </div> : undefined}
        actions={<>
          <button type="button" onClick={() => navigate("/app/speaking")}>Speaking sahifasiga qaytish</button>
          <button type="button" onClick={() => { setEvaluation(null); resumeSession(); }}>Yana davom etish</button>
          <button type="button" onClick={() => navigate("/home")}>
            <Icon name="home" /> {uz.common.backHome}
          </button>
        </>}
      />
    </main>,
    document.body,
  );

  return createPortal(
    <main className={`live-room live-room--${tutor.color} is-${status}`}>
      <div className="live-room__atmosphere" aria-hidden="true" />
      <header className="live-room__header">
        <button className="live-room__round-button" onClick={() => void finishSession()} aria-label="Suhbatdan chiqish"><Icon name="close" /></button>
        <div className="live-room__identity"><span data-testid="tutor-identity-badge">{tutor.code}</span><div><strong>{tutor.name}</strong><small>{tutor.accent} accent</small></div></div>
        <div className="live-room__signal"><i /><span>LIVE</span></div>
      </header>

      <section className="live-room__stage" aria-live="polite">
        <div className="voice-core" aria-hidden="true" data-testid="live-voice-state">
          <div className="voice-core__surface">
            {(status === "listening" || status === "hearing") ? <div className="voice-core__state-icon voice-core__state-icon--ear">
              <i /><i /><i />
              <Icon name="hearing" filled />
            </div> : status === "speaking" ? <div className="voice-core__state-icon voice-core__state-icon--speaking">
              <i /><i /><i />
              <Icon name="record_voice_over" filled />
            </div> : <div className="voice-core__processing"><span /><span /><span /></div>}
          </div>
        </div>
        <div className="live-room__status" role="status" aria-live="polite">
          <span>{status === "hearing" ? "SIZNI ESHITYAPMAN" : status === "speaking" ? "TUTOR GAPIRYAPTI" : status === "recognizing" ? "MATN TAYYORLANMOQDA" : status === "thinking" ? "JAVOB TAYYORLANMOQDA" : status === "ready" ? "MIKROFON O‘CHIQ" : status === "listening" ? "MIKROFON TAYYOR — GAPIRING" : liveConnected ? "TAYYORLANMOQDA" : "ULANMOQDA"}</span>
          <p>{currentStatus.description}</p>
        </div>
        <p
          ref={captionRef}
          className={`live-room__caption ${liveCaption.words.length > 0 ? "is-visible" : ""}`}
          data-testid="live-caption"
          aria-live="off"
          aria-hidden={liveCaption.words.length === 0}
        >
          <span className="live-room__caption-row">
            {liveCaption.words.map((word, index) => {
              const isActive = index === liveCaption.visibleWords - 1;
              const className = isActive
                ? "is-active"
                : index < liveCaption.visibleWords ? "is-spoken" : "is-upcoming";
              return <span
                ref={isActive ? activeCaptionWordRef : undefined}
                className={className}
                data-active-word={isActive ? "true" : undefined}
                key={`${word}-${index}`}
              >
                {word}{index < liveCaption.words.length - 1 ? " " : ""}
              </span>;
            })}
          </span>
        </p>
      </section>

      {status === "starting" && <div className="live-room__starting" role="status" aria-label="Tayyorlanmoqda...">
        <Icon name="progress_activity" />
        <span>Tayyorlanmoqda...</span>
      </div>}

      {/* The microphone auto-starts on entry, so the "turn on microphone" gate is gone.
          This card now appears only when the mic or connection genuinely failed, offering a retry. */}
      {status === "ready" && startupError && <div className="live-room__idle" role="alert">
        <strong>Ulanishda muammo</strong>
        <p>{startupError}</p>
        <button type="button" onClick={() => setStartupVersion((current) => current + 1)}>Qayta urinish</button>
      </div>}

      {status === "idle" && <div className="live-room__idle" role="status">
        <strong>Suhbat pauzada</strong>
        <p>Jimlik tufayli sessiya to‘xtatildi. Davom etish uchun bosing.</p>
        <button type="button" onClick={resumeSession}>Davom etish</button>
      </div>}

      {status === "evaluating" && <div className="live-room__evaluating" role="status"><Icon name="progress_activity" /><span>Natija tayyorlanmoqda…</span></div>}

      <aside className="live-feed" aria-label="Jonli suhbat matni">
        <div className="live-feed__label"><span><i /> SUHBAT MATNI</span><small>{turns.length} xabar</small></div>
        <div className="live-feed__messages" ref={feedRef}>
          {turns.length === 0 && <div className="live-feed__empty">
            <Icon name="graphic_eq" />
            <strong>Gaplaringiz shu yerda ko‘rinadi</strong>
            <p>Mikrofonga inglizcha gapiring. Gapingiz aniqlangach, to‘liq matn shu yerda chiqadi.</p>
          </div>}
          {turns.slice(-8).map((turn) => (
            <article key={turn.id} className={`live-message live-message--${turn.role}`}>
              <span className="live-message__avatar">{turn.role === "tutor" ? tutor.code : "YOU"}</span>
              <div><strong>{turn.role === "tutor" ? tutor.accent : "Siz"}</strong><p>{highlightedWords(turn)}</p></div>
              <span className="live-message__reaction" aria-hidden="true">{reactionFor(turn.text)}</span>
            </article>
          ))}
          {(status === "hearing" || status === "recognizing" || status === "thinking") && <article className="live-message live-message--pending"><span className="live-message__avatar">AI</span><div><strong>{status === "hearing" ? "Ovozingiz qabul qilinmoqda" : status === "recognizing" ? "Gapingiz tahlil qilinmoqda" : "Javob tayyorlanmoqda"}</strong><p><i /><i /><i /></p></div></article>}
        </div>
        {mistakes.length > 0 && <div className="live-feed__coach"><strong>Talaffuz mashqi</strong><div>{mistakes.slice(-4).map((word, index) => <span key={`${word.word}-${index}`}>{word.word}<small>{Math.round(word.accuracyScore)}%</small></span>)}</div></div>}
      </aside>

    </main>,
    document.body,
  );
}
