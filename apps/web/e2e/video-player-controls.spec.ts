import { expect, test, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

const LESSON_ID = "7d4771fb-6c8d-4f40-ae53-28d6bf5d7e5f";
const LESSON_URL = `/video/${LESSON_ID}/play`;
const lesson = {
  id: LESSON_ID, youTubeVideoId: "I_tRSrPru94", title: "How to introduce yourself",
  channel: "BBC Learning English", durationSeconds: 157, topic: "Introductions",
  level: 2, status: 1, transcriptStatus: 1,
  transcript: [
    { startSeconds: 6, endSeconds: 9, englishText: "Hello, what's your name?", uzbekTranslation: "Salom, ismingiz nima?", words: [] },
    { startSeconds: 9, endSeconds: 14, englishText: "Hi, I'm Tim.", uzbekTranslation: "Salom, men Timman.", words: [] },
    { startSeconds: 14, endSeconds: 18, englishText: "Hello, I'm Sian.", uzbekTranslation: "Salom, men Sianman.", words: [] },
    { startSeconds: 18, endSeconds: 22, englishText: "It's nice to meet you.", uzbekTranslation: "Tanishganimdan xursandman.", words: [] },
  ],
  glossary: [],
  questions: [{ id: "q1", prompt: "What is his name?", options: ["Tim", "Tom", "John", "Sam"], hintCode: null }],
};

const quizFixture = {
  quizId: "a1111111-1111-4111-8111-111111111111", videoLessonId: LESSON_ID,
  title: lesson.title, youTubeVideoId: lesson.youTubeVideoId, expiresAt: "2030-01-01T00:00:00Z", result: null,
  questions: Array.from({ length: 5 }, (_, index) => ({
    id: `question-${index}`, prompt: index === 0 ? "What does Tim say next?" : `Which phrase is used in the video? (${index + 1})`,
    promptUz: index === 0 ? "Tim keyin nima deydi?" : `Videoda qaysi ibora ishlatilgan? (${index + 1})`,
    options: ["It's nice to meet you.", "How old are you?", "Where do you live?", "I don't know."],
    sourceStartSeconds: 18, sourceEndSeconds: 22, sourceText: "It's nice to meet you.",
  })),
};
const quizResult = { videoLessonId: LESSON_ID, totalQuestions: 5, correctCount: 4, scorePercent: 80, passed: true, awardedXp: 40,
  outcomes: quizFixture.questions.map((q, index) => ({ questionId: q.id, selectedOptionIndex: index === 1 ? 1 : 0, correctOptionIndex: 0,
    isCorrect: index !== 1, hint: "Tim tanishganidan xursandligini aytyapti." })),
};

async function videoFixture(page: Page, options: { native?: boolean; unavailable?: boolean; longTranscript?: boolean; longCaption?: boolean; aiFailure?: boolean } = {}) {
  await page.addInitScript(() => {
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    localStorage.setItem("englishai.level.mock-learner-0001", "1");
    localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");
  });
  // Fixture traffic is isolated from the developer's signed-in backend and never writes data.
  await page.route("**/api/**", async route => {
    const url = new URL(route.request().url());
    // Vite also serves source modules under /src/api/; those are not API requests.
    if (!url.pathname.startsWith("/api/")) {
      await route.continue();
      return;
    }
    if (url.pathname.endsWith("/energy")) {
      await route.fulfill({ json: { current: 4, maximum: 5, consumed: true, nextRefillAt: "2030-01-01T00:00:00Z" } });
    } else if (url.pathname.endsWith("/points")) {
      await route.fulfill({ json: { lifetimeXp: 360, spendableCoins: 0, tiers: [], activeRedemptions: [] } });
    } else if (url.pathname === "/api/video/explain/stream") {
      await route.fulfill(options.aiFailure
        ? { status: 429, json: { message: "Too many requests" } }
        : { contentType: "text/event-stream", body: `event: chunk\ndata: ${JSON.stringify({ text: "Bu ibora birinchi marta tanishganda aytiladi. “It’s” — “It is” qisqartmasi.\nJavob: “Nice to meet you too.” — “Men ham xursandman.”" })}\n\n` });
    } else if (url.pathname === `/api/video/${LESSON_ID}/quiz` || url.pathname === `/api/video/quiz/${quizFixture.quizId}`) {
      await route.fulfill({ json: quizFixture });
    } else if (url.pathname === "/api/video/quiz" && route.request().method() === "POST") {
      await route.fulfill({ json: quizResult });
    } else if (url.pathname === `/api/video/${LESSON_ID}`) {
      const transcript = options.longTranscript
        ? Array.from({ length: 2300 }, (_, index) => ({ ...lesson.transcript[index % 4], startSeconds: index * 5, endSeconds: index * 5 + 5 }))
        : options.longCaption ? [{ ...lesson.transcript[3], englishText: "This is a very long sentence with useful vocabulary. ".repeat(60), uzbekTranslation: "Uzun tarjima. ".repeat(70) }] : lesson.transcript;
      await route.fulfill({ json: { ...lesson, transcript: options.unavailable ? [] : transcript, transcriptStatus: options.unavailable ? 2 : 1 } });
    } else if (url.pathname === "/api/auth/me") {
      await route.fulfill({ json: {
        id: "mock-learner-0001", email: "e2e@englishai.uz", displayName: "E2E", username: "e2e_user",
        hasCompletedDemographics: true, hasOnboarded: true, learningGoal: 1, isAdmin: false,
      } });
    } else {
      const response = await route.fetch({ url: `http://127.0.0.1:5055${url.pathname}${url.search}` });
      await route.fulfill({ response });
    }
  });
  if (options.native) return;
  // The real YouTube service is deliberately not required in CI. Capture its options and
  // reproduce the injected iframe's intrinsic size, not a fake set of YouTube controls.
  await page.addInitScript(() => {
    type PlayerOptions = { playerVars?: Record<string, unknown>; events?: { onReady?: (event: { target: unknown }) => void } };
    const scope = window as unknown as { YT: unknown; __videoOptions: PlayerOptions; __videoTime: number };
    scope.__videoTime = 18;
    scope.YT = {
      Player: class {
        iframe: HTMLIFrameElement;
        constructor(target: string, options: PlayerOptions) {
          scope.__videoOptions = options;
          const node = document.getElementById(target);
          this.iframe = document.createElement("iframe");
          this.iframe.id = target;
          this.iframe.width = "640";
          this.iframe.height = "360";
          node?.replaceWith(this.iframe);
          setTimeout(() => options.events?.onReady?.({ target: this }), 0);
        }
        getIframe() { return this.iframe; }
        getCurrentTime() { return scope.__videoTime; }
        getDuration() { return 157; }
        getPlayerState() { return 2; }
        getPlaybackRate() { return 1; }
        getVolume() { return 80; }
        isMuted() { return false; }
        playVideo() {}
        pauseVideo() {}
        seekTo(seconds: number) { scope.__videoTime = seconds; }
        destroy() { this.iframe.remove(); }
      },
    };
  });
}

test.afterEach(async ({ page }) => { await page.unrouteAll({ behavior: "wait" }); });

async function expectBounded(page: Page) {
  const geometry = await page.evaluate(() => ({
    width: document.documentElement.scrollWidth, height: document.documentElement.scrollHeight,
    viewportWidth: innerWidth, viewportHeight: innerHeight,
    body: document.body.scrollHeight,
  }));
  expect(geometry.width).toBeLessThanOrEqual(geometry.viewportWidth);
  expect(geometry.height).toBeLessThanOrEqual(geometry.viewportHeight + 1);
  expect(geometry.body).toBeLessThanOrEqual(geometry.viewportHeight + 1);
  await page.mouse.move(4, 200);
  await page.mouse.wheel(0, 800);
  expect(await page.evaluate(() => window.scrollY)).toBe(0);
}

for (const [width, height] of [[360, 800], [375, 667], [390, 808], [390, 844], [412, 915], [700, 844], [768, 1024], [1024, 768], [1280, 720], [1440, 900], [1920, 1080]]) {
  test(`@smoke Pen 72 native video, timed transcript and inline AI at ${width}x${height}`, async ({ page }, info) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.setViewportSize({ width, height });
    await videoFixture(page);
    await page.goto(LESSON_URL);
    await expect(page.getByRole("heading", { name: lesson.title })).toBeVisible();
    await page.evaluate(() => document.fonts.ready);
    await expect(page.locator('[data-caption-index="3"]')).toHaveAttribute("aria-current", "true");
    await expect(page.locator(".video-player-frame iframe")).toHaveAttribute("allowfullscreen", "");
    await expect(page.locator(".video-player-frame button, .video-player-frame img, [data-video-controls], [data-video-caption]")).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Tanlangan gapni takrorlash", includeHidden: true })).toHaveCount(0);
    await expect(page.locator(".shadowing-practice-page, .shadowing-panel, .ea-assistant-trigger")).toHaveCount(0);
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(page.getByRole("link", { name: "Energiya: 4 / 5" })).toBeVisible();
    const geometry = await page.evaluate(() => {
      const box = (selector: string) => {
        const rect = document.querySelector(selector)!.getBoundingClientRect();
        return { x: rect.x, y: rect.y, width: rect.width, height: rect.height, bottom: rect.bottom, right: rect.right };
      };
      const iframe = document.querySelector(".video-player-frame iframe")!;
      const rect = iframe.getBoundingClientRect();
      return { video: box(".video-player-frame"), captions: box(".video-player-captions"), info: box(".video-player-info"),
        ai: box(".video-player-contextual-ai"), iframeReceivesClicks: document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2) === iframe,
        options: (window as unknown as { __videoOptions: { playerVars: unknown } }).__videoOptions.playerVars };
    });
    expect(geometry.video.width / geometry.video.height).toBeCloseTo(16 / 9, 2);
    expect(geometry.iframeReceivesClicks).toBe(true);
    expect(geometry.options).toMatchObject({ controls: 1, disablekb: 0, fs: 1, playsinline: 1 });
    if (width <= 700) {
      expect(geometry.captions.y).toBeGreaterThanOrEqual(geometry.info.bottom);
      if (height >= 760 && width < 500) {
        expect(geometry.ai.y).toBeGreaterThanOrEqual(geometry.captions.bottom);
        await expect(page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" })).toBeVisible();
      } else await expect(page.locator(".video-player-ai-toggle")).toBeVisible();
    } else {
      expect(geometry.captions.right).toBeLessThanOrEqual(geometry.video.x - 10);
      expect(geometry.captions.y).toBeCloseTo(geometry.video.y, 0);
      expect(geometry.ai.y).toBeGreaterThan(geometry.info.bottom);
      await expect(page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" })).toBeVisible();
    }
    if (width === 1440) {
      expect(geometry.captions.x).toBe(40);
      expect(geometry.captions.width).toBeCloseTo(488, 0);
      expect(geometry.video.width).toBeCloseTo(848, 0);
      expect(geometry.video.y).toBe(88);
    }
    await expectBounded(page);
    await page.screenshot({ path: info.outputPath(`pen72-${width}-${height}.png`), fullPage: true });
    expect(errors).toEqual([]);
  });
}

for (const width of [390, 1440]) {
  test(`energy notice is removed and AI input has only the outer focus border at ${width}px`, async ({ page }, info) => {
    await page.setViewportSize({ width, height: width === 390 ? 808 : 900 });
    await videoFixture(page);
    await page.goto(LESSON_URL);
    await expect(page.getByRole("heading", { name: lesson.title })).toBeVisible();
    await expect(page.locator(".video-player-energy")).toHaveCount(0);
    await expect(page.getByText("Video ochildi. Yaxshi tomosha!", { exact: true })).toHaveCount(0);
    await expect(page.getByRole("link", { name: "Energiya: 4 / 5" })).toBeVisible();
    const input = page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" });
    const composer = page.locator(".video-ai-contextual__composer");
    await input.click();
    await expect(input).toBeFocused();
    await expect(input).toHaveCSS("outline-style", "none");
    await expect(input).toHaveCSS("box-shadow", "none");
    await expect(input).toHaveCSS("border-top-width", "0px");
    await expect(composer).toHaveCSS("border-top-width", "1px");
    await expect(composer).toHaveCSS("border-top-color", "rgb(117, 69, 232)");
    await page.keyboard.press("Shift+Tab");
    await page.keyboard.press("Tab");
    await expect(input).toBeFocused();
    await expect(input).toHaveCSS("outline-style", "none");
    await expect(composer).toHaveCSS("border-top-color", "rgb(117, 69, 232)");
    await page.screenshot({ path: info.outputPath(`video-single-focus-${width}.png`), fullPage: true });
    await expectBounded(page);
  });

  test(`inline sentence translation and real request context at ${width}px`, async ({ page }, info) => {
    await page.setViewportSize({ width, height: width === 390 ? 808 : 900 });
    await videoFixture(page);
    await page.goto(LESSON_URL);
    const sentence = page.getByRole("button", { name: "00:14 — Hello, I'm Sian." });
    await sentence.click();
    await expect(page.locator('[data-caption-index="2"] .video-practice-translation')).toContainText("Salom, men Sianman.");
    await sentence.click();
    await expect(page.locator('[data-caption-index="2"] .video-practice-translation')).toBeHidden();
    await sentence.click();
    const request = page.waitForRequest(request => request.url().includes("/api/video/explain/stream"));
    await page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" }).fill("Bu jumla nimani anglatadi?");
    await page.getByRole("button", { name: "Yuborish", exact: true }).click();
    expect((await request).postDataJSON()).toMatchObject({ videoLessonId: LESSON_ID, focusText: "[00:14] Hello, I'm Sian.", userMessage: "Bu jumla nimani anglatadi?" });
    await expect(page.getByRole("log")).toContainText("Bu ibora birinchi marta");
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(page.locator(".video-player-frame iframe")).toBeVisible();
    await expectBounded(page);
    await page.screenshot({ path: info.outputPath(`pen72-ai-${width}.png`), fullPage: true });
  });
}

test("short mobile screen keeps video and composer visible when AI is expanded", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 });
  await videoFixture(page);
  await page.goto(LESSON_URL);
  await page.locator(".video-player-ai-toggle").click();
  await expect(page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" })).toBeVisible();
  await expect(page.locator(".video-player-frame iframe")).toBeVisible();
  await expect(page.locator(".video-player-captions")).toBeHidden();
  await expectBounded(page);
  await page.getByRole("button", { name: "Subtitrlarga qaytish" }).click();
  await expect(page.locator(".video-player-captions")).toBeVisible();
  await expectBounded(page);
});

