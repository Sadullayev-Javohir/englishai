import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import type { VocabularyTopicSummaryDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { usePaywall } from "@/components/PaywallProvider";
import { TopicImage } from "@/components/TopicImage";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { uz } from "@/content/uz";
import { accentFor, CARD_STYLE, type CardAccent } from "@/lib/cardPalette";
import { cn } from "@/lib/cn";
import { cefrShort } from "@/lib/labels";
import { useAsync } from "@/lib/useAsync";
import "./SpeakingPage.css";
import "@/components/catalog/CatalogTheme.css";

const FILTER_LEVELS: CefrLevel[] = [
  CefrLevel.A1,
  CefrLevel.A2,
  CefrLevel.B1,
  CefrLevel.B2,
  CefrLevel.C1,
  CefrLevel.C2,
];

type LevelFilter = CefrLevel | "all";

function categoryLabel(category: string): string {
  const spaced = category.replace(/_/g, " ");
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
}

export function SpeakingTopicsPage() {
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const [filter, setFilter] = useState<LevelFilter>("all");

  useEffect(() => {
    const pageRoots = [document.documentElement, document.body, document.getElementById("root")].filter(
      (element): element is HTMLElement => Boolean(element),
    );
    pageRoots.forEach((element) => element.classList.add("speaking-hub-page-active"));
    return () => pageRoots.forEach((element) => element.classList.remove("speaking-hub-page-active"));
  }, []);

  const { data, loading, error, reload } = useAsync(
    () =>
      filter === "all"
        ? api.vocabulary.topics(learnerId, undefined, true)
        : api.vocabulary.topics(learnerId, filter),
    [learnerId, filter],
  );

  const groups = useMemo(() => {
    const map = new Map<string, VocabularyTopicSummaryDto[]>();
    for (const topic of data ?? []) {
      const key = filter === "all" ? cefrShort(topic.level) : categoryLabel(topic.category);
      const list = map.get(key) ?? [];
      list.push(topic);
      map.set(key, list);
    }
    return [...map.entries()];
  }, [data, filter]);

  return (
    <div className="sp17 sp17--hub sp17--topics-catalog">
      <div className="sp17__content">
        <motion.div
          initial={{ opacity: 0, y: 18 }}
          animate={{ opacity: 1, y: 0 }}
          className="sp17__topics-heading"
        >
          <div>
            <span className="speaking-tag">
              <Icon name="chat_bubble" filled className="text-[17px]" />
              Mavzuli suhbat
            </span>
            <h1>{uz.speaking.topicTitle}</h1>
            <p>O‘rgangan so‘zlaringiz bo‘yicha mavzuni tanlang va AI tutor bilan suhbatni boshlang.</p>
          </div>
          <span className="sp17__topics-count">{filter === "all" ? "A1–C2" : cefrShort(filter)} · {data?.length ?? 0} ta mavzu</span>
        </motion.div>

        <section className="sp17__filters" aria-labelledby="speaking-level-filter">
          <div className="sp17__filter-heading">
            <div>
              <span className="sp17__section-kicker">Moslashtirish</span>
              <h2 id="speaking-level-filter">{uz.speaking.levelLabel}</h2>
            </div>
          </div>
          <div className="sp17__filter-list" role="group" aria-label={uz.speaking.levelLabel}>
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
        </section>

        {loading && !data ? (
          <ModulePageLoader icon="mic" accent="red" embedded />
        ) : error ? (
          <div className="sp17__state">
            <p>{uz.common.error}</p>
            <Button variant="ghost" icon="refresh" onClick={reload}>
              {uz.speaking.error.retry}
            </Button>
          </div>
        ) : !data || data.length === 0 ? (
          <p className="sp17__state">{uz.speaking.topicsEmpty}</p>
        ) : (
          <div className="space-y-lg md:space-y-xl">
            {groups.map(([heading, topics], groupIndex) => (
              <section key={heading}>
                <h2 className="sp17__group-title">{heading}</h2>
                <div className="sp17__topic-grid grid grid-cols-1 gap-sm min-[420px]:grid-cols-2 md:gap-md xl:grid-cols-3">
                  {topics.map((topic, topicIndex) => {
                    const speakingModule = topic.modules.find((module) => module.module === "Speaking");
                    const locked = topic.isLocked || speakingModule?.unlocked !== true;
                    return (
                      <SpeakingTopicTile
                        key={topic.id}
                        topic={topic}
                        accent={accentFor(groupIndex * 3 + topicIndex)}
                        locked={locked}
                        proLocked={!locked && topic.requiresPro}
                        passed={Boolean(speakingModule?.passed)}
                        disabled={locked}
                        onClick={() =>
                          navigate(`/app/speaking/topic/${encodeURIComponent(topic.id)}`, {
                            state: { topicTitle: topic.title },
                          })
                        }
                      />
                    );
                  })}
                </div>
              </section>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function LevelPill({ active, label, onClick }: { active: boolean; label: string; onClick: () => void }) {
  return (
    <button type="button" aria-pressed={active} onClick={onClick} className={cn("sp17__level-pill", active && "is-active")}>
      {label}
    </button>
  );
}

function SpeakingTopicTile({
  topic,
  accent,
  locked,
  proLocked,
  passed,
  disabled,
  onClick,
}: {
  topic: VocabularyTopicSummaryDto;
  accent: CardAccent;
  locked: boolean;
  proLocked: boolean;
  passed: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  const { open: openPaywall } = usePaywall();
  const completedModules = Math.min(topic.passedModuleCount, topic.requiredModuleCount);
  const progress = topic.requiredModuleCount > 0
    ? Math.round((completedModules / topic.requiredModuleCount) * 100)
    : 0;
  const footer = locked ? (
    <span className="sp17__topic-status">
      <Icon name="lock" filled className="text-[14px]" />
      {uz.skillTopic.lockedHint}
    </span>
  ) : proLocked ? (
    <span className="sp17__topic-status">
      <Icon name="workspace_premium" filled className="text-[14px]" />
      {uz.skillTopic.proHint}
    </span>
  ) : passed ? (
    <span className="sp17__topic-status">
      <Icon name="check_circle" filled className="text-[14px]" />
      {uz.skillTopic.done}
    </span>
  ) : null;

  return (
    <motion.button
      type="button"
      disabled={disabled}
      onClick={() => {
        if (disabled) return;
        if (proLocked) openPaywall();
        else onClick();
      }}
      initial={{ opacity: 0, y: 18 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ type: "spring", stiffness: 260, damping: 22 }}
      className={cn(
        "group relative text-left rounded-[20px] overflow-hidden border-2 border-white/25 transition-all",
        CARD_STYLE[accent].face,
        CARD_STYLE[accent].lip,
        disabled ? "opacity-90" : "hover:brightness-105 ",
      )}
    >
      <div className="sp17__topic-image">
        <TopicImage
          topicId={topic.id}
          title={topic.title}
          level={topic.level}
          hideLevelBadge
          className="h-full w-full object-cover"
        />
        <span className="sp17__topic-level">{cefrShort(topic.level)}</span>
      </div>
      <div className="sp17__topic-body">
        <span className="sp17__topic-icon" aria-hidden="true">
          <Icon name={locked ? "lock" : proLocked ? "workspace_premium" : "record_voice_over"} filled />
        </span>
        <span className="sp17__topic-label">Speaking mavzusi</span>
        <h3>{topic.title}</h3>
        <p>{topic.titleUz}</p>
        <span className="sp17__topic-progress-meta">
          <span><Icon name="trending_up" />{progress}%</span>
          <span>{completedModules}/{topic.requiredModuleCount}</span>
        </span>
        <span className="sp17__topic-progress" aria-label={`Mavzu progressi ${progress}%`}>
          <span style={{ width: `${progress}%` }} />
        </span>
        {footer}
      </div>
    </motion.button>
  );
}
