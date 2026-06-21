import { expect, test, type Page, type TestInfo } from "@playwright/test";
import { listeningFixture, listeningTopicId } from "./listening-pen.fixture";

async function viewportContract(page: Page, info: TestInfo, state: string, allowContentScroll = false) {
  await page.evaluate(() => document.fonts.ready);
  // Let the existing 300ms entrance animation settle before measuring/capturing.
  await page.waitForTimeout(350);
  const metrics = await page.evaluate(() => {
    const body = document.querySelector<HTMLElement>("[data-lesson-stage-body]")!;
    const footer = document.querySelector<HTMLElement>("[data-lesson-stage-footer]")!;
    const assistant = document.querySelector<HTMLElement>(".ea-assistant-trigger")?.getBoundingClientRect();
    const overlapsAssistant = assistant && [...footer.querySelectorAll("button,a")].some(element => {
      const box = element.getBoundingClientRect();
      return box.width > 0 && box.height > 0 && box.left < assistant.right && box.right > assistant.left && box.top < assistant.bottom && box.bottom > assistant.top;
    });
    return { documentY: document.documentElement.scrollHeight - innerHeight, documentX: document.documentElement.scrollWidth - innerWidth,
      contentY: body.scrollHeight - body.clientHeight, contentX: body.scrollWidth - body.clientWidth,
      footerTop: footer.getBoundingClientRect().top, footerBottom: footer.getBoundingClientRect().bottom, height: innerHeight, overlapsAssistant: Boolean(overlapsAssistant) };
  });
  await info.attach(`${state}-geometry`, { body: JSON.stringify(metrics), contentType: "application/json" });
  expect(metrics.documentY, `${state}: document must not scroll`).toBeLessThanOrEqual(1);
  expect(metrics.documentX, `${state}: horizontal document overflow`).toBeLessThanOrEqual(1);
  expect(metrics.contentX, `${state}: horizontal content overflow`).toBeLessThanOrEqual(1);
  if (!allowContentScroll) expect(metrics.contentY, `${state}: normal content should fit`).toBeLessThanOrEqual(1);
  expect(metrics.footerTop).toBeGreaterThanOrEqual(0);
  expect(metrics.footerBottom).toBeLessThanOrEqual(metrics.height + 1);
  expect(metrics.overlapsAssistant, `${state}: assistant must not cover footer buttons`).toBe(false);
  await page.screenshot({ path: info.outputPath(`${state}.png`) });
}

async function openPractice(page: Page) {
  await page.goto(`/listening/topic/${listeningTopicId}`);
  await page.getByRole("button", { name: "Darsni boshlash", exact: true }).click();
  await page.getByRole("button", { name: "Tinglash", exact: true }).click();
  await page.getByRole("button", { name: "Tushunishni tekshirish", exact: true }).click();
  await expect(page.locator('[data-pen-screen="38"]')).toBeVisible();
}

