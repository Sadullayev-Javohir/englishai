import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { TopicImage } from "@/components/TopicImage";
import { useSkillTopicGates } from "@/lib/skillTopicGates";
import { CefrLevel } from "@/api/types";
import "./WritingCatalogPage.css";
import { publishAssistantContext } from "@/components/assistantContext";
import "@/components/catalog/CatalogTheme.css";

interface WritingTaskState {
  topicTitle?: string;
  vocabularyTopicId?: string;
}

const LEVELS: CefrLevel[] = [
  CefrLevel.A1,
  CefrLevel.A2,
  CefrLevel.B1,
  CefrLevel.B2,
  CefrLevel.C1,
  CefrLevel.C2,
];

type LevelFilter = CefrLevel | "all" | "completed" | null;

const LEVEL_LABEL: Record<CefrLevel, string> = {
  [CefrLevel.A1]: "A1",
  [CefrLevel.A2]: "A2",
  [CefrLevel.B1]: "B1",
  [CefrLevel.B2]: "B2",
  [CefrLevel.C1]: "C1",
  [CefrLevel.C2]: "C2",
};


export function WritingCatalogPage() {
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const location = useLocation();
  const topicState = (location.state as WritingTaskState | null) ?? undefined;
  const [filter, setFilter] = useState<LevelFilter>(null);
  const [query, setQuery] = useState("");
  const { data, loading, error, reload } = useAsync(
    () =>
      filter === "all" || filter === "completed"
        ? api.writing.catalog(learnerId, undefined, true)
        : api.writing.catalog(learnerId, filter ?? undefined),
    [learnerId, filter],
  );

  const { gateOf, ready: gatesReady } = useSkillTopicGates(
    learnerId,
    filter === "all" || filter === "completed" || filter === null ? undefined : filter,
    "Writing",
    filter === "all" || filter === "completed",
  );

  const topics = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase();
    return (data ?? []).filter((topic) => {
      if (filter === "completed" && !gateOf(topic.topicId).isMastered) return false;
      if (filter !== "all" && filter !== "completed" && filter !== null && topic.level !== filter) return false;
      if (!normalizedQuery) return true;
      return [topic.title, topic.titleUz, topic.category]
        .some((value) => value.toLocaleLowerCase().includes(normalizedQuery));
    });
  }, [data, filter, gateOf, query]);

  const progress = topics.reduce(
    (summary, topic) => {
      const gate = gateOf(topic.topicId);
      if (gate.passed) summary.done += 1;
      if (!gate.isLocked) summary.available += 1;
      return summary;
    },
    { done: 0, available: 0 },
  );
  const learnerLevel = topics[0]?.level ?? CefrLevel.A1;

  useEffect(() => publishAssistantContext({
    area: "writing",
    title: "Writing mavzulari",
    context: `Mavzular: ${topics.map((topic) => topic.title).join(" | ")}`,
    focusText: "",
    route: "/writing",
    stage: "catalog",
  }), [topics]);

  return (
    <div className="writing-catalog">
      <motion.header
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 220, damping: 24 }}
        className="writing-catalog__hero"
      >
        <div className="writing-catalog__hero-copy">
          <span className="writing-catalog__eyebrow">
            <Icon name="edit_note" filled />
            {uz.nav.writing}
          </span>
          <h1>{uz.writing.title}</h1>
          <p>{uz.writing.subtitle}</p>
          {topicState?.topicTitle ? (
            <p className="writing-catalog__topic-link">
              <Icon name="link" />
              {uz.writing.forTopic(topicState.topicTitle)}
            </p>
          ) : null}
          <div className="writing-catalog__hero-metrics" aria-label="Yozuv katalogi holati">
            <span><strong>{topics.length}</strong> topshiriq</span>
            <span><strong>{progress.available}</strong> ochiq</span>
            <span><strong>{progress.done}</strong> tugatilgan</span>
          </div>
        </div>
        <div className="writing-catalog__hero-mark" aria-hidden="true">
          <Icon name="draw" filled />
          <span>{LEVEL_LABEL[learnerLevel]}</span>
        </div>
      </motion.header>

      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.08, type: "spring", stiffness: 260, damping: 22 }}
        className="writing-catalog__filters"
        aria-labelledby="writing-level-filter"
      >
        <label className="writing-catalog__search" aria-label="Yozuv mavzusini qidiring">
          <Icon name="search" />
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Mavzuni qidiring…"
            type="search"
          />
        </label>
        <div className="writing-catalog__filter-heading">
          <h2 id="writing-level-filter" className="sr-only">{uz.writing.levelLabel}</h2>
        </div>
        <div className="writing-catalog__filter-list" role="group" aria-label={uz.writing.levelLabel}>
          <LevelPill active={filter === null} onClick={() => setFilter(null)} label={LEVEL_LABEL[learnerLevel]} />
          <LevelPill active={filter === "all"} onClick={() => setFilter("all")} label={uz.writing.allLevels} />
          <LevelPill active={filter === "completed"} onClick={() => setFilter("completed")} label="Tugatilgan" />
          {filter === "all" ? LEVELS.map((level) => (
            <LevelPill key={level} active={false} onClick={() => setFilter(level)} label={LEVEL_LABEL[level]} />
          )) : null}
        </div>
      </motion.section>

      {loading && !data ? (
        <section className="writing-catalog__state writing-catalog__state--loading" aria-live="polite">
          <ModulePageLoader icon="edit_note" accent="orange" embedded />
          <p>Topshiriqlar yuklanmoqda...</p>
        </section>
      ) : error ? (
        <section className="writing-catalog__state" aria-live="polite">
          <span className="writing-catalog__state-icon"><Icon name="wifi_off" /></span>
          <h2>Topshiriqlar yuklanmadi</h2>
          <p>{uz.writing.catalogLoadError}</p>
          <button type="button" onClick={reload}>{uz.common.retry}</button>
        </section>
      ) : topics.length === 0 ? (
        <section className="writing-catalog__state" aria-live="polite">
          <span className="writing-catalog__state-icon"><Icon name="edit_note" /></span>
          <h2>Topshiriqlar topilmadi</h2>
          <p>{uz.writing.catalogEmpty}</p>
          <button type="button" onClick={() => setFilter("all")}>Barcha darajalarni ko‘rish</button>
        </section>
      ) : (
        <motion.section
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.12, type: "spring", stiffness: 260, damping: 22 }}
        >
          <div className="writing-catalog__section-heading">
            <div>
              <span className="writing-catalog__section-kicker">WRITING</span>
              <h2>Yozuv topshiriqlari</h2>
            </div>
            <span>{topics.length} ta topshiriq</span>
          </div>
          <div className="writing-catalog__grid">
            {topics.map((task, index) => {
              const gate = gateOf(task.topicId);
              const locked = !gatesReady || gate.isLocked;
              const proLocked = !locked && gate.requiresPro;
              const completedModules = Math.min(gate.passedModuleCount, gate.requiredModuleCount);
              const topicProgress = gate.requiredModuleCount > 0
                ? Math.round((completedModules / gate.requiredModuleCount) * 100)
                : 0;
              return (
                <motion.button
                  key={task.topicId}
                  type="button"
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.12 + index * 0.025, type: "spring", stiffness: 260, damping: 22 }}
                  disabled={locked}
                  onClick={() => {
                    if (locked || proLocked) return;
                    navigate(`/writing/task/${task.topicId}`, {
                      state: { vocabularyTopicId: task.topicId, topicTitle: task.title, lessonOrigin: "catalog" },
                    });
                  }}
                  className={`writing-catalog__card${locked ? " is-locked" : ""}${proLocked ? " is-pro" : ""}`}
                >
                  {proLocked ? (
                    <span className="writing-catalog__pro-badge">
                      <Icon name="workspace_premium" filled />
                      {uz.skillTopic.proBadge}
                    </span>
                  ) : null}
                  <span className="writing-catalog__card-image">
                    <TopicImage topicId={task.topicId} title={task.title} level={task.level} category={task.category} hideLevelBadge className="h-full w-full" />
                    {locked ? (
                      <span className="writing-catalog__lock-overlay">
                        <span className="writing-catalog__lock-icon"><Icon name="lock" filled /></span>
                      </span>
                    ) : null}
                  </span>
                  <span className="writing-catalog__card-body">
                    <span className="writing-catalog__card-meta">
                      <span className="writing-catalog__level-badge">{LEVEL_LABEL[task.level]}</span>
                      <span className="writing-catalog__focus-badge"><Icon name="edit_note" />Yozuv</span>
                    </span>
                    <h3>{task.title}</h3>
                    <span className="writing-catalog__translation">{task.titleUz}</span>
                    <span className="writing-catalog__focus-name">{task.category}</span>
                    <span className="writing-catalog__badges">
                      <span className={`writing-catalog__status-badge${gate.isMastered ? " is-complete" : ""}`}>
                        <Icon name={gate.isMastered ? "verified" : "trending_up"} filled={gate.isMastered} />
                        {topicProgress}%
                      </span>
                      <span className="writing-catalog__module-count">{completedModules}/{gate.requiredModuleCount}</span>
                    </span>
                    <span className="writing-catalog__progress" aria-label={`Mavzu progressi ${topicProgress}%`}>
                      <span style={{ width: `${topicProgress}%` }} />
                    </span>
                  </span>
                </motion.button>
              );
            })}
          </div>
        </motion.section>
      )}

    </div>
  );
}

function LevelPill({
  active,
  onClick,
  label,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`writing-catalog__level-pill${active ? " is-active" : ""}`}
      aria-pressed={active}
    >
      {label}
    </button>
  );
}
