import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, useLocation } from "react-router-dom";
import { DailyHabit, DailyLesson, DailyWord, DueReviewCard, HomeDiscoveries, SkillLauncher, VideoRecommendations } from "./HomeDashboardSections";
import { buildHomeViewModel } from "./homeViewModel";
import { HOME_SKILLS, HOME_VIDEOS } from "./homeDashboardContent";

const { openVideo, learnWord, speakWord } = vi.hoisted(() => ({
  openVideo: vi.fn(),
  learnWord: vi.fn(),
  speakWord: vi.fn(() => true),
}));
vi.mock("@/api/client", () => ({ api: { video: { open: openVideo }, vocabulary: { learn: learnWord } } }));
vi.mock("@/lib/audio", () => ({ speakEnglishWord: speakWord }));
vi.mock("@/lib/useLocalDay", () => ({ useLocalDay: () => "2026-09-10" }));

const model = buildHomeViewModel({
  name: "Javohir",
  activeTopic: {
    id: "mother",
    title: "My Mother",
    modules: HOME_SKILLS.map((skill, index) => ({ module: skill.label, score: index < 2 ? 100 : 0, passed: index < 2, unlocked: index < 3, achievedAt: null })),
  },
  activeTopicLevel: 1,
  currentStreak: 7,
  todayCompletedTasks: 1,
  skillsCompletedToday: ["Vocabulary"],
  dailyGoalTarget: 3,
  dueSavedCount: 20,
  dueTopicTitle: "My Mother",
  dueReviewDay: 3,
  last7Days: Array.from({ length: 7 }, (_, index) => ({ day: `2026-09-${String(index + 4).padStart(2, "0")}`, seconds: index < 6 ? 60 : 0 })),
});
function Location() { return <output aria-label="Joriy sahifa">{useLocation().pathname}</output>; }
function show(children: React.ReactNode) {
  return render(<MemoryRouter initialEntries={["/home"]}>{children}<Location /></MemoryRouter>);
}
afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  localStorage.clear();
  speakWord.mockReturnValue(true);
});

