import { useLocation, useNavigate, useParams } from "react-router-dom";
import { api } from "@/api/client";
import {
  CefrLevel,
  CefrLevelName,
  SkillType,
  type LeaderboardEntryDto,
  type PublicLearnerProgressDto,
} from "@/api/types";
import { previewLearnerProgress } from "@/components/leaderboard/previewProgress";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { UserAvatar } from "@/components/UserAvatar";
import { formatDate, formatStudyTime, skillLabel } from "@/lib/labels";
import { useAsync } from "@/lib/useAsync";
import { useLocalDay } from "@/lib/useLocalDay";
import "./LeaderboardLearnerPage.css";

type LearnerLocationState = { learner?: LeaderboardEntryDto };

const SKILLS = [
  SkillType.Speaking,
  SkillType.Listening,
  SkillType.Reading,
  SkillType.Writing,
  SkillType.Grammar,
  SkillType.Vocabulary,
];

const SKILL_ICONS: Record<SkillType, string> = {
  [SkillType.Speaking]: "record_voice_over",
  [SkillType.Listening]: "headphones",
  [SkillType.Reading]: "auto_stories",
  [SkillType.Writing]: "edit_note",
  [SkillType.Grammar]: "rule",
  [SkillType.Vocabulary]: "menu_book",
};

function weeklyAverages(progress: PublicLearnerProgressDto) {
  const buckets = new Map<string, number[]>();
  for (const point of progress.growth) {
    const values = buckets.get(point.weekEnding) ?? [];
    values.push(point.score);
    buckets.set(point.weekEnding, values);
  }
  return [...buckets.entries()]
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([week, values]) => ({
      week,
      score: Math.round(values.reduce((sum, value) => sum + value, 0) / values.length),
    }));
}

