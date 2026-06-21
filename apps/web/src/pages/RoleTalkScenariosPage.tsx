import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import type { RoleplayScenarioDto } from "@/api/types";
import { getStoredLevel } from "@/app/session";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { TopicImage } from "@/components/TopicImage";
import { useAsync } from "@/lib/useAsync";
import { cefrShort } from "@/lib/labels";
import { cn } from "@/lib/cn";
import "./RoleTalkScenariosPage.css";
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

export function RoleTalkScenariosPage() {
  const navigate = useNavigate();
  const storedLevel = (getStoredLevel() as CefrLevel | null) ?? null;
  const [filter, setFilter] = useState<LevelFilter>(storedLevel ?? undefined);

  useEffect(() => {
    const pageRoots = [document.documentElement, document.body, document.getElementById("root")].filter(
      (element): element is HTMLElement => Boolean(element),
    );
    pageRoots.forEach((element) => element.classList.add("role-talk-page-active"));
    return () => pageRoots.forEach((element) => element.classList.remove("role-talk-page-active"));
  }, []);

  const { data, loading, error } = useAsync(
    () => api.speaking.roleplayScenarios(filter === "all" ? undefined : filter, filter === "all"),
    [filter],
  );

  const groups = useMemo(() => {
    const scenarios = Array.isArray(data) ? data : [];
    const grouped = new Map<string, RoleplayScenarioDto[]>();
    for (const scenario of scenarios) {
      const key = cefrShort(scenario.level);
      const levelScenarios = grouped.get(key) ?? [];
      levelScenarios.push(scenario);
      grouped.set(key, levelScenarios);
    }
    return [...grouped.entries()];
  }, [data]);
  const scenarios = Array.isArray(data) ? data : [];
  const selectedLevel = filter === "all" ? uz.speaking.allLevels : cefrShort(filter ?? storedLevel ?? CefrLevel.A1);

  function pickScenario(scenario: RoleplayScenarioDto) {
    navigate(`/app/speaking/role-talk/${encodeURIComponent(scenario.code)}`);
  }

  return (
    <div className="role-talk-catalog">
      <button type="button" className="role-talk-catalog__back" onClick={() => navigate("/app/speaking")}>
        <span className="role-talk-catalog__back-icon">
          <Icon name="arrow_back" />
        </span>
        {uz.common.back}
      </button>

      <motion.header
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 220, damping: 24 }}
        className="role-talk-catalog__hero"
      >
        <div className="role-talk-catalog__hero-copy">
          <span className="role-talk-catalog__eyebrow">
            <Icon name="theater_comedy" filled className="text-[18px]" />
            Rolli suhbat katalogi
          </span>
          <h1>{uz.speaking.roleplay.pickTitle}</h1>
          <p>{uz.speaking.roleplay.pickSubtitle}</p>
          <div className="role-talk-catalog__hero-metrics" aria-label="Rolli suhbat katalogi holati">
            <span><strong>{scenarios.length}</strong> senariy</span>
            <span><strong>{groups.length}</strong> daraja</span>
            <span><strong>{selectedLevel}</strong> tanlangan</span>
          </div>
        </div>

        <div className="role-talk-catalog__hero-mark" aria-hidden="true">
          <Icon name="theater_comedy" filled />
          <span>{selectedLevel}</span>
        </div>
      </motion.header>

      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.08, type: "spring", stiffness: 260, damping: 22 }}
        className="role-talk-catalog__filters"
        aria-labelledby="role-talk-level-filter"
      >
        <div className="role-talk-catalog__filter-heading">
          <div>
            <span className="role-talk-catalog__section-kicker">Moslashtirish</span>
            <h2 id="role-talk-level-filter">{uz.speaking.levelLabel}</h2>
          </div>
          <span className="role-talk-catalog__filter-hint">O‘zingizga qulay bosqichni tanlang</span>
        </div>

        <div className="role-talk-catalog__filter-list" role="group" aria-label={uz.speaking.levelLabel}>
          <LevelPill
            active={filter === "all"}
            label={uz.speaking.allLevels}
            onClick={() => setFilter("all")}
          />
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
          <ModulePageLoader icon="theater_comedy" accent="purple" embedded />
        </CatalogState>
      ) : error ? (
        <CatalogState tone="error" icon="cloud_off" title={uz.speaking.roleplay.loadError} />
      ) : !data || data.length === 0 ? (
        <CatalogState tone="empty" icon="event_busy" title={uz.speaking.roleplay.empty} />
      ) : (
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.12, type: "spring", stiffness: 260, damping: 22 }}
          className="role-talk-catalog__groups"
        >
          {groups.map(([heading, scenarios]) => (
            <section key={heading} className="role-talk-catalog__group">
              <div className="role-talk-catalog__group-heading">
                <div>
                  <span className="role-talk-catalog__section-kicker">CEFR bosqichi</span>
                  <h2>{heading} vaziyatlari</h2>
                </div>
                <span>{scenarios.length} ta senariy</span>
              </div>

              <div className="role-talk-catalog__grid">
                {scenarios.map((scenario, scenarioIndex) => {
                  const label = uz.speaking.roleplay.scenarios[scenario.code] ?? {
                    title: scenario.englishTitle,
                    description: "",
                  };
                  return (
                    <ScenarioCard
                      key={scenario.code}
                      scenario={scenario}
                      title={label.title}
                      description={label.description}
                      index={scenarioIndex}
                      onClick={() => pickScenario(scenario)}
                    />
                  );
                })}
              </div>
            </section>
          ))}
        </motion.div>
      )}
    </div>
  );
}

function LevelPill({
  active,
  label,
  onClick,
}: {
  active: boolean;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={cn("role-talk-catalog__level-pill", active && "is-active")}
    >
      {label}
    </button>
  );
}

function ScenarioCard({
  scenario,
  title,
  description,
  index,
  onClick,
}: {
  scenario: RoleplayScenarioDto;
  title: string;
  description: string;
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
      className="role-talk-catalog__card"
    >
      <span className="role-talk-catalog__card-image">
        <TopicImage
          topicId={scenario.imageId}
          title={title}
          level={scenario.level}
          category="roleplay"
          hideLevelBadge
          className="h-full w-full"
        />
        <span className="role-talk-catalog__level-badge">{cefrShort(scenario.level)}</span>
      </span>

      <span className="role-talk-catalog__card-body">
        <span className="role-talk-catalog__card-meta">
          <span className="role-talk-catalog__level-badge">{cefrShort(scenario.level)}</span>
          <span className="role-talk-catalog__focus-badge">
            <Icon name="record_voice_over" />
            Rolli suhbat
          </span>
        </span>
        <h3>{title}</h3>
        {description ? <span className="role-talk-catalog__card-description">{description}</span> : null}
        <span className="role-talk-catalog__card-cta">
          {uz.speaking.roleplay.start}
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
    <section className={cn("role-talk-catalog__state", `role-talk-catalog__state--${tone}`)} aria-live="polite">
      {children ?? (
        <>
          {icon ? <Icon name={icon} className="text-[32px]" /> : null}
          {title ? <p>{title}</p> : null}
        </>
      )}
    </section>
  );
}
