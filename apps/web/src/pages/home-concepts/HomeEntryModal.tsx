import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Check, ChevronRight, CirclePlay, Clock3, Sparkles, Trophy, X } from "lucide-react";
import { api } from "@/api/client";
import type { CefrLevel, DueReviewDto, VideoSummaryDto, VocabularyItemDto } from "@/api/types";
import { DesignModal } from "@/components/design";
import { TopicImage } from "@/components/TopicImage";
import { formatDuration } from "@/lib/labels";
import "./homeEntryModal.css";

export type HomeEntryPrompt =
  | {
      kind: "mini-quiz";
      word: string;
      correctTranslation: string;
      options: string[];
      topicTitle: string;
    }
  | {
      kind: "due-review";
      dueCount: number;
      topicTitle: string;
      topicImage?: HomeEntryTopic;
      dueReviewDay: 3 | 7 | 21;
      estimatedMinutes: number;
    }
  | {
      kind: "streak";
      streakDays: number;
    }
  | {
      kind: "video";
      video: VideoSummaryDto;
    };

export interface HomeEntryTopic {
  id: string;
  title: string;
  level: CefrLevel;
}

export interface HomeEntryPromptInput {
  savedWords: VocabularyItemDto[];
  dueWords: DueReviewDto[];
  currentStreak: number;
  activeTopic?: HomeEntryTopic | null;
  videos: VideoSummaryDto[];
}

