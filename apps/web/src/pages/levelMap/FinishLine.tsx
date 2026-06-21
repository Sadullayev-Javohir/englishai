import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/cn";
import { LevelExitState } from "@/api/types";
import type { LevelReadinessDto } from "@/api/types";

/**
 * Level Exit Test milestone - the end of the road, rendered as a panel that lines
 * up with the chapters above it.
 */
export function FinishLine({
  exitState,
  readiness,
  onStart,
  superAdminPreview = false,
}: {
  exitState: LevelExitState;
  readiness: LevelReadinessDto | null;
  onStart: () => void;
  superAdminPreview?: boolean;
}) {
  const isMax = exitState === LevelExitState.MaxLevel;
  const isCompleted = exitState === LevelExitState.Completed;
  const isCurrent = exitState === LevelExitState.Current;
  const isLocked = exitState === LevelExitState.Locked;
  const ready = isCurrent && (readiness?.exitTestRecommended ?? false);

  const icon = isMax ? "trophy" : isCompleted ? "verified" : isLocked ? "lock" : "flag";

  const title = isMax
    ? uz.levelMap.exitMaxTitle
    : isCompleted
      ? "Keyingi daraja ochildi."
      : "Keyingi daraja sizni kutmoqda.";
  const message = isMax
    ? uz.levelMap.exitAtMax
    : isCompleted
      ? uz.levelMap.exitCompleted
      : isLocked
        ? uz.levelMap.exitLocked
        : ready
          ? "Barcha shartlar bajarildi. Chiqish testiga tayyorsiz."
          : "Barcha mavzularni yakunlang va daraja chiqish testini topshiring.";
  const checks = readiness
    ? [
        {
          met: readiness.masteredSkillCount >= readiness.requiredMasteredSkills,
          text: `${readiness.requiredMasteredSkills} ta ko'nikma ${readiness.masteryThreshold}+ ball (${readiness.masteredSkillCount}/${readiness.requiredMasteredSkills})`,
        },
        {
          met: readiness.productiveSkillsMeetFloor,
          text: `Speaking va Writing kamida ${readiness.productiveSkillFloor} ball`,
        },
        {
          met: readiness.hasSufficientSamples,
          text: `Kamida ${readiness.minimumSamplesPerSkill} tadan yangi baholangan mashq`,
        },
        {
          met: false,
          text: `Chiqish testi: umumiy ${readiness.minimumOverallScore}+, har bo'lim ${readiness.minimumStageScore}+`,
          pending: true,
        },
      ]
    : [];

  return (
    <section
      className={cn(
        "lvmap-finish",
        isMax || isCompleted
          ? "lvmap-finish--done"
          : ready
            ? "lvmap-finish--ready"
            : "lvmap-finish--todo",
      )}
    >
      <span className="lvmap-finish__node" aria-hidden>
        <Icon name={icon} filled className="text-[26px]" />
      </span>

      <div className="min-w-0">
        <span className="lvmap-finish__eyebrow">{uz.levelMap.roadmap.finishTitle}</span>
        <h3 className="lvmap-finish__title">{title}</h3>
        <p className="lvmap-finish__message">{message}</p>
        {isCurrent && readiness && (
          <ul className="lvmap-finish__checks" aria-label="Darajaga o'tish shartlari">
            {checks.map((check) => (
              <li key={check.text} className={cn(check.met && "is-met", check.pending && "is-pending")}>
                <Icon name={check.met ? "check_circle" : check.pending ? "flag" : "radio_button_unchecked"} filled={check.met} />
                <span>{check.text}</span>
              </li>
            ))}
          </ul>
        )}
        {(isCurrent || superAdminPreview) && (
          <Button
            variant={ready || superAdminPreview ? "primary" : "outline"}
            icon={ready || superAdminPreview ? "play_arrow" : "lock"}
            onClick={onStart}
            className="lvmap-finish__cta"
          >
            {superAdminPreview && !isCurrent
              ? "Super-admin: testni sinash"
              : ready
                ? uz.levelMap.exitStartCta
                : "Daraja chiqish testini topshirish"}
          </Button>
        )}
      </div>
    </section>
  );
}
