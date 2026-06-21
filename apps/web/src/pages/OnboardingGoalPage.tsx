import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { LearningGoal } from "@/api/types";
import { useAuth } from "@/app/auth";
import { markGoalPromptSeen } from "@/app/session";
import { BriefcaseBusiness, Compass, GraduationCap, MessagesSquare, PlaneTakeoff, Target } from "lucide-react";
import { OnboardingChrome, PlayButton, PlayOption } from "./onboarding/OnboardingChrome";
import { returnTargetOr } from "@/app/returnTarget";

const GOALS = [
  { goal: LearningGoal.IeltsCefr, Icon: Target, label: "IELTS / CEFR", tone: "peach" },
  { goal: LearningGoal.Work, Icon: BriefcaseBusiness, label: "Ish va karyera", tone: "lilac" },
  { goal: LearningGoal.Migration, Icon: PlaneTakeoff, label: "Chet elda yashash", tone: "mint" },
  { goal: LearningGoal.Travel, Icon: Compass, label: "Sayohat", tone: "blue" },
  { goal: LearningGoal.GeneralSpeaking, Icon: MessagesSquare, label: "Erkin gapirish", tone: "lilac" },
  { goal: LearningGoal.School, Icon: GraduationCap, label: "Maktab va o‘qish", tone: "peach" },
];

export function OnboardingGoalPage() {
  const navigate = useNavigate();
  const { applyUser } = useAuth();
  const [selectedGoal, setSelectedGoal] = useState<LearningGoal | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(false);

  const choose = async (goal: LearningGoal) => {
    if (saving) return;
    setSaving(true);
    setError(false);
    try {
      const user = await api.auth.setLearningGoal(goal);
      markGoalPromptSeen();
      applyUser(user);
      navigate(returnTargetOr("/home"), { replace: true });
    } catch {
      setError(true);
      setSaving(false);
    }
  };

  return (
    <OnboardingChrome className="goal-page" step={3} backTo="/onboarding/profile-details">
      <section className="play-goal">
        <div className="play-goal__heading">
          <h1 className="onboarding-play__title">Ingliz tili sizga<br />nima uchun kerak?</h1>
          <p className="onboarding-play__lede">Maqsad sizniki. Yo‘lni birga topamiz.</p>
        </div>
        <div className="play-goal__grid" role="group" aria-label="O‘rganish maqsadi">
          {GOALS.map(({ goal, Icon, label, tone }) => <PlayOption key={goal} className={`tone-${tone}`} selected={selectedGoal === goal} disabled={saving} onClick={() => setSelectedGoal(goal)} icon={<Icon size={24} aria-hidden />}>{label}</PlayOption>)}
        </div>
        {error && <p role="alert" className="onboarding-play__alert">{uz.onboardingGoal.error}</p>}
        <div className="play-goal__actions">
          <PlayButton disabled={selectedGoal === null || saving} onClick={() => selectedGoal !== null && choose(selectedGoal)} arrow={!saving}>{saving ? uz.onboardingGoal.saving : "Maqsadim shu!"}</PlayButton>
          <button type="button" className="onboarding-play__quiet" onClick={() => choose(LearningGoal.Unspecified)} disabled={saving}>Keyinroq tanlayman</button>
        </div>
      </section>
    </OnboardingChrome>
  );
}
