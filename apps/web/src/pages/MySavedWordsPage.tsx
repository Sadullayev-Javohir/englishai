import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import type { VocabularyItemDto, VocabularyTopicDetailDto } from "@/api/types";
import { ReviewStage } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { AppButton, AppIconButton, DesignCard, DesignModal, DesignState } from "@/components/design";
import { MilestoneTrack } from "@/components/vocabulary/MilestoneTrack";
import { Icon } from "@/components/ui/Icon";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { uz } from "@/content/uz";
import { formatDate } from "@/lib/labels";
import { useAsync } from "@/lib/useAsync";
import { useWordVoice } from "@/lib/useWordVoice";
import { isDue, stageMilestones, topicAggregate, wordStatus } from "@/lib/srsStage";
import "./MySavedWordsPage.css";

interface SavedTopicGroup {
  topicId: string;
  words: VocabularyItemDto[];
  topic: VocabularyTopicDetailDto | null;
}

type Filter = "all" | "due" | "planned" | "mastered";

const FILTERS: Array<{ value: Filter; label: string; icon: string }> = [
  { value: "all", label: "Barchasi", icon: "grid_view" },
  { value: "due", label: "Bugun", icon: "schedule" },
  { value: "planned", label: "Kutilmoqda", icon: "event" },
  { value: "mastered", label: "O‘zlashtirilgan", icon: "verified" },
];


