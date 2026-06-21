import { expect, test, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

const learnerId = "mock-learner-0001";
const topicId = "22222222-2222-2222-2222-222222222222";

test.afterEach(async ({ page }) => {
  // Route fetches started by the destination page can still be in flight at teardown.
  await page.unrouteAll({ behavior: "wait" });
});

/** Only the isolated mock preview is seeded; never rewrite the user's development account. */
async function homeFixture(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    localStorage.setItem("englishai.level.mock-learner-0001", "1");
    localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");
  });
  await page.route("**/api/auth/me", route => route.fulfill({ json: {
    id: learnerId,
    email: "javohir@example.test",
    displayName: "Javohir",
    username: "javohir",
    pictureUrl: null,
    hasOnboarded: true,
    preferredName: "Javohir",
    learningGoal: 1,
    birthDate: "2000-01-01",
    gender: 1,
    acquisitionSource: 1,
    acquisitionSourceOther: null,
    hasCompletedDemographics: true,
  } }));
  await page.route(`**/api/gamification/${learnerId}`, route => route.fulfill({ json: {
    currentStreak: 7, longestStreak: 7, todayCompletedTasks: 1, dailyGoalTarget: 4,
    isGoalMet: false, isStreakAtRisk: false, skillsCompletedToday: ["Vocabulary"],
  } }));
  await page.route(`**/api/gamification/${learnerId}/points`, route => route.fulfill({ json: { lifetimeXp: 360, spendableCoins: 0, tiers: [], activeRedemptions: [] } }));
  await page.route(`**/api/gamification/${learnerId}/leaderboard`, route => route.fulfill({ json: {
    level: 1, currentUserEntry: null,
    top: [{ rank: 7, learnerId, displayName: "Javohir", pictureUrl: null, score: 360, isPremium: false, isCurrentUser: true }],
  } }));
  await page.route(`**/api/levels/map/${learnerId}*`, async route => {
    const response = await route.fetch();
    const data = await response.json();
    const topic = {
      ...data.topics[1], id: topicId, title: "My Mother", titleUz: "Mening onam",
      modules: ["Vocabulary", "Grammar", "Reading", "Writing", "Speaking", "Listening"].map((module, index) => ({
        module, score: index < 2 ? 100 : 0, passed: index < 2, unlocked: index < 3, achievedAt: null,
      })),
    };
    await route.fulfill({ response, json: { ...data, topics: [topic], activeTopicId: topicId, topicsMastered: 0 } });
  });
  await page.route(`**/api/learning/${learnerId}/study-stats*`, route => route.fulfill({ json: {
    weekSeconds: 9720,
    last7Days: Array.from({ length: 7 }, (_, index) => ({
      day: `2026-09-${String(index + 4).padStart(2, "0")}`, seconds: index < 6 ? 1620 : 0,
    })),
  } }));
  await page.route(`**/api/speaking/practice-words/${learnerId}`, route => route.fulfill({ json: [{
    id: "practice-word-1", word: "grateful", lastAccuracyScore: 45, lastErrorType: 0,
    errorCount: 1, bestPracticeScore: null, firstFailedAt: "2026-09-10T08:00:00Z", lastFailedAt: "2026-09-10T08:00:00Z",
  }] }));
  await page.route("**/api/assistant/sessions?*", route => route.fulfill({ json: { items: [], nextCursor: null } }));
  await page.route(`**/api/vocabulary/${learnerId}/notifications?*`, route => route.fulfill({ json: {
    items: Array.from({ length: 3 }, (_, index) => ({
      id: `notification-${index}`, code: "review", title: null, message: "Takrorlash vaqti keldi.",
      createdAt: "2026-09-10T08:00:00Z", isRead: false, linkUrl: "/app/vocabulary/saved",
    })), nextCursor: null,
  } }));
  await page.route(`**/api/vocabulary/${learnerId}`, route => route.fulfill({ json: Array.from({ length: 128 }, (_, index) => ({
    id: `word-${index}`, learnerId, word: `word${index}`, translation: index === 0 ? "minnatdor" : index === 1 ? "mehribon" : index === 2 ? "xotirjam" : "so‘z", sourceTopicId: topicId,
    source: 0, stage: index < 20 ? 0 : 3, failCount: 0, nextReviewAt: index < 20 ? "2020-01-01T00:00:00Z" : null,
  })) }));
  await page.route(`**/api/vocabulary/${learnerId}/due`, route => route.fulfill({ json: Array.from({ length: 20 }, (_, index) => ({
    id: `due-word-${index}`, word: index === 0 ? "grateful" : `word${index}`, translation: index === 0 ? "minnatdor" : "so‘z",
    sourceTopicId: topicId, stage: 0, miniTestType: 0, partOfSpeech: null, exampleSentence: null, options: null,
  })) }));
  await page.route(`**/api/video/catalog/${learnerId}`, route => route.fulfill({ json: [{
    id: "v1111111-1111-4111-8111-111111111111", youTubeVideoId: "I_tRSrPru94",
    title: "Introduce yourself", channel: "BBC Learning English", durationSeconds: 157, topic: "everyday", level: 2,
  }] }));
}