test("long transcript windows remain reachable and manual browsing survives playback", async ({ page }) => {
  await videoFixture(page, { longTranscript: true });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(LESSON_URL);
  await expect(page.locator(".video-practice-line")).toHaveCount(4);
  await expect(page.locator(".video-practice-line").first()).toHaveAttribute("aria-setsize", "2300");
  await page.getByRole("button", { name: "Keyingi jumlalar" }).click();
  await expect(page.locator(".video-practice-line").first()).toHaveAttribute("data-caption-index", "4");
  await page.evaluate(() => { (window as unknown as { __videoTime: number }).__videoTime = 9000; });
  await expect(page.locator(".video-ai-contextual__heading")).toContainText("150:00");
  await expect(page.locator(".video-practice-line").first()).toHaveAttribute("data-caption-index", "4");
  await page.getByRole("button", { name: "Videoga mos kuzatish" }).click();
  await expect(page.locator('[data-caption-index="1800"]')).toHaveAttribute("aria-current", "true");
  await page.getByRole("button", { name: "Oldingi jumlalar" }).click();
  await expect(page.locator(".video-practice-line").first()).toHaveAttribute("data-caption-index", "1793");
  await expectBounded(page);
});

test("oversized individual captions scroll only inside the caption, not the page", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await videoFixture(page, { longCaption: true });
  await page.goto(LESSON_URL);
  await expect(page.locator(".video-practice-list.is-single-caption")).toBeVisible();
  await expect(page.getByRole("button", { name: "Keyingi jumlalar" })).toBeVisible();
  await expectBounded(page);
  const lastWord = page.locator(".video-practice-line").getByRole("button", { name: "vocabulary", exact: true }).last();
  await lastWord.scrollIntoViewIfNeeded();
  await expect(lastWord).toBeVisible();
  expect(await page.evaluate(() => window.scrollY)).toBe(0);
});

