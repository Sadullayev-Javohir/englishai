import { test, expect } from "@playwright/test";
import fs from "node:fs";
import type { PlacementItemDto } from "../src/api/types";

// Opt-in live local API test; never the operator's learner profile. No provider mocks.
const accountFile = process.env.PLACEMENT_LIVE_ACCOUNT;
const audioFile = process.env.PLACEMENT_LIVE_AUDIO;
test.use({ trace: "off", video: "off", launchOptions: { args: [
  "--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream",
  ...(audioFile ? [`--use-file-for-fake-audio-capture=${audioFile}`] : []),
] } });
test("@live-placement 23 real items, real speech, persisted result and reload", async ({ page, context }, info) => {
  test.skip(!accountFile || !audioFile, "Requires an isolated local QA account and spoken WAV fixture.");
  test.setTimeout(240_000);
  const account = JSON.parse(fs.readFileSync(accountFile!, "utf8")) as { learnerId: string; token: string };
  const origin = new URL(process.env.PW_BASE_URL ?? "http://127.0.0.1:5173").origin;
  expect(["127.0.0.1", "localhost"]).toContain(new URL(origin).hostname);
  await context.addCookies([{ name: "englishai_auth", value: account.token, url: origin, httpOnly: true, sameSite: "Lax" }]);
  await page.setViewportSize({ width: 1440, height: 1080 });
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.goto("/assessment");
  await expect(page.getByRole("heading", { name: /Qanchalik bilasiz/ })).toBeVisible();
  await page.getByRole("button", { name: "Mikrofonni tekshirish" }).click();
  await expect(page.getByRole("button", { name: /Mikrofon tayyor/ })).toBeVisible();
  await page.getByRole("button", { name: "Tayyorman, boshlaymiz!" }).click();
  const startResponse = page.waitForResponse(response => response.url().endsWith("/api/placement/start") && response.request().method() === "POST");
  await page.getByRole("button", { name: "Testni boshlash", exact: true }).click();
  const start = await (await startResponse).json();
  expect(start.firstItem.totalItems).toBe(23);
  expect(start.firstItem.stageCount).toBe(6);
  const sessionId = start.sessionId;
  let current: PlacementItemDto = start.firstItem;
  const stages = new Set<number>();
  let answered = 0;
  while (current && answered < 24) {
    stages.add(current.stage);
    await expect(page.getByRole("heading", { level: 1 })).toHaveText(current.prompt);
    await expect(page.getByRole("progressbar", { name: "Test jarayoni" })).toHaveAttribute("aria-valuenow", String(current.stageNumber));
    if (current.hasAudio) {
      const audioResponse = page.waitForResponse(response => response.url().includes("/api/placement/audio/") && [200, 206].includes(response.status()));
      await page.getByRole("button", { name: "Tinglash", exact: true }).click();
      expect((await audioResponse).headers()["content-type"]).toContain("audio/");
      await expect.poll(() => page.locator("audio").evaluate((audio: HTMLAudioElement) => audio.currentTime)).toBeGreaterThan(.1);
      const pause = page.getByRole("button", { name: "To'xtatib turish" });
      if (await pause.isVisible()) await pause.click();
    }
    let endpoint = "/api/placement/answer";
    if (current.kind === 1) {
      endpoint += "/writing";
      const words = "My name is Alex and I live in a small town with my family. I am twenty years old. At the weekend I usually meet my friends in the park. We play football and then drink tea together. I would like to visit London because I enjoy learning about other places. I prefer studying in a classroom because I can ask my teacher questions and discuss new ideas with my friends. Technology is useful for learning, but we should also spend time with people in real life. Protecting the environment is important for our future. We should support new businesses that use clean energy and create jobs for young people because education and work can improve our lives.".split(/\s+/);
      const text = words.slice(0, Math.max(current.minWords ?? 25, Math.min(words.length, current.maxWords ?? words.length))).join(" ");
      await page.getByRole("textbox", { name: "Javobingiz" }).fill(text);
      // Resume exact current item and persisted draft after reload.
      await page.reload();
      await page.getByRole("button", { name: "Testni davom ettirish" }).click();
      await expect(page.getByRole("textbox", { name: "Javobingiz" })).toHaveValue(text);
    } else if (current.kind === 2) {
      endpoint += "/speaking";
      await page.getByRole("button", { name: "Yozishni boshlash" }).click();
      await expect(page.locator(".play-placement__timer")).toHaveText("0:31", { timeout: 40_000 });
      await page.getByRole("button", { name: "Yozishni to‘xtatish" }).click();
      await expect(page.getByRole("button", { name: "Ovozli javobni yuborish" })).toBeEnabled();
      await page.screenshot({ path: info.outputPath("speaking-before-real-assessment.png"), fullPage: true });
    } else await page.getByRole("group", { name: "Javob variantlari" }).getByRole("button").first().click();
    const responsePromise = page.waitForResponse(response => response.url().endsWith(endpoint) && response.request().method() === "POST", { timeout: 90_000 });
    await page.getByRole("button", { name: current.kind === 1 ? "Yuborish va davom etish" : current.kind === 2 ? "Ovozli javobni yuborish" : "Javobni yuborish", exact: true }).click();
    const response = await responsePromise;
    expect(response.status()).toBe(200);
    const answer = await response.json();
    expect(answer.retryable ?? false, JSON.stringify(answer)).toBe(false);
    answered++;
    if (answer.isTestCompleted) break;
    expect(answer.nextItem).toBeTruthy();
    current = answer.nextItem;
  }
  expect(answered).toBe(23);
  expect([...stages].sort()).toEqual([1, 2, 3, 4, 5, 6]);
  await expect(page).toHaveURL(/\/placement\/result$/);
  await expect(page.getByRole("article")).toHaveCount(6);
  const resultText = await page.locator(".play-result").innerText();
  await page.screenshot({ path: info.outputPath("real-six-skill-result.png"), fullPage: true });
  expect((await (await page.request.get("/api/auth/me")).json()).hasOnboarded).toBe(true);
  await page.reload();
  await expect(page).toHaveURL(/\/placement\/result$/);
  await expect(page.locator(".play-result")).toHaveText(resultText.replace(/\s+/g, " "), { useInnerText: true });
  const repeated = await page.request.post("/api/placement/finalize", { data: { sessionId } });
  expect(repeated.status()).toBe(200);
  const result = await repeated.json();
  expect(result.stageResults).toHaveLength(6);
  fs.writeFileSync(info.outputPath("verified-result.json"), JSON.stringify({ learnerId: account.learnerId, sessionId, answered, result }, null, 2));
  expect(errors).toEqual([]);
});
