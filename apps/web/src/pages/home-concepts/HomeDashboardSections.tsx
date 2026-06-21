import { Fragment, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  ArrowRight, ArrowUpRight, BookA, BookmarkCheck, BookmarkPlus, Check,
  ChevronRight, Flame, LoaderCircle, Mic, Play, Repeat2, Timer, Trophy, Volume2,
} from "lucide-react";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { cefrShort, formatDuration } from "@/lib/labels";
import { speakEnglishWord } from "@/lib/audio";
import { REVIEW_MILESTONE_DAYS } from "@/lib/srsStage";
import { useLocalDay } from "@/lib/useLocalDay";
import type { HomeAction, HomeViewModel } from "./homeViewModel";
import { HOME_SKILLS, HOME_VIDEOS, HOME_WORD, homeEyebrow, homeFocus, homePrimaryButton } from "./homeDashboardContent";

interface LessonProps {
  model: HomeViewModel;
  onAction: (action: HomeAction) => void;
}

export function DailyLesson({ model, onAction }: LessonProps) {
  const fallback = Boolean(model.nextAction.fallbackKind);
  const title = fallback ? model.nextAction.title : model.activeTopicTitle || model.nextAction.title;
  const nextSkill = HOME_SKILLS.find(skill => skill.key === model.nextAction.skillKey);
  return (
    <section data-testid="home-daily-lesson" aria-label="Bugungi dars" className="flex min-w-0 flex-col gap-4 rounded-[26px] bg-ea-primary-soft p-5 min-[1200px]:h-[292px] min-[1200px]:p-6">
      <div className="flex min-w-0 flex-1 items-center gap-3 min-[1200px]:gap-4">
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          <span className={homeEyebrow}>{fallback ? "KEYINGI QADAM" : "BUGUNGI DARS"}</span>
          <h2 className="break-words font-duo text-[28px] font-black leading-[1.1] tracking-[-0.5px] min-[1200px]:text-[38px]">{title}</h2>
          <p className="text-[11px] font-semibold leading-[1.45] text-[var(--home-muted)] min-[1200px]:text-[13px]">
            {model.activeTopicLevel ? `${cefrShort(model.activeTopicLevel)} · ` : ""}
            {nextSkill?.label ?? model.nextAction.text}
          </p>
        </div>
        <img src={fallback ? "/assets/play/words-world.svg" : "/assets/play/home-photos/my-mother.jpg"} alt="" width={270} height={180} className={`h-24 w-[min(46%,144px)] shrink-0 min-[1200px]:h-[180px] min-[1200px]:w-[270px] ${fallback ? "object-contain" : "rounded-[16px] object-cover"}`} />
      </div>
      <div className="flex flex-col gap-3 min-[1200px]:flex-row min-[1200px]:items-center min-[1200px]:gap-5">
        <button type="button" disabled={model.nextAction.locked} onClick={() => onAction(model.nextAction)} className={`${homePrimaryButton} order-2 h-[45px] shrink-0 px-4 min-[1200px]:order-1`}>
          {model.completed ? "Davom etish" : "Darsni boshlash"}<Play size={18} aria-hidden />
        </button>
        <ol aria-label="Dars ko‘nikmalari" className="order-1 flex min-w-0 flex-1 items-center min-[1200px]:order-2">
          {HOME_SKILLS.map((skill, index) => {
            const action = model.daily.find(item => (item.skillKey ?? item.key.toLowerCase()) === skill.key);
            const previous = HOME_SKILLS[index - 1];
            const previousDone = previous && model.daily.some(item => (item.skillKey ?? item.key.toLowerCase()) === previous.key && item.done);
            return (
              <Fragment key={skill.key}>
                {index > 0 && <li aria-hidden className={`flex min-w-0 flex-1 justify-center ${previousDone && action?.done ? "text-ea-primary" : "text-[var(--home-step-arrow)]"}`}><ArrowRight className="h-[13px] w-[13px] min-[1200px]:h-5 min-[1200px]:w-5" /></li>}
                <li>
                  <button type="button" aria-label={`${skill.label} — ${action?.done ? "o‘rganilgan" : "o‘rganilmagan"}`} aria-current={action?.current ? "step" : undefined} data-completed={Boolean(action?.done)} disabled={!action || action.locked} onClick={() => action && onAction(action)} className={`flex h-9 w-9 items-center justify-center rounded-[11px] border min-[1200px]:h-11 min-[1200px]:w-11 min-[1200px]:rounded-[14px] disabled:cursor-not-allowed ${homeFocus} ${action?.done ? `${skill.done} text-ea-on-primary` : "border-[var(--home-step-border)] bg-[var(--home-step-bg)] text-[var(--home-step-ink)]"}`}>
                    <skill.icon aria-hidden className="h-[19px] w-[19px] min-[1200px]:h-[23px] min-[1200px]:w-[23px]" />
                  </button>
                </li>
              </Fragment>
            );
          })}
        </ol>
      </div>
    </section>
  );
}

