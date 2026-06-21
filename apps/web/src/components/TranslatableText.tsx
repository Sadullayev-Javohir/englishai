import { useEffect, useState } from "react";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { Spinner } from "@/components/ui/Spinner";
import { DesignModal } from "@/components/design";
import { splitSentences } from "@/lib/sentences";
import { cn } from "@/lib/cn";

// Process-wide cache so a sentence is fetched (and the user waits) at most once per session;
// the backend also caches, so repeats are cheap either way (rules 10, 11).
const translationCache = new Map<string, string | null>();

interface TranslatableTextProps {
  /** The English teaching text. Each sentence becomes individually clickable. */
  text: string;
  /** Optional CEFR level so the Uzbek wording can match the learner's level. */
  level?: CefrLevel;
  className?: string;
}

/**
 * Renders English teaching text where each sentence can be clicked to see its accurate Uzbek
 * translation in a modal. Translation is explicit-tap only - never on hover, so reading past a
 * sentence never pops a modal. The translation is fetched on demand from the backend (cache-first)
 * and never fabricated - an unavailable translation shows an honest message (rules 8, 10, 11).
 */
export function TranslatableText({ text, level, className }: TranslatableTextProps) {
  const [active, setActive] = useState<string | null>(null);

  const sentences = splitSentences(text);
  // Nothing to split (e.g. a single fragment) - still make the whole thing translatable.
  const pieces = sentences.length > 0 ? sentences : [{ text: text.trim(), trailing: "" }];

  return (
    <>
      <span className={className}>
        {pieces.map((piece, i) => (
          <span key={i}>
            <span
              role="button"
              tabIndex={0}
              title={uz.translate.hint}
              onClick={() => setActive(piece.text)}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  setActive(piece.text);
                }
              }}
              className="cursor-pointer rounded-sm decoration-dotted decoration-border underline-offset-4 hover:underline hover:bg-primary-container/10 focus:outline-none focus:underline transition-colors"
            >
              {piece.text}
            </span>
            {piece.trailing || (i < pieces.length - 1 ? " " : "")}
          </span>
        ))}
      </span>

      {active !== null && (
        <TranslationModal sentence={active} level={level} onClose={() => setActive(null)} />
      )}
    </>
  );
}

function TranslationModal({
  sentence,
  level,
  onClose,
}: {
  sentence: string;
  level?: CefrLevel;
  onClose: () => void;
}) {
  const cacheKey = `${level ?? 0}|${sentence}`;
  const [translation, setTranslation] = useState<string | null | undefined>(
    translationCache.has(cacheKey) ? translationCache.get(cacheKey) : undefined,
  );
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    if (translationCache.has(cacheKey)) {
      setTranslation(translationCache.get(cacheKey));
      return;
    }
    let cancelled = false;
    setTranslation(undefined);
    setFailed(false);
    api.translate(sentence, level)
      .then((res) => {
        if (cancelled) return;
        translationCache.set(cacheKey, res.translation);
        setTranslation(res.translation);
      })
      .catch(() => {
        if (!cancelled) setFailed(true);
      });
    return () => {
      cancelled = true;
    };
  }, [cacheKey, sentence, level]);

  const loading = translation === undefined && !failed;

  return (
    <DesignModal
      open
      onClose={onClose}
      title={uz.translate.title}
      closeLabel={uz.translate.close}
      description={`“${sentence}”`}
      className="translation-modal"
    >
      {loading ? (
        <div className="ea-state ea-state--loading">
          <Spinner />
        </div>
      ) : failed || translation === null ? (
        <p className="ea-state ea-state--empty">
          <Icon name="info" />
          <span>{uz.translate.unavailable}</span>
        </p>
      ) : (
        <p className={cn("translation-modal__text")}>{translation}</p>
      )}
    </DesignModal>
  );
}
