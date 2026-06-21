import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import type { FreeTalkTopicDto } from "@/api/types";
import { getStoredLevel } from "@/app/session";
import { TopicImage } from "@/components/TopicImage";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { useAsync } from "@/lib/useAsync";
import { cefrShort } from "@/lib/labels";
import { cn } from "@/lib/cn";
import "./FreeTalkTopicsPage.css";
import "@/components/catalog/CatalogTheme.css";

const FILTER_LEVELS: CefrLevel[] = [
  CefrLevel.A1,
  CefrLevel.A2,
  CefrLevel.B1,
  CefrLevel.B2,
  CefrLevel.C1,
  CefrLevel.C2,
];

type LevelFilter = CefrLevel | "all" | undefined;

export function FreeTalkTopicsPage() {
  const navigate = useNavigate();
  const storedLevel = (getStoredLevel() as CefrLevel | null) ?? null;
  const [filter, setFilter] = useState<LevelFilter>(storedLevel ?? undefined);

  const { data, loading, error } = useAsync(
    () => api.speaking.freeTalkTopics(filter === "all" ? undefined : filter, filter === "all"),
    [filter],
  );

  const groups = useMemo(() => {
    const topics = Array.isArray(data) ? data : [];
    const grouped = new Map<string, FreeTalkTopicDto[]>();
    for (const topic of topics) {
      const key = cefrShort(topic.level);
      const levelTopics = grouped.get(key) ?? [];
      levelTopics.push(topic);
      grouped.set(key, levelTopics);
    }
    return [...grouped.entries()];
  }, [data]);
  const topicCount = groups.reduce((total, [, topics]) => total + topics.length, 0);
  const activeLevel = filter === "all" ? uz.speaking.allLevels : cefrShort(filter ?? storedLevel ?? CefrLevel.A1);

  function topicLabel(code: string, fallback: string): string {
    return uz.speaking.freeTalkTopics[code] ?? fallback;
  }

  function pickTopic(topic: FreeTalkTopicDto) {
    navigate(`/app/speaking/free-talk/${encodeURIComponent(topic.code)}`);
  }

  return (
    <div className="free-talk-catalog">
      <button type="button" className="free-talk-catalog__back" onClick={() => navigate("/app/speaking")}>
        <Icon name="arrow_back" />
        Orqaga
      </button>
      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 220, damping: 24 }}
        className="free-talk-catalog__hero"
      >
        <div className="free-talk-catalog__hero-copy">
          <span className="free-talk-catalog__eyebrow">
            <Icon name="graphic_eq" filled className="text-[18px]" />
            Erkin suhbat katalogi
          </span>
          <h1>{uz.speaking.freeTalkHeading}</h1>
          <p>{uz.speaking.freeTalkPageSubtitle}</p>
          <div className="free-talk-catalog__hero-metrics" aria-label="Erkin suhbat katalogi holati">
            <span><strong>{topicCount}</strong> mavzu</span>
            <span><strong>{groups.length}</strong> daraja</span>
            <span><strong>{activeLevel}</strong> tanlangan</span>
          </div>
        </div>

        <div className="free-talk-catalog__hero-mark" aria-hidden="true">
          <Icon name="graphic_eq" filled />
          <span>{activeLevel}</span>
        </div>
      </motion.section>

      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.08, type: "spring", stiffness: 260, damping: 22 }}
        className="free-talk-catalog__filters"
        aria-labelledby="free-talk-level-filter"
      >
        <div className="free-talk-catalog__filter-heading">
          <div>
            <span className="free-talk-catalog__section-kicker">Moslashtirish</span>
            <h2 id="free-talk-level-filter">{uz.speaking.levelLabel}</h2>
          </div>
          <span className="free-talk-catalog__filter-hint">O‘zingizga qulay bosqichni tanlang</span>
        </div>

        <div className="free-talk-catalog__filter-list" role="group" aria-label={uz.speaking.levelLabel}>
          <LevelPill active={filter === "all"} label={uz.speaking.allLevels} onClick={() => setFilter("all")} />
          {FILTER_LEVELS.map((level) => (
            <LevelPill
              key={level}
              active={filter === level}
              label={cefrShort(level)}
              onClick={() => setFilter(level)}
            />
          ))}
        </div>
      </motion.section>

      {loading ? (
        <CatalogState tone="loading">
          <ModulePageLoader icon="graphic_eq" accent="purple" embedded />
        </CatalogState>
      ) : error ? (
        <CatalogState tone="error" icon="cloud_off" title="Mavzularni yuklab bo‘lmadi. Qayta urinib ko‘ring." />
      ) : !data || data.length === 0 ? (
        <CatalogState tone="empty" icon="forum" title={uz.speaking.freeTalkEmpty} />
      ) : (
        <div className="free-talk-catalog__groups">
          {groups.map(([heading, topics]) => (
            <section key={heading} className="free-talk-catalog__group">
              <div className="free-talk-catalog__group-heading">
                <div>
                  <span className="free-talk-catalog__section-kicker">CEFR bosqichi</span>
                  <h2>{heading} mavzulari</h2>
                </div>
                <span>{topics.length} ta mavzu</span>
              </div>

              <div className="free-talk-catalog__grid">
                {topics.map((topic, topicIndex) => (
                  <TopicCard
                    key={topic.code}
                    topic={topic}
                    title={topicLabel(topic.code, topic.englishTitle)}
                    index={topicIndex}
                    onClick={() => pickTopic(topic)}
                  />
                ))}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}

function LevelPill({ active, label, onClick }: { active: boolean; label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={cn("free-talk-catalog__level-pill", active && "is-active")}
    >
      {label}
    </button>
  );
}

function TopicCard({
  topic,
  title,
  index,
  onClick,
}: {
  topic: FreeTalkTopicDto;
  title: string;
  index: number;
  onClick: () => void;
}) {
  return (
    <motion.button
      type="button"
      onClick={onClick}
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: Math.min(index * 0.035, 0.2), duration: 0.28 }}
      className="free-talk-catalog__card"
    >
      <span className="free-talk-catalog__card-visual">
        <TopicImage
          topicId={topic.imageId}
          title={title}
          level={topic.level}
          category="free-talk"
          hideLevelBadge
          className="h-full w-full"
        />
      </span>

      <span className="free-talk-catalog__card-body">
        <span className="free-talk-catalog__card-meta">
          <span className="free-talk-catalog__level-badge">{cefrShort(topic.level)}</span>
          <span className="free-talk-catalog__focus-badge">
            <Icon name="record_voice_over" />
            Nutq
          </span>
        </span>
        <h3>{title}</h3>
        <span className="free-talk-catalog__translation">Erkin suhbat mavzusi</span>
        <span className="free-talk-catalog__focus-name">Mavzu bo‘yicha erkin gapiring va nutqingizni mashq qiling.</span>
        <span className="free-talk-catalog__card-cta">
          Suhbatni boshlash
          <Icon name="arrow_forward" className="text-[18px]" />
        </span>
      </span>
    </motion.button>
  );
}

function CatalogState({
  tone,
  icon,
  title,
  children,
}: {
  tone: "loading" | "error" | "empty";
  icon?: string;
  title?: string;
  children?: React.ReactNode;
}) {
  return (
    <section className={cn("free-talk-catalog__state", `free-talk-catalog__state--${tone}`)} aria-live="polite">
      {children ?? (
        <>
          {icon ? <Icon name={icon} className="text-[32px]" /> : null}
          {title ? <p>{title}</p> : null}
        </>
      )}
    </section>
  );
}
