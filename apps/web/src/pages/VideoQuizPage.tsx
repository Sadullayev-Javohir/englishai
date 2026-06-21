import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { CheckCircle2, Circle, RotateCcw, XCircle } from "lucide-react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { api } from "@/api/client";
import type { GeneratedVideoQuizDto, VideoQuizResultDto } from "@/api/types";
import { VideoDifficultyRating } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import { formatDuration } from "@/lib/labels";
import { VideoLessonAction, VideoLessonHeader, VideoLessonHeading, VideoLessonProgress } from "@/components/video/VideoLessonChrome";
import "./VideoQuizPage.css";

// React StrictMode may mount a route twice. Share only the in-flight request, scoped to its owner.
const generations = new Map<string, Promise<GeneratedVideoQuizDto>>();
function generateQuiz(id: string, learnerId: string) {
  const key = `${learnerId}:${id}`;
  const pending = generations.get(key);
  if (pending) return pending;
  const request = api.video.generateQuiz(id, learnerId).finally(() => generations.delete(key));
  generations.set(key, request);
  return request;
}

export function VideoQuizPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const learnerId = getLearnerId();
  const [params, setParams] = useSearchParams();
  const sessionId = params.get("quiz");
  const seenSessions = useRef(new Set<string>());
  const { data: quiz, loading, error, reload } = useAsync(
    () => sessionId ? api.video.quizSession(sessionId, learnerId) : generateQuiz(id, learnerId),
    [id, learnerId, sessionId],
  );
  useDocumentTitle(quiz?.title, "Video Quiz");
  useLayoutEffect(() => {
    document.body.classList.add("video-quiz-route-active");
    return () => document.body.classList.remove("video-quiz-route-active");
  }, []);
  useEffect(() => {
    if (!quiz || quiz.videoLessonId !== id || seenSessions.current.has(quiz.quizId)) return;
    seenSessions.current.add(quiz.quizId);
    if (!sessionId) setParams({ quiz: quiz.quizId }, { replace: true });
  }, [id, quiz, sessionId, setParams]);

  const back = () => navigate(`/video/${id}/play`);
  return (
    <section className="video-lesson-page video-quiz-page">
      <VideoLessonHeader backTo={`/video/${id}/play`} />
      {loading && (!quiz || sessionId !== quiz.quizId) ? (
        <div className="video-lesson-body video-quiz-recovery" role="status">
          <VideoLessonHeading>Quiz tayyorlanmoqda…</VideoLessonHeading>
          <p>AI videoning haqiqiy transkripti asosida savollar tuzmoqda.</p>
          <button type="button" onClick={back} className="video-lesson-secondary">Videoga qaytish</button>
        </div>
      ) : error || !quiz || quiz.videoLessonId !== id ? (
        <div className="video-lesson-body video-quiz-recovery">
          <VideoLessonHeading>Quizni tayyorlab bo‘lmadi.</VideoLessonHeading>
          <p role="alert">Transkript yoki AI xizmati hozir mavjud emas. Qayta urinib ko‘ring. Eski quizning vaqti tugagan bo‘lsa, yangi quiz boshlang.</p>
          <VideoLessonAction onClick={sessionId ? () => setParams({}) : reload}>Qayta urinish</VideoLessonAction>
          <button type="button" onClick={back} className="video-lesson-secondary">Videoga qaytish</button>
        </div>
      ) : (
        <VideoQuizAttempt key={quiz.quizId} quiz={quiz} learnerId={learnerId} onBack={back} onRestart={() => setParams({})} />
      )}
    </section>
  );
}