for (const width of [360, 390, 412, 820, 1100, 1440]) {
  test(`screen 61 matches Pen layout at ${width}px`, async ({ page }, info) => {
    await homeFixture(page);
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.setViewportSize({ width, height: 900 });
    await page.goto("/home");
    await expect(page.getByRole("heading", { name: "Salom, Javohir!" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "My Mother" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Reyting: 7-o‘rin" })).toBeVisible();
    await page.evaluate(() => document.fonts.ready);
    await expect(page.getByRole("dialog")).toHaveCount(1);
    await expect(page.getByRole("button", { name: /Talaffuz mashqi/ })).toHaveCount(0);
    await expect(page.locator(".play-home-alert")).toHaveCount(0);
    await expect(page.getByRole("list", { name: "Dars ko‘nikmalari" }).locator('[data-completed="true"]')).toHaveCount(2);
    await expect(page.getByRole("region", { name: "Mashq tanlang" }).getByRole("button")).toHaveCount(6);
    await expect(page.getByRole("progressbar", { name: "Kunlik maqsad" })).toHaveAttribute("aria-valuenow", "17");
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)).toBeLessThanOrEqual(1);
    const lesson = await page.getByTestId("home-daily-lesson").boundingBox();
    expect(lesson).not.toBeNull();
    const secondaryVideo = page.getByRole("button", { name: "Daily routines videosini ochish" });
    if (width >= 1200) {
      await expect(page.getByRole("navigation", { name: "Mobil navigatsiya" })).toBeHidden();
      const sidebar = page.getByRole("complementary", { name: "Javohir o‘quv navigatsiyasi" });
      await expect(sidebar.getByRole("button", { name: "Saqlangan" })).toHaveCount(0);
      await expect(sidebar.getByRole("button", { name: "AI yordamchi" })).toHaveCount(0);
      await expect(sidebar.getByRole("button", { name: "Bildirishnomalar" })).toHaveCount(0);
      await expect(page.getByRole("banner").getByRole("button", { name: /AI yordamchi/i })).toHaveCount(0);
      await expect(page.getByRole("button", { name: "AI Yordamchi", exact: true })).toBeVisible();
      expect(lesson!.x).toBe(280);
      expect(lesson!.width).toBe(width - 640);
      expect(lesson!.height).toBe(292);
      expect(lesson!.y).toBe(216);
      const header = page.getByRole("banner", { name: "EnglishAI navigatsiyasi" });
      expect((await header.boundingBox())!.height).toBe(84);
      for (const name of ["360 XP", "Energiya holati"]) {
        await expect(header.getByRole("button", { name, exact: true }).locator("svg")).toHaveCount(1);
      }
      const notifications = header.getByRole("button", { name: "Bildirishnomalar" });
      expect((await notifications.boundingBox())!.width).toBe(44);
      expect((await notifications.boundingBox())!.height).toBe(44);
      await expect(notifications.getByText("3", { exact: true })).toHaveCSS("position", "absolute");
      const stats = page.getByRole("region", { name: "O‘qish statistikasi" });
      expect((await stats.boundingBox())!.height).toBe(80);
      expect((await stats.boundingBox())!.width).toBe(410);
      for (const card of await stats.getByRole("button").all()) {
        expect((await card.boundingBox())!.width).toBe(130);
        await expect(card).toHaveCSS("background-color", "rgb(255, 255, 255)");
        await expect(card).toHaveCSS("border-radius", "16px");
      }
      await expect(page.getByRole("region", { name: "Video darslar" }).getByText("A2", { exact: true })).toBeHidden();
      const photo = page.getByTestId("home-daily-lesson").locator("img");
      await expect(photo).toHaveAttribute("src", "/assets/play/home-photos/my-mother.jpg");
      await expect(photo).toHaveCSS("object-fit", "cover");
      expect((await photo.boundingBox())!.width).toBe(270);
      expect((await photo.boundingBox())!.height).toBe(180);
      const assistant = page.getByRole("button", { name: "AI Yordamchi", exact: true });
      expect((await assistant.boundingBox())!.x).toBe(width - 80);
      expect((await assistant.boundingBox())!.y).toBe(820);
      await expect(secondaryVideo.locator("img")).toHaveCSS("border-radius", "0px");
      expect((await secondaryVideo.locator("img").boundingBox())!.width).toBeGreaterThan(200);
    } else {
      const nav = page.getByRole("navigation", { name: "Mobil navigatsiya" });
      await expect(nav).toBeVisible();
      await expect(nav.getByRole("button")).toHaveCount(5);
      await expect(page.getByRole("button", { name: "Saqlangan", exact: true })).toBeVisible();
      expect(lesson!.x).toBe(20);
      expect(lesson!.width).toBe(width - 40);
      expect((await secondaryVideo.locator("img").boundingBox())!.width).toBe(112);
      const review = page.getByRole("button", { name: "Takrorlash: 20 ta so‘z tayyor" });
      const videos = page.getByRole("region", { name: "Video darslar" });
      const word = page.getByRole("region", { name: "Kun so‘zi" });
      const discoveries = page.getByRole("region", { name: "Kitoblar va suhbat" });
      expect((await review.boundingBox())!.y).toBeLessThan((await videos.boundingBox())!.y);
      expect((await discoveries.boundingBox())!.y).toBeLessThan((await word.boundingBox())!.y);
    }
    for (const image of await page.getByTestId("home-dashboard").locator("img").all()) {
      if (!await image.isVisible()) continue;
      await image.scrollIntoViewIfNeeded();
      await expect.poll(() => image.evaluate(element => (element as HTMLImageElement).naturalWidth)).toBeGreaterThan(0);
    }
    await page.evaluate(() => window.scrollTo(0, 0));
    expect(errors).toEqual([]);
    await page.screenshot({ path: info.outputPath(`home-${width}.png`), fullPage: true });
  });
}

for (const variant of [
  { random: 0.00, name: "Kunlik mini-mashq", testId: "home-entry-modal-mini-quiz" },
  { random: 0.26, name: "Takrorlash vaqti", testId: "home-entry-modal-due-review" },
  { random: 0.51, name: "7 kunlik yutuq", testId: "home-entry-modal-streak" },
  { random: 0.76, name: "Video tavsiyasi", testId: "home-entry-modal-video" },
]) {
for (const viewport of [
  { label: "WEB", width: 1440, height: 900, modalWidth: 560 },
  { label: "MOBILE", width: 390, height: 844, modalWidth: 350 },
]) {
test(`shows the ${variant.name} ${viewport.label} entry modal from real eligible learner data`, async ({ page }) => {
  await homeFixture(page);
  await page.addInitScript((value) => { Math.random = () => value; }, variant.random);
  await page.setViewportSize(viewport);
  await page.goto("/home");
  const dialog = page.getByRole("dialog", { name: variant.name });
  await expect(dialog).toBeVisible();
  await expect(page.getByTestId(variant.testId)).toBeVisible();
  const box = await dialog.boundingBox();
  expect(box).not.toBeNull();
  expect(box!.width).toBe(viewport.modalWidth);
  expect(box!.height).toBeLessThanOrEqual(viewport.height - 40);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)).toBeLessThanOrEqual(1);
});
}
}