export function MySavedWordsPage() {
  const learnerId = getLearnerId(); const navigate = useNavigate(); const speak = useWordVoice();
  const [query, setQuery] = useState(""); const [filter, setFilter] = useState<Filter>("all");
  const [selectedWord, setSelectedWord] = useState<VocabularyItemDto | null>(null);
  const [topicDetails, setTopicDetails] = useState<Record<string, VocabularyTopicDetailDto | null>>({});
  const { data, loading, error, reload } = useAsync(() => api.vocabulary.list(learnerId), [learnerId]);
  const topicIds = useMemo(() => [...new Set((data ?? []).flatMap(w => w.sourceTopicId ? [w.sourceTopicId] : []))], [data]);
  useEffect(() => {
    let cancelled = false; const ids = topicIds.filter(id => !(id in topicDetails));
    if (!ids.length) return;
    void Promise.all(ids.map(id => api.vocabulary.topic(id).catch(() => null))).then(topics => {
      if (!cancelled) setTopicDetails(current => ({ ...current, ...Object.fromEntries(ids.map((id, i) => [id, topics[i]])) }));
    });
    return () => { cancelled = true; };
  }, [topicIds, topicDetails]);
  const match = useCallback((w: VocabularyItemDto) => filter === "all" || (filter === "due" ? isDue(w) : filter === "mastered" ? w.stage === ReviewStage.Mastered : w.stage !== ReviewStage.Mastered && !isDue(w)), [filter]);
  const groups = useMemo(() => topicIds.map(topicId => ({ topicId, topic: topicDetails[topicId] ?? null, words: (data ?? []).filter(w => w.sourceTopicId === topicId) })), [topicIds, data, topicDetails]);
  const normalized = query.toLocaleLowerCase().trim();
  // Filter groups, not the words inside each group: partial filters must never fabricate mastery.
  const visible = groups.filter(g => g.words.some(match) && (!normalized || [g.topic?.title, g.topic?.titleUz, ...g.words.flatMap(w => [w.word, w.translation])].some(t => t?.toLocaleLowerCase().includes(normalized)))).sort((a,b) => Number(b.words.some(w => isDue(w))) - Number(a.words.some(w => isDue(w))));
  const others = (data ?? []).filter(w => !w.sourceTopicId && match(w) && (!normalized || (w.word + w.translation).toLocaleLowerCase().includes(normalized)));
  const dueTopics = groups.filter(g => g.words.some(w => isDue(w))).length;
  return <main className="saved-vocabulary"><header className="play-page-heading"><span className="play-eyebrow">TOPICLAR BO‘YICHA TAKRORLASH</span><h1>Saqlangan mavzular.</h1><p>Har bir vocabulary mavzusi — birgalikda takrorlanadigan so‘zlar to‘plami.</p></header>
    {dueTopics > 0 && <section className="saved-play-due"><Icon name="event"/><div><strong>Bugun {dueTopics} ta topic tayyor</strong><p>Mavzuni oching va takrorlashni boshlang.</p></div></section>}
    <section className="saved-vocabulary__controls" aria-label="Saqlangan so‘zlarni filtrlash"><label className="saved-vocabulary__search"><Icon name="search"/><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Topic nomini qidiring…" aria-label="Mavzu yoki so‘zni qidirish"/>{query && <AppIconButton icon="close" label="Qidiruvni tozalash" onClick={() => setQuery("")}/>}</label><div className="saved-vocabulary__filters">{FILTERS.map(f => <button key={f.value} type="button" aria-pressed={filter === f.value} onClick={() => setFilter(f.value)}>{f.label}</button>)}</div></section>
    {loading && <LoadingSkeleton variant="list" rows={3}/>}
    {Boolean(error) && <PageState error icon="cloud_off" title={uz.mySavedWords.loadError} action="Qayta urinish" onAction={reload}/>}
    {!loading && !error && !data?.length && <PageState icon="bookmarks" title={uz.mySavedWords.empty} hint={uz.mySavedWords.emptyHint} action={uz.mySavedWords.browseTopics} onAction={() => navigate("/app/vocabulary/topics")}/>}
    {!loading && !error && !!data?.length && !visible.length && !others.length && <PageState icon="search_off" title={uz.mySavedWords.noSearchResults} action={uz.mySavedWords.clearSearch} onAction={() => { setQuery(""); setFilter("all"); }}/>}
    <div className="saved-play-topics">{visible.map(g => <TopicGroup key={g.topicId} group={g} onPractice={() => navigate(`/app/vocabulary/saved/${g.topicId}/practice`)} onOpen={setSelectedWord} onSpeak={speak}/>)}</div>
    {others.length > 0 && <section className="saved-play-other"><h2>{uz.mySavedWords.otherWordsTitle}</h2><div className="saved-vocabulary__word-grid">{others.map(w => <WordCard key={w.id} word={w} onOpen={() => setSelectedWord(w)} onSpeak={() => speak(w.word)}/>)}</div></section>}
    <p className="saved-play-note">3, 7 va 21 — joriy siklda so‘z o‘rganilgan kundan boshlab hisoblanadi. Bosqich barcha so‘zlar muvaffaqiyatli takrorlanganda bajariladi.</p><small className="saved-play-total">{groups.length} ta topic · jami {data?.length ?? 0} ta so‘z</small>
    {selectedWord && <WordModal word={selectedWord} topic={selectedWord.sourceTopicId ? topicDetails[selectedWord.sourceTopicId] : null} onClose={() => setSelectedWord(null)} onSpeak={() => speak(selectedWord.word)}/>}
  </main>;
}
function TopicGroup({ group, onOpen, onSpeak, onPractice }: { group: SavedTopicGroup; onOpen: (word: VocabularyItemDto) => void; onSpeak: (word: string) => void; onPractice: () => void }) {
 const [expanded, setExpanded] = useState(false); const agg = topicAggregate(group.words); const due = agg.dueCount > 0;
 const nextDates = group.words.filter(w => w.nextReviewAt && w.stage !== ReviewStage.Mastered).map(w => w.nextReviewAt!).sort();
 return <article className={`saved-play-topic${due ? " is-due" : ""}`}><span className="play-eyebrow">VOCABULARY <span>{group.words.length} ta so‘z</span></span><h2>{group.topic?.title || topicName(group)}</h2><p>{group.topic?.titleUz || uz.mySavedWords.unknownTopic}{due ? " · Takrorlash vaqti keldi" : ""}</p><span className="saved-play-stage-label">TAKRORLASH BOSQICHLARI</span><MilestoneTrack milestones={agg.milestones} due={due} mastered={agg.allMastered}/><div className="saved-play-actions"><AppButton onClick={onPractice} tone={due ? "primary" : "standard"} leadingIcon="play_arrow">{due ? `Start · ${[3,7,21][agg.minStage] ?? 21}-kun testi` : "Erkin mashq"}</AppButton><button type="button" className="saved-vocabulary__group-toggle" aria-label={`${topicName(group)} so‘zlarini ${expanded ? "yopish" : "ko‘rish"}`} aria-expanded={expanded} onClick={() => setExpanded(!expanded)}>{expanded ? "So‘zlarni yopish" : "So‘zlarni ko‘rish"}</button></div><small>{due ? `${agg.dueCount} ta so‘z takrorlash uchun tayyor` : agg.allMastered ? "Barcha bosqichlar bajarildi" : nextDates[0] ? `Keyingi takrorlash: ${formatDate(nextDates[0])}` : "Takrorlash rejalashtirilmoqda"}</small>{expanded && <div className="saved-play-word-details"><p>Lug‘atga saqlangan <strong>{group.words.length}/{group.topic?.words.length ?? group.words.length}</strong></p><div className="saved-vocabulary__word-grid" data-testid={`saved-topic-grid-${group.topicId}`}>{group.words.map(w => <WordCard key={w.id} word={w} onOpen={() => onOpen(w)} onSpeak={() => onSpeak(w.word)}/>)}</div></div>}</article>;
}
function WordCard({ word, onOpen, onSpeak }: { word: VocabularyItemDto; onOpen: () => void; onSpeak: () => void }) {
  const status = statusFor(word);
  return <DesignCard as="article" padding="none" className={`saved-vocabulary__word is-${status.tone}`}>
    <div className="saved-vocabulary__word-accent" aria-hidden="true"><Icon name={status.icon} filled /></div>
    <div className="saved-vocabulary__word-top">
      <span className={`saved-vocabulary__status is-${status.tone}`}><Icon name={status.icon} filled />{status.label}</span>
      <AppIconButton type="button" size="sm" icon="volume_up" label={`${word.word} so‘zini tinglash`} onClick={onSpeak} />
    </div>
    <button type="button" className="saved-vocabulary__word-main" onClick={onOpen} aria-label={`${word.word} tafsilotlarini ochish`}>
      <span className="saved-vocabulary__word-index" aria-hidden="true">{word.word.slice(0, 1).toUpperCase()}</span>
      <div className="saved-vocabulary__word-copy">
        <h3>{word.word}</h3>
        <p>{word.translation}</p>
      </div>
      <div className="saved-vocabulary__word-footer">
        {word.partOfSpeech ? <small>{partOfSpeech(word.partOfSpeech)}</small> : <small>So‘z</small>}
        <span>Batafsil <Icon name="arrow_forward" /></span>
      </div>
    </button>
    <div className="saved-vocabulary__word-track">
      <MilestoneTrack milestones={stageMilestones(word.stage)} mastered={status.tone === "mastered"} due={status.tone === "due"} size="sm" />
    </div>
  </DesignCard>;
}

