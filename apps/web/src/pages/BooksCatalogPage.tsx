import { useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import type { BookSummaryDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { tapLight } from "@/lib/haptics";
import { Icon } from "@/components/ui/Icon";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { BookCover } from "@/components/BookCover";
import { HomeBackButton } from "@/components/HomeBackButton";
import { cefrShort } from "@/lib/labels";
import { cn } from "@/lib/cn";
import "./BooksCatalogPage.css";
import "@/components/catalog/CatalogTheme.css";
import { publishAssistantContext } from "@/components/assistantContext";

const LEVELS: CefrLevel[] = [
  CefrLevel.A1,
  CefrLevel.A2,
  CefrLevel.B1,
  CefrLevel.B2,
  CefrLevel.C1,
  CefrLevel.C2,
];

type LevelFilter = CefrLevel | "all" | null;

export function BooksCatalogPage() {
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const [filter, setFilter] = useState<LevelFilter>(null);
  const { data, loading, error } = useAsync(
    () =>
      filter === "all"
        ? api.books.catalog(learnerId, undefined, true)
        : api.books.catalog(learnerId, filter ?? undefined),
    [learnerId, filter],
  );

  const books = useMemo(() => (Array.isArray(data) ? data : []), [data]);
  const learnerLevel = books[0]?.level ?? CefrLevel.A1;
  const selectedLevel = filter === "all" ? uz.books.allLevels : cefrShort(filter ?? learnerLevel);
  const completedBooks = books.filter((book) => book.isCompleted).length;
  const totalSections = books.reduce((total, book) => total + book.sectionCount, 0);
  const sectionsRead = books.reduce((total, book) => total + book.sectionsRead, 0);
  const groups = useMemo(() => {
    const grouped = new Map<string, BookSummaryDto[]>();
    for (const book of books) {
      const key = cefrShort(book.level);
      grouped.set(key, [...(grouped.get(key) ?? []), book]);
    }
    return [...grouped.entries()];
  }, [books]);

  useEffect(() => publishAssistantContext({
    area: "books",
    title: "Kutubxona",
    context: `Kitoblar: ${books.map((book) => `${book.title} — ${book.author} (${cefrShort(book.level)})`).join(" | ")}`,
    focusText: "",
    route: "/books",
    stage: "catalog",
  }), [books]);

  function openBook(book: BookSummaryDto) {
    tapLight();
    navigate(`/books/${book.id}`);
  }

  return (
    <div className="books-catalog" data-module="books">
      <HomeBackButton className="books-catalog__back" />

      <motion.header
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 220, damping: 24 }}
        className="books-catalog__hero"
      >
        <div className="books-catalog__hero-copy">
          <span className="books-catalog__eyebrow">
            <Icon name="auto_stories" filled />
            Kitoblar katalogi
          </span>
          <h1>{uz.books.title}</h1>
          <p>{uz.books.subtitle}</p>
          <div className="books-catalog__hero-metrics" aria-label="Kitoblar katalogi holati">
            <span><strong>{books.length}</strong> kitob</span>
            <span><strong>{sectionsRead}/{totalSections}</strong> bo‘lim</span>
            <span><strong>{completedBooks}</strong> tugatilgan</span>
          </div>
        </div>
        <div className="books-catalog__hero-mark" aria-hidden="true">
          <Icon name="menu_book" filled />
          <span>{selectedLevel}</span>
        </div>
      </motion.header>

      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.08, type: "spring", stiffness: 260, damping: 22 }}
        className="books-catalog__filters"
        aria-labelledby="books-level-filter"
      >
        <div className="books-catalog__filter-heading">
          <div>
            <span className="books-catalog__section-kicker">Moslashtirish</span>
            <h2 id="books-level-filter">{uz.books.levelLabel}</h2>
          </div>
          <span>Kitoblarni CEFR bosqichi bo‘yicha saralang</span>
        </div>
        <div className="books-catalog__filter-list" role="group" aria-label={uz.books.levelLabel}>
          <LevelPill active={filter === "all"} label={uz.books.allLevels} onClick={() => setFilter("all")} />
          {LEVELS.map((level) => (
            <LevelPill
              key={level}
              active={filter === level || (filter === null && learnerLevel === level)}
              label={cefrShort(level)}
              onClick={() => setFilter(level)}
            />
          ))}
        </div>
      </motion.section>

      {loading && !data ? (
        <CatalogState tone="loading">
          <ModulePageLoader icon="auto_stories" accent="orange" embedded />
          <p>Kitoblar yuklanmoqda...</p>
        </CatalogState>
      ) : error ? (
        <CatalogState tone="error" icon="cloud_off" title={uz.common.error} />
      ) : books.length === 0 ? (
        <CatalogState tone="empty" icon="menu_book" title={uz.books.empty} />
      ) : (
        <div className="books-catalog__groups">
          {groups.map(([heading, levelBooks]) => (
            <motion.section
              key={heading}
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.12, type: "spring", stiffness: 260, damping: 22 }}
              className="books-catalog__group"
            >
              <div className="books-catalog__section-heading">
                <div>
                  <span className="books-catalog__section-kicker">CEFR bosqichi</span>
                  <h2>{heading} kitoblari</h2>
                </div>
                <span>{levelBooks.length} ta kitob</span>
              </div>
              <div className="books-catalog__grid">
                {levelBooks.map((book, index) => (
                  <BookCard
                    key={book.id}
                    book={book}
                    index={index}
                    onClick={() => openBook(book)}
                  />
                ))}
              </div>
            </motion.section>
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
      className={cn("books-catalog__level-pill", active && "is-active")}
      aria-pressed={active}
      onClick={onClick}
    >
      {label}
    </button>
  );
}

function BookCard({ book, index, onClick }: { book: BookSummaryDto; index: number; onClick: () => void }) {
  const progress = book.sectionCount > 0 ? Math.round((book.sectionsRead / book.sectionCount) * 100) : 0;
  return (
    <motion.button
      type="button"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.12 + index * 0.025, type: "spring", stiffness: 260, damping: 22 }}
      className="books-catalog__card"
      onClick={onClick}
    >
      <span className="books-catalog__card-image">
        <BookCover title={book.title} level={book.level} coverImageUrl={book.coverImageUrl} />
      </span>
      <span className="books-catalog__card-body">
        <span className="books-catalog__card-meta">
          <span className="books-catalog__level-badge">{cefrShort(book.level)}</span>
          <span className="books-catalog__focus-badge"><Icon name="auto_stories" />Kitob</span>
        </span>
        <h3>{book.title}</h3>
        <span className="books-catalog__translation">{book.titleUz}</span>
        <span className="books-catalog__focus-name">{book.author ? uz.books.by(book.author) : book.topic || uz.books.title}</span>
        <span className="books-catalog__badges">
          <span className={cn("books-catalog__status-badge", book.isCompleted && "is-complete")}>
            <Icon name={book.isCompleted ? "verified" : "trending_up"} filled={book.isCompleted} />
            {progress}%
          </span>
          <span className="books-catalog__module-count">{book.sectionsRead}/{book.sectionCount}</span>
        </span>
        <span className="books-catalog__progress" aria-label={`Kitob progressi ${progress}%`}>
          <span style={{ width: `${progress}%` }} />
        </span>
      </span>
    </motion.button>
  );
}

function CatalogState({ children, tone, icon, title }: { children?: ReactNode; tone: "loading" | "error" | "empty"; icon?: string; title?: string }) {
  return (
    <section className={cn("books-catalog__state", `books-catalog__state--${tone}`)} aria-live="polite">
      {children ?? (
        <>
          {icon ? <span className="books-catalog__state-icon"><Icon name={icon} filled /></span> : null}
          {title ? <h2>{title}</h2> : null}
        </>
      )}
    </section>
  );
}
