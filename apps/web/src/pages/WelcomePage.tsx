import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import { getLearnerId, setStoredLevel } from "@/app/session";
import { ArrowRight, Compass, Sprout } from "lucide-react";
import { OnboardingChrome, PlayButton, PlayChip } from "./onboarding/OnboardingChrome";
import { returnTargetOr } from "@/app/returnTarget";

const A1_LEVEL = 1;

export function WelcomePage() {
  const navigate = useNavigate();
  const [startingA1, setStartingA1] = useState(false);
  const [startError, setStartError] = useState<string | null>(null);

  const startFromA1 = async () => {
    if (startingA1) return;
    setStartingA1(true);
    setStartError(null);
    try {
      await api.learning.start(getLearnerId(), CefrLevel.A1);
      setStoredLevel(A1_LEVEL);
      navigate(returnTargetOr("/home"), { replace: true });
    } catch {
      setStartError(uz.welcome.startError);
      setStartingA1(false);
    }
  };

  return (
    <OnboardingChrome className="welcome-page" backTo="/onboarding/goal" testId="welcome-viewport">
      <div className="play-welcome">
        <section className="play-welcome__journey">
          <h2 className="onboarding-play__title">Har kimning<br />yo‘li o‘zgacha.</h2>
          <img src="/assets/play/words-world.svg" alt="So‘zlardan yangi dunyoga: kitob, suhbat va tinglash" />
          <div className="play-welcome__levels" aria-label="CEFR darajalari">{["A1", "A2", "B1", "B2", "C1", "C2"].map(level => <span key={level}>{level}</span>)}</div>
        </section>
        <section className="play-welcome__choices">
          <h1 className="onboarding-play__title">Qayerdan<br />boshlaymiz?</h1>
          <div className="play-welcome__recommended">
            <div><Compass size={30} aria-hidden /><PlayChip tone="white">Tavsiya qilamiz</PlayChip></div>
            <h2>O‘z darajamdan</h2>
            <p>Qisqa test bilan mos yo‘lni topamiz.</p>
            <PlayButton onClick={() => navigate("/assessment")}>Darajamni aniqlash</PlayButton>
          </div>
          <button type="button" className="play-welcome__a1" onClick={startFromA1} disabled={startingA1} aria-label="A1 darajadan boshlash">
            <Sprout size={28} aria-hidden /><span><strong>{startingA1 ? uz.welcome.startingA1 : "Noldan boshlayman"}</strong><small>A1 · Testsiz boshlash</small></span><ArrowRight size={20} aria-hidden />
          </button>
          {startError && <p className="onboarding-play__alert" role="alert">{startError}</p>}
        </section>
      </div>
    </OnboardingChrome>
  );
}