function StudyDays({ values }: { values: number[] }) {
  const today = useLocalDay();
  const dayLabels = ["Yak", "Dush", "Sesh", "Chor", "Pay", "Juma", "Shan"];
  return <ol aria-label="So‘nggi 7 kundagi faollik" className="flex gap-1">
    {values.map((value, index) => {
      const day = new Date(Date.parse(`${today}T00:00:00Z`) - (values.length - index - 1) * 86400000).toISOString().slice(0, 10);
      return <li key={day} title={`${day} · ${value} daqiqa`} aria-label={`${day}: ${value > 0 ? "mashq bajarildi" : "mashq bajarilmadi"}`} className="flex flex-col items-center min-[1200px]:h-[50px] min-[1200px]:min-w-0 min-[1200px]:flex-1 min-[1200px]:gap-[6px]">
        <span aria-hidden className="hidden text-[9px] leading-[13px] text-[var(--home-muted)] min-[1200px]:block">{dayLabels[new Date(`${day}T00:00:00Z`).getUTCDay()]}</span>
        <span className={`flex h-[15px] w-[15px] items-center justify-center rounded-[8px] text-[var(--home-green)] min-[1200px]:h-[26px] min-[1200px]:w-[26px] ${value > 0 ? "bg-[var(--home-day-active)]" : "bg-ea-surface"}`}>{value > 0 && <Check className="h-[9px] w-[9px] min-[1200px]:h-[13px] min-[1200px]:w-[13px]" aria-hidden />}</span>
      </li>;
    })}
  </ol>;
}

function GoalRing({ completed, target }: { completed: number; target: number }) {
  const percent = target > 0 ? Math.min(100, Math.max(0, Math.round(completed / target * 100))) : 0;
  return <span role="progressbar" aria-label="Kunlik maqsad" aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100} className="relative grid h-11 w-11 shrink-0 place-items-center min-[1200px]:h-14 min-[1200px]:w-14">
    <svg viewBox="0 0 56 56" className="absolute inset-0 h-full w-full -rotate-90" aria-hidden>
      <circle cx="28" cy="28" r="24.9" fill="none" stroke="var(--ea-border)" strokeWidth="6.2" />
      <circle cx="28" cy="28" r="24.9" fill="none" stroke="var(--ea-primary)" strokeWidth="6.2" pathLength="100" strokeDasharray={`${percent} 100`} />
    </svg>
    <span className="text-[11px] font-extrabold text-ea-primary">{percent}%</span>
  </span>;
}

