import { expect, test, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { VIDEO_CATEGORIES } from "../src/pages/videoCategories";

const learnerId = "mock-learner-0001";
const videos = [
  { youTubeVideoId: "I_tRSrPru94", title: "How to introduce yourself: Easy English Conversations", durationSeconds: 157 },
  { youTubeVideoId: "bq6GBbh3uhU", title: "How to talk about your daily routine: Easy English Conversations", durationSeconds: 297 },
  { youTubeVideoId: "4C4wlOAscvY", title: "Talking about food | Real Easy English", durationSeconds: 343 },
].map((video) => ({
  ...video,
  lessonId: null,
  topic: "everyday",
  level: 2,
  channel: "BBC Learning English",
  channelAvatarUrl: "https://yt3.ggpht.com/catalog-fixture",
  hasClosedCaptions: true,
}));

async function videoFixture(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    localStorage.setItem("englishai.level.mock-learner-0001", "2");
  });
  await page.route("**/api/auth/me", (route) => route.fulfill({ json: {
    id: learnerId, email: "demo@englishai.uz", displayName: "Demo Learner", username: "demo",
    pictureUrl: null, hasOnboarded: true, preferredName: "Demo", learningGoal: 1,
    birthDate: "2000-01-01", gender: 1, acquisitionSource: 1, acquisitionSourceOther: null,
    hasCompletedDemographics: true,
  } }));
  await page.route("**/api/gamification/**/points", (route) => route.fulfill({ json: { lifetimeXp: 360 } }));
  await page.route("**/api/gamification/**/energy", (route) => route.fulfill({ json: { current: 5, maximum: 5, nextRefillAt: "2030-01-01T00:00:00Z" } }));
  await page.route("**/api/vocabulary/**/notifications", (route) => route.fulfill({ json: [] }));
  await page.route("https://yt3.ggpht.com/catalog-fixture", (route) => route.fulfill({
    contentType: "image/svg+xml",
    body: '<svg xmlns="http://www.w3.org/2000/svg" width="36" height="36"><rect width="36" height="36" fill="#7545e8"/></svg>',
  }));
  await page.route("**/api/video/search?*", (route) => {
    const query = new URL(route.request().url()).searchParams.get("q");
    const category = VIDEO_CATEGORIES.find((item) => item.query === query);
    const categoryVideo = category?.id === "all" ? videos : [{ ...videos[0], title: `${category?.label} category lesson` }];
    return route.fulfill({ json: { items: categoryVideo, nextCursor: null } });
  });
}

test.afterEach(async ({ page }) => {
  await page.unrouteAll({ behavior: "wait" });
});

test("screen 56 matches the Pen desktop frame", async ({ page }, info) => {
  await videoFixture(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/video");
  await expect(page.getByRole("heading", { level: 1 })).toHaveText("Tomosha qiling. O‘rganing.");
  await expect(page.getByText("Sevimli video, film va multfilmlaringiz bilan ingliz tilini mashq qiling.")).toBeVisible();
  await expect(page.getByRole("button", { name: "YouTube link qo‘shish" })).toBeVisible();
  await expect(page.getByRole("group", { name: "Video kataloglari" }).getByRole("button")).toHaveCount(8);
  await expect(page.getByRole("heading", { name: "Puss in Boots bilan inglizcha qismlar" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Playlistni ochish" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Siz uchun video darslar" })).toBeVisible();
  expect((await page.locator(".video-catalog__grid").evaluate((element) => getComputedStyle(element).gridTemplateColumns.split(" ").length))).toBe(2);
  await expect(page.locator(".video-catalog__card")).toHaveCount(2);
  await expect(page.locator(".video-catalog__featured-fallback")).toBeVisible();
  await expect(page.locator(".video-catalog-header__module")).toHaveText("Video");
  await expect(page.getByRole("button", { name: "Bildirishnomalar" })).toBeVisible();
  await expect(page.locator(".video-catalog-nav")).toBeHidden();
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
  await page.screenshot({ path: info.outputPath("video-pen-1440.png"), fullPage: true });
});

test("screen 56 matches the Pen mobile frame and preserves category behavior", async ({ page }, info) => {
  await videoFixture(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/video");
  await expect(page.getByRole("heading", { level: 1 })).toHaveText("Tomosha qiling. O‘rganing.");
  await expect(page.locator(".video-catalog-header__module")).toBeHidden();
  await expect(page.getByRole("navigation", { name: "Mobil navigatsiya" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Mobil navigatsiya" }).getByRole("button")).toHaveText(["Bugun", "Yo‘l", "Mashq", "Natija", "Profil"]);
  expect((await page.locator(".video-catalog__grid").evaluate((element) => getComputedStyle(element).gridTemplateColumns.split(" ").length))).toBe(1);
  expect((await page.locator(".video-catalog__grid").boundingBox())?.width).toBe(350);
  await expect(page.locator(".video-catalog__card")).toHaveCount(2);
  await expect(page.getByRole("button", { name: "Ko‘proq video" })).toBeVisible();
  await page.getByRole("button", { name: "Grammatika", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Grammatika category lesson" })).toBeVisible();
  await expect(page).toHaveURL(/category=grammar$/);
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
  await page.screenshot({ path: info.outputPath("video-pen-390.png"), fullPage: true });
});

test("screen 56 has no serious accessibility violations", async ({ page }) => {
  await videoFixture(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/video");
  await expect(page.locator(".video-catalog__card")).toHaveCount(2);
  const results = await new AxeBuilder({ page }).include('[data-testid="video-catalog"]').analyze();
  expect(results.violations.filter((violation) => violation.impact === "critical" || violation.impact === "serious")).toEqual([]);
});
