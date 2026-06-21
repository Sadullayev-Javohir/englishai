import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import { PronunciationErrorType, type SpeakingPracticeWordDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { Icon } from "@/components/ui/Icon";
import { ProgressRing } from "@/components/ui/ProgressRing";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { useAsync } from "@/lib/useAsync";
import "./SpeakingPracticeWordsPage.css";

// A word is considered mastered once a dedicated practice attempt clears this score.
const MASTERY_TARGET = 85;

const ERROR_LABELS: Record<PronunciationErrorType, string> = {
  [PronunciationErrorType.None]: "Talaffuz xatosi",
  [PronunciationErrorType.Mispronunciation]: "Noto‘g‘ri talaffuz",
  [PronunciationErrorType.Omission]: "Tushib qolgan tovush",
  [PronunciationErrorType.Insertion]: "Ortiqcha tovush",
};

type ScoreBand = "danger" | "warn" | "good";

// Best achievement so far = the closest this learner has come to mastering the word.
function bestSoFar(word: SpeakingPracticeWordDto) {
  return Math.round(Math.max(word.lastAccuracyScore, word.bestPracticeScore ?? 0));
}

function scoreBand(score: number): ScoreBand {
  if (score >= MASTERY_TARGET) return "good";
  if (score >= 50) return "warn";
  return "danger";
}

export function SpeakingPracticeWordsPage() {
  const navigate = useNavigate();
  const learnerId = getLearnerId();
  const state = useAsync(() => api.speaking.practiceWords(learnerId), [learnerId]);

  const words = useMemo(() => state.data ?? [], [state.data]);
  const summary = useMemo(() => {
    if (words.length === 0) return { total: 0, errors: 0, average: 0 };
    const errors = words.reduce((sum, word) => sum + word.errorCount, 0);
    const average = Math.round(words.reduce((sum, word) => sum + bestSoFar(word), 0) / words.length);
    return { total: words.length, errors, average };
  }, [words]);

  if (state.loading && !state.data) {
    return (
      <div className="spw-page">
        <div className="spw-hero spw-hero--skeleton" aria-hidden="true" />
        <LoadingSkeleton rows={6} />
      </div>
    );
  }

  if (state.error) {
    return (
      <div className="spw-page">
        <PageState
          icon="cloud_off"
          title="Talaffuz mashqlari yuklanmadi"
          hint="Internet aloqasini tekshirib, qayta urinib ko‘ring."
          action="Qayta urinish"
          onAction={state.reload}
          error
        />
      </div>
    );
  }

  return (
    <main className="spw-page" data-pen-screen="48a">
      <header className="spw-hero">
        <div className="spw-hero__copy">
          <span className="spw-eyebrow"><Icon name="record_voice_over" filled /> SPEAKING · XATOLARDAN MASHQQA</span>
          <h1>Xatolardan mashqqa</h1>
          <p>Suhbatda qiyin bo‘lgan so‘zlarni alohida tinglang, takrorlang va {MASTERY_TARGET}+ ball bilan mustahkamlang.</p>
        </div>
        {words.length > 0 ? (
          <dl className="spw-hero__stats" aria-label="Talaffuz mashqi statistikasi">
            <div><dt><Icon name="checklist" /> So‘zlar</dt><dd>{summary.total}</dd></div>
            <div><dt><Icon name="target" /> O‘rtacha ball</dt><dd>{summary.average}</dd></div>
            <div><dt><Icon name="error" /> Jami xatolar</dt><dd>{summary.errors}</dd></div>
          </dl>
        ) : null}
      </header>

      {words.length === 0 ? (
        <PageState
          icon="verified"
          title="Barcha so‘zlar o‘zlashtirilgan"
          hint="Keyingi suhbatda yangi talaffuz xatosi aniqlansa, u shu yerda paydo bo‘ladi."
          action="Speakingni boshlash"
          onAction={() => navigate("/app/speaking")}
        />
      ) : (
        <section className="spw-grid" aria-label="Talaffuz mashq so‘zlari">
          {words.map((word) => (
            <PracticeWordCard
              key={word.id}
              word={word}
              onPractice={() => navigate(
                `/app/speaking/pronunciation/${encodeURIComponent(word.word)}?practiceWordId=${word.id}`,
              )}
            />
          ))}
        </section>
      )}
    </main>
  );
}

function PracticeWordCard({
  word,
  onPractice,
}: {
  word: SpeakingPracticeWordDto;
  onPractice: () => void;
}) {
  const best = bestSoFar(word);
  const band = scoreBand(best);
  const progress = Math.min(best / MASTERY_TARGET, 1);

  return (
    <article className="spw-card" data-band={band}>
      <div className="spw-card__head">
        <div className="spw-card__ring">
          <ProgressRing
            value={progress}
            size={72}
            strokeWidth={7}
            trackClassName="spw-ring__track"
            arcClassName="spw-ring__arc"
          >
            <b className="spw-card__score">{best}</b>
            <small className="spw-card__target">/{MASTERY_TARGET}</small>
          </ProgressRing>
        </div>
        <div className="spw-card__id">
          <h2>{word.word}</h2>
          <span className="spw-card__error"><Icon name="graphic_eq" filled /> {ERROR_LABELS[word.lastErrorType] ?? ERROR_LABELS[PronunciationErrorType.None]}</span>
        </div>
      </div>

      <dl className="spw-card__stats">
        <div><dt>Xatolar</dt><dd>{word.errorCount} marta</dd></div>
        <div><dt>Oxirgi natija</dt><dd>{Math.round(word.lastAccuracyScore)}</dd></div>
        <div><dt>Eng yaxshi mashq</dt><dd>{word.bestPracticeScore == null ? "—" : Math.round(word.bestPracticeScore)}</dd></div>
      </dl>

      <button type="button" className="spw-card__cta" onClick={onPractice}>
        <Icon name="mic" filled /> Mashq qilish <Icon name="arrow_forward" className="spw-card__cta-arrow" />
      </button>
    </article>
  );
}

function PageState({
  icon,
  title,
  hint,
  action,
  onAction,
  error = false,
}: {
  icon: string;
  title: string;
  hint: string;
  action: string;
  onAction: () => void;
  error?: boolean;
}) {
  return (
    <section className={`spw-state${error ? " spw-state--error" : ""}`} role={error ? "alert" : "status"}>
      <span className="spw-state__icon"><Icon name={icon} filled /></span>
      <h2>{title}</h2>
      <p>{hint}</p>
      <button type="button" className="spw-state__cta" onClick={onAction}>{action}</button>
    </section>
  );
}