export function DailyHabit({ model }: { model: HomeViewModel }) {
  return <section aria-label="Kundalik faollik" className="grid grid-cols-2 gap-3 min-[1200px]:flex min-[1200px]:h-[292px] min-[1200px]:flex-col min-[1200px]:justify-center min-[1200px]:gap-[18px] min-[1200px]:rounded-[26px] min-[1200px]:bg-[var(--home-blue)] min-[1200px]:p-[22px]">
    <div className="flex h-28 min-w-0 flex-col justify-center gap-[14px] rounded-[20px] bg-ea-orange-50 p-[14px] min-[1200px]:h-auto min-[1200px]:gap-[18px] min-[1200px]:bg-transparent min-[1200px]:p-0">
      <div className="flex items-center gap-3">
        <span className="flex shrink-0 items-center justify-center text-[var(--home-flame)] min-[1200px]:h-11 min-[1200px]:w-11 min-[1200px]:rounded-[14px] min-[1200px]:bg-ea-orange-50"><Flame size={24} aria-hidden /></span>
        <div className="min-w-0"><strong className="block font-duo text-2xl font-black leading-[1.1] tracking-[-0.5px] min-[1200px]:text-[30px]">{model.currentStreak} kun</strong><span className="mt-0.5 hidden text-[11px] leading-[1.45] text-[var(--home-muted)] min-[1200px]:block">Ketma-ket o‘rganish</span></div>
      </div>
      <StudyDays values={model.weeklyProgress} />
    </div>
    <div className="hidden h-px w-full bg-[var(--home-divider)] min-[1200px]:block" />
    <div className="flex h-28 min-w-0 flex-col justify-center gap-2 rounded-[20px] bg-ea-green-50 p-[14px] min-[1200px]:h-auto min-[1200px]:flex-row min-[1200px]:items-center min-[1200px]:justify-start min-[1200px]:gap-3 min-[1200px]:bg-transparent min-[1200px]:p-0">
      <span className="text-[9px] font-extrabold text-ea-primary min-[1200px]:hidden">KUNLIK MAQSAD</span>
      <div className="flex items-center gap-[14px] min-[1200px]:contents">
        <GoalRing completed={model.todayCompletedSkills} target={HOME_SKILLS.length} />
        <div className="min-w-0"><span className="mb-[5px] hidden text-[10px] font-extrabold leading-[1.45] text-[var(--home-green)] min-[1200px]:block">BUGUNGI MAQSAD</span><strong className="block whitespace-nowrap font-duo text-2xl font-black leading-[1.1] tracking-[-0.5px]">{model.todayCompletedSkills}/{HOME_SKILLS.length}<span className="hidden min-[1200px]:inline"> skill</span></strong></div>
      </div>
    </div>
  </section>;
}

export function HomeStats({ model }: { model: HomeViewModel }) {
  const navigate = useNavigate();
  const hours = Number((model.weeklyMinutes / 60).toFixed(1));
  return <section aria-label="O‘qish statistikasi" className="grid h-[66px] grid-cols-3 gap-3 px-2 py-3 min-[1200px]:h-20 min-[1200px]:w-[410px] min-[1200px]:gap-[10px] min-[1200px]:p-0">
    {[
      { icon: BookA, value: model.savedCount, label: "so‘z", description: `${model.savedCount} ta saqlangan so‘z`, to: "/app/vocabulary/saved" },
      { icon: Timer, value: hours, label: "soat", description: `Haftalik vaqt: ${hours} soat`, to: "/progress" },
      { icon: Trophy, value: model.leaderboardRank ? `#${model.leaderboardRank}` : "—", label: "reyting", description: model.leaderboardRank ? `Reyting: ${model.leaderboardRank}-o‘rin` : "Reyting hali mavjud emas", to: "/leaderboard" },
    ].map(item => <button type="button" key={item.label} aria-label={item.description} onClick={() => navigate(item.to)} className={`flex min-w-0 items-center gap-3 text-left min-[1200px]:gap-2 min-[1200px]:rounded-[16px] min-[1200px]:border min-[1200px]:border-ea-border min-[1200px]:bg-ea-surface min-[1200px]:px-[11px] min-[1200px]:hover:border-ea-primary ${homeFocus}`}>
      <item.icon size={18} className="shrink-0 text-ea-primary" aria-hidden /><span className="min-w-0 flex-1"><strong className="block font-duo text-[22px] font-black leading-[1.1] tracking-[-0.5px]">{item.value}</strong><span className="mt-0.5 block text-[10px] font-semibold leading-[1.45] text-[var(--home-muted)]">{item.label}</span></span>
    </button>)}
  </section>;
}