function VideoQuizAttempt({ quiz, learnerId, onBack, onRestart }: {
  quiz: GeneratedVideoQuizDto; learnerId: string; onBack: () => void; onRestart: () => void;
}) {
  const navigate = useNavigate();
  const [index, setIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, number>>({});
  const [selected, setSelected] = useState<number | null>(null);
  const [result, setResult] = useState<VideoQuizResultDto | null>(quiz.result);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [replayOpen, setReplayOpen] = useState(false);
  const submissionRef = useRef(false);
  const question = quiz.questions[index];

  async function submit() {
    if (selected === null || submissionRef.current || !question) return;
    const next = { ...answers, [question.id]: selected };
    setAnswers(next);
    setReplayOpen(false);
    if (index < quiz.questions.length - 1) {
      setIndex(index + 1);
      setSelected(null);
      return;
    }
    submissionRef.current = true;
    setSubmitting(true);
    setError(null);
    try {
      // Submit once, after every question is answered. Answers are graded only on the server.
      setResult(await api.video.quiz(quiz.videoLessonId, learnerId, next, quiz.quizId));
    } catch {
      setError("Javoblarni yuborib bo‘lmadi. Tanlovlaringiz saqlandi — qayta yuboring.");
    } finally {
      submissionRef.current = false;
      setSubmitting(false);
    }
  }

  if (result) return <VideoQuizResult quiz={quiz} result={result} learnerId={learnerId} onRestart={onRestart} onCatalog={() => navigate("/video")} />;
  if (!question) return <div className="video-lesson-body"><p role="alert">Quiz savollari mavjud emas.</p><VideoLessonAction onClick={onBack}>Videoga qaytish</VideoLessonAction></div>;

  return (
    <div className="video-lesson-body video-quiz-question" data-screen="59">
      <VideoLessonProgress current={index + 1} total={quiz.questions.length} onBack={onBack} />
      <VideoLessonHeading>{question.promptUz}</VideoLessonHeading>
      <div className="video-quiz-prompt">
        <span className="video-lesson-tag">{formatDuration(question.sourceStartSeconds).padStart(5, "0")} · {question.sourceText}</span>
        <h2>{question.prompt}</h2>
      </div>
      <div className="video-quiz-options" role="radiogroup" aria-label={question.prompt}>
        {question.options.map((option, optionIndex) => (
          <button key={optionIndex} type="button" role="radio" aria-checked={selected === optionIndex}
            className={`video-quiz-option${selected === optionIndex ? " is-selected" : ""}`}
            onClick={() => setSelected(optionIndex)} disabled={submitting}>
            <span className="video-quiz-option__key">{String.fromCharCode(65 + optionIndex)}</span>
            <span className="video-quiz-option__text">{option}</span>
            {selected === optionIndex ? <CheckCircle2 size={20} /> : <Circle size={20} />}
          </button>
        ))}
      </div>
      <button type="button" className="video-quiz-replay" onClick={() => setReplayOpen(!replayOpen)} aria-expanded={replayOpen}>
        <RotateCcw size={16} />{replayOpen ? "Parchani yopish" : "Parchani qayta ko‘rish"}
      </button>
      {replayOpen && /^[A-Za-z0-9_-]{11}$/.test(quiz.youTubeVideoId) && (
        <iframe className="video-quiz-excerpt" title="Savolga tegishli video parchasi" allow="autoplay; encrypted-media; fullscreen" allowFullScreen
          src={`https://www.youtube.com/embed/${quiz.youTubeVideoId}?start=${Math.floor(question.sourceStartSeconds)}&end=${Math.ceil(question.sourceEndSeconds)}&autoplay=1&controls=1&playsinline=1`} />
      )}
      {error && <p role="alert" className="video-quiz-error">{error}</p>}
      <VideoLessonAction onClick={() => void submit()} disabled={selected === null || submitting}>
        {submitting ? "Tekshirilmoqda…" : "Javobni yuborish"}
      </VideoLessonAction>
    </div>
  );
}

function VideoQuizResult({ quiz, result, learnerId, onRestart, onCatalog }: {
  quiz: GeneratedVideoQuizDto; result: VideoQuizResultDto; learnerId: string; onRestart: () => void; onCatalog: () => void;
}) {
  const [rated, setRated] = useState<number | null>(null);
  const [ratingPending, setRatingPending] = useState(false);
  const [ratingError, setRatingError] = useState(false);
  const last = result.outcomes.at(-1);
  const question = quiz.questions.find(q => q.id === last?.questionId);
  async function rate(value: VideoDifficultyRating) {
    if (ratingPending) return;
    setRatingPending(true);
    setRatingError(false);
    try { await api.video.rate(quiz.videoLessonId, learnerId, value); setRated(value); }
    catch { setRatingError(true); }
    finally { setRatingPending(false); }
  }
  return (
    <div className="video-lesson-body video-quiz-result" data-screen="60">
      <VideoLessonHeading>{result.passed ? <>Videoni yaxshi<br />tushundingiz!</> : <>Yana bir marta<br />mashq qilib ko‘ring.</>}</VideoLessonHeading>
      {last && question && (
        <>
          <div className="video-quiz-option is-correct">
            <span className="video-quiz-option__key">{String.fromCharCode(65 + last.correctOptionIndex)}</span>
            <span className="video-quiz-option__text">{question.options[last.correctOptionIndex]}</span>
            <CheckCircle2 size={20} />
          </div>
          <div className={`video-quiz-feedback${last.isCorrect ? "" : " is-wrong"}`}>
            <h2>{last.isCorrect ? <CheckCircle2 size={24} /> : <XCircle size={24} />}{last.isCorrect ? "To‘g‘ri! Videoda aynan shunday deyiladi." : "To‘g‘ri javobni yana eslab qoling."}</h2>
            <p>{formatDuration(question.sourceStartSeconds).padStart(5, "0")} — “{question.sourceText}”</p>
            {last.hint && <p>{last.hint}</p>}
          </div>
        </>
      )}
      <div className="video-quiz-stats">
        <div><strong>{result.correctCount} / {result.totalQuestions}</strong><span>to‘g‘ri</span></div>
        <div><strong>+{result.awardedXp ?? 0}</strong><span>XP</span></div>
      </div>
      <details className="video-quiz-review"><summary>Barcha javoblarni ko‘rish</summary>
        {result.outcomes.map(outcome => {
          const item = quiz.questions.find(q => q.id === outcome.questionId);
          return <article key={outcome.questionId}><h3>{outcome.isCorrect ? "✓" : "✕"} {item?.prompt}</h3>
            <p>Sizning javobingiz: {item?.options[outcome.selectedOptionIndex] ?? "—"}</p>
            {!outcome.isCorrect && <p>To‘g‘ri javob: {item?.options[outcome.correctOptionIndex]}</p>}
            {outcome.hint && <p>{outcome.hint}</p>}
          </article>;
        })}
      </details>
      <section className="video-quiz-rating" aria-label="Video qiyinligi">
        <h2>Video siz uchun qanday bo‘ldi?</h2>
        <div>{[[VideoDifficultyRating.TooEasy, "Oson"], [VideoDifficultyRating.JustRight, "Mos keldi"], [VideoDifficultyRating.TooHard, "Qiyin"]].map(([value, label]) => (
          <button key={value} type="button" aria-pressed={rated === value} disabled={ratingPending} onClick={() => void rate(value as VideoDifficultyRating)}>{label}</button>
        ))}</div>
        {ratingError && <p role="alert">Baho saqlanmadi. Qayta urinib ko‘ring.</p>}
      </section>
      <VideoLessonAction onClick={onCatalog}>Video katalogiga qaytish</VideoLessonAction>
      <button type="button" className="video-lesson-secondary" onClick={onRestart}>Yana bir marta takrorlash</button>
    </div>
  );
}
