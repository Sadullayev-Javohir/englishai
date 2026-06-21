import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { HomeConceptLab, isHomeNavRoute, isPracticeNavRoute, MobileNav, SportSideNav, SportTopBar, type HomeConceptModel } from "./HomeConceptLab";

Object.defineProperty(window, "matchMedia", {
  configurable: true,
  value: vi.fn().mockReturnValue({ matches: true }),
});

vi.mock("@/components/ParrotLogo", () => ({ ParrotLogo: () => <span aria-hidden>Parrot</span> }));
vi.mock("@/components/NotificationBell", () => ({ NotificationBell: ({ className = "" }: { className?: string }) => <button type="button" className={className} aria-label="Bildirishnomalar" /> }));
vi.mock("@/components/game/EnergyProvider", () => ({ useEnergy: () => ({ energy: { current: 5, maximum: 5 }, loading: false }) }));
vi.mock("@/lib/useLocalDay", () => ({ useLocalDay: () => "2026-08-07" }));
const { adminAccess, lifetimeXp } = vi.hoisted(() => ({
  adminAccess: vi.fn(async () => ({ role: "None", isAdmin: false, canManageAdmins: false })),
  lifetimeXp: { value: 1200 },
}));
vi.mock("@/api/client", () => ({ api: { gamification: { points: async () => ({ lifetimeXp: lifetimeXp.value }) }, admin: { access: adminAccess } } }));
vi.mock("react-chartjs-2", () => ({
  Line: ({ data }: { data: { labels: string[]; datasets: Array<{ data: number[]; borderColor: string }> } }) => (
    <div
      role="img"
      aria-label="Haftalik natijalar grafigi"
      data-labels={data.labels.join(",")}
      data-values={data.datasets[0]?.data.join(",")}
      data-line-color={data.datasets[0]?.borderColor}
    />
  ),
}));

const action = (key: string, title: string, to: string) => ({ key, title, to, text: `${title} matni`, cta: "Boshlash", icon: "school" });
const model: HomeConceptModel = {
  name: "Test o'quvchi", completed: 2, total: 6, remaining: 4, complete: false, streakAtRisk: false, currentStreak: 12, dailyGoalTarget: 5, todayCompletedTasks: 3, todayCompletedSkills: 3, practiceWordCount: 3, savedCount: 8, dueSavedCount: 0,
  daily: [action("Vocabulary", "Lug‘at", "/app/vocabulary/topics"), action("Speaking", "Gapirish", "/app/speaking")],
  vocabulary: [action("saved", "Mening so'zlarim", "/app/vocabulary/saved")], modules: [action("speaking", "Gapirish", "/app/speaking"), action("reading", "O‘qish", "/reading")],
  nextAction: action("speaking", "Gapirish", "/app/speaking"), weakestSkill: action("reading", "O‘qish", "/reading"),
  weeklyMinutes: 350, weeklyProgress: [20, 30, 40, 50, 60, 70, 80], badges: ["Bir", "Ikki", "Uch"],
};

afterEach(() => {
  cleanup();
  adminAccess.mockResolvedValue({ role: "None", isAdmin: false, canManageAdmins: false });
  localStorage.clear();
  delete document.documentElement.dataset.homeDesign;
  lifetimeXp.value = 1200;
});

function CurrentPath() {
  return <output aria-label="Joriy sahifa">{useLocation().pathname}</output>;
}