for (const [width, height] of [[1440, 900], [1366, 768], [1280, 720], [1024, 768], [820, 1180], [768, 1024], [390, 844], [360, 800], [375, 667], [320, 568]]) {
  test(`listening 36–40 viewport ${width}×${height}`, async ({ page }, info) => {
    await page.setViewportSize({ width, height });
    const { submissions } = await listeningFixture(page);
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.goto("/listening");
    await expect(page.locator(".listening-library__card")).toHaveCount(4);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
    await page.screenshot({ path: info.outputPath("36-catalog.png"), fullPage: true });
    await page.goto(`/listening/topic/${listeningTopicId}`);
    await expect(page.getByRole("button", { name: "Darsni boshlash", exact: true })).toBeVisible();
    await viewportContract(page, info, "introduction");
    await page.getByRole("button", { name: "Darsni boshlash", exact: true }).click();
    await expect(page.locator('[data-pen-screen="37"]')).toBeVisible();
    await viewportContract(page, info, "37-audio");
    await expect(page.getByRole("button", { name: "Tushunishni tekshirish", exact: true })).toBeDisabled();
    await page.getByRole("button", { name: "Tinglash", exact: true }).click();
    await expect.poll(() => page.locator("audio").evaluate((audio: HTMLAudioElement) => audio.currentTime)).toBeGreaterThan(0);
    await page.getByRole("button", { name: "Tushunishni tekshirish", exact: true }).click();
    await expect(page.locator('[data-pen-screen="38"]')).toBeVisible();
    await viewportContract(page, info, "38-quiz");
    await page.locator(".listening-topic__option").first().click();
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await expect(page.locator('[data-pen-screen="39"]')).toBeVisible();
    await expect(page.locator(".listening-topic__option")).toHaveCount(2);
    await viewportContract(page, info, "39-wrong");
    await page.getByRole("button", { name: "Keyingi savol", exact: true }).click();
    await expect(page.getByRole("heading", { name: "How does she get to work?" })).toBeVisible();
    await page.locator(".listening-topic__option").first().click();
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await viewportContract(page, info, "38-correct");
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await expect(page.locator(".listening-topic__transcript-card")).toBeVisible();
    await viewportContract(page, info, "transcript", true);
    await page.getByRole("button", { name: "Natijani ko'rish", exact: true }).click();
    await expect(page.locator('[data-pen-screen="40"]')).toBeVisible();
    await viewportContract(page, info, "40-result");
    expect(submissions).toHaveLength(1);
    expect(errors).toEqual([]);
  });
}

for (const [width, height] of [[1440, 900], [820, 1180], [390, 844]]) {
  test(`passed result stays inside ${width}×${height} and preserves next-skill navigation`, async ({ page }, info) => {
    await page.setViewportSize({ width, height });
    await listeningFixture(page);
    await openPractice(page);
    await page.locator(".listening-topic__option").nth(1).click();
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await page.getByRole("button", { name: "Keyingi savol", exact: true }).click();
    await page.locator(".listening-topic__option").first().click();
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await page.getByRole("button", { name: "Natijani ko'rish", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Endi yanada aniq eshitasiz." })).toBeVisible();
    await viewportContract(page, info, "40-passed");
    await page.locator("[data-lesson-stage-footer]").getByRole("button").first().click();
    await expect(page).toHaveURL(new RegExp(`/writing/task/${listeningTopicId}`));
  });
}

for (const [width, height] of [[1440, 900], [390, 844], [844, 390]]) {
  test(`long text scrolls internally, never hides the footer ${width}×${height}`, async ({ page }, info) => {
    await page.setViewportSize({ width, height });
    await listeningFixture(page, { longText: true, longQuestion: true });
    await openPractice(page);
    await viewportContract(page, info, "long-question", true);
    const body = page.locator("[data-lesson-stage-body]");
    expect(await body.evaluate(element => element.scrollHeight - element.clientHeight)).toBeGreaterThan(50);
    await body.evaluate(element => { element.scrollTop = element.scrollHeight; });
    expect(await body.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
    await page.locator(".listening-topic__option").first().click();
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await expect(page.locator(".listening-topic__transcript-evidence")).toBeVisible();
    await page.waitForTimeout(3200);
    await expect(page.locator('[data-pen-screen="39"]')).toBeVisible();
    await viewportContract(page, info, "long-transcript", true);
    await page.getByRole("button", { name: "Keyingi savol", exact: true }).click();
    await expect(page.locator('[data-pen-screen="38"]')).toBeVisible();
  });
}

test("unavailable audio cannot silently unlock the quiz", async ({ page }, info) => {
  await listeningFixture(page, { unavailableAudio: true });
  await page.goto(`/listening/topic/${listeningTopicId}`);
  await page.getByRole("button", { name: "Darsni boshlash", exact: true }).click();
  await expect(page.locator(".listening-player__error")).toBeVisible();
  await expect(page.getByRole("button", { name: "Tushunishni tekshirish", exact: true })).toBeDisabled();
  await expect(page.getByRole("button", { name: "Qayta tinglash", exact: true })).toBeEnabled();
  await viewportContract(page, info, "audio-error");
});
