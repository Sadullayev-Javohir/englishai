import { useContext, useEffect, useMemo } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import type { PlacementResultDto } from "@/api/types";
import { AuthContext } from "@/app/auth";
import { setStoredLevel } from "@/app/session";
import { cefrShort } from "@/lib/labels";
import { PLACEMENT_RESULT_KEY } from "@/pages/PlacementTestPage";
import { returnTargetOr } from "@/app/returnTarget";
import { OnboardingChrome, PlayButton, PlayChip } from "./onboarding/OnboardingChrome";
import { PLACEMENT_SKILLS } from "./onboarding/placementSkills";
import { isPlacementResult, placementStorage } from "./onboarding/placementStorage";

function storedPlacementResult(): PlacementResultDto | null {
  try {
    const value = sessionStorage.getItem(PLACEMENT_RESULT_KEY);
    const result: unknown = value ? JSON.parse(value) : null;
    return isPlacementResult(result) ? result : null;
  } catch {
    return null;
  }
}

const LEVEL_NAMES: Record<string, string> = {
  A1: "Beginner", A2: "Elementary", B1: "Intermediate", B2: "Upper Intermediate", C1: "Advanced", C2: "Proficient",
};

export function PlacementResultPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const auth = useContext(AuthContext);
  const learnerId = auth?.user?.id ?? "anon";
  const result = useMemo(() => {
    const state = location.state as { result?: PlacementResultDto; learnerId?: string } | null;
    if (isPlacementResult(state?.result) && (!auth?.user || state?.learnerId === learnerId)) return state.result;
    // Pen preview fixtures must never seed the learner's real result or level.
    if (import.meta.env.DEV && location.pathname.startsWith("/dev/onboarding/")) return storedPlacementResult();
    return placementStorage(learnerId).readResult();
  }, [location.state, location.pathname, learnerId, auth?.user]);
  useEffect(() => {
    if (!result) { navigate("/assessment", { replace: true }); return; }
    if (location.pathname.startsWith("/dev/")) return;
    try { setStoredLevel(result.overallLevel); } catch { /* The server has already persisted the profile. */ }
  }, [navigate, result, location.pathname]);
  if (!result) return null;
  const level = cefrShort(result.overallLevel);
  const name = (auth?.user?.preferredName || auth?.user?.displayName || "").trim().split(/\s+/)[0];
  const scores = new Map(result.stageResults.map(stage => [stage.stage, stage.score]));
  const weakest = result.stageResults.reduce<typeof result.stageResults[number] | undefined>((lowest, next) => !lowest || next.score < lowest.score ? next : lowest, undefined);
  const focus = PLACEMENT_SKILLS.find(skill => skill.stage === weakest?.stage);
  return (
    <OnboardingChrome className="placement-result-page" backTo="/welcome">
      <section className="play-result">
        <div className="play-result__hero">
          <img src="/assets/play/parrot.svg" alt="EnglishAI to‘tiqushi sizni tabriklaydi" />
          <div className="play-result__headline">
            <PlayChip tone="green">Birinchi qadam bajarildi</PlayChip>
            <h1 className="onboarding-play__title">{name ? `Ajoyib, ${name}!` : "Ajoyib natija!"}</h1>
            <div className="play-result__level" aria-label={`Aniqlangan daraja: ${level}`}>
              <strong>{level}</strong><span><b>{LEVEL_NAMES[level]}</b><small>Umumiy natija: {Math.round(result.overallScore)}%</small></span>
            </div>
          </div>
        </div>
        <section className="play-result__skills" aria-labelledby="placement-skills-title">
          <h2 id="placement-skills-title">SIZNING KO‘NIKMALARINGIZ</h2>
          <div className="play-result__grid">
            {PLACEMENT_SKILLS.map(({ stage, label, Icon }) => {
              const score = scores.get(stage);
              const isFocus = stage === weakest?.stage;
              return (
                <article className="play-result__skill" key={stage}>
                  <h3><Icon size={18} aria-hidden />{label}</h3>
                  <strong>{score === undefined ? "—" : `${Math.round(score)}%`}</strong>
                  <div className={`play-result__track ${isFocus ? "is-focus" : ""}`} aria-hidden>
                    {Array.from({ length: 10 }, (_, index) => <span key={index} className={score !== undefined && index < Math.round(Math.max(0, Math.min(100, score)) / 10) ? "is-filled" : ""} />)}
                  </div>
                </article>
              );
            })}
          </div>
        </section>
        {focus && <p className="play-result__focus"><focus.Icon size={22} aria-hidden />{focus.label}’ni birga kuchaytiramiz.</p>}
        <div className="play-result__actions">
          <PlayButton onClick={() => navigate(returnTargetOr("/home"), { replace: true })}>Birinchi darsimga!</PlayButton>
          <p className="onboarding-play__note">Bu o‘quv tavsiyasi, rasmiy sertifikat emas.</p>
        </div>
      </section>
    </OnboardingChrome>
  );
}