export function SkillLauncher({ model, onAction }: LessonProps) {
  const navigate = useNavigate();
  return <section className="flex min-w-0 flex-col gap-[14px]" aria-labelledby="home-skills-heading">
    <h2 id="home-skills-heading" className="font-duo text-[22px] font-black leading-[1.1] tracking-[-0.5px] min-[1200px]:text-[26px]">Mashq tanlang</h2>
    <div className="grid grid-cols-3 gap-x-[10px] gap-y-[14px] min-[1200px]:grid-cols-6 min-[1200px]:gap-3">
      {HOME_SKILLS.map(skill => {
        const action = model.modules.find(item => item.key.toLowerCase() === skill.key);
        return <button key={skill.key} type="button" disabled={action?.locked} onClick={() => action ? onAction(action) : navigate(skill.to)} className={`flex min-w-0 flex-col items-center gap-[9px] rounded-[18px] px-[6px] py-[14px] disabled:cursor-not-allowed disabled:opacity-50 ${homeFocus} ${skill.key === "vocabulary" ? "border-[1.5px] border-ea-primary bg-[var(--home-selected)] text-ea-primary" : "border border-ea-border bg-ea-surface hover:border-ea-primary"}`}>
          <span className={`flex h-10 w-10 items-center justify-center rounded-[13px] text-ea-primary min-[1200px]:h-[46px] min-[1200px]:w-[46px] min-[1200px]:rounded-[15px] ${skill.tile}`}><skill.icon className="h-[21px] w-[21px] min-[1200px]:h-[24px] min-[1200px]:w-[24px]" aria-hidden /></span>
          <span className="text-[11px] font-extrabold leading-[1.45] min-[1200px]:text-[13px]">{skill.label}</span>
        </button>;
      })}
    </div>
  </section>;
}

export function DueReviewCard({ model }: { model: HomeViewModel }) {
  const navigate = useNavigate();
  const hasDue = model.dueSavedCount > 0;
  const topic = model.dueTopicTitle || "Saqlangan so‘zlar";
  return <button type="button" onClick={() => navigate("/app/vocabulary/saved")} aria-label={`Takrorlash: ${model.dueSavedCount} ta so‘z tayyor`} className={`flex w-full min-w-0 items-center gap-3 rounded-[20px] bg-ea-green-50 p-4 text-left min-[1200px]:flex-col min-[1200px]:items-stretch min-[1200px]:gap-4 min-[1200px]:rounded-[22px] min-[1200px]:p-5 ${homeFocus}`}>
    <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-[14px] bg-ea-surface text-ea-primary min-[1200px]:hidden"><Repeat2 size={23} aria-hidden /></span>
    <span className="min-w-0 flex-1 min-[1200px]:hidden"><strong className="block font-duo text-[19px] font-black leading-[1.1] tracking-[-0.5px]">{hasDue ? `${model.dueSavedCount} ta so‘z tayyor` : "So‘zlaringizni takrorlang"}</strong><span className="mt-1 block text-[11px] font-semibold leading-[1.45] text-[var(--home-green)]">{topic}{hasDue && model.dueReviewDay ? ` · ${model.dueReviewDay}-kun` : ""}</span></span>
    <ArrowRight size={20} aria-hidden className="shrink-0 text-[var(--home-green)] min-[1200px]:hidden" />
    <span className="hidden items-center gap-[9px] min-[1200px]:flex"><Repeat2 size={22} className="text-ea-primary" aria-hidden /><strong className="min-w-0 flex-1 font-duo text-[21px] font-black leading-[1.1] tracking-[-0.5px]">Takrorlash</strong><span className="rounded-[18px] bg-ea-surface px-[9px] py-[6px] text-[10px] font-extrabold leading-[1.45] text-[var(--home-green)]">Bugun</span></span>
    <span className="hidden items-center gap-3 min-[1200px]:flex"><strong className="min-w-0 flex-1 font-duo text-[34px] font-black leading-[1.1] tracking-[-0.5px]">{model.dueSavedCount}</strong><span className="min-w-0 flex-1 text-[12px] font-semibold leading-[1.45] text-[var(--home-green)]">{hasDue ? `so‘z · ${topic}` : "Hozircha takrorlash yo‘q"}</span></span>
    <span aria-hidden className="hidden items-center gap-2 min-[1200px]:flex">{REVIEW_MILESTONE_DAYS.map(day => <span key={day} className={`grid h-[30px] w-[30px] place-items-center rounded-[10px] text-[11px] font-extrabold ${hasDue && day === model.dueReviewDay ? "bg-ea-primary text-ea-on-primary" : "bg-ea-surface text-[var(--home-muted)]"}`}>{day}</span>)}<ArrowRight size={20} className="ml-auto text-ea-primary" /></span>
  </button>;
}