describe("Screen 61 learning components", () => {
  it("colors only completed skills and retains canonical next-action locking", () => {
    const onAction = vi.fn();
    show(<DailyLesson model={model} onAction={onAction} />);
    const steps = within(screen.getByRole("list", { name: "Dars ko‘nikmalari" })).getAllByRole("button");
    expect(steps.map(step => step.dataset.completed)).toEqual(["true", "true", "false", "false", "false", "false"]);
    expect(steps[2].getAttribute("aria-current")).toBe("step");
    expect((steps[3] as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(screen.getByRole("button", { name: "Davom etish" }));
    expect(onAction).toHaveBeenCalledWith(model.nextAction);
    expect(model.nextAction.skillKey).toBe("reading");
  });
  it("uses the approved Pen photographs for the lesson and book discovery", () => {
    const { container } = show(<><DailyLesson model={model} onAction={vi.fn()} /><HomeDiscoveries /></>);
    const sources = Array.from(container.querySelectorAll("img")).map(image => image.getAttribute("src"));
    expect(sources).toContain("/assets/play/home-photos/my-mother.jpg");
    expect(sources).toContain("/assets/play/home-photos/the-island-secret.jpg");
    expect(sources).not.toContain("/assets/play/my-mother.svg");
    expect(sources).not.toContain("/assets/play/island-secret.svg");
  });
  it("shows a fallback recommendation rather than the completed topic's old title", () => {
    show(<DailyLesson model={{ ...model, nextAction: { ...model.nextAction, title: "Kitoblar", fallbackKind: "books" } }} onAction={vi.fn()} />);
    expect(screen.getByRole("heading", { name: "Kitoblar" })).toBeTruthy();
    expect(screen.queryByRole("heading", { name: "My Mother" })).toBeNull();
  });
  it("uses six compact catalog shortcuts in the approved order", () => {
    const onAction = vi.fn();
    show(<SkillLauncher model={model} onAction={onAction} />);
    const buttons = within(screen.getByRole("region", { name: "Mashq tanlang" })).getAllByRole("button");
    expect(buttons.map(button => button.textContent)).toEqual(HOME_SKILLS.map(skill => skill.label));
    fireEvent.click(buttons[3]);
    expect(onAction).toHaveBeenCalledWith(model.modules.find(item => item.key === "writing"));
  });
  it("reads streak and six-skill progress, independently of the personal task goal", () => {
    const { unmount } = show(<DailyHabit model={model} />);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe("17");
    expect(screen.getByText("7 kun")).toBeTruthy();
    expect(screen.getByRole("list", { name: "So‘nggi 7 kundagi faollik" }).querySelectorAll("svg")).toHaveLength(6);
    expect(within(screen.getByRole("list", { name: "So‘nggi 7 kundagi faollik" })).getAllByRole("listitem").map(day => day.textContent)).toEqual(["Juma", "Shan", "Yak", "Dush", "Sesh", "Chor", "Pay"]);
    unmount();
    const second = show(<DailyHabit model={{ ...model, todayCompletedSkills: 6, todayCompletedTasks: 8, dailyGoalTarget: 3 }} />);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe("100");
    expect(screen.getByText("6/6", { exact: false })).toBeTruthy();
    second.unmount();
    show(<DailyHabit model={{ ...model, todayCompletedSkills: 0, dailyGoalTarget: 0 }} />);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe("0");
    expect(screen.getByText("0/6", { exact: false })).toBeTruthy();
  });
  it("renders 3/6 skills and 50 percent even when the personal daily goal is four tasks", () => {
    show(<DailyHabit model={{ ...model, todayCompletedSkills: 3, todayCompletedTasks: 3, dailyGoalTarget: 4 }} />);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe("50");
    expect(screen.getByText("3/6", { exact: false }).textContent).toBe("3/6 skill");
    expect(screen.queryByText("3/4", { exact: false })).toBeNull();
  });
  it("shows real due words and 3/7/21 milestones; opens the saved vocabulary", () => {
    show(<DueReviewCard model={model} />);
    expect(screen.getByText("My Mother · 3-kun")).toBeTruthy();
    expect(screen.getByText("21")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Takrorlash: 20 ta so‘z tayyor" }));
    expect(screen.getByLabelText("Joriy sahifa").textContent).toBe("/app/vocabulary/saved");
  });
  it("does not claim a due topic when no words are due", () => {
    show(<DueReviewCard model={{ ...model, dueSavedCount: 0, dueTopicTitle: null, dueReviewDay: undefined }} />);
    expect(screen.getByText("Hozircha takrorlash yo‘q")).toBeTruthy();
    expect(screen.queryByText("My Mother · 3-kun")).toBeNull();
  });
  it.each([["The Island Secret — O‘qish", "/books"], ["1 daqiqa suhbat — Boshlash", "/app/speaking"]])("keeps discovery action %s working", (name, path) => {
    show(<HomeDiscoveries />);
    fireEvent.click(screen.getByRole("button", { name }));
    expect(screen.getByLabelText("Joriy sahifa").textContent).toBe(path);
  });
});

describe("Home video cards", () => {
  it("opens the API's lesson id, not a YouTube id masquerading as a lesson id", async () => {
    openVideo.mockResolvedValue({ id: "stored-lesson-42" });
    show(<VideoRecommendations />);
    fireEvent.click(screen.getByRole("button", { name: "Introduce yourself videosini ochish" }));
    expect(openVideo).toHaveBeenCalledWith(HOME_VIDEOS[0]);
    await waitFor(() => expect(screen.getByLabelText("Joriy sahifa").textContent).toBe("/video/stored-lesson-42/play"));
  });
  it("prevents repeated opens while the first request is pending", async () => {
    let resolve!: (value: { id: string }) => void;
    openVideo.mockImplementation(() => new Promise(done => { resolve = done; }));
    show(<VideoRecommendations />);
    const first = screen.getByRole("button", { name: "Introduce yourself videosini ochish" });
    fireEvent.click(first); fireEvent.click(first);
    expect(openVideo).toHaveBeenCalledTimes(1);
    expect((screen.getByRole("button", { name: "Daily routines videosini ochish" }) as HTMLButtonElement).disabled).toBe(true);
    resolve({ id: "lesson" });
    await waitFor(() => expect(screen.getByLabelText("Joriy sahifa").textContent).toBe("/video/lesson/play"));
  });
  it("provides a retry and catalog fallback after a failed request", async () => {
    openVideo.mockRejectedValue(new Error("offline"));
    show(<VideoRecommendations />);
    fireEvent.click(screen.getByRole("button", { name: "Daily routines videosini ochish" }));
    expect(await screen.findByRole("alert")).toBeTruthy();
    expect((screen.getByRole("button", { name: "Daily routines videosini ochish" }) as HTMLButtonElement).disabled).toBe(false);
    fireEvent.click(screen.getByRole("button", { name: "video katalogini oching" }));
    expect(screen.getByLabelText("Joriy sahifa").textContent).toBe("/video");
  });
});

describe("Home word of the day", () => {
  it("saves the displayed word once and refreshes the real vocabulary count", async () => {
    learnWord.mockResolvedValue({ id: "word" });
    const onSaved = vi.fn();
    show(<DailyWord onSaved={onSaved} />);
    fireEvent.click(screen.getByRole("button", { name: "grateful so‘zini saqlash" }));
    await screen.findByRole("button", { name: "grateful saqlangan" });
    expect(learnWord).toHaveBeenCalledWith(expect.any(String), "grateful", "minnatdor");
    expect(onSaved).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole("button", { name: "grateful saqlangan" }));
    expect(learnWord).toHaveBeenCalledOnce();
  });
  it("recognizes a word already in the learner's saved vocabulary", () => {
    show(<DailyWord alreadySaved />);
    expect((screen.getByRole("button", { name: "grateful saqlangan" }) as HTMLButtonElement).disabled).toBe(true);
    expect(learnWord).not.toHaveBeenCalled();
  });
  it("reports save errors and permits retry without displaying false success", async () => {
    learnWord.mockRejectedValue(new Error("offline"));
    show(<DailyWord />);
    fireEvent.click(screen.getByRole("button", { name: "grateful so‘zini saqlash" }));
    expect(await screen.findByText("So‘z saqlanmadi. Qayta urinib ko‘ring.")).toBeTruthy();
    expect((screen.getByRole("button", { name: "grateful so‘zini saqlash" }) as HTMLButtonElement).disabled).toBe(false);
  });
  it("plays the word through the existing speech utility and reports unsupported devices", () => {
    speakWord.mockReturnValue(false);
    show(<DailyWord />);
    fireEvent.click(screen.getByRole("button", { name: "grateful talaffuzini tinglash" }));
    expect(speakWord).toHaveBeenCalledWith("grateful");
    expect(within(screen.getByRole("region", { name: "Kun so‘zi" })).getByRole("status").textContent).toContain("ovoz mavjud emas");
  });
});
