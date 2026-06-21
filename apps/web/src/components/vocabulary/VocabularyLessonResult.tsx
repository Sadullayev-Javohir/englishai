import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { CheckCircle2 } from "lucide-react";
import type { RecordTopicModuleScoreResult, VocabularyTopicDetailDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { markHomeTopicCompleted } from "@/pages/home-concepts/homeRecommendation";
import { VocabularyAction, VocabularyProgress } from "./VocabularyChrome";

export function VocabularyLessonResult({ topic, correctCount, total, elapsedSeconds, result, onRetry }: {
  topic: VocabularyTopicDetailDto; correctCount: number; total: number; elapsedSeconds: number;
  result: RecordTopicModuleScoreResult; onRetry: () => void;
}) {
  const navigate = useNavigate();
  const passed = result.completion.modules.find(module => module.module === "Vocabulary")?.passed ?? false;
  const grammarUnlocked = result.completion.modules.find(module => module.module === "Grammar")?.unlocked === true;
  useEffect(() => {
    if (passed && result.completion.isMastered) markHomeTopicCompleted(getLearnerId(), topic.id);
  }, [passed, result.completion.isMastered, topic.id]);
  return (
    <section className="vocabulary-result" data-pen-screen="20">
      <VocabularyProgress step="result" />
      <div className="vocabulary-result__celebration">
        <img src="/assets/play/parrot.svg" alt="" width={230} height={192} />
        <span className="vocabulary-tag vocabulary-tag--green">{passed ? "BIR QADAM OLDINGA" : "YANA BIR BOR URINIB KO‘RING"}</span>
        <h1>{passed ? "So‘zlar endi sizniki!" : "Yana birga mashq qilamiz."}</h1>
        <dl className="vocabulary-result__metrics">
          <div><dd>{correctCount}/{total}</dd><dt>to‘g‘ri</dt></div>
          <div><dd>+{result.reward.awardedXp}</dd><dt>XP</dt></div>
          <div><dd>{elapsedSeconds < 60 ? `${elapsedSeconds} sek` : `${Math.round(elapsedSeconds / 60)} min`}</dd><dt>mashq</dt></div>
        </dl>
        <div className="vocabulary-feedback" role="status"><h2><CheckCircle2 size={24} />Natijangiz saqlandi.</h2><p>{passed ? "Bugun o‘rganganingizni ertaga ham ishlating." : "Xatolarni takrorlab, natijangizni yaxshilang."}</p></div>
        {result.reward.alreadyCreditedToday && <p className="vocabulary-result__credit">Bugungi XP mukofoti avval olingan. Natijangiz saqlandi.</p>}
      </div>
      {passed && grammarUnlocked && <div className="vocabulary-result__next"><span>KEYINGI QADAM</span><h2>Grammar bilan gap tuzing.</h2></div>}
      <footer className="vocabulary-slide__actions">
        <VocabularyAction onClick={passed && grammarUnlocked ? () => navigate(`/app/grammar/topic/${topic.id}`) : onRetry}>{passed && grammarUnlocked ? "Grammar’ga o‘tish" : "Qayta mashq qilish"}</VocabularyAction>
        {passed && <button type="button" className="vocabulary-secondary" onClick={onRetry}>Qiyin so‘zlarni takrorlash</button>}
      </footer>
    </section>
  );
}
