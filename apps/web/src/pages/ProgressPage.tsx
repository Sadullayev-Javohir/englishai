import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import { useAuth } from "@/app/auth";
import { Icon } from "@/components/ui/Icon";
import { publishAssistantContext } from "@/components/assistantContext";
import { SkillType } from "@/api/types";
import type { DailyStudyBucketDto, ProgressDashboardDto, SkillScoreDto } from "@/api/types";
import { formatCompactNumber } from "@/lib/formatCompactNumber";
import { skillLabel } from "@/lib/labels";
import { navigateFromProgress } from "@/lib/routeReturn";
import { useAsync } from "@/lib/useAsync";
import { useLocalDay } from "@/lib/useLocalDay";
import "./ProgressPage.css";

const PEN_SKILLS: SkillType[] = [
  SkillType.Vocabulary,
  SkillType.Grammar,
  SkillType.Reading,
  SkillType.Writing,
  SkillType.Speaking,
  SkillType.Listening,
];

const SKILL_ROUTE: Record<SkillType, string> = {
  [SkillType.Speaking]: "/app/speaking",
  [SkillType.Listening]: "/listening",
  [SkillType.Reading]: "/reading",
  [SkillType.Writing]: "/writing",
  [SkillType.Grammar]: "/app/grammar",
  [SkillType.Vocabulary]: "/app/vocabulary/topics",
};

const DAY_LABELS = ["Du", "Se", "Ch", "Pa", "Ju", "Sh", "Ya"];
type Period = "week" | "month" | "all";

function weakestSkill(scores: SkillScoreDto[]) {
  if (scores.length === 0) return null;
  return [...scores].sort((first, second) => first.score - second.score)[0];
}

function weeklyBars(days: DailyStudyBucketDto[]) {
  const values = days.slice(-7);
  const max = Math.max(...values.map((item) => item.seconds), 1);
  return DAY_LABELS.map((label, index) => {
    const seconds = values[index]?.seconds ?? 0;
    return { label, seconds, height: seconds === 0 ? 0 : Math.max(8, Math.round((seconds / max) * 100)) };
  });
}

function periodCopy(period: Period, data: ProgressDashboardDto) {
  const vocabulary = data.progressInsight.snapshot.vocabulary;
  if (period === "month") return { xp: "Bu oygi umumiy XP", words: `+${vocabulary.addedLast30Days} bu oy` };
  if (period === "all") return { xp: "Barcha vaqt natijasi", words: `${vocabulary.total} jami so‘z` };
  return { xp: "Barcha vaqt natijasi", words: `+${vocabulary.addedLast7Days} shu hafta` };
}

function ProgressMetrics({ data, lifetimeXp, period }: { data: ProgressDashboardDto; lifetimeXp: number; period: Period }) {
  const vocabulary = data.progressInsight.snapshot.vocabulary;
  const gamification = data.progressInsight.snapshot.gamification;
  const copy = periodCopy(period, data);
  return (
    <section className="progress-pen__metrics" aria-label="Asosiy ko‘rsatkichlar">
      <article className="progress-pen__metric progress-pen__metric--xp"><small>JAMI XP</small><strong>{formatCompactNumber(lifetimeXp)}</strong><span>{copy.xp}</span></article>
      <article className="progress-pen__metric progress-pen__metric--words"><small>O‘RGANILGAN SO‘Z</small><strong>{formatCompactNumber(vocabulary.total)}</strong><span>{copy.words}</span></article>
      <article className="progress-pen__metric progress-pen__metric--streak"><small>KUNLIK SERIYA</small><strong>{gamification.currentStreak} kun</strong><span>Eng yaxshi: {gamification.longestStreak} kun</span></article>
    </section>
  );
}

function WeeklyActivity({ data }: { data: ProgressDashboardDto }) {
  const bars = weeklyBars(data.studyStats.last7Days);
  const totalMinutes = Math.round(data.studyStats.weekSeconds / 60);
  return (
    <section className="progress-pen__panel progress-pen__activity" aria-labelledby="progress-weekly-activity">
      <header className="progress-pen__panel-head"><h2 id="progress-weekly-activity">Haftalik faollik</h2><span>7 kun</span></header>
      <div className="progress-pen__activity-total"><strong>{totalMinutes}</strong><span>daqiqa mashq</span></div>
      <div className="progress-pen__bar-chart" role="img" aria-label={`Oxirgi 7 kun ichida ${totalMinutes} daqiqa mashq qildingiz.`}>
        {bars.map((bar) => <div className="progress-pen__bar-day" key={`${bar.label}-${bar.seconds}`}><i aria-hidden="true" style={{ height: `${bar.height}%` }} /><span>{bar.label}</span></div>)}
      </div>
      <p className="progress-pen__positive">Haftalik odatingizni saqlab qoling.</p>
    </section>
  );
}

function SkillsBreakdown({ scores, onNavigate }: { scores: SkillScoreDto[]; onNavigate: (route: string) => void }) {
  const scoreBySkill = new Map(scores.map((item) => [item.skill, Math.max(0, Math.min(100, Math.round(item.score)))]));
  return (
    <section className="progress-pen__panel progress-pen__skills" aria-labelledby="progress-skills">
      <header className="progress-pen__panel-head"><h2 id="progress-skills">Ko‘nikmalar kesimida</h2></header>
      <div className="progress-pen__skills-list">
        {PEN_SKILLS.map((skill) => {
          const score = scoreBySkill.get(skill) ?? 0;
          return <button className="progress-pen__skill" key={skill} type="button" onClick={() => onNavigate(SKILL_ROUTE[skill])}><span>{skillLabel(skill)}</span><b>{score}%</b><i aria-hidden="true"><em style={{ width: `${score}%` }} /></i></button>;
        })}
      </div>
    </section>
  );
}