test("today's goal counts three of six skills, not three of four tasks", async ({ page }, info) => {
  await homeFixture(page);
  await page.route(`**/api/gamification/${learnerId}`, route => route.fulfill({ json: {
    currentStreak: 7, longestStreak: 7, todayCompletedTasks: 9, dailyGoalTarget: 4,
    isGoalMet: true, isStreakAtRisk: false,
    skillsCompletedToday: ["Vocabulary", "Grammar", "Reading", "Vocabulary", "FirstLesson"],
  } }));
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/home");
  const activity = page.getByRole("region", { name: "Kundalik faollik" });
  await expect(activity.getByText("3/6 skill", { exact: true })).toBeVisible();
  await expect(activity.getByRole("progressbar", { name: "Kunlik maqsad" })).toHaveAttribute("aria-valuenow", "50");
  await expect(activity.getByText("50%", { exact: true })).toBeVisible();
  await expect(activity.getByText("3/4", { exact: false })).toHaveCount(0);
  await activity.screenshot({ path: info.outputPath("today-three-of-six-skills.png") });
});

test("home has no serious accessibility violations", async ({ page }) => {
  await homeFixture(page);
  await page.goto("/home");
  await expect(page.getByRole("heading", { name: "My Mother" })).toBeVisible();
  const results = await new AxeBuilder({ page }).include('[data-testid="home-dashboard"]').analyze();
  expect(results.violations.filter(item => item.impact === "critical" || item.impact === "serious")).toEqual([]);
});

