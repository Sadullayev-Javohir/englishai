import { useEffect, useState } from "react";
import { Bookmark, ChevronDown, LockKeyhole, Search } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useDocumentTitle } from "@/app/documentTitle";
import { useAsync } from "@/lib/useAsync";
import { useSkillTopicGates } from "@/lib/skillTopicGates";
import { cefrShort } from "@/lib/labels";
import { TopicImage } from "@/components/TopicImage";
import { publishAssistantContext } from "@/components/assistantContext";
import { grammarPhoto, grammarFocusTitle, readSavedGrammar, savedGrammarKey } from "@/components/grammar/grammarContent";
import "./GrammarCatalogPage.css";

type LevelFilter = CefrLevel | "all" | null;

export function GrammarCatalogPage() {
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const [filter, setFilter] = useState<LevelFilter>(null);
  const [search, setSearch] = useState("");
  const [savedOnly, setSavedOnly] = useState(false);
  const [saved, setSaved] = useState(() => readSavedGrammar(learnerId));
  const [reload, setReload] = useState(0);
  const { data, loading, error } = useAsync(
    () => api.grammar.catalog(learnerId, typeof filter === "number" ? filter : undefined, filter === "all"),
    [learnerId, filter, reload],
  );
  const { gateOf, ready } = useSkillTopicGates(learnerId, typeof filter === "number" ? filter : undefined, "Grammar", filter === "all");
  useDocumentTitle("Grammar");
  useEffect(() => setSaved(readSavedGrammar(learnerId)), [learnerId]);
  useEffect(() => publishAssistantContext({
    area: "grammar", title: "Grammar mavzulari", context: (data ?? []).map(topic => `${grammarFocusTitle(topic)} — ${topic.title}`).join("\n"),
    focusText: "", route: "/app/grammar", stage: "catalog",
  }), [data]);
  const learnerLevel = data?.[0]?.level ?? CefrLevel.A1;
  const filtered = (data ?? []).filter(topic =>
    (!savedOnly || saved.includes(topic.topicId)) &&
    `${grammarFocusTitle(topic)} ${topic.title} ${topic.titleUz}`.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase()),
  );
  function toggleSaved(id: string) {
    setSaved(current => {
      const next = current.includes(id) ? current.filter(item => item !== id) : [...current, id];
      try { localStorage.setItem(savedGrammarKey(learnerId), JSON.stringify(next)); } catch { /* Current-tab saving still works if storage is disabled. */ }
      return next;
    });
  }
  return (
    <section className="grammar-catalog" data-pen-screen="21">
      <div className="grammar-heading">
        <span className="grammar-tag">GRAMMAR</span>
        <h1>Qoida emas,<br />gap tuzing.</h1>
        <p>Bilgan so‘zlaringizni gapda ishlating.</p>
      </div>
      <label className="grammar-catalog__search"><Search size={20} /><input aria-label="Mavzuni qidiring" placeholder="Mavzuni qidiring…" value={search} onChange={event => setSearch(event.target.value)} /></label>
      <div className="grammar-catalog__filters" aria-label="Grammar filtrlari">
        <label className={`grammar-catalog__level${filter !== "all" && !savedOnly ? " is-active" : ""}`}>
          <select aria-label="CEFR darajasi" value={typeof filter === "number" ? filter : learnerLevel} onChange={event => { setFilter(Number(event.target.value) as CefrLevel); setSavedOnly(false); }}>
            {[1,2,3,4,5,6].map(level => <option value={level} key={level}>{cefrShort(level)}</option>)}
          </select><ChevronDown size={12} />
        </label>
        <button type="button" aria-pressed={filter === "all" && !savedOnly} onClick={() => { setFilter("all"); setSavedOnly(false); }}>Barchasi</button>
        <button type="button" aria-pressed={savedOnly} onClick={() => { setFilter("all"); setSavedOnly(true); }}>Saqlangan</button>
      </div>
      {loading ? <p className="grammar-catalog__status" role="status">Mavzular yuklanmoqda…</p> : error ? (
        <div className="grammar-catalog__status" role="alert"><p>Mavzularni yuklab bo‘lmadi.</p><button className="grammar-primary" onClick={() => setReload(value => value + 1)}>Qayta urinish</button></div>
      ) : filtered.length === 0 ? <p className="grammar-catalog__status" role="status">{savedOnly ? "Hali saqlangan mavzu yo‘q." : "Mavzu topilmadi."}</p> : (
        <div className="grammar-catalog__grid">
          {filtered.map(topic => {
            const gate = gateOf(topic.topicId);
            const locked = !ready || gate.isLocked || gate.requiresPro;
            const title = grammarFocusTitle(topic);
            const photo = grammarPhoto(topic.grammarFocusCode, topic.title);
            return (
              <article key={topic.topicId} className={`grammar-catalog__card${locked ? " is-locked" : ""}`}>
                <button className="grammar-catalog__open" disabled={locked} onClick={() => navigate(`/app/grammar/topic/${topic.topicId}`, { state: { grammarFocusTitle: title } })} aria-label={`${title} — ${locked ? "Qulflangan" : "Boshlash"}`}>
                  <span className="grammar-catalog__photo">{photo ? <img src={photo} alt={title} /> : <TopicImage topicId={topic.topicId} title={topic.title} level={topic.level} hideLevelBadge hideTitle className="h-full w-full" />}</span>
                  <span className="grammar-catalog__copy">
                    <span className="grammar-catalog__meta"><span className="grammar-tag">{cefrShort(topic.level)}</span><span>{gate.passed ? "TUGATILGAN" : gate.passedModuleCount > 0 ? "DAVOM ETISH" : "YANGI MAVZU"}</span></span>
                    <strong>{title}</strong>
                    <span className="grammar-catalog__description">{topic.grammarFocusCode === "present-perfect" ? "have / has + V3 · 6 bosqich" : topic.titleUz || topic.title}</span>
                    <span className="grammar-catalog__cta">{locked ? <><LockKeyhole size={14} />{gate.requiresPro ? "PRO" : "Qulflangan"}</> : gate.passedModuleCount > 0 ? "Boshlash →" : "Ko‘rish →"}</span>
                  </span>
                </button>
                <button className={`grammar-catalog__save${saved.includes(topic.topicId) ? " is-saved" : ""}`} aria-label={`${title}: ${saved.includes(topic.topicId) ? "saqlangandan olib tashlash" : "saqlash"}`} aria-pressed={saved.includes(topic.topicId)} onClick={() => toggleSaved(topic.topicId)}><Bookmark size={16} fill={saved.includes(topic.topicId) ? "currentColor" : "none"} /></button>
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}
