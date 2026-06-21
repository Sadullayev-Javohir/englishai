import { useEffect } from "react";
import { ArrowRight, CircleCheck, RotateCcw } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import type { ReadingPassageDto, ReadingQuizResultDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { SKILL_STEPS, openSkill } from "@/lib/skillSteps";
import { lessonOriginFrom } from "@/lib/lessonNavigation";
import { markHomeTopicCompleted } from "@/pages/home-concepts/homeRecommendation";

export function ReadingResult({ topic, result, xp, elapsedSeconds, onRetry }: { topic: ReadingPassageDto; result: ReadingQuizResultDto; xp: number; elapsedSeconds: number; onRetry: () => void }) {
  const navigate = useNavigate();
  const location = useLocation();
  const { data, error, reload } = useAsync(() => api.vocabulary.topicCompletion(topic.topicId, getLearnerId()), [topic.topicId], !result.completion && result.passed);
  const completion = result.completion ?? data;
  const next = SKILL_STEPS.find(step => completion?.modules?.some(module => module.module === step.module && module.unlocked && !module.passed));
  useEffect(() => { if (result.passed && completion?.isMastered) markHomeTopicCompleted(getLearnerId(), topic.topicId); }, [completion?.isMastered, result.passed, topic.topicId]);
  const label = next ? `${next.module}’ga o‘tish` : completion?.isMastered ? "Mavzular xaritasiga o‘tish" : "Davom etish";
  return (
    <section className="reading-result" data-pen-screen="35">
      <div className="reading-result__celebration">
        <img src="/assets/play/parrot.svg" alt="" className="reading-result__parrot" />
        <span className="reading-tag reading-tag--green">{result.passed ? "BIR QADAM OLDINGA" : "YANA BIR BOR URINIB KO‘RING"}</span>
        <h1>{result.passed ? "Hikoyani tushundingiz!" : "Yana bir bor o‘qib ko‘ring."}</h1>
        <dl className="reading-result__metrics">
          <div><dd>{result.correctCount}/{result.totalQuestions}</dd><dt>to‘g‘ri</dt></div>
          <div><dd>+{xp}</dd><dt>XP</dt></div>
          <div><dd>{elapsedSeconds >= 60 ? `${Math.floor(elapsedSeconds / 60)} min` : `${Math.max(1, elapsedSeconds)} s`}</dd><dt>mashq</dt></div>
        </dl>
        <div className={`reading-feedback${result.passed ? "" : " reading-feedback--wrong"}`}><h2><CircleCheck size={24} />Natijangiz saqlandi.</h2><p>{result.passed ? "Bugun o‘rganganingizni ertaga ham ishlating." : "Matnni qayta o‘qib, tushunishingizni mustahkamlang."}</p></div>
      </div>
      <div className="reading-result__words" aria-label="Mashqdagi so‘zlar">{topic.targetWords.slice(0, 3).map(word => <span className="reading-tag" key={word.word}>{word.word}</span>)}</div>
      <div className="reading-result__actions">
        {result.passed ? <button className="reading-primary" type="button" disabled={!completion} onClick={() => next ? openSkill(navigate, next, topic.topicId, topic.title, lessonOriginFrom(location)) : navigate("/levels")}>{label}<ArrowRight size={20} /></button> : <button className="reading-primary" type="button" onClick={onRetry}>Qayta boshlash<RotateCcw size={20} /></button>}
        {Boolean(error) && <button type="button" className="reading-secondary" onClick={reload}>Keyingi modulni qayta yuklash</button>}
        <div className="reading-result__links"><Link to="/app/vocabulary/saved">Saqlangan so‘zlarim</Link>{result.passed && <button type="button" onClick={onRetry}>Qayta mashq qilish</button>}</div>
      </div>
    </section>
  );
}
