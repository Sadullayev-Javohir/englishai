import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Search } from "lucide-react";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { useAuth } from "@/app/auth";
import { useAsync } from "@/lib/useAsync";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { TopicImage } from "@/components/TopicImage";
import { useSkillTopicGates } from "@/lib/skillTopicGates";
import { cefrShort } from "@/lib/labels";
import { CefrLevel } from "@/api/types";
import { publishAssistantContext } from "@/components/assistantContext";
import "./ListeningCatalogPage.css";

const LEVELS = [CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2];
type LevelFilter = CefrLevel | "all" | null;

export function ListeningCatalogPage() {
  const navigate = useNavigate();
  const { user, status } = useAuth();
  const learnerId = user?.id ?? "";
  const ready = status === "authenticated" && !!user;
  const [filter, setFilter] = useState<LevelFilter>(null);
  const [query, setQuery] = useState("");
  const [progressFilter, setProgressFilter] = useState("all");
  const { data, loading, error, reload } = useAsync(
    () => filter === "all" ? api.listening.catalog(learnerId, undefined, true) : api.listening.catalog(learnerId, filter ?? undefined),
    [learnerId, filter, ready], ready,
  );
  const { gateOf, ready: gatesReady } = useSkillTopicGates(learnerId, typeof filter === "number" ? filter : undefined, "Listening", filter === "all");
  const learnerLevel = data?.[0]?.level ?? null;
  const visible = (data ?? []).filter(topic => {
    const gate = gateOf(topic.topicId);
    const matchesText = `${topic.title} ${topic.titleUz ?? ""} ${topic.category}`.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase());
    return matchesText && (progressFilter === "all" || (progressFilter === "done" ? gate.passed : !gate.passed && gate.passedModuleCount > 0));
  });
  useEffect(() => publishAssistantContext({ area: "listening", title: "Listening mavzulari", context: `Mavzular: ${(data ?? []).map(topic => topic.title).join(" | ")}`, focusText: "", route: "/listening", stage: "catalog" }), [data]);

  return (
    <div className="listening-library" data-pen-screen="36">
      <header className="listening-library__heading">
        <span className="listening-tag">LISTENING</span>
        <h1>Quloq soling.<br />Ma’noni tuting.</h1>
        <p>Qisqa audio. Haqiqiy tushunish.</p>
      </header>
      <label className="listening-library__search"><Search size={20} /><input type="search" placeholder="Mavzu qidiring..." aria-label="Listening mavzusini qidirish" value={query} onChange={event => setQuery(event.target.value)} /></label>
      <div className="listening-library__filters">
        <div className="listening-library__tabs" aria-label="Mavzu holati">
          {[["all", "Barchasi"], ["started", "Boshlangan"], ["done", "Tugatilgan"]].map(([value, label]) => <button key={value} type="button" aria-pressed={progressFilter === value} onClick={() => setProgressFilter(value)}>{label}</button>)}
        </div>
        <div className="listening-library__levels" aria-label={uz.listening.levelLabel}>
          <button type="button" aria-pressed={filter === "all"} onClick={() => setFilter("all")}>{uz.listening.allLevels}</button>
          {LEVELS.map(level => <button key={level} type="button" aria-pressed={filter === level || filter === null && learnerLevel === level} onClick={() => setFilter(level)}>{cefrShort(level)}</button>)}
        </div>
      </div>
      {loading || !ready ? <ModulePageLoader icon="headphones" accent="teal" embedded />
        : error ? <section className="listening-library__state" role="alert"><h2>{uz.common.error}</h2><p>Audio mavzularni hozir yuklab bo‘lmadi.</p><button type="button" onClick={reload}>{uz.listening.retry}</button></section>
        : visible.length === 0 ? <section className="listening-library__state" aria-live="polite"><h2>Audio mavzular topilmadi</h2><p>{uz.listening.empty}</p></section>
        : <section className="listening-library__grid" aria-label="Listening mavzulari">
          {visible.map(topic => {
            const gate = gateOf(topic.topicId);
            const locked = !gatesReady || gate.isLocked;
            const proLocked = !locked && gate.requiresPro;
            const completed = Math.min(gate.passedModuleCount, gate.requiredModuleCount);
            return <button type="button" key={topic.topicId} disabled={locked || proLocked} className={`listening-library__card${locked || proLocked ? " is-locked" : ""}`} onClick={() => navigate(`/listening/topic/${topic.topicId}`)}>
              <span className="listening-library__cover"><TopicImage topicId={topic.topicId} title={topic.title} level={topic.level} category={topic.category} hideLevelBadge hideTitle className="h-full w-full" />{(locked || proLocked) && <span className="listening-library__lock"><Icon name={locked ? "lock" : "workspace_premium"} /></span>}</span>
              <span className="listening-library__copy"><span className="listening-tag">{cefrShort(topic.level)} · AUDIO</span><h2>{topic.title}</h2><span className="listening-library__translation">{topic.titleUz}</span>
                <span className="listening-library__progress">{gate.requiredModuleCount > 0 ? Math.round(completed / gate.requiredModuleCount * 100) : 0}% · {completed}/{gate.requiredModuleCount}</span>
                <span className="listening-library__action">{locked ? uz.skillTopic.lockedHint : proLocked ? uz.skillTopic.proHint : gate.passed ? "Takrorlash" : completed > 0 ? "Davom etish" : "Ko‘rish"}<Icon name={locked ? "lock" : "arrow_forward"} /></span>
              </span>
            </button>;
          })}
        </section>}
    </div>
  );
}