describe("EnglishAI Play dashboard", () => {
  function show(overrides: Partial<HomeConceptModel> = {}) {
    return render(<MemoryRouter initialEntries={["/home"]}><Routes><Route path="*" element={<><HomeConceptLab model={{ ...model, ...overrides }}/><CurrentPath/></>}/></Routes></MemoryRouter>);
  }
  it.each(["/app/vocabulary/topics", "/app/grammar", "/reading", "/writing", "/listening", "/books", "/video"])("recognizes lesson navigation for %s", path => expect(isPracticeNavRoute(path)).toBe(true));
  it("recognizes the canonical home route", () => { expect(isHomeNavRoute("/home")).toBe(true); expect(isHomeNavRoute("/levels")).toBe(false); });
  it("uses the approved Play design without a theme or concept selector", () => {
    show(); expect(screen.getByTestId("home-dashboard").dataset.themeScope).toBe("englishai-play");
    expect(screen.getByRole("heading", { name: "Salom, Test o'quvchi!" })).toBeTruthy();
    expect(screen.queryByRole("button", { name: /qorong|dark/i })).toBeNull();
  });
  it.each([[0,"0"],[99,"1.6"],[100,"1.7"],[120,"2"],[350,"5.8"]])("formats real weekly study minutes %i", (weeklyMinutes, hours) => {
    show({ weeklyMinutes }); expect(screen.getByRole("button", { name: `Haftalik vaqt: ${hours} soat` })).toBeTruthy();
  });
  it("renders actual XP, energy and vocabulary counts", async () => {
    show(); expect(await screen.findByText("1.2k XP")).toBeTruthy();
    expect(screen.getByRole("button",{name:"Energiya holati"}).textContent).toContain("5 / 5");
    expect(screen.getByText("8")).toBeTruthy();
  });
  it("opens the current topic skill rather than the generic catalog", async () => {
    show({ nextAction: {...action("Writing","Yozish","/writing/task/travel"),topicId:"travel",topicTitle:"Travel",skillKey:"writing"} });
    fireEvent.click(screen.getByRole("button",{name:"Davom etish"}));
    await waitFor(() => expect(screen.getByLabelText("Joriy sahifa").textContent).toBe("/writing/task/travel"));
  });
  it("never navigates a locked next activity", () => {
    show({nextAction:{...model.nextAction,locked:true}});
    expect((screen.getByRole("button",{name:"Davom etish"}) as HTMLButtonElement).disabled).toBe(true);
  });
  it("keeps accessible mobile destinations and active state", () => {
    render(<MemoryRouter initialEntries={["/progress"]}><MobileNav/></MemoryRouter>);
    expect(screen.getByRole("navigation",{name:"Mobil navigatsiya"})).toBeTruthy();
    expect(screen.getByRole("button",{name:"Natijalar"}).getAttribute("aria-current")).toBe("page");
  });
  it("uses screen 61's five mobile destinations, with profile in the header", () => {
    render(<MemoryRouter initialEntries={["/home"]}><MobileNav homeDesign /></MemoryRouter>);
    const nav = screen.getByRole("navigation", { name: "Mobil navigatsiya" });
    expect(Array.from(nav.querySelectorAll("button")).map(button => button.textContent)).toEqual(["Bugun", "O‘quv yo‘li", "Video", "Natijalar", "Reyting"]);
    expect(screen.getByRole("button", { name: "Bugun" }).getAttribute("aria-current")).toBe("page");
  });
  it("renders all screen 61 sections and real learner rank without the old numeric lesson counter", () => {
    show({ leaderboardRank: 17 });
    for (const title of ["Mashq tanlang", "Video darslar", "KUN SO‘ZI"]) {
      expect(screen.getByRole("heading", { name: title })).toBeTruthy();
    }
    expect(screen.getByRole("button", { name: "Reyting: 17-o‘rin" }).textContent).toContain("#17");
    expect(screen.getByRole("list", { name: "Dars ko‘nikmalari" }).querySelectorAll("button")).toHaveLength(6);
    expect(screen.queryByText(/ko‘nikma bajarildi/)).toBeNull();
    expect(screen.getByRole("button", { name: "Profil" })).toBeTruthy();
  });
  it("does not invent a rank when the server has none", () => {
    show(); expect(screen.getByRole("button", { name: "Reyting hali mavjud emas" }).textContent).toContain("—");
  });
  it("shows admin navigation only to the allowed role", async () => {
    adminAccess.mockResolvedValue({role:"SuperAdmin",isAdmin:true,canManageAdmins:true}); show();
    expect(await screen.findByRole("button",{name:"Admin"})).toBeTruthy();
  });
  it("hides admin navigation from learners", async () => { show(); await screen.findByText("1.2k XP"); expect(screen.queryByRole("button",{name:"Admin"})).toBeNull(); });
  it("leaves the single global assistant launcher to AppShell", () => {
    show();
    expect(screen.queryByRole("button", { name: /AI yordamchi/i })).toBeNull();
  });
  it("keeps saved words and assistant actions outside the home sidebar", () => {
    show();
    const sidebar = screen.getByRole("complementary", { name: `${model.name} o‘quv navigatsiyasi` });
    expect(within(sidebar).queryByRole("button", { name: "Saqlangan" })).toBeNull();
    expect(within(sidebar).queryByRole("button", { name: "AI yordamchi" })).toBeNull();
    expect(sidebar.querySelector("img")).toBeNull();
    expect(within(sidebar).getByRole("button", { name: "Bugun" })).toBeTruthy();
    expect(within(sidebar).queryByRole("button", { name: "Bildirishnomalar" })).toBeNull();

    const header = screen.getByRole("banner", { name: "EnglishAI navigatsiyasi" });
    expect(within(header).getByRole("button", { name: "Bildirishnomalar" })).toBeTruthy();
    expect(within(header).queryByRole("button", { name: /AI yordamchi/i })).toBeNull();
    // The mobile quick-action row stays available outside the desktop sidebar.
    expect(screen.getByRole("button", { name: "Saqlangan" })).toBeTruthy();
    expect(within(screen.getByTestId("home-dashboard")).queryByRole("button", { name: /AI yordamchi/i })).toBeNull();
  });
  it.each(["/levels", "/progress", "/leaderboard", "/profile", "/app/vocabulary/saved", "/support"])("omits saved words and the assistant block from the shared sidebar on %s", path => {
    render(<MemoryRouter initialEntries={[path]}><SportSideNav model={model} /></MemoryRouter>);
    const sidebar = screen.getByRole("complementary", { name: `${model.name} o‘quv navigatsiyasi` });
    expect(within(sidebar).queryByRole("button", { name: "Saqlangan" })).toBeNull();
    expect(within(sidebar).queryByRole("button", { name: "AI yordamchi" })).toBeNull();
    expect(within(sidebar).queryByText("Birga o‘rganamiz.")).toBeNull();
    expect(sidebar.querySelector(".play-sidebar-help")).toBeNull();
    for (const name of ["Bugun", "O‘quv yo‘li", "Natijalar", "Reyting", "Bildirishnomalar"]) {
      expect(within(sidebar).getByRole("button", { name })).toBeTruthy();
    }
  });
  it("does not duplicate the global launcher in the shared header or sidebar", () => {
    render(<MemoryRouter initialEntries={["/progress"]}><SportSideNav model={model} /><SportTopBar model={model} /></MemoryRouter>);
    expect(screen.queryByRole("button", { name: /AI yordamchi/i })).toBeNull();
  });
  it.each([0, 1, 3])("does not show a pronunciation reminder for %i practice words", (practiceWordCount) => {
    show({ practiceWordCount });
    expect(screen.queryByRole("button", { name: /Talaffuz mashqi/ })).toBeNull();
    expect(document.querySelector(".play-home-alert")).toBeNull();
    expect(Object.keys(localStorage).filter(key => key.startsWith("englishai.home-alert.dismissed."))).toEqual([]);
  });
  it("keeps the streak reminder actionable", async () => {
    show({ streakAtRisk: true });
    fireEvent.click(screen.getByRole("button", { name: /Streakni saqlang/ }));
    await waitFor(() => expect(screen.getByLabelText("Joriy sahifa").textContent).toBe(model.nextAction.to));
    expect(localStorage.getItem("englishai.home-alert.dismissed.streak")).toBe("1");
  });
  it("dismisses a streak alert without changing its underlying progress", () => {
    show({streakAtRisk:true}); fireEvent.click(screen.getByRole("button",{name:"Bildirishnomani yopish"}));
    expect(screen.queryByText("Streakni saqlang")).toBeNull(); expect(screen.getByText("12 kun")).toBeTruthy();
  });
  it("keeps a dismissed streak reminder hidden after remounting", () => {
    const { unmount } = show({ streakAtRisk: true });
    fireEvent.click(screen.getByRole("button", { name: "Bildirishnomani yopish" }));
    unmount();
    show({ streakAtRisk: true });
    expect(screen.queryByText("Streakni saqlang")).toBeNull();
    expect(document.querySelector(".play-home-alert")).toBeNull();
  });
  it("shows loading and recoverable error states", () => {
    const {unmount} = render(<MemoryRouter><HomeConceptLab model={model} loading/></MemoryRouter>);
    expect(screen.getByRole("status")).toBeTruthy(); unmount(); const retry=vi.fn();
    render(<MemoryRouter><HomeConceptLab model={model} error={new Error("offline")} onRetry={retry}/></MemoryRouter>);
    fireEvent.click(screen.getByRole("button",{name:"Qayta urinish"})); expect(retry).toHaveBeenCalledOnce();
  });
});