export function VideoRecommendations() {
  const navigate = useNavigate();
  const [opening, setOpening] = useState<string | null>(null);
  const openingRef = useRef(false);
  const mounted = useRef(true);
  const [error, setError] = useState(false);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  async function openVideo(video: typeof HOME_VIDEOS[number]) {
    if (openingRef.current) return;
    openingRef.current = true;
    setOpening(video.youTubeVideoId);
    setError(false);
    try {
      const lesson = await api.video.open(video);
      if (mounted.current) navigate(`/video/${lesson.id}/play`);
    } catch {
      if (mounted.current) setError(true);
    } finally {
      openingRef.current = false;
      if (mounted.current) setOpening(null);
    }
  }
  return <section aria-labelledby="home-videos-heading" className="flex min-w-0 flex-col gap-[14px]">
    <header className="flex items-center gap-3"><h2 id="home-videos-heading" className="min-w-0 flex-1 font-duo text-[22px] font-black leading-[1.1] tracking-[-0.5px] min-[1200px]:text-[26px]">Video darslar</h2><button type="button" onClick={() => navigate("/video")} className={`text-[11px] font-extrabold leading-[1.45] text-ea-primary ${homeFocus}`}>Barchasi →</button></header>
    <div className="grid gap-[14px] min-[1200px]:grid-cols-2 min-[1200px]:gap-4">
      {HOME_VIDEOS.map((video, index) => <button type="button" key={video.youTubeVideoId} onClick={() => void openVideo(video)} disabled={opening !== null} aria-label={`${video.title} videosini ochish`} aria-busy={opening === video.youTubeVideoId} className={`group min-w-0 text-left disabled:cursor-wait ${homeFocus} ${index === 1 ? "flex items-center gap-3 rounded-[16px] border border-ea-border bg-ea-surface p-[10px] min-[1200px]:block min-[1200px]:border-0 min-[1200px]:bg-transparent min-[1200px]:p-0" : "block rounded-[16px]"}`}>
        <span className={`relative block overflow-hidden bg-ea-primary-soft ${index === 1 ? "h-16 w-28 shrink-0 rounded-[10px] min-[1200px]:aspect-video min-[1200px]:h-auto min-[1200px]:w-full min-[1200px]:rounded-[16px]" : "aspect-video w-full rounded-[16px]"}`}>
          <img src={video.thumbnail} alt="" width={640} height={360} loading="lazy" className="absolute inset-0 h-full w-full object-cover" />
          {index === 0 && <span className="absolute left-3 top-3 rounded-[18px] bg-ea-surface px-[9px] py-[6px] text-[10px] font-extrabold leading-[1.45] text-ea-primary min-[1200px]:hidden">{cefrShort(video.level)}</span>}
          <span className={`${index === 1 ? "hidden min-[1200px]:flex" : "flex"} absolute bottom-3 left-3 h-8 w-8 items-center justify-center rounded-[10px] bg-ea-surface text-ea-primary`}>{opening === video.youTubeVideoId ? <LoaderCircle size={17} className="animate-spin motion-reduce:animate-none" aria-hidden /> : <Play size={17} aria-hidden />}</span>
          <span className={`absolute rounded-[6px] bg-[var(--home-video-duration)] px-[6px] py-1 text-[10px] font-extrabold leading-[1.45] text-ea-on-primary ${index === 1 ? "bottom-[6px] right-[6px] min-[1200px]:bottom-3 min-[1200px]:right-3" : "bottom-3 right-3"}`}>{formatDuration(video.durationSeconds)}</span>
        </span>
        <span className={`flex min-w-0 flex-1 flex-col ${index === 1 ? "gap-[5px] min-[1200px]:gap-[6px] min-[1200px]:px-1 min-[1200px]:pb-0.5 min-[1200px]:pt-3" : "gap-[6px] px-1 pb-0.5 pt-3"}`}>
          <strong className={`font-duo font-black leading-[1.1] tracking-[-0.5px] ${index === 1 ? "text-[17px] min-[1200px]:text-[22px]" : "text-[22px]"}`}>{video.title}</strong>
          <span className="flex items-center gap-3"><span className={`min-w-0 flex-1 leading-[1.45] text-[var(--home-muted)] ${index === 1 ? "text-[10px] min-[1200px]:text-[11px]" : "text-[11px]"}`}>{video.channel}</span><ArrowUpRight size={17} aria-hidden className={`text-ea-primary ${index === 1 ? "hidden min-[1200px]:block" : ""}`} /></span>
        </span>
        {index === 1 && <ChevronRight size={18} className="shrink-0 text-[var(--home-muted)] min-[1200px]:hidden" aria-hidden />}
      </button>)}
    </div>
    {error && <p role="alert" className="text-[12px] text-ea-muted">Video ochilmadi. Qayta urinib ko‘ring yoki <button type="button" onClick={() => navigate("/video")} className={`font-bold text-ea-primary underline ${homeFocus}`}>video katalogini oching</button>.</p>}
  </section>;
}

