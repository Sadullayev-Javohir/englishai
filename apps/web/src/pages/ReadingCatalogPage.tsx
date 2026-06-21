import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { ArrowRight, Bookmark, LockKeyhole, Search } from "lucide-react";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { useDocumentTitle } from "@/app/documentTitle";
import { useSkillTopicGates } from "@/lib/skillTopicGates";
import { cefrShort } from "@/lib/labels";
import { lessonOriginFrom, lessonState } from "@/lib/lessonNavigation";
import { TopicImage } from "@/components/TopicImage";
import { publishAssistantContext } from "@/components/assistantContext";
import { useReadingBookmarks } from "@/components/reading/useReadingBookmarks";
import "./ReadingCatalogPage.css";

const LEVELS = [CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2];

export function ReadingCatalogPage() {
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const location = useLocation();
  const origin = lessonOriginFrom(location);
  const state = location.state as { vocabularyTopicId?: string; topicTitle?: string } | null;
  const [level, setLevel] = useState<CefrLevel | null>(null);
  const [tab, setTab] = useState<"level" | "all" | "saved">("level");
  const [query, setQuery] = useState("");
  const bookmarks = useReadingBookmarks();
  const allLevels = tab !== "level";
  const { data, loading, error, reload } = useAsync(
    () => api.reading.catalog(learnerId, allLevels ? undefined : level ?? undefined, allLevels),
    [learnerId, level, allLevels],
  );
  const { gateOf, ready: gatesReady } = useSkillTopicGates(learnerId, allLevels ? undefined : level ?? undefined, "Reading", allLevels);
  useDocumentTitle("Reading", "Matnlar katalogi");
  useEffect(() => {
    if (state?.vocabularyTopicId) navigate(`/reading/topic/${state.vocabularyTopicId}`, { replace:true, state:lessonState(origin, { topicTitle:state.topicTitle }) });
  }, [navigate, origin, state?.topicTitle, state?.vocabularyTopicId]);
  useEffect(() => publishAssistantContext({ area:"reading", title:"Reading mavzulari", context:(data ?? []).map(topic => topic.title).join(" | "), focusText:"", route:"/reading", stage:"catalog" }), [data]);
  const visible = (data ?? []).filter(topic => (tab !== "saved" || bookmarks.has(topic.topicId)) && `${topic.title} ${topic.titleUz} ${topic.category}`.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase()));
  const activeLevel = level ?? data?.[0]?.level ?? CefrLevel.A1;

  return (
    <div className="reading-library" data-pen-screen="31">
      <header className="reading-heading"><span className="reading-tag">READING</span><h1>Hikoya ichiga<br />kiring.</h1><p>O‘qing. Tasavvur qiling. Tushuning.</p></header>
      <label className="reading-library__search"><Search size={20} /><input type="search" value={query} onChange={event => setQuery(event.target.value)} placeholder="Mavzuni qidiring…" aria-label="Reading mavzusini qidirish" /></label>
      <div className="reading-library__filters" aria-label="Reading filtrlari">
        <select aria-label="Reading darajasi" value={activeLevel} className={tab === "level" ? "is-active" : ""} onClick={() => setTab("level")} onChange={event => { setLevel(Number(event.target.value) as CefrLevel); setTab("level"); }}>
          {LEVELS.map(item => <option key={item} value={item}>{cefrShort(item)}</option>)}
        </select>
        <button type="button" aria-pressed={tab === "all"} onClick={() => setTab("all")}>Barchasi</button>
        <button type="button" aria-pressed={tab === "saved"} onClick={() => setTab("saved")}>Saqlangan</button>
      </div>
      {loading ? <p role="status" className="reading-library__state">Matnlar yuklanmoqda…</p>
        : error ? <div role="alert" className="reading-library__state"><p>Matnlarni yuklab bo‘lmadi.</p><button type="button" className="reading-secondary" onClick={reload}>Qayta urinish</button></div>
        : visible.length === 0 ? <p role="status" className="reading-library__state">{tab === "saved" ? "Hozircha saqlangan matn yo‘q. Kartadagi belgi orqali saqlashingiz mumkin." : "Qidiruvga mos matn topilmadi."}</p>
        : <section className="reading-library__grid" aria-label="Reading matnlari">
          {visible.map(topic => {
            const gate = gateOf(topic.topicId);
            const locked = !gatesReady || gate.isLocked || gate.requiresPro;
            return <article key={topic.topicId} className="reading-library__card">
              <button type="button" className="reading-library__open" disabled={locked} onClick={() => navigate(`/reading/topic/${topic.topicId}`, { state:lessonState(origin) })}>
                <span className="reading-library__photo"><TopicImage topicId={topic.topicId} title={topic.title} level={topic.level} category={topic.category} hideTitle hideLevelBadge className="h-full w-full" />{locked && <span className="reading-library__lock"><LockKeyhole size={26} /></span>}</span>
                <span className="reading-library__details">
                  <span className="reading-library__metadata"><span className="reading-tag">{cefrShort(topic.level)}</span><span>{gate.passed ? "O‘QILGAN" : gate.passedModuleCount > 0 ? "DAVOM ETISH" : "YANGI MATN"}</span></span>
                  <h2>{topic.title}</h2><span className="reading-library__description">{topic.titleUz || topic.category}</span>
                  <span className="reading-library__action">{locked ? (gate.requiresPro ? "Pro kerak" : "Qulflangan") : gate.passed ? "Takrorlash" : "Boshlash"}<ArrowRight size={16} /></span>
                </span>
              </button>
              <button type="button" className="reading-library__bookmark" aria-label={`${topic.title} — ${bookmarks.has(topic.topicId) ? "saqlanganlardan olib tashlash" : "saqlash"}`} aria-pressed={bookmarks.has(topic.topicId)} onClick={() => bookmarks.toggle(topic.topicId)}><Bookmark size={18} fill={bookmarks.has(topic.topicId) ? "currentColor" : "none"} /></button>
            </article>;
          })}
        </section>}
    </div>
  );
}
