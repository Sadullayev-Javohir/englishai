import { Link, useParams, useSearchParams } from "react-router-dom";
import { CefrLevel, TestStage, PlacementItemKind } from "@/api/types";
import type { PlacementResultDto } from "@/api/types";
import { LoginPage } from "@/pages/LoginPage";
import { WelcomePage } from "@/pages/WelcomePage";
import { UsernameSetupPage } from "@/pages/UsernameSetupPage";
import { DemographicsSetupPage } from "@/pages/DemographicsSetupPage";
import { OnboardingGoalPage } from "@/pages/OnboardingGoalPage";
import { useState } from "react";
import { AssessmentIntroPage } from "@/pages/AssessmentIntroPage";
import { PlacementResultPage } from "@/pages/PlacementResultPage";
import { McqContent, PlacementBoard, PlacementTestPage, SpeakingItem, WritingItem, PLACEMENT_RESULT_KEY } from "@/pages/PlacementTestPage";
import { OnboardingChrome } from "./OnboardingChrome";
import { PLACEMENT_PREVIEWS } from "./placementPreviewItems";
import { PLACEMENT_SKILLS } from "./placementSkills";

/**
 * DEV-ONLY visual preview harness for the login → /home onboarding journey. It renders
 * each real page component ungated (bypassing RequireAuth / onboarding gates) so the
 * redesign can be screenshotted and scored without a running backend. Never mounted in
 * production — the route is registered only under `import.meta.env.DEV`.
 */

const MOCK_RESULT: PlacementResultDto = {
  overallLevel: CefrLevel.B1,
  overallScore: 72,
  stageResults: [
    { stage: TestStage.Vocabulary, level: CefrLevel.B2, score: 81 },
    { stage: TestStage.Grammar, level: CefrLevel.B1, score: 68 },
    { stage: TestStage.Listening, level: CefrLevel.B1, score: 74 },
    { stage: TestStage.Reading, level: CefrLevel.B2, score: 79 },
    { stage: TestStage.Writing, level: CefrLevel.A2, score: 58 },
    { stage: TestStage.Speaking, level: CefrLevel.B1, score: 70 },
  ],
} as unknown as PlacementResultDto;

function PlacementBoardPreview({ skill }: { skill: string }) {
  const item = PLACEMENT_PREVIEWS[skill] ?? PLACEMENT_PREVIEWS.grammar;
  const [selected, setSelected] = useState<number | null>(skill === "grammar" ? 2 : null);
  const [submitted, setSubmitted] = useState(false);
  const submit = () => { setSubmitted(true); setSelected(null); };
  return (
    <OnboardingChrome>
      <PlacementBoard item={item}>
        {item.kind === PlacementItemKind.Writing ? <WritingItem item={item} busy={false} onSubmit={submit} />
          : item.kind === PlacementItemKind.Speaking ? <SpeakingItem item={item} busy={false} onSubmit={submit} submitError={null} withoutFullscreenWatch={action => action()} />
            : <McqContent item={item} selected={selected} busy={false} onSelect={setSelected} onSubmit={submit} />}
        {submitted && <p role="status" className="onboarding-play__note">Bu dizayn namunasi. Javob serverga yuborilmadi va baholanmadi.</p>}
      </PlacementBoard>
    </OnboardingChrome>
  );
}

const PAGES = [
  { path: "login", label: "02 · Kirish" },
  { path: "username", label: "03 · Foydalanuvchi nomi" },
  { path: "profile-details", label: "04 · Profil" },
  { path: "goal", label: "05 · Maqsad" },
  { path: "welcome", label: "06 · Boshlanish" },
  { path: "assessment", label: "07 · Testga tayyorgarlik" },
  { path: "board", label: "08 · Test" },
  { path: "result", label: "09 · B1 natija" },
] as const;

export function OnboardingPreview() {
  const { page } = useParams();
  const [searchParams] = useSearchParams();
  const requestedSkill = searchParams.get("skill") ?? "grammar";
  const skill = Object.prototype.hasOwnProperty.call(PLACEMENT_PREVIEWS, requestedSkill) ? requestedSkill : "grammar";
  const pageIndex = PAGES.findIndex((entry) => entry.path === page);
  const previousPage = PAGES[pageIndex - 1];
  const nextPage = PAGES[pageIndex + 1];

  // Seed synchronously (before the child reads it) so the result page renders
  // instead of bouncing back to /assessment on an empty session.
  if (page === "result") {
    try { sessionStorage.setItem(PLACEMENT_RESULT_KEY, JSON.stringify(MOCK_RESULT)); } catch { /* preview only */ }
  }

  const node = (() => {
    switch (page) {
      case "login": return <LoginPage />;
      case "username": return <UsernameSetupPage />;
      case "profile-details": return <DemographicsSetupPage />;
      case "goal": return <OnboardingGoalPage />;
      case "welcome": return <WelcomePage />;
      case "assessment": return <AssessmentIntroPage />;
      case "placement": return <PlacementTestPage />;
      case "board": return <PlacementBoardPreview key={skill} skill={skill} />;
      case "result": return <PlacementResultPage />;
      default: return null;
    }
  })();

  return (
    <>
      {node}
      {searchParams.get("clean") !== "1" && <nav aria-label="Onboarding previews" style={{ position: "fixed", left: 12, right: 12, bottom: 12, zIndex: 9999, display: "flex", flexWrap: "wrap", alignItems: "center", gap: 6, padding: 10, borderRadius: 12, background: "rgba(0,0,0,0.9)", fontSize: 12, fontFamily: "Plus Jakarta Sans, sans-serif" }}>
        <span style={{ color: "#ddd", padding: "4px 8px" }}>Dizayn ko‘rish</span>
        {PAGES.map(({ path, label }) => (
          <Link key={path} to={`/dev/onboarding/${path}`} aria-current={path === page ? "page" : undefined} style={{ padding: "8px 10px", borderRadius: 6, background: path === page ? "#7545e8" : "#333", color: "#fff", textDecoration: "none" }}>{label}</Link>
        ))}
        {previousPage && <Link to={`/dev/onboarding/${previousPage.path}`} aria-label={`Oldingi sahifa: ${previousPage.label}`} style={{ padding: "8px 10px", color: "#fff" }}>← Oldingi</Link>}
        {nextPage && <Link to={`/dev/onboarding/${nextPage.path}`} aria-label={`Keyingi sahifa: ${nextPage.label}`} style={{ padding: "8px 12px", borderRadius: 6, background: "#7545e8", color: "#fff", fontWeight: 800, textDecoration: "none" }}>Keyingi →</Link>}
        {page === "board" && <div className="placement-preview__skills" role="group" aria-label="Olti ko‘nikma namunasi">
          <span>08 · Ko‘nikmalar</span>
          {PLACEMENT_SKILLS.map(({ label }) => <Link key={label} to={`/dev/onboarding/board?skill=${label.toLowerCase()}`} aria-current={skill === label.toLowerCase() ? "page" : undefined}>{label}</Link>)}
          <small>Namuna · baholanmaydi. Listening uchun haqiqiy test audiosi talab etiladi.</small>
        </div>}
      </nav>}
    </>
  );
}