export function DailyWord({ alreadySaved = false, onSaved }: { alreadySaved?: boolean; onSaved?: () => void }) {
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);
  const [message, setMessage] = useState("");
  const learnerId = getLearnerId();
  const isSaved = alreadySaved || saved;
  async function saveWord() {
    if (isSaved || savingRef.current) return;
    savingRef.current = true;
    setSaving(true);
    setMessage("");
    try {
      await api.vocabulary.learn(learnerId, HOME_WORD.word, HOME_WORD.translation);
      setSaved(true);
      setMessage("So‘z saqlandi.");
      onSaved?.();
    } catch {
      setMessage("So‘z saqlanmadi. Qayta urinib ko‘ring.");
    } finally {
      savingRef.current = false;
      setSaving(false);
    }
  }
  return <section aria-label="Kun so‘zi" className="flex min-w-0 flex-col gap-[14px] rounded-[22px] border border-ea-border bg-ea-surface p-[18px] min-[1200px]:p-5">
    <header className="flex items-center gap-3"><h2 className={`${homeEyebrow} flex-1`}>KUN SO‘ZI</h2><button type="button" onClick={() => void saveWord()} disabled={isSaved || saving} aria-label={isSaved ? "grateful saqlangan" : "grateful so‘zini saqlash"} className={`relative text-ea-primary after:absolute after:-inset-3 ${homeFocus}`}>{saving ? <LoaderCircle size={18} className="animate-spin motion-reduce:animate-none" aria-hidden /> : isSaved ? <BookmarkCheck size={18} aria-hidden /> : <BookmarkPlus size={18} aria-hidden />}</button></header>
    <div className="flex items-center gap-3"><div className="flex min-w-0 flex-1 flex-col gap-[5px]"><strong className="font-duo text-[26px] font-black leading-[1.1] tracking-[-0.5px] min-[1200px]:text-[30px]">{HOME_WORD.word}</strong><span className="text-[12px] leading-[1.45] text-[var(--home-muted)]">{HOME_WORD.translation}</span></div><button type="button" aria-label="grateful talaffuzini tinglash" onClick={() => { if (!speakEnglishWord(HOME_WORD.word)) setMessage("Qurilmada inglizcha ovoz mavjud emas."); }} className={`grid h-10 w-10 shrink-0 place-items-center rounded-[13px] bg-ea-primary-soft text-ea-primary ${homeFocus}`}><Volume2 size={21} aria-hidden /></button></div>
    {message && <p role="status" className="text-[11px] leading-[1.5] text-[var(--home-muted)]">{message}</p>}
  </section>;
}

