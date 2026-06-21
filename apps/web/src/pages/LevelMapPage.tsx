import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { AdminRole, CefrLevel, LevelExitState } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { cn } from "@/lib/cn";
import { cefrLong, cefrShort } from "@/lib/labels";
import { buildLevelRoad, chapterLabel } from "./levelMap/chapters";
import { TopicRow } from "./levelMap/TopicRow";
import { FinishLine } from "./levelMap/FinishLine";
import "./levelMap/levelMap.css";

const LEVELS: CefrLevel[] = [
  CefrLevel.A1,
  CefrLevel.A2,
  CefrLevel.B1,
  CefrLevel.B2,
  CefrLevel.C1,
  CefrLevel.C2,
];

const LEVEL_DESCRIPTIONS: Partial<Record<CefrLevel, string>> = {
  [CefrLevel.A1]: "O‘zingiz va oilangiz haqida gapirishni o‘rganing.",
  [CefrLevel.A2]: "Kundalik hayotdagi tanish mavzularda erkinroq gapiring.",
  [CefrLevel.B1]: "Fikrlaringizni aniqroq va ishonchliroq ifodalang.",
  [CefrLevel.B2]: "Murakkab mavzularni tushunib, muhokama qiling.",
  [CefrLevel.C1]: "Kasbiy va akademik vaziyatlarda ravon muloqot qiling.",
  [CefrLevel.C2]: "Ingliz tilidan chuqur va moslashuvchan foydalaning.",
};

function displayLevel(level: CefrLevel): string {
  return cefrLong(level).replace(" - ", " — ");
}

/**
 * Screen 62 /levels: focus the first view on the learner's next chapter.
 * Remaining chapters stay available in the adjacent guide instead of becoming
 * a dense, generic full-map list.
 */