test("missing transcript retains the native video and disables unavailable learning actions", async ({ page }) => {
  await videoFixture(page, { unavailable: true });
  await page.goto(LESSON_URL);
  await expect(page.locator(".video-player-frame iframe")).toBeVisible();
  await expect(page.getByRole("button", { name: "Video testiga o‘tish" })).toBeDisabled();
  await expect(page.getByText(/interaktiv transkript mavjud emas/)).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" })).toBeDisabled();
  await expectBounded(page);
});

test("AI failure is honest and retry stays in the same inline panel", async ({ page }) => {
  await videoFixture(page, { aiFailure: true });
  await page.goto(LESSON_URL);
  await page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" }).fill("Izoh bering");
  await page.getByRole("button", { name: "Yuborish", exact: true }).click();
  await expect(page.getByRole("log")).toContainText("Juda ko'p savol yuborildi");
  await expect(page.locator(".video-player-frame iframe")).toBeVisible();
});

test("typing pins AI context while the video keeps playing", async ({ page }) => {
  await videoFixture(page);
  await page.goto(LESSON_URL);
  await expect(page.locator('[data-caption-index="3"]')).toHaveAttribute("aria-current", "true");
  const input = page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" });
  await input.fill("Shu jumla haqida");
  await page.evaluate(() => { (window as unknown as { __videoTime: number }).__videoTime = 9; });
  await expect(page.getByRole("button", { name: "Videoga mos kuzatish" })).toHaveAttribute("aria-pressed", "false");
  await expect(input).toHaveValue("Shu jumla haqida");
  const request = page.waitForRequest(request => request.url().includes("/api/video/explain/stream"));
  await page.getByRole("button", { name: "Yuborish", exact: true }).click();
  expect((await request).postDataJSON().focusText).toBe("[00:18] It's nice to meet you.");
  await expect(page.getByRole("log")).toContainText("Bu ibora");
  await page.getByRole("button", { name: "Videoga mos kuzatish" }).click();
  await expect(page.locator('[data-caption-index="1"]')).toHaveAttribute("aria-current", "true");
  await expect(page.getByRole("log")).not.toContainText("Bu ibora");
});