test("desktop header notifications and viewport-fixed assistant keep the home route", async ({ page }) => {
  await homeFixture(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/home");
  await expect(page.getByRole("heading", { name: "My Mother" })).toBeVisible();
  await page.getByRole("dialog").getByRole("button", { name: "Modal oynani yopish" }).click();
  const assistant = page.getByRole("button", { name: "AI Yordamchi", exact: true });
  await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
  await expect(assistant).toBeVisible();
  expect((await assistant.boundingBox())!.y).toBe(820);
  await assistant.click();
  await expect(page.getByRole("dialog", { name: "AI Yordamchi" })).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page).toHaveURL(/\/home$/);
  await page.getByRole("banner", { name: "EnglishAI navigatsiyasi" }).getByRole("button", { name: "Bildirishnomalar" }).click();
  await expect(page).toHaveURL(/\/home\?notifications=open$/);
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page).toHaveURL(/\/home$/);
});

test("assistant and notifications open and close without losing the home route", async ({ page }) => {
  await homeFixture(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/home");
  await page.getByRole("dialog").getByRole("button", { name: "Modal oynani yopish" }).click();
  await page.getByRole("button", { name: "AI Yordamchi", exact: true }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.getByRole("banner").getByRole("button", { name: "Bildirishnomalar" }).click();
  await expect(page).toHaveURL(/\/home\?notifications=open$/);
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page).toHaveURL(/\/home$/);
});

test("word save persists via API, while video opening uses the returned lesson route", async ({ page }) => {
  await homeFixture(page);
  await page.route("**/api/vocabulary/learn", async route => {
    expect(route.request().postDataJSON()).toMatchObject({ learnerId, word: "grateful", translation: "minnatdor" });
    await route.fulfill({ json: { id: "saved-grateful" } });
  });
  await page.route("**/api/video/open", async route => {
    expect(route.request().postDataJSON().youTubeVideoId).toBe("I_tRSrPru94");
    await route.fulfill({ json: { id: "v1111111-1111-4111-8111-111111111111" } });
  });
  await page.goto("/home");
  await page.getByRole("dialog").getByRole("button", { name: "Modal oynani yopish" }).click();
  await page.getByRole("button", { name: "grateful so‘zini saqlash" }).click();
  await expect(page.getByRole("button", { name: "grateful saqlangan" })).toBeDisabled();
  await page.getByRole("button", { name: "Introduce yourself videosini ochish" }).click();
  await expect(page).toHaveURL(/\/video\/v1111111-1111-4111-8111-111111111111\/play$/);
});

for (const path of ["/levels", "/progress", "/leaderboard", "/profile", "/app/vocabulary/saved", "/support"]) {
  test(`shared sidebar omits assistant and saved words on ${path}`, async ({ page }) => {
    await homeFixture(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(path);
    const sidebar = page.getByRole("complementary", { name: "Javohir o‘quv navigatsiyasi" });
    await expect(sidebar).toBeVisible();
    await expect(sidebar.getByRole("button", { name: "Saqlangan" })).toHaveCount(0);
    await expect(sidebar.getByRole("button", { name: "AI yordamchi" })).toHaveCount(0);
    await expect(sidebar.getByText("Birga o‘rganamiz.")).toHaveCount(0);
    await expect(sidebar.getByRole("button", { name: "Bugun" })).toBeVisible();
    await expect(sidebar.getByRole("button", { name: "Bildirishnomalar" })).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`${path}$`));
    await expect(page.getByRole("button", { name: "AI Yordamchi", exact: true })).toBeVisible();
    if (path === "/progress") {
      await page.getByRole("button", { name: "AI Yordamchi", exact: true }).click();
      await expect(page.getByRole("dialog", { name: "AI Yordamchi" })).toBeVisible();
      await page.keyboard.press("Escape");
      await expect(page.getByRole("dialog")).toHaveCount(0);
      await expect(page).toHaveURL(/\/progress$/);
    }
  });
}