export function LevelMapPage() {
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const [level, setLevel] = useState<CefrLevel | undefined>(undefined);
  const [chapterOverride, setChapterOverride] = useState<{
    level: CefrLevel | undefined;
    key: string | null;
  } | null>(null);
  const [topicOverride, setTopicOverride] = useState<{
    level: CefrLevel | undefined;
    id: string | null;
  } | null>(null);
  const [showMoreChapters, setShowMoreChapters] = useState(false);

  useEffect(() => {
    document.documentElement.classList.add("levels-page-active");
    document.body.classList.add("levels-page-active");
    document.getElementById("root")?.classList.add("levels-page-active");

    return () => {
      document.documentElement.classList.remove("levels-page-active");
      document.body.classList.remove("levels-page-active");
      document.getElementById("root")?.classList.remove("levels-page-active");
    };
  }, []);

  const { data, loading, error } = useAsync(
    () => api.levels.map(learnerId, level),
    [learnerId, level],
  );
  const { data: adminAccess } = useAsync(() => api.admin.access(), []);
  const isSuperAdmin = adminAccess?.role === AdminRole.SuperAdmin;

  const activeLevel = level ?? data?.level;
  const topics = useMemo(() => data?.topics ?? [], [data?.topics]);
  const road = useMemo(() => buildLevelRoad(topics), [topics]);
  const activeTopic = road.activeIndex >= 0 ? topics[road.activeIndex] : undefined;
  const activeChapterKey = useMemo(() => {
    const holder = road.chapters.find(
      (chapter) =>
        road.activeIndex >= chapter.startIndex &&
        road.activeIndex < chapter.startIndex + chapter.topics.length,
    );
    return holder?.category ?? road.chapters[0]?.category ?? null;
  }, [road]);
  const matchingChapterOverride =
    chapterOverride !== null && chapterOverride.level === activeLevel
      ? chapterOverride
      : null;
  const openChapterKey = matchingChapterOverride?.key ?? activeChapterKey;
  const openChapter =
    road.chapters.find((chapter) => chapter.category === openChapterKey) ??
    road.chapters[0];
  const matchingTopicOverride =
    topicOverride !== null && topicOverride.level === activeLevel
      ? topicOverride
      : null;
  // `null` is an intentional override: it means the learner collapsed the
  // active topic's six-step card. Do not fall back to the active topic in
  // that case, otherwise the panel would immediately re-open.
  const openTopicId = matchingTopicOverride
    ? matchingTopicOverride.id
    : activeTopic?.id ?? openChapter?.topics[0]?.id ?? null;
  const progressPct =
    data && data.topicsTotal > 0
      ? Math.round((data.topicsMastered / data.topicsTotal) * 100)
      : 0;
  const futureChapters = openChapter
    ? road.chapters.filter((chapter) => chapter.category !== openChapter.category)
    : [];
  const visibleFutureChapters = showMoreChapters
    ? futureChapters
    : futureChapters.slice(0, 4);

  function selectLevel(nextLevel: CefrLevel) {
    setLevel(nextLevel);
    setChapterOverride(null);
    setTopicOverride(null);
    setShowMoreChapters(false);
  }

  function selectChapter(category: string) {
    const nextChapter = road.chapters.find((chapter) => chapter.category === category);
    setChapterOverride({ level: activeLevel, key: category });
    setTopicOverride({ level: activeLevel, id: nextChapter?.topics[0]?.id ?? null });
  }

  return (
    <div className="lvmap">
      <div className="lvmap-container">
        {loading ? (
          <LoadingSkeleton variant="list" rows={7} />
        ) : error || !data ? (
          <p className="lvmap-error">Ma&apos;lumotlarni yuklab bo&apos;lmadi.</p>
        ) : (
          <div className="lvmap-stack">
            <header className="lvmap-heading">
              <span className="lvmap-eyebrow">O‘QUV YO‘LI</span>
              <h1>Har kuni bir qadam.</h1>
              <p>Mavzuni 6 ta ko‘nikma bilan to‘liq o‘zlashtiring.</p>
            </header>

            <section
              className="lvmap-levels"
              aria-label="CEFR darajalari"
              data-testid="level-selector-grid"
            >
              {LEVELS.map((candidate) => {
                const currentLevel = data.currentLevel ?? data.level;
                const isActive = activeLevel === candidate;
                const isUnlocked = Boolean(data.hasFullAccess) || candidate === currentLevel;
                return (
                  <button
                    key={candidate}
                    type="button"
                    disabled={!isUnlocked}
                    onClick={() => isUnlocked && selectLevel(candidate)}
                    aria-label={`${cefrLong(candidate)} - ${
                      isUnlocked ? "darajani tanlash" : "Bu daraja qulflangan"
                    }`}
                    className={cn(
                      "lvmap-level-pill",
                      isActive && "is-active",
                      !isUnlocked && "is-locked",
                    )}
                  >
                    {cefrShort(candidate)}
                  </button>
                );
              })}
            </section>

            <section className="lvmap-progress-card" aria-label="Joriy daraja rivojlanishi">
              <div className="lvmap-progress-card__meta">
                <span>JORIY DARAJA</span>
                <strong>
                  {data.topicsMastered} / {data.topicsTotal} mavzu
                </strong>
              </div>
              <h2>{displayLevel(data.level)}</h2>
              <p>{LEVEL_DESCRIPTIONS[data.level] ?? "Keyingi bosqichga izchil tayyorlaning."}</p>
              <div
                className="lvmap-progress-bar"
                role="progressbar"
                aria-label="Daraja rivojlanishi"
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={progressPct}
              >
                <i style={{ width: `${progressPct}%` }} />
              </div>
              <small>
                {progressPct}% yakunlandi
                {activeTopic ? ` · keyingi mavzu: ${activeTopic.title}` : ""}
              </small>
            </section>

            {road.chapters.length === 0 || !openChapter ? (
              <div className="lvmap-empty">
                <Icon name="route" className="text-[32px]" />
                <p>Bu daraja uchun mavzular topilmadi.</p>
              </div>
            ) : (
              <section className="lvmap-road-grid" aria-label="Daraja o‘quv yo‘li">
                <section
                  className={cn(
                    "lvmap-current-chapter",
                    `lvmap-current-chapter--${openChapter.state}`,
                  )}
                >
                  <header className="lvmap-current-chapter__heading">
                    <h2>
                      {String(openChapter.position).padStart(2, "0")} · {chapterLabel(openChapter.category)}
                    </h2>
                  </header>
                  <ol className="lvmap-chapter__rows">
                    {openChapter.topics.map((topic, index) => {
                      const levelIndex = openChapter.startIndex + index;
                      const state = topic.isMastered
                        ? "mastered"
                        : topic.isLocked
                          ? "locked"
                          : levelIndex === road.activeIndex
                            ? "active"
                            : "upcoming";
                      return (
                        <TopicRow
                          key={topic.id}
                          topic={topic}
                          index={levelIndex}
                          state={state}
                          expanded={openTopicId === topic.id}
                          onToggle={() =>
                            setTopicOverride({
                              level: activeLevel,
                              id: openTopicId === topic.id ? null : topic.id,
                            })
                          }
                        />
                      );
                    })}
                  </ol>
                </section>

                <aside className="lvmap-guide">
                  <div className="lvmap-guide__card">
                    <header className="lvmap-guide__heading">
                      <span>DAVOMI</span>
                      <h2>Keyingi bo‘limlar</h2>
                    </header>
                    <div className="lvmap-guide__list">
                      {visibleFutureChapters.map((chapter) => (
                        <button
                          key={chapter.category}
                          type="button"
                          className="lvmap-guide-row"
                          onClick={() => selectChapter(chapter.category)}
                        >
                          <span className="lvmap-guide-row__number">
                            {String(chapter.position).padStart(2, "0")}
                          </span>
                          <span>
                            <strong>{chapterLabel(chapter.category)}</strong>
                            <small>
                              {chapter.topics.length} mavzudan {chapter.masteredCount} tasi
                            </small>
                          </span>
                          <Icon name="chevron_right" className="text-[18px]" />
                        </button>
                      ))}
                    </div>
                    {futureChapters.length > 4 && (
                      <button
                        type="button"
                        className="lvmap-guide__more"
                        onClick={() => setShowMoreChapters((visible) => !visible)}
                      >
                        {showMoreChapters
                          ? "Kamroq ko‘rsatish"
                          : `Yana ${futureChapters.length - 4} bo‘lim`}
                        <Icon name={showMoreChapters ? "expand_less" : "expand_more"} />
                      </button>
                    )}
                  </div>

                  <FinishLine
                    exitState={data.exitState}
                    readiness={data.readiness}
                    superAdminPreview={isSuperAdmin && data.exitState !== LevelExitState.MaxLevel}
                    onStart={() =>
                      navigate(
                        isSuperAdmin && activeLevel && activeLevel < CefrLevel.C2
                          ? `/levels/exit-test?level=${activeLevel}`
                          : "/levels/exit-test",
                      )
                    }
                  />
                </aside>
              </section>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
