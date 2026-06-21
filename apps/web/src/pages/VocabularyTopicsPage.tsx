import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Search, X, LockKeyhole } from "lucide-react";
import { api } from "@/api/client";
import { CefrLevel, type VocabularyTopicSummaryDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { usePaywall } from "@/components/PaywallProvider";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { VocabularyHeading } from "@/components/vocabulary/VocabularyChrome";
import { VocabularyPhoto } from "@/components/vocabulary/VocabularyPhoto";
import { publishAssistantContext } from "@/components/assistantContext";
import { cefrShort } from "@/lib/labels";
import { useAsync } from "@/lib/useAsync";
import "./VocabularyTopicsPage.css";

export function VocabularyTopicsPage() {
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const { open: openPaywall } = usePaywall();
  const [allLevels, setAllLevels] = useState(false);
  const [query, setQuery] = useState("");
  const { data, loading, error, reload } = useAsync(
    () => api.vocabulary.topics(learnerId, undefined, allLevels), [learnerId, allLevels],
  );
  const learnerLevel = data?.[0]?.level ?? CefrLevel.B1;
  const visibleTopics = useMemo(() => (data ?? []).filter(topic =>
    [topic.title, topic.titleUz, topic.category, cefrShort(topic.level)].join(" ").toLocaleLowerCase("uz").includes(query.trim().toLocaleLowerCase("uz")),
  ), [data, query]);
  useEffect(() => publishAssistantContext({
    area: "vocabulary", title: "Vocabulary mavzulari",
    context: visibleTopics.filter(topic => !topic.isLocked).map(topic => `${topic.title} — ${topic.titleUz}`).join(" | "),
    focusText: query, route: "/app/vocabulary/topics", stage: "catalog",
  }), [query, visibleTopics]);
  return (
    <section className="vocabulary-pen-catalog" data-pen-screen="11">
      <VocabularyHeading title={"So‘zlar bilan\ndunyoni oching."} subtitle="Tanish vaziyat. Yangi so‘z. Haqiqiy suhbat." />
      <label className="vocabulary-pen-catalog__search">
        <Search size={20} />
        <span className="sr-only">Vocabulary mavzularini qidirish</span>
        <input type="search" placeholder="Mavzuni qidiring…" value={query} onChange={event => setQuery(event.target.value)} />
        {query && <button type="button" onClick={() => setQuery("")} aria-label="Qidiruvni tozalash"><X size={18} /></button>}
      </label>
      <nav className="vocabulary-pen-catalog__filters" aria-label="Mavzu filtrlari">
        <button type="button" aria-pressed={!allLevels} onClick={() => setAllLevels(false)}>{cefrShort(learnerLevel)}</button>
        <button type="button" aria-pressed={allLevels} onClick={() => setAllLevels(true)}>Barchasi</button>
        <button type="button" onClick={() => navigate("/app/vocabulary/saved")}>Saqlangan</button>
      </nav>
      {loading ? <ModulePageLoader icon="bookmarks" accent="green" embedded /> : error ? (
        <div className="vocabulary-pen-catalog__state" role="alert"><h2>Ma’lumotni yuklab bo‘lmadi</h2><button type="button" onClick={reload}>Qayta urinish</button></div>
      ) : visibleTopics.length === 0 ? (
        <div className="vocabulary-pen-catalog__state" role="status"><h2>{query ? "Qidiruv natijasi topilmadi" : "Mavzular topilmadi"}</h2>{query && <button type="button" onClick={() => setQuery("")}>Qidiruvni tozalash</button>}</div>
      ) : (
        <div className="vocabulary-pen-catalog__grid">
          {visibleTopics.map(topic => <VocabularyCard key={topic.id} topic={topic} onOpen={() => topic.requiresPro ? openPaywall() : navigate(`/app/vocabulary/topic/${topic.id}`)} />)}
        </div>
      )}
    </section>
  );
}

function VocabularyCard({ topic, onOpen }: { topic: VocabularyTopicSummaryDto; onOpen: () => void }) {
  const locked = topic.isLocked || topic.modules.find(module => module.module === "Vocabulary")?.unlocked === false;
  const started = topic.isStarted || topic.learned || topic.passedModuleCount > 0;
  return (
    <button type="button" disabled={locked} onClick={onOpen} className="vocabulary-pen-catalog__card" aria-label={`${topic.title} - ${topic.titleUz}`}>
      <VocabularyPhoto topic={topic} className="vocabulary-pen-catalog__photo" />
      <span className="vocabulary-pen-catalog__details">
        <span className="vocabulary-pen-catalog__meta"><span className="vocabulary-tag">{cefrShort(topic.level)}</span><span>{locked ? <><LockKeyhole size={12} /> YOPIQ</> : topic.requiresPro ? "PREMIUM" : topic.isMastered ? "TUGATILGAN" : started ? "DAVOM ETISH" : "YANGI MAVZU"}</span></span>
        <span className="vocabulary-pen-catalog__title">{topic.title}</span>
        <span className="vocabulary-pen-catalog__description">{topic.titleUz}{topic.wordCount != null ? ` · ${topic.wordCount} ta so‘z` : ""}</span>
        <span className="vocabulary-pen-catalog__cta">{started ? "Boshlash" : "Ko‘rish"} →</span>
      </span>
    </button>
  );
}