export function HomeDiscoveries() {
  const navigate = useNavigate();
  return <section aria-label="Kitoblar va suhbat" className="grid grid-cols-2 gap-3 min-[1200px]:gap-5">
    <button type="button" onClick={() => navigate("/books")} aria-label="The Island Secret — O‘qish" className={`flex min-h-[223px] min-w-0 flex-col items-center justify-between gap-[10px] rounded-[22px] bg-ea-primary-soft p-4 text-left min-[1200px]:h-48 min-[1200px]:min-h-0 min-[1200px]:flex-row min-[1200px]:gap-3 min-[1200px]:rounded-[24px] min-[1200px]:px-6 min-[1200px]:py-5 ${homeFocus}`}>
      <span className="w-full font-duo text-xl font-black leading-[1.1] tracking-[-0.5px] min-[1200px]:hidden">Kitoblar</span>
      <span className="hidden min-w-0 flex-1 flex-col gap-3 min-[1200px]:flex"><span className={homeEyebrow}>KUTUBXONA</span><strong className="font-duo text-[27px] font-black leading-[1.1] tracking-[-0.5px]">The Island Secret</strong><span className="flex h-[45px] w-fit items-center gap-[9px] rounded-[14px] border border-ea-border bg-ea-surface px-4 text-[13px] font-extrabold text-ea-primary">O‘qish<ArrowRight size={18} aria-hidden /></span></span>
      <img src="/assets/play/home-photos/the-island-secret.jpg" alt="" width={112} height={144} loading="lazy" className="h-[108px] w-[84px] shrink-0 rounded-xl object-cover min-[1200px]:h-36 min-[1200px]:w-28" />
      <span className="w-full text-[10px] font-bold leading-[1.45] min-[1200px]:hidden">The Island Secret</span>
      <span className="flex w-full items-center justify-between text-[11px] font-extrabold text-ea-primary min-[1200px]:hidden">O‘qish<ArrowUpRight size={16} aria-hidden /></span>
    </button>
    <button type="button" onClick={() => navigate("/app/speaking")} aria-label="1 daqiqa suhbat — Boshlash" className={`flex min-h-[223px] min-w-0 flex-col items-center justify-between gap-[10px] rounded-[22px] bg-ea-orange-50 p-4 text-left min-[1200px]:h-48 min-[1200px]:min-h-0 min-[1200px]:flex-row min-[1200px]:gap-3 min-[1200px]:rounded-[24px] min-[1200px]:px-6 min-[1200px]:py-5 ${homeFocus}`}>
      <span className="w-full font-duo text-xl font-black leading-[1.1] tracking-[-0.5px] min-[1200px]:hidden">Speaking</span>
      <span className="hidden min-w-0 flex-1 flex-col gap-3 min-[1200px]:flex"><span className={homeEyebrow}>SPEAKING CHALLENGE</span><strong className="font-duo text-[27px] font-black leading-[1.1] tracking-[-0.5px]">1 daqiqa suhbat</strong><span className="flex h-[45px] w-fit items-center gap-[9px] rounded-[14px] border border-ea-border bg-ea-surface px-4 text-[13px] font-extrabold text-ea-primary">Boshlash<Mic size={18} aria-hidden /></span></span>
      <img src="/assets/play/parrot.svg" alt="" width={180} height={150} loading="lazy" className="h-[90px] w-[108px] shrink-0 object-contain min-[1200px]:h-[150px] min-[1200px]:w-[180px]" />
      <span className="rounded-[18px] bg-ea-surface px-[9px] py-[6px] text-[10px] font-extrabold leading-[1.45] text-ea-primary min-[1200px]:hidden">1 daqiqa</span>
      <span className="flex w-full items-center justify-between text-[11px] font-extrabold text-ea-primary min-[1200px]:hidden">Boshlash<Mic size={18} aria-hidden /></span>
    </button>
  </section>;
}