function ProgressContent({ progress }: { progress: PublicLearnerProgressDto }) {
  const levelProgress = progress.topicsTotal > 0
    ? Math.round((progress.topicsLearned / progress.topicsTotal) * 100)
    : 0;
  const scores = new Map(progress.skillScores.map((score) => [score.skill, score]));
  const growth = weeklyAverages(progress);
  const maxActivity = Math.max(1, ...progress.studyStats.last7Days.map((day) => day.seconds));

  return (
    <>
      <section className="llp-hero">
        <div className="llp-hero__identity">
          <UserAvatar
            pictureUrl={progress.pictureUrl}
            name={progress.displayName}
            alt={progress.displayName}
            className="llp-hero__avatar"
            fallbackClassName="font-duo font-extrabold"
          />
          <div className="llp-hero__copy">
            <div className="llp-hero__badges">
              <span><Icon name="leaderboard" filled />{progress.rank ? `${progress.rank}-o‘rin` : "Reyting ishtirokchisi"}</span>
              {progress.isPremium && <span className="is-pro"><Icon name="workspace_premium" filled />PRO</span>}
            </div>
            <h1>{progress.displayName}</h1>
            <p>O‘quvchining ommaviy natijalari va rivojlanish ko‘rsatkichlari</p>
          </div>
        </div>
        <div className="llp-hero__level">
          <small>Joriy daraja</small>
          <strong>{CefrLevelName[progress.overallLevel] ?? CefrLevelName[CefrLevel.A1]}</strong>
          <span>{progress.lifetimeXp.toLocaleString("uz-UZ")} XP</span>
        </div>
      </section>

      <section className="llp-metrics" aria-label="Asosiy ko‘rsatkichlar">
        <article><span><Icon name="bolt" filled /></span><div><small>Jami XP</small><strong>{progress.lifetimeXp.toLocaleString("uz-UZ")}</strong></div></article>
        <article><span><Icon name="local_fire_department" filled /></span><div><small>Joriy seriya</small><strong>{progress.gamification.currentStreak} kun</strong></div></article>
        <article><span><Icon name="schedule" /></span><div><small>Jami o‘qish</small><strong>{formatStudyTime(progress.studyStats.totalSeconds)}</strong></div></article>
        <article><span><Icon name="calendar_month" /></span><div><small>Faol kunlar</small><strong>{progress.studyStats.activeDays}</strong></div></article>
      </section>

      <div className="llp-grid">
        <section className="llp-panel llp-level">
          <header><div><span>CEFR YO‘LI</span><h2>Daraja progressi</h2></div><b>{levelProgress}%</b></header>
          <div className="llp-level__bar"><i style={{ width: `${Math.min(100, levelProgress)}%` }} /></div>
          <div className="llp-level__stats">
            <div><small>Jami mavzu</small><strong>{progress.topicsTotal}</strong></div>
            <div><small>O‘rganilgan</small><strong>{progress.topicsLearned}</strong></div>
            <div><small>O‘zlashtirilgan</small><strong>{progress.topicsMastered}</strong></div>
          </div>
        </section>

        <section className="llp-panel llp-streak">
          <header><div><span>BARQARORLIK</span><h2>Faollik seriyasi</h2></div><Icon name="local_fire_department" filled /></header>
          <div className="llp-streak__numbers">
            <div><strong>{progress.gamification.currentStreak}</strong><small>joriy seriya</small></div>
            <div><strong>{progress.gamification.longestStreak}</strong><small>eng uzun seriya</small></div>
          </div>
          <p>Bugun {progress.gamification.todayCompletedTasks}/{progress.gamification.dailyGoalTarget} ta kunlik vazifa bajarilgan.</p>
        </section>
      </div>

      <section className="llp-panel llp-skills">
        <header><div><span>6 TA KO‘NIKMA</span><h2>Ko‘nikmalar bo‘yicha natijalar</h2></div></header>
        <div className="llp-skills__grid">
          {SKILLS.map((skill) => {
            const score = scores.get(skill);
            const value = Math.round(score?.score ?? 0);
            return (
              <article key={skill}>
                <span className="llp-skills__icon"><Icon name={SKILL_ICONS[skill]} /></span>
                <div className="llp-skills__body">
                  <div><strong>{skillLabel(skill)}</strong><b>{value}</b></div>
                  <div className="llp-skills__bar"><i style={{ width: `${Math.max(0, Math.min(100, value))}%` }} /></div>
                  <small>{score?.sampleCount ?? 0} ta baholangan mashq</small>
                </div>
              </article>
            );
          })}
        </div>
      </section>

      <div className="llp-grid llp-grid--charts">
        <section className="llp-panel llp-activity">
          <header><div><span>OXIRGI 7 KUN</span><h2>O‘qish faolligi</h2></div><strong>{formatStudyTime(progress.studyStats.weekSeconds)}</strong></header>
          <div className="llp-activity__chart">
            {progress.studyStats.last7Days.map((day) => (
              <div key={day.day}>
                <span><i style={{ height: `${Math.max(6, (day.seconds / maxActivity) * 100)}%` }} /></span>
                <small>{formatDate(day.day).slice(0, 5)}</small>
              </div>
            ))}
          </div>
        </section>

        <section className="llp-panel llp-growth">
          <header><div><span>8 HAFTALIK TREND</span><h2>Umumiy o‘sish</h2></div><Icon name="trending_up" /></header>
          {growth.length > 0 ? (
            <div className="llp-growth__list">
              {growth.map((point) => (
                <div key={point.week}>
                  <small>{formatDate(point.week).slice(0, 5)}</small>
                  <span><i style={{ width: `${Math.max(0, Math.min(100, point.score))}%` }} /></span>
                  <strong>{point.score}</strong>
                </div>
              ))}
            </div>
          ) : <p className="llp-empty">O‘sish tarixi hali shakllanmagan.</p>}
        </section>
      </div>
    </>
  );
}

export function LeaderboardLearnerPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { learnerId = "" } = useParams();
  const today = useLocalDay();
  const stateLearner = (location.state as LearnerLocationState | null)?.learner;
  const preview = stateLearner ? previewLearnerProgress(stateLearner, today) : null;
  const { data, loading, error } = useAsync(
    () => preview ? Promise.resolve(preview) : api.learning.publicProgress(learnerId, today),
    [learnerId, today],
  );

  return (
    <main className="llp-page">
      <button type="button" className="llp-back" onClick={() => navigate("/leaderboard")}>
        <Icon name="arrow_back" />
        Reytingga qaytish
      </button>
      {loading && <ModulePageLoader icon="monitoring" accent="blue" embedded />}
      {Boolean(error) && !data && (
        <section className="llp-state">
          <Icon name="person_off" />
          <h1>O‘quvchi progressi topilmadi</h1>
          <p>Ma’lumotlarni yuklab bo‘lmadi yoki bu profil mavjud emas.</p>
          <button type="button" onClick={() => navigate("/leaderboard")}>Reytingga qaytish</button>
        </section>
      )}
      {data && <ProgressContent progress={data} />}
    </main>
  );
}
