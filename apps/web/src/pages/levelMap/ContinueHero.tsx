import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { TopicImage } from "@/components/TopicImage";
import { cefrShort } from "@/lib/labels";
import { cn } from "@/lib/cn";
import type { VocabularyTopicSummaryDto } from "@/api/types";

/**
 * "Resume here" mission card for the level's active topic - the one place on the
 * page that actually starts a topic (and therefore spends energy). Mirrors the
 * home dashboard's gradient primary card.
 */
export function ContinueHero({
  topic,
  opening,
  disabled,
  onOpen,
}: {
  topic: VocabularyTopicSummaryDto;
  opening: boolean;
  disabled: boolean;
  onOpen: () => void;
}) {
  const fresh = topic.passedModuleCount === 0;
  const pct =
    topic.requiredModuleCount > 0
      ? Math.round((topic.passedModuleCount / topic.requiredModuleCount) * 100)
      : 0;

  return (
    <section className="lvmap-mission" aria-label={uz.levelMap.roadmap.continueTitle}>
      <div className="lvmap-mission__body">
        <div className="lvmap-mission__copy">
          <span className="lvmap-mission__eyebrow">
            <Icon name="play_arrow" filled className="text-[14px]" />
            {fresh ? uz.levelMap.roadmap.startTitle : uz.levelMap.roadmap.continueTitle}
            <span className="lvmap-mission__level">{cefrShort(topic.level)}</span>
          </span>
          <h2>{topic.title}</h2>
          <p>{topic.titleUz}</p>
        </div>

        <button
          type="button"
          onClick={onOpen}
          disabled={disabled}
          className="lvmap-mission__cta"
        >
          <Icon
            name={opening ? "progress_activity" : "arrow_forward"}
            filled
            className={cn("text-[20px]", opening && "animate-spin")}
          />
          {opening
            ? uz.common.loading
            : fresh
              ? uz.levelMap.roadmap.startCta
              : uz.levelMap.roadmap.continueCta}
        </button>

        {!fresh && (
          <div className="lvmap-mission__progress">
            <span>
              <b>{pct}%</b>
              <small>
                {uz.levelMap.roadmap.inProgress(
                  topic.passedModuleCount,
                  topic.requiredModuleCount,
                )}
              </small>
            </span>
            <div aria-hidden>
              <i style={{ width: `${pct}%` }} />
            </div>
          </div>
        )}
      </div>

      <TopicImage
        topicId={topic.id}
        title={topic.title}
        level={topic.level}
        category={topic.category}
        hideLevelBadge
        hideTitle
        className="lvmap-mission__art"
      />
    </section>
  );
}