test("mobile visual viewport resize keeps the keyboard composer above the keyboard", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 });
  await videoFixture(page);
  await page.goto(LESSON_URL);
  await page.locator(".video-player-ai-toggle").click();
  await page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" }).focus();
  await page.evaluate(() => {
    Object.defineProperty(window.visualViewport, "height", { configurable: true, get: () => 400 });
    window.visualViewport?.dispatchEvent(new Event("resize"));
  });
  await expect(page.locator(".video-player-page")).toHaveAttribute("data-keyboard", "true");
  const composer = await page.locator(".video-ai-contextual__composer").boundingBox();
  expect(composer!.y + composer!.height).toBeLessThanOrEqual(400);
  await expect(page.locator(".video-player-frame iframe")).toBeVisible();
});

for (const width of [390, 1440]) {
  test(`quiz and results still work without shadowing at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    await videoFixture(page);
    await page.goto(LESSON_URL);
    await page.getByRole("button", { name: width === 390 ? "Quiz" : "Video testiga o‘tish", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Tim keyin nima deydi?" })).toBeVisible();
    for (let question = 0; question < 5; question++) {
      await page.getByRole("radio").nth(question === 1 ? 1 : 0).click();
      await page.getByRole("button", { name: "Javobni yuborish" }).click();
    }
    await expect(page.getByText("4 / 5", { exact: true })).toBeVisible();
    await expect(page.getByText("+40", { exact: true })).toBeVisible();
  });
}

for (const width of [390, 1440]) {
  test(`Pen 72 accessibility outside YouTube at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    await videoFixture(page);
    await page.goto(LESSON_URL);
    await expect(page.getByRole("heading", { name: lesson.title })).toBeVisible();
    const results = await new AxeBuilder({ page }).include(".video-player-page").exclude("#video-lesson-player").analyze();
    expect(results.violations).toEqual([]);
  });
}

test("network QA: renders the real YouTube player without app overlays", async ({ page }, info) => {
  test.skip(process.env.PW_LIVE_YOUTUBE !== "1", "Opt-in third-party player QA.");
  await videoFixture(page, { native: true });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(LESSON_URL);
  const iframe = page.locator(".video-player-frame iframe");
  await expect(iframe).toHaveAttribute("src", /youtube\.com\/embed\/I_tRSrPru94\?.*controls=1/);
  await expect(iframe).toHaveAttribute("allowfullscreen", "");
  await expect(page.frameLocator(".video-player-frame iframe").getByRole("button").first()).toBeVisible({ timeout: 30_000 });
  await page.screenshot({ path: info.outputPath("pen72-native-youtube.png"), fullPage: true });
});