function WordModal({ word, topic, onClose, onSpeak }: { word: VocabularyItemDto; topic: VocabularyTopicDetailDto | null; onClose: () => void; onSpeak: () => void }) {
  return <DesignModal
    open
    onClose={onClose}
    title={word.word}
    description={`${word.word} · ${word.translation}`}
    closeLabel="Yopish"
    className="saved-vocabulary-detail-modal"
    footer={<AppButton tone="standard" onClick={onClose}>Yopish</AppButton>}
  >
      <div className="saved-vocabulary-sheet__hero"><div><h3>{word.word}</h3><p>{word.translation}</p></div><AppIconButton type="button" icon="volume_up" label={`${word.word} so‘zini tinglash`} onClick={onSpeak} /></div>
      <dl>{word.partOfSpeech && <div><dt>So‘z turkumi</dt><dd>{partOfSpeech(word.partOfSpeech)}</dd></div>}<div><dt>Mavzu</dt><dd>{topic?.titleUz || topic?.title || uz.mySavedWords.unknownTopic}</dd></div><div><dt>Takrorlash</dt><dd>{reviewLabel(word)}</dd></div></dl>
      {word.exampleSentence && <div className="saved-vocabulary-sheet__example"><span>Misol gap</span><p>{word.exampleSentence}</p></div>}
  </DesignModal>;
}

function PageState({ icon, title, hint, action, onAction, loading = false, error = false }: { icon: string; title: string; hint?: string; action?: string; onAction?: () => void; loading?: boolean; error?: boolean }) {
  return <DesignState className={`${loading ? "is-loading" : ""}${error ? " ea-state--error" : ""}`} role={error ? "alert" : "status"} icon={icon} title={title} description={hint} action={action && onAction ? <AppButton type="button" onClick={onAction}>{action}</AppButton> : undefined} />;
}

function topicName(group: SavedTopicGroup) { return group.topic?.titleUz || group.topic?.title || uz.mySavedWords.unknownTopic; }
function statusFor(word: VocabularyItemDto) {
  const status = wordStatus(word);
  if (status === "mastered") return { label: uz.mySavedWords.mastered, icon: "verified", tone: "mastered" as const };
  if (status === "due") return { label: uz.mySavedWords.dueNow, icon: "schedule", tone: "due" as const };
  return { label: "Rejada", icon: "event", tone: "planned" as const };
}
function reviewLabel(word: VocabularyItemDto) {
  if (word.stage === ReviewStage.Mastered) return uz.mySavedWords.mastered;
  if (isDue(word)) return uz.mySavedWords.dueNow;
  if (!word.nextReviewAt) return "Hozircha rejalashtirilmagan";
  return uz.mySavedWords.nextReview(formatDate(word.nextReviewAt));
}
function partOfSpeech(value: string) {
  const labels: Record<string, string> = { noun: "Ot", verb: "Fe‘l", adjective: "Sifat", adverb: "Ravish", phrasalverb: "Frazeologik fe‘l", phrase: "Ibora" };
  return labels[value.toLowerCase()] ?? value;
}
