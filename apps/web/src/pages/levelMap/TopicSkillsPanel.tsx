import { useNavigate } from "react-router-dom";
import { Icon } from "@/components/ui/Icon";
import { usePaywall } from "@/components/PaywallProvider";
import { SKILL_STEPS, openSkill } from "@/lib/skillSteps";
import { cn } from "@/lib/cn";
import type { VocabularyTopicSummaryDto } from "@/api/types";
import { useSkillStates } from "./skillStates";

export function TopicSkillsPanel({ topic }: { topic: VocabularyTopicSummaryDto }) {
  const navigate = useNavigate();
  const { open: openPaywall } = usePaywall();
  const { stateOf } = useSkillStates(topic.modules);

  function start(stepIndex: number) {
    if (topic.requiresPro) {
      openPaywall();
      return;
    }
    const step = SKILL_STEPS[stepIndex];
    openSkill(navigate, step, topic.id, topic.title, "levels");
  }

  return (
    <div className="lvmap-panel">
      <span className="lvmap-panel__eyebrow">6 TA BOSQICH · TARTIB BILAN O‘RGANING</span>
      <ol className="lvmap-panel__grid">
        {SKILL_STEPS.map((step, index) => {
          const state = stateOf(step);
          const locked = state === "locked";
          return (
            <li key={step.key}>
              <button
                type="button"
                disabled={locked}
                onClick={() => start(index)}
                className={cn(
                  "lvmap-step",
                  `lvmap-step--${state}`,
                  `lvmap-skill--${step.key}`,
                )}
              >
                <span className="lvmap-step__number">{index + 1}</span>
                <span className="lvmap-step__icon">
                  <Icon
                    name={state === "done" ? "check" : locked ? "lock" : step.icon}
                    filled={state === "done"}
                    className="text-[17px]"
                  />
                </span>
                <span className="lvmap-step__title">
                  {step.key === "vocabulary"
                    ? "Vocabulary"
                    : step.key[0].toUpperCase() + step.key.slice(1)}
                </span>
                <span className="lvmap-step__status">
                  {state === "done"
                    ? "Yakunlandi"
                    : state === "current"
                      ? "Hozir shu"
                      : locked
                        ? "Qulflangan"
                        : "Boshlanmagan"}
                </span>
              </button>
            </li>
          );
        })}
      </ol>
      <button type="button" className="lvmap-panel__cta" onClick={() => start(0)}>
        Vocabularyni boshlash
        <Icon name="arrow_forward" className="text-[18px]" />
      </button>
    </div>
  );
}