function uniqueValues(values: string[]) {
  const seen = new Set<string>();
  return values.filter(value => {
    const key = value.trim().toLocaleLowerCase();
    if (!key || seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

function topicFor(sourceTopicId: string | null, activeTopic?: HomeEntryTopic | null) {
  return sourceTopicId && activeTopic?.id === sourceTopicId ? activeTopic : undefined;
}

function titleFor(topic?: HomeEntryTopic) {
  return topic?.title ?? "Saqlangan so‘zlar";
}

/**
 * Every value comes from an existing learner API response. A prompt is omitted rather than
 * filling missing data with sample copy, so entry modals never claim an unavailable review,
 * streak, word or video.
 */
export function buildHomeEntryPrompts(input: HomeEntryPromptInput): HomeEntryPrompt[] {
  const prompts: HomeEntryPrompt[] = [];
  const savedWords = input.savedWords.filter(word => word.word.trim() && word.translation.trim());
  const quizWord = savedWords.find(word => uniqueValues([
    word.translation,
    ...savedWords.filter(candidate => candidate.id !== word.id).map(candidate => candidate.translation),
  ]).length >= 3);

  if (quizWord) {
    const translations = uniqueValues([
      quizWord.translation,
      ...savedWords.filter(word => word.id !== quizWord.id).map(word => word.translation),
    ]).slice(0, 3);
    const topic = topicFor(quizWord.sourceTopicId, input.activeTopic);
    prompts.push({
      kind: "mini-quiz",
      word: quizWord.word,
      correctTranslation: quizWord.translation,
      options: translations,
      topicTitle: titleFor(topic),
    });
  }

  const firstDue = input.dueWords.find(word => word.word.trim() && word.translation.trim());
  if (firstDue) {
    const topic = topicFor(firstDue.sourceTopicId, input.activeTopic);
    const day = firstDue.stage === 0 ? 3 : firstDue.stage === 1 ? 7 : 21;
    prompts.push({
      kind: "due-review",
      dueCount: input.dueWords.length,
      topicTitle: titleFor(topic),
      topicImage: topic,
      dueReviewDay: day,
      estimatedMinutes: Math.max(1, Math.ceil(input.dueWords.length / 5)),
    });
  }

  if (input.currentStreak >= 7) {
    prompts.push({ kind: "streak", streakDays: input.currentStreak });
  }

  const video = input.videos.find(item => item.youTubeVideoId.trim() && item.title.trim() && item.channel.trim() && item.durationSeconds > 0);
  if (video) prompts.push({ kind: "video", video });

  return prompts;
}

export function selectHomeEntryPrompt(prompts: HomeEntryPrompt[], random = Math.random): HomeEntryPrompt | null {
  if (!prompts.length) return null;
  return prompts[Math.min(prompts.length - 1, Math.floor(random() * prompts.length))] ?? null;
}

function PromptHeader({ icon: Icon, eyebrow, onClose }: { icon: typeof Sparkles; eyebrow: string; onClose: () => void }) {
  return <div className="home-entry-modal__header">
    <span className="home-entry-modal__eyebrow"><span><Icon size={13} aria-hidden /></span>{eyebrow}</span>
    <button type="button" className="home-entry-modal__close" aria-label="Modal oynani yopish" onClick={onClose}><X size={15} aria-hidden /></button>
  </div>;
}

function DeferredHint({ onSuppressToday }: { onSuppressToday: () => void }) {
  return <button type="button" className="home-entry-modal__suppress" onClick={onSuppressToday}><span aria-hidden />Bugun boshqa ko‘rsatma</button>;
}

function MiniQuiz({ prompt, onClose, onSuppressToday }: { prompt: Extract<HomeEntryPrompt, { kind: "mini-quiz" }>; onClose: () => void; onSuppressToday: () => void }) {
  const [selected, setSelected] = useState<string | null>(null);
  const [checked, setChecked] = useState(false);
  const isCorrect = checked && selected === prompt.correctTranslation;

  return <>
    <PromptHeader icon={Sparkles} eyebrow="KUNLIK MINI-MASHQ" onClose={onClose} />
    <div className="home-entry-modal__intro home-entry-modal__intro--quiz">
      <div><h3>30 soniya,<br />bitta yangi qadam.</h3><p>{prompt.topicTitle} · 1 savol</p></div>
      <img src="/assets/play/parrot.svg" alt="" width={104} height={90} />
    </div>
    <div className="home-entry-modal__question"><span>TO‘G‘RI TARJIMANI TANLANG</span><strong>{prompt.word}</strong></div>
    <div className="home-entry-modal__answers" role="radiogroup" aria-label={`${prompt.word} tarjimasi`}>
      {prompt.options.map((option, index) => <button key={option} type="button" role="radio" aria-checked={selected === option} onClick={() => { setSelected(option); setChecked(false); }} className={`${selected === option ? "is-selected" : ""} ${checked && option === prompt.correctTranslation ? "is-correct" : ""} ${checked && selected === option && option !== prompt.correctTranslation ? "is-incorrect" : ""}`}><span>{String.fromCharCode(65 + index)}</span>{option}<i aria-hidden>{selected === option && <Check size={14} />}</i></button>)}
    </div>
    {checked && <p className={`home-entry-modal__feedback ${isCorrect ? "is-correct" : ""}`} role="status">{isCorrect ? "To‘g‘ri javob!" : `To‘g‘ri javob: ${prompt.correctTranslation}`}</p>}
    <div className="home-entry-modal__actions"><button type="button" className="home-entry-modal__primary" disabled={!selected} onClick={() => checked ? onClose() : setChecked(true)}>{checked ? "Davom etish" : "Tekshirish"}<Check size={16} aria-hidden /></button><button type="button" className="home-entry-modal__later" onClick={onClose}>Keyinroq</button><DeferredHint onSuppressToday={onSuppressToday} /></div>
  </>;
}

function ReviewPrompt({ prompt, onClose, onSuppressToday }: { prompt: Extract<HomeEntryPrompt, { kind: "due-review" }>; onClose: () => void; onSuppressToday: () => void }) {
  const navigate = useNavigate();
  return <>
    <PromptHeader icon={Clock3} eyebrow="TAKRORLASH VAQTI" onClose={onClose} />
    <h3 className="home-entry-modal__title">So‘zlarni eslab<br />qolamizmi?</h3>
    <div className="home-entry-modal__review-topic">
      {prompt.topicImage ? <TopicImage topicId={prompt.topicImage.id} title={prompt.topicImage.title} level={prompt.topicImage.level} hideLevelBadge hideTitle className="home-entry-modal__topic-image" /> : <span className="home-entry-modal__topic-placeholder"><Sparkles size={28} aria-hidden /></span>}
      <strong>{prompt.topicTitle}</strong><p>{prompt.dueCount} ta so‘z · taxminan {prompt.estimatedMinutes} daqiqa</p>
    </div>
    <ol className="home-entry-modal__schedule" aria-label="3 7 21 takrorlash jadvali">{([3, 7, 21] as const).map(day => <li key={day} className={day === prompt.dueReviewDay ? "is-current" : ""}><span>{day}</span><strong>{day}-kun</strong><small>{day === prompt.dueReviewDay ? "Bugun" : `${Math.max(1, day - prompt.dueReviewDay)} kundan so‘ng`}</small></li>)}</ol>
    <div className="home-entry-modal__actions"><button type="button" className="home-entry-modal__primary" onClick={() => navigate("/app/vocabulary/saved")}>Takrorlashni boshlash<ChevronRight size={16} aria-hidden /></button><button type="button" className="home-entry-modal__later" onClick={onClose}>Keyinroq</button><DeferredHint onSuppressToday={onSuppressToday} /></div>
  </>;
}

function StreakPrompt({ prompt, onClose, onSuppressToday }: { prompt: Extract<HomeEntryPrompt, { kind: "streak" }>; onClose: () => void; onSuppressToday: () => void }) {
  return <>
    <PromptHeader icon={Trophy} eyebrow="TABRIKLAYMIZ!" onClose={onClose} />
    <div className="home-entry-modal__streak-hero"><img src="/assets/play/parrot.svg" alt="" width={156} height={130} /><h3>7 kunlik seriya!</h3><p>{prompt.streakDays} kun ketma-ket mashq qildingiz.</p></div>
    <ol className="home-entry-modal__streak-days" aria-label="Bajarilgan yetti kun">{Array.from({ length: 7 }, (_, index) => <li key={index}><span><Check size={13} aria-hidden /></span><small>{index + 1}</small></li>)}</ol>
    <div className="home-entry-modal__actions"><button type="button" className="home-entry-modal__primary" onClick={onClose}>Ajoyib, davom etamiz!<ChevronRight size={16} aria-hidden /></button><button type="button" className="home-entry-modal__later" onClick={onClose}>Hozir emas</button><DeferredHint onSuppressToday={onSuppressToday} /></div>
  </>;
}

function VideoPrompt({ prompt, onClose, onSuppressToday }: { prompt: Extract<HomeEntryPrompt, { kind: "video" }>; onClose: () => void; onSuppressToday: () => void }) {
  const navigate = useNavigate();
  const [opening, setOpening] = useState(false);
  const [error, setError] = useState(false);
  const thumbnail = `https://i.ytimg.com/vi/${encodeURIComponent(prompt.video.youTubeVideoId)}/hqdefault.jpg`;
  const openVideo = async () => {
    if (opening) return;
    setOpening(true); setError(false);
    try {
      const lesson = await api.video.open({ ...prompt.video, lessonId: prompt.video.id, hasClosedCaptions: false });
      navigate(`/video/${lesson.id}/play`);
    } catch {
      setError(true);
      setOpening(false);
    }
  };
  return <>
    <PromptHeader icon={CirclePlay} eyebrow="BUGUN SIZ UCHUN" onClose={onClose} />
    <h3 className="home-entry-modal__title">Bitta video.<br />Yangi iboralar.</h3>
    <div className="home-entry-modal__video"><img src={thumbnail} alt="" onError={event => { event.currentTarget.style.visibility = "hidden"; }} /><span><CirclePlay size={26} aria-hidden /></span><b>{formatDuration(prompt.video.durationSeconds)}</b></div>
    <div className="home-entry-modal__video-title"><h4>{prompt.video.title}</h4><p><span>{String(prompt.video.level)}</span><span>{formatDuration(prompt.video.durationSeconds)}</span>{prompt.video.channel}</p></div>
    <div className="home-entry-modal__actions"><button type="button" className="home-entry-modal__primary" disabled={opening} onClick={() => void openVideo()}>{opening ? "Ochilmoqda…" : "Tomosha qilish"}<ChevronRight size={16} aria-hidden /></button>{error && <p className="home-entry-modal__feedback" role="alert">Video ochilmadi. Qayta urinib ko‘ring.</p>}<button type="button" className="home-entry-modal__later" onClick={onClose}>Keyinroq</button><DeferredHint onSuppressToday={onSuppressToday} /></div>
  </>;
}

export function HomeEntryModal({ prompt, open, onClose, onSuppressToday }: { prompt: HomeEntryPrompt | null; open: boolean; onClose: () => void; onSuppressToday: () => void }) {
  const title = useMemo(() => prompt?.kind === "mini-quiz" ? "Kunlik mini-mashq" : prompt?.kind === "due-review" ? "Takrorlash vaqti" : prompt?.kind === "streak" ? "7 kunlik yutuq" : "Video tavsiyasi", [prompt?.kind]);
  if (!prompt) return null;
  return <DesignModal open={open} onClose={onClose} title={title} closeLabel="Modal oynani yopish" showClose={false} className={`home-entry-modal home-entry-modal--${prompt.kind}`} panelProps={{ "data-testid": `home-entry-modal-${prompt.kind}` } as React.HTMLAttributes<HTMLElement>}>
    <div className="home-entry-modal__content">
      {prompt.kind === "mini-quiz" && <MiniQuiz prompt={prompt} onClose={onClose} onSuppressToday={onSuppressToday} />}
      {prompt.kind === "due-review" && <ReviewPrompt prompt={prompt} onClose={onClose} onSuppressToday={onSuppressToday} />}
      {prompt.kind === "streak" && <StreakPrompt prompt={prompt} onClose={onClose} onSuppressToday={onSuppressToday} />}
      {prompt.kind === "video" && <VideoPrompt prompt={prompt} onClose={onClose} onSuppressToday={onSuppressToday} />}
    </div>
  </DesignModal>;
}