function AiRecommendation({ data, focus, onNavigate }: { data: ProgressDashboardDto; focus: SkillScoreDto | null; onNavigate: (route: string) => void }) {
  const targetRoute = focus ? SKILL_ROUTE[focus.skill] : data.progressInsight.insight.targetRoute;
  const targetLabel = focus ? skillLabel(focus.skill) : "Bugungi";
  const copy = data.progressInsight.insight.overallCode === "no_data"
    ? "Birinchi mashqdan so‘ng rivojlanishingiz bu yerda aniqroq ko‘rinadi."
    : "Sizning keyingi mashqingiz rivojlanish ritmini saqlashga yordam beradi.";
  return (
    <section className="progress-pen__recommendation" aria-labelledby="progress-recommendation">
      <div className="progress-pen__recommendation-title"><span><Icon name="auto_awesome" /></span><h2 id="progress-recommendation">{targetLabel}ga vaqt ajratamizmi?</h2></div>
      <p>{copy}</p>
      <button type="button" onClick={() => onNavigate(targetRoute)}><Icon name="arrow_forward" />{targetLabel} mashqini boshlash</button>
    </section>
  );
}

function Achievements({ data, onNavigate }: { data: ProgressDashboardDto; onNavigate: (route: string) => void }) {
  const gamification = data.progressInsight.snapshot.gamification;
  const vocabulary = data.progressInsight.snapshot.vocabulary;
  return (
    <section className="progress-pen__achievements" aria-labelledby="progress-achievements">
      <h2 id="progress-achievements">So‘nggi yutuqlar</h2>
      <div>
        <button type="button" className="progress-pen__achievement" onClick={() => onNavigate("/home")}><Icon name="local_fire_department" /><span><strong>Bir haftalik odat</strong><small>{gamification.currentStreak} kun ketma-ket o‘rganish</small></span></button>
        <button type="button" className="progress-pen__achievement" onClick={() => onNavigate("/app/vocabulary/topics")}><Icon name="menu_book" /><span><strong>{formatCompactNumber(vocabulary.total)} ta yangi so‘z</strong><small>So‘z boyligingiz kengaymoqda</small></span></button>
      </div>
    </section>
  );
}

function ProgressContent({ data, lifetimeXp, onNavigate }: { data: ProgressDashboardDto; lifetimeXp: number; onNavigate: (route: string) => void }) {
  const [period, setPeriod] = useState<Period>("week");
  const focus = useMemo(() => weakestSkill(data.overview.skillScores), [data.overview.skillScores]);
  return (
    <>
      <div className="progress-pen__heading"><span>NATIJALAR</span><h1>Mehnatingiz ko‘rinib turibdi.</h1><p>O‘sishingizni kuzating va keyingi maqsadni belgilang.</p></div>
      <div className="progress-pen__periods" aria-label="Natijalar davri">
        {([ ["week", "Shu hafta"], ["month", "Shu oy"], ["all", "Barchasi"] ] as const).map(([value, label]) => <button key={value} type="button" aria-pressed={period === value} onClick={() => setPeriod(value)}>{label}</button>)}
      </div>
      <ProgressMetrics data={data} lifetimeXp={lifetimeXp} period={period} />
      <div className="progress-pen__chart-grid"><WeeklyActivity data={data} /><SkillsBreakdown scores={data.overview.skillScores} onNavigate={onNavigate} /></div>
      <AiRecommendation data={data} focus={focus} onNavigate={onNavigate} />
      <Achievements data={data} onNavigate={onNavigate} />
    </>
  );
}

export function ProgressPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const learnerId = user?.id ?? "";
  const localDay = useLocalDay();
  const navigateToLearning = (route: string) => navigateFromProgress(navigate, route);
  const request = useAsync(() => api.learning.progressDashboard(learnerId, localDay), [learnerId, localDay], learnerId.length > 0);
  const points = useAsync(() => api.gamification.points(learnerId), [learnerId], learnerId.length > 0);
  const data = request.data;

  useEffect(() => {
    const focus = data ? weakestSkill(data.overview.skillScores) : null;
    publishAssistantContext({
      area: "progress", resourceId: learnerId || undefined, title: "Natijalar va rivojlanish",
      context: data ? `Daraja: ${data.overview.overallLevel}. Skill natijalari: ${data.overview.skillScores.map((item) => `${skillLabel(item.skill)} ${Math.round(item.score)}%`).join(" | ")}` : "",
      focusText: focus ? `Eng zaif skill: ${skillLabel(focus.skill)} ${Math.round(focus.score)}%` : "",
      route: "/progress", stage: "dashboard",
    });
  }, [data, learnerId]);

  return (
    <div className="progress-pen" data-testid="progress-page">
      <div className="progress-pen__mobile-actions"><button type="button" onClick={() => navigate("/app/vocabulary/saved")}><Icon name="bookmark_border" />Saqlangan</button></div>
      {request.error ? <section className="progress-pen__state" role="alert"><Icon name="cloud_off" /><h1>Natijalarni yuklab bo‘lmadi</h1><p>Ulanishni tekshirib, yana urinib ko‘ring.</p><button type="button" onClick={request.reload}><Icon name="refresh" />Qayta urinish</button></section>
        : data ? <ProgressContent data={data} lifetimeXp={points.data?.lifetimeXp ?? 0} onNavigate={navigateToLearning} />
          : <section className="progress-pen__state" aria-live="polite"><Icon name="progress_activity" className="animate-spin" /><p>Natijalar yuklanmoqda…</p></section>}
    </div>
  );
}
