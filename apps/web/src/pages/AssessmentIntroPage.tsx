import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { CefrLevel } from "@/api/types";
import { getLearnerId, setStoredLevel } from "@/app/session";
import { ArrowRight, CircleCheck, Clock3, Mic, Shapes, ShieldCheck } from "lucide-react";
import { OnboardingChrome, PlayButton, PlayChip } from "./onboarding/OnboardingChrome";
import { PLACEMENT_SKILLS } from "./onboarding/placementSkills";
import { returnTargetOr } from "@/app/returnTarget";

const A1_LEVEL = 1;

type PermissionState = "idle" | "checking" | "granted" | "denied";

export function AssessmentIntroPage() {
  const navigate = useNavigate();
  const [choosingA1, setChoosingA1] = useState(false);
  const [startError, setStartError] = useState<string | null>(null);
  const [permissionState, setPermissionState] = useState<PermissionState>("idle");

  const requestMicrophone = async () => {
    if (permissionState === "checking" || permissionState === "granted") return;
    if (!navigator.mediaDevices?.getUserMedia) {
      setPermissionState("denied");
      return;
    }

    setPermissionState("checking");
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      stream.getTracks().forEach((track) => track.stop());
      setPermissionState("granted");
    } catch {
      setPermissionState("denied");
    }
  };

  const startFromA1 = async () => {
    if (choosingA1) return;
    setChoosingA1(true);
    setStartError(null);
    try {
      await api.learning.start(getLearnerId(), CefrLevel.A1);
      setStoredLevel(A1_LEVEL);
      navigate(returnTargetOr("/home"), { replace: true });
    } catch {
      setStartError(uz.assessmentIntro.startError);
      setChoosingA1(false);
    }
  };

  const permissionCopy = permissionState === "granted"
    ? "Mikrofon tayyor"
    : permissionState === "checking"
      ? "Mikrofon tekshirilmoqda..."
      : permissionState === "denied"
        ? "Mikrofonga ruxsat berilmadi"
        : "Mikrofonni tekshirish";

  return (
    <OnboardingChrome className="assessment-intro" backTo="/welcome">
      <section className="play-assessment">
        <PlayChip>Boshlanish nuqtangizni topamiz</PlayChip>
        <h1 className="onboarding-play__title">Qanchalik bilasiz?<br />Birga bilib olamiz.</h1>
        <div className="play-assessment__facts">
          <div className="play-assessment__fact"><Clock3 size={26} aria-hidden /><span><strong>12–18</strong><small>daqiqa</small></span></div>
          <div className="play-assessment__fact"><Shapes size={26} aria-hidden /><span><strong>6</strong><small>ko‘nikma</small></span></div>
        </div>
        <div className="play-assessment__skills">{PLACEMENT_SKILLS.map(({ label, Icon }) => <div key={label} className="play-assessment__skill"><Icon size={20} aria-hidden />{label}</div>)}</div>
        <p className="play-assessment__adaptive">Savollar javoblaringizga moslashadi.</p>
        <div className="play-assessment__checklist">
          <button type="button" className="play-assessment__mic" onClick={requestMicrophone} disabled={permissionState === "checking" || permissionState === "granted"}>
            {permissionState === "granted" ? <CircleCheck size={24} aria-hidden /> : <Mic size={24} aria-hidden />}<span>{permissionCopy}</span><ArrowRight size={18} aria-hidden />
          </button>
          <p className="play-assessment__notice"><ShieldCheck size={19} aria-hidden />Tinch joy tanlang. Testda boshqa oynaga o‘tmang.</p>
          {permissionState === "denied" && <p role="alert" className="onboarding-play__alert">Brauzer sozlamalaridan mikrofon ruxsatini yoqing. Ruxsatsiz ham testni boshlashingiz mumkin, ammo gapirish qismi ishlamasligi mumkin.</p>}
        </div>
        <div className="play-assessment__actions">
          <PlayButton onClick={() => navigate("/placement")}>Tayyorman, boshlaymiz!</PlayButton>
          <button type="button" className="onboarding-play__quiet" onClick={startFromA1} disabled={choosingA1}>{choosingA1 ? uz.assessmentIntro.choosingA1 : "A1 darajadan boshlayman"}</button>
          {startError && <p className="onboarding-play__alert" role="alert">{startError}</p>}
        </div>
      </section>
    </OnboardingChrome>
  );
}
