import { expect, test, type Page, type TestInfo } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { readFile } from "node:fs/promises";
import { answerExercise, catalog, completionResponse, learnAllCards, openCards, pairs, topic, topicId, vocabularyFixture } from "./vocabulary-pen.fixture";

for (const viewport of [{ width: 1366, height: 768 }, { width: 390, height: 844 }]) {
  test(`turn reveals the next word image at ${viewport.width}px, not a blank backing card`, async ({ page }, info) => {
    await page.setViewportSize(viewport);
    await page.emulateMedia({ reducedMotion: "reduce" });
    const pictures = await Promise.all(["my-mother", "daily-routines", "at-the-market"].map(name => readFile(`public/assets/vocabulary/${name}.jpg`)));
    const requests: string[] = [];
    await page.route("**/api/images/vocabulary-topics/deck-test/*", route => {
      requests.push(route.request().url());
      const index = Number(new URL(route.request().url()).pathname.split("/").pop()!.split(".")[0]);
      return route.fulfill({ contentType: "image/jpeg", body: pictures[index % pictures.length] });
    });
    const imageUrl = (index: number) => `/api/images/vocabulary-topics/deck-test/${index}.jpg`;
    await page.route(`**/api/vocabulary/topic/${topicId}`, route => route.fulfill({ json: {
      ...topic, title: "My Family",
      words: topic.words.map((word, index) => ({ ...word, imageUrl: imageUrl(index) })),
    } }));
    await openCards(page);
    const preview = page.locator(".vocabulary-card-stack__preview");
    await expect(preview).toHaveAttribute("data-preview-word", "father");
    await expect(preview.locator("img")).toHaveAttribute("src", imageUrl(1));
    await expect(preview.locator("img")).toHaveAttribute("loading", "eager");
    await expect.poll(() => preview.locator("img").evaluate(img => (img as HTMLImageElement).naturalWidth)).toBeGreaterThan(0);
    await expect(page.locator(".vocabulary-card-stack__layers img")).toHaveCount(1);
    expect(requests.every(url => url.endsWith("/0.jpg") || url.endsWith("/1.jpg"))).toBe(true);
    await page.getByRole("button", { name: "Kartani aylantirish", exact: true }).click();
    const rotor = page.locator(".vocabulary-flashcard__rotor");
    await rotor.evaluate(el => {
      const animation = el.getAnimations()[0];
      if (!animation) throw new Error("The real-time turn must start");
      animation.pause();
      animation.currentTime = Number(animation.effect!.getComputedTiming().duration) / 2;
    });
    const [firstBox, nextBox] = await Promise.all([
      page.locator(".vocabulary-flashcard__side--front").boundingBox(), preview.boundingBox(),
    ]);
    expect(firstBox!.width).toBeLessThan(nextBox!.width * .1);
    await page.screenshot({ path: info.outputPath("next-word-image-revealed-mid-turn.png") });
    await rotor.evaluate(el => el.getAnimations().forEach(animation => animation.finish()));
    await page.getByRole("button", { name: "O‘rgandim →", exact: true }).click();
    await expect(page.locator(".vocabulary-flashcard__side--front img")).toHaveAttribute("src", imageUrl(1));
    await expect(preview).toHaveAttribute("data-preview-word", "family");
    await expect(preview.locator("img")).toHaveAttribute("src", imageUrl(2));
    await expect.poll(() => preview.locator("img").evaluate(img => (img as HTMLImageElement).naturalWidth)).toBeGreaterThan(0);
    await fitsViewport(page, info, "next-word-becomes-active");
    await page.getByRole("button", { name: /20\. minnatdor/ }).click();
    await expect(preview).toHaveCount(0);
    await expect(page.locator(".vocabulary-card-stack__card")).toHaveCount(0);
  });
}

// Route reloads and real media capture may share the machine with a production build.
test.setTimeout(60_000);
test.use({ viewport: { width: 1440, height: 980 }, permissions: ["microphone"], launchOptions: { args: ["--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream"] } });
const clientErrors = new WeakMap<Page, string[]>();
test.beforeEach(async ({ page }) => {
  const errors: string[] = []; clientErrors.set(page, errors);
  page.on("pageerror", error => errors.push(error.message));
  await vocabularyFixture(page);
});
test.afterEach(async ({ page }) => { await page.unrouteAll({ behavior: "wait" }); expect(clientErrors.get(page)).toEqual([]); });

async function verifyScreen(page: Page, info: TestInfo, number: string) {
  await page.evaluate(() => document.fonts.ready);
  await expect(page.locator(`[data-pen-screen="${number}"]`)).toBeVisible();
  await expect(page.locator(".vocabulary-header")).toHaveCount(1);
  await expect(page.locator(".play-sidebar,.play-catalog-topbar,.play-lesson-brand")).toHaveCount(0);
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)).toBeLessThanOrEqual(1);
  await expect(page.locator(".vocabulary-design")).toHaveCSS("background-color", "rgb(255, 252, 247)");
  await page.screenshot({ path: info.outputPath(`screen-${number}.png`), fullPage: true });
}

test("11 · Mavzular: Pen catalog geometry, exact photos, search and filters", async ({ page }, info) => {
  await page.goto("/app/vocabulary/topics");
  await expect(page.locator(".vocabulary-pen-catalog__card")).toHaveCount(4);
  await verifyScreen(page, info, "11");
  const grid = await page.locator(".vocabulary-pen-catalog__grid").boundingBox();
  expect(grid?.x).toBe(64); expect(grid?.width).toBe(1312); expect(grid?.y).toBe(444);
  const image = page.locator(".vocabulary-pen-catalog__photo").first();
  await expect(image).toHaveAttribute("src", "/assets/vocabulary/my-mother.jpg");
  await expect.poll(() => image.evaluate(el => (el as HTMLImageElement).naturalWidth)).toBeGreaterThan(0);
  await page.getByRole("searchbox").fill("market");
  await expect(page.locator(".vocabulary-pen-catalog__card")).toHaveCount(1);
  await page.getByRole("searchbox").fill("does-not-exist");
  await expect(page.getByRole("heading", { name: "Qidiruv natijasi topilmadi" })).toBeVisible();
  await page.getByRole("button", { name: "Qidiruvni tozalash", exact: true }).last().click();
  const allRequest = page.waitForRequest(request => request.url().includes("/api/vocabulary/topics/") && request.url().includes("all=true"));
  await page.getByRole("button", { name: "Barchasi", exact: true }).click(); await allRequest;
  await page.getByRole("button", { name: "Saqlangan", exact: true }).click(); await expect(page).toHaveURL(/\/app\/vocabulary\/saved/);
});

test("12 · Darsga kirish: photo, lesson facts, start and back", async ({ page }, info) => {
  await page.goto(`/app/vocabulary/topic/${topicId}`);
  await expect(page.getByRole("heading", { name: "My Mother", exact: true })).toBeVisible();
  await verifyScreen(page, info, "12");
  const hero = await page.locator(".vocabulary-intro").boundingBox(); expect(hero?.x).toBe(240); expect(hero!.y).toBeGreaterThan(140); expect(hero?.height).toBe(260);
  await expect(page.getByText("20 so‘z", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Darsni boshlash", exact: true }).click();
  await expect(page.locator('[data-pen-screen="13"]')).toBeVisible();
  await page.locator(".vocabulary-progress").getByRole("button", { name: "Orqaga", exact: true }).click();
  await expect(page.locator('[data-pen-screen="12"]')).toBeVisible();
});

test("13 · Kontekst matni: all 20 words highlighted and real translation toggle", async ({ page }, info) => {
  await page.goto(`/app/vocabulary/topic/${topicId}`); await page.getByRole("button", { name: "Darsni boshlash", exact: true }).click();
  await expect(page.locator(".hp-word")).toHaveCount(20);
  await verifyScreen(page, info, "13");
  await page.getByRole("button", { name: "O‘zbekcha ma’nosi", exact: true }).click();
  await expect(page.getByText("Qiyinchilikka duch kelganimda, onam menga yordam beradi.")).toBeVisible();
  await page.getByRole("button", { name: "O‘zbekcha ma’nosi", exact: true }).click();
  await expect(page.locator("#vocabulary-translation")).toHaveCount(0);
});

test("16 · Flashcard old tomon: exact stack, navigation never unlocks test", async ({ page }, info) => {
  await openCards(page); await verifyScreen(page, info, "16");
  const card = await page.locator(".vocabulary-flashcard").boundingBox();
  expect(card!.x).toBeGreaterThan(600); expect(card!.width).toBeGreaterThan(480);
  expect(card!.height).toBeLessThanOrEqual(440);
  await expect(page.locator(".vocabulary-collection__word")).toHaveCount(20);
  await expect(page.getByRole("button", { name: "Oldingi karta", exact: true })).toBeDisabled();
  await page.getByRole("button", { name: "Keyingi karta", exact: true }).click();
  await page.getByRole("button", { name: /20\. minnatdor/ }).click();
  await expect(page.getByRole("button", { name: "Keyingi karta", exact: true })).toBeDisabled();
  await expect(page.getByRole("button", { name: /Test hozircha yopiq/ })).toBeDisabled();
  await expect(page.locator(".vocabulary-flashcards__heading")).toContainText("0 / 20");
});

test("16B · Flashcard orqa tomon: sound, word, IPA and learned-once accounting", async ({ page }, info) => {
  await openCards(page); await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
  await verifyScreen(page, info, "16B");
  await expect(page.locator(".vocabulary-flashcard__example")).toContainText("My mother always supports me.");
  const soundRequest = page.waitForRequest("**/api/speaking/word/mother");
  await page.getByRole("button", { name: "mother — tinglash", exact: true }).click(); await soundRequest;
  await page.getByRole("button", { name: "O‘rgandim →", exact: true }).click();
  await expect(page.locator(".vocabulary-flashcards__heading")).toContainText("1 / 20");
  await page.getByRole("button", { name: /1\. ona — o‘rganildi/ }).click();
  await page.getByRole("button", { name: "Aylantirish", exact: true }).click(); await page.getByRole("button", { name: "O‘rgandim →", exact: true }).click();
  await expect(page.locator(".vocabulary-flashcards__heading")).toContainText("1 / 20");
});

test("16C · Test ochildi: only after all 20 learned, review keeps progress", async ({ page }, info) => {
  await openCards(page); await learnAllCards(page); await verifyScreen(page, info, "16C");
  await expect(page.locator(".vocabulary-collection__word.is-learned")).toHaveCount(20);
  await page.getByRole("button", { name: "Kartalarni yana ko‘rish", exact: true }).click();
  await expect(page.locator(".vocabulary-flashcards__heading")).toContainText("20 / 20");
  await page.getByRole("button", { name: "Testga tayyorsiz!", exact: true }).click();
  await page.getByRole("button", { name: "Testga o‘tish", exact: true }).click();
  await expect(page.locator(".vocabulary-practice")).toBeVisible();
});

async function openPatient(page: Page) {
  await openCards(page); await learnAllCards(page); await page.getByRole("button", { name: "Testga o‘tish", exact: true }).click();
  for (let i = 0; i < 5; i++) await answerExercise(page, i);
}

test("17 · So‘zni yozish: patient input, blank gating and Enter", async ({ page }, info) => {
  await openPatient(page); await verifyScreen(page, info, "17");
  await expect(page.getByRole("heading", { name: "sabrli", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Tekshirish", exact: true })).toBeDisabled();
  await page.getByRole("textbox", { name: "Javobingiz" }).fill("  PATIENT  "); await page.getByRole("textbox", { name: "Javobingiz" }).press("Enter");
  await expect(page.getByRole("button", { name: "Keyingi", exact: true })).toBeVisible();
});

test("18 · Xato va tuzatish: correction persists, retry does not charge a second heart", async ({ page }, info) => {
  await openPatient(page);
  await page.getByRole("textbox", { name: "Javobingiz" }).fill("patiant"); await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
  await verifyScreen(page, info, "18");
  await expect(page.getByText("Bitta harfga e’tibor bering.", { exact: true })).toBeVisible();
  await expect(page.getByLabel("4 ta jon")).toBeVisible();
  await page.getByRole("button", { name: "Qayta urinib ko‘rish", exact: true }).click();
  await page.getByRole("textbox", { name: "Javobingiz" }).fill("wrong-again"); await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
  await expect(page.getByLabel("4 ta jon")).toBeVisible();
  await page.getByRole("button", { name: "Qayta urinib ko‘rish", exact: true }).click();
  await page.getByRole("textbox", { name: "Javobingiz" }).fill("patient"); await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
  await expect(page.getByRole("button", { name: "Keyingi", exact: true })).toBeVisible();
});

test("19 · Talaffuz natijasi: real browser recorder to isolated score API, return retains card", async ({ page }, info) => {
  await openCards(page); await page.getByRole("button", { name: /4\. yordam/ }).click();
  await page.getByRole("button", { name: "Talaffuzni mashq qilish", exact: true }).click();
  await expect(page.getByRole("heading", { name: "support", exact: true })).toBeVisible();
  await expect(page.getByText("82 / 100", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "So‘zni aytish", exact: true }).click();
  await expect(page.getByRole("button", { name: "Yozishni tugatish", exact: true })).toBeVisible();
  // A synthetic browser microphone still needs a nonempty media chunk before stop.
  await page.waitForTimeout(600);
  const scoring = page.waitForRequest("**/api/vocabulary/pronounce");
  await page.getByRole("button", { name: "Yozishni tugatish", exact: true }).click();
  const request = await scoring; expect(request.postDataJSON().audioContent.length).toBeGreaterThan(100);
  await expect(page.getByText("82 / 100", { exact: true })).toBeVisible();
  await verifyScreen(page, info, "19");
  await expect(page.locator(".vocabulary-pronunciation__phonemes>div")).toHaveCount(2);
  await page.getByRole("button", { name: "Ovoz tezligini o‘zgartirish", exact: true }).click();
  await expect(page.getByRole("button", { name: "Ovoz tezligini o‘zgartirish", exact: true })).toHaveText("0.6×");
  await page.getByRole("button", { name: "So‘zga qaytish", exact: true }).click();
  await expect(page.locator(".vocabulary-collection__word[aria-current=step]")).toContainText("yordam");
});

test("20 · Dars yakuni: end-to-end score saved once and same-topic grammar continuation", async ({ page }, info) => {
  const submissions: number[] = [];
  page.on("request", request => { if (request.url().endsWith("/api/vocabulary/topic/completion")) submissions.push(request.postDataJSON().score); });
  await openCards(page); await learnAllCards(page); await page.getByRole("button", { name: "Testga o‘tish", exact: true }).click();
  for (let i = 0; i < pairs.length; i++) await answerExercise(page, i);
  await expect(page.getByRole("heading", { name: "So‘zlar endi sizniki!", exact: true })).toBeVisible();
  await verifyScreen(page, info, "20");
  await expect(page.locator(".vocabulary-result__metrics")).toContainText("20/20"); await expect(page.locator(".vocabulary-result__metrics")).toContainText("+100");
  expect(submissions).toEqual([100]);
  await page.getByRole("button", { name: "Grammar’ga o‘tish", exact: true }).click(); await expect(page).toHaveURL(new RegExp(`/app/grammar/topic/${topicId}`));
});

test("catalog keyboard accessibility and no client exceptions", async ({ page }) => {
  const errors: string[] = []; page.on("pageerror", error => errors.push(error.message));
  await page.goto("/app/vocabulary/topics"); await expect(page.locator(".vocabulary-pen-catalog__card")).toHaveCount(4);
  const result = await new AxeBuilder({ page }).include(".vocabulary-design").withTags(["wcag2a", "wcag2aa"]).analyze();
  expect(result.violations).toEqual([]); expect(errors).toEqual([]);
});

test("catalog error, empty and locked states are honest", async ({ page }) => {
  await page.route("**/api/vocabulary/topics/**", route => route.fulfill({ json: [{ ...catalog[0], isLocked: true }] }));
  await page.goto("/app/vocabulary/topics"); await expect(page.locator(".vocabulary-pen-catalog__card")).toBeDisabled();
  await page.route("**/api/vocabulary/topics/**", route => route.fulfill({ json: [] }));
  await page.reload(); await expect(page.getByRole("heading", { name: "Mavzular topilmadi" })).toBeVisible();
  await page.route("**/api/vocabulary/topics/**", route => route.fulfill({ status: 400, json: { message: "test error" } }));
  await page.reload(); await expect(page.getByRole("heading", { name: "Ma’lumotni yuklab bo‘lmadi" })).toBeVisible();
});

test("lesson pending and invalid pronunciation index do not fake success", async ({ page }) => {
  await page.goto(`/app/vocabulary/topic/${topicId}/pronunciation/3abc`);
  await expect(page.getByRole("heading", { name: "Talaffuz mashqini yuklab bo‘lmadi" })).toBeVisible();
  await page.route(`**/api/vocabulary/topic/${topicId}`, route => route.fulfill({ json: { ...topic, isReady: false } }));
  await page.goto(`/app/vocabulary/topic/${topicId}`); await expect(page.locator(".vocabulary-state")).toBeVisible();
  await expect(page.getByRole("button", { name: "Darsni boshlash", exact: true })).toHaveCount(0);
});

test("save failure exposes retry and only confirms the successful server response", async ({ page }) => {
  let attempts = 0;
  await page.route("**/api/vocabulary/topic/completion", route => {
    attempts++;
    return attempts === 1 ? route.fulfill({ status: 400, json: { message: "save rejected" } }) : route.fulfill({ json: completionResponse() });
  });
  await openCards(page); await learnAllCards(page); await page.getByRole("button", { name: "Testga o‘tish", exact: true }).click();
  for (let i = 0; i < pairs.length; i++) await answerExercise(page, i);
  await expect(page.getByRole("button", { name: "Qayta urinish", exact: true })).toBeVisible();
  await expect(page.getByText("Natijangiz saqlandi.", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Qayta urinish", exact: true }).click();
  await expect(page.getByText("Natijangiz saqlandi.", { exact: true })).toBeVisible(); expect(attempts).toBe(2);
});

for (const width of [1280, 1920]) {
  test(`desktop layout remains unclipped at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 980 });
    await page.goto("/app/vocabulary/topics"); await expect(page.locator(".vocabulary-pen-catalog__card")).toHaveCount(4);
    expect(await page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
    await openCards(page);
    expect(await page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
    await expect(page.locator(".vocabulary-flashcard")).toBeVisible();
  });
}
test("card learning survives passage and practice back navigation within the lesson", async ({ page }) => {
  await openCards(page);
  await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
  await page.getByRole("button", { name: "O‘rgandim →", exact: true }).click();
  await page.getByRole("button", { name: "Matnga qaytish", exact: true }).click();
  await page.getByRole("button", { name: "So‘zlarga o‘tamiz", exact: true }).click();
  await expect(page.locator(".vocabulary-flashcards__heading")).toContainText("1 / 20");
  await expect(page.locator(".vocabulary-collection__word[aria-current=step]")).toContainText("ota");
});

async function fitsViewport(page: Page, info: TestInfo, state: string) {
  await page.evaluate(() => document.fonts.ready);
  const expectedStep = await page.locator("[data-pen-screen]").evaluateAll(nodes => {
    const screen = nodes.map(n => n.getAttribute("data-pen-screen")).find(id => id !== "11");
    if (screen === "12") return "intro";
    if (screen === "13") return "text";
    if (screen?.startsWith("16") || screen === "19") return "flashcards";
    if (screen === "17" || screen === "18") return "test";
    if (screen === "20") return "result";
    return null;
  });
  if (expectedStep) {
    await expect(page.locator(".vocabulary-progress")).toHaveAttribute("data-lesson-step", expectedStep);
    await expect(page.locator(".vocabulary-progress__segment")).toHaveCount(5);
    await expect(page.locator('.vocabulary-progress__segment[aria-current="step"]')).toHaveCount(1);
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuemax", "5");
  }
  await page.locator(".vocabulary-flashcard__rotor").evaluateAll(elements => Promise.all(elements.flatMap(el => el.getAnimations().map(animation => animation.finished.catch(() => undefined)))));
  const geometry = await page.evaluate(() => {
    const root = document.documentElement;
    const visible = (el: HTMLElement) => el.checkVisibility() && !el.closest('[aria-hidden="true"]');
    const controls = [...document.querySelectorAll<HTMLElement>(".vocabulary-design button, .vocabulary-design input")]
      .filter(visible).filter(el => !el.closest(".vocabulary-header"));
    return {
      horizontal: root.scrollWidth - innerWidth,
      vertical: root.scrollHeight - innerHeight,
      clippedControls: controls.filter(el => {
        const box = el.getBoundingClientRect();
        return box.left < -1 || box.right > innerWidth + 1 || box.top < -1 || box.bottom > innerHeight + 1;
      }).map(el => el.textContent || el.getAttribute("aria-label")),
      overflowing: [...document.querySelectorAll<HTMLElement>(".vocabulary-design section, .vocabulary-reader, .vocabulary-flashcard__side:not([aria-hidden=true]), .vocabulary-flashcard__side:not([aria-hidden=true]) .vocabulary-flashcard__face")]
        .filter(visible).filter(el => el.scrollHeight > el.clientHeight + 2).map(el => `${el.className}: ${el.scrollHeight}/${el.clientHeight}`),
    };
  });
  if (geometry.vertical) console.log(state, await page.evaluate(() => [...document.querySelectorAll<HTMLElement>("body *")].filter(el => el.getBoundingClientRect().bottom > innerHeight + 1 && el.checkVisibility()).map(el => ({ tag: el.tagName, class: el.className, bottom: el.getBoundingClientRect().bottom, height: el.getBoundingClientRect().height, transform: getComputedStyle(el).transform, animation: getComputedStyle(el).animation, margin: getComputedStyle(el).margin, padding: getComputedStyle(el).padding })).slice(0, 20)));
  expect(geometry, state).toEqual({ horizontal: 0, vertical: 0, clippedControls: [], overflowing: [] });
  await page.screenshot({ path: info.outputPath(`${state}.png`) });
}

async function verifyQuizScreen(page: Page, info: TestInfo, state: string) {
  await fitsViewport(page, info, state);
  const covered = await page.locator(".vocabulary-exercise button:not(:disabled)").evaluateAll(elements => {
    const assistant = document.querySelector(".ea-assistant-trigger")?.getBoundingClientRect();
    if (!assistant) return [];
    return elements.filter(el => {
      const box = el.getBoundingClientRect();
      return box.right > assistant.left && box.left < assistant.right && box.bottom > assistant.top && box.top < assistant.bottom;
    }).map(el => el.textContent);
  });
  expect(covered, `${state}: assistant must not cover the quiz controls`).toEqual([]);
}

for (const viewport of [
  { width: 1440, height: 900 }, { width: 1366, height: 768 }, { width: 1280, height: 720 },
  { width: 1024, height: 768 }, { width: 820, height: 1180 }, { width: 768, height: 1024 },
  { width: 390, height: 844 }, { width: 360, height: 640 }, { width: 320, height: 640 },
]) {
  test(`responsive complete lesson ${viewport.width}x${viewport.height}: no short-stage scrolling`, async ({ page }, info) => {
    // This covers every stage, 20 cards, 20 answers and real media conversion.
    // Allow concurrent local builds without weakening any geometry assertion.
    test.setTimeout(120_000);
    await page.setViewportSize(viewport);
    await page.goto(`/app/vocabulary/topic/${topicId}`);
    await expect(page.getByRole("button", { name: "Darsni boshlash", exact: true })).toBeVisible();
    await fitsViewport(page, info, "12-introduction");
    await page.getByRole("button", { name: "Darsni boshlash", exact: true }).click();
    await expect(page.locator('[data-pen-screen="13"]')).toBeVisible();
    await page.screenshot({ path: info.outputPath("13-passage.png"), fullPage: true });
    await page.getByRole("button", { name: "So‘zlarga o‘tamiz", exact: true }).click();
    await fitsViewport(page, info, "16-front");
    await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
    await fitsViewport(page, info, "16B-back");
    await page.getByRole("button", { name: "Talaffuzni mashq qilish", exact: true }).click();
    await expect(page.getByRole("button", { name: "So‘zni aytish", exact: true })).toBeVisible();
    await fitsViewport(page, info, "19-pronunciation-idle");
    await page.getByRole("button", { name: "So‘zni aytish", exact: true }).click();
    await expect(page.locator(".vocabulary-pronunciation__empty")).toHaveClass(/is-recording/);
    await fitsViewport(page, info, "19-recording");
    await page.waitForTimeout(600);
    await page.getByRole("button", { name: "Yozishni tugatish", exact: true }).click();
    await expect(page.getByText("82 / 100", { exact: true })).toBeVisible();
    await fitsViewport(page, info, "19-result");
    await page.getByRole("button", { name: "So‘zga qaytish", exact: true }).click();
    await learnAllCards(page);
    await fitsViewport(page, info, "16C-ready");
    await page.getByRole("button", { name: "Testga o‘tish", exact: true }).click();
    await fitsViewport(page, info, "17-choice");
    await page.locator(".vocabulary-topic__test-options").getByRole("button", { name: /ona/ }).click();
    await fitsViewport(page, info, "17-choice-feedback");
    await page.getByRole("button", { name: "Keyingi", exact: true }).click();
    await fitsViewport(page, info, "17-typing");
    await page.getByRole("textbox", { name: "Javobingiz" }).fill("fathar");
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await fitsViewport(page, info, "18-correction");
    await page.getByRole("button", { name: "Qayta urinib ko‘rish", exact: true }).click();
    for (let index = 1; index < pairs.length; index++) await answerExercise(page, index);
    await expect(page.locator('[data-pen-screen="20"]')).toBeVisible();
    await fitsViewport(page, info, "20-result");
    await page.mouse.wheel(0, 700);
    expect(await page.evaluate(() => scrollY)).toBe(0);
  });
}

test("long passage and translation remain scrollable, returning to cards resets scroll", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route(`**/api/vocabulary/topic/${topicId}`, route => route.fulfill({ json: { ...topic, passage: `${topic.passage}\n\n`.repeat(8) } }));
  await page.goto(`/app/vocabulary/topic/${topicId}`);
  await page.getByRole("button", { name: "Darsni boshlash", exact: true }).click();
  await expect(page.locator(".vocabulary-reading")).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBeGreaterThan(844);
  await page.getByRole("button", { name: "O‘zbekcha ma’nosi", exact: true }).click();
  await expect(page.locator("#vocabulary-translation")).toBeVisible();
  await page.getByRole("button", { name: "So‘zlarga o‘tamiz", exact: true }).click();
  await expect(page.locator('[data-pen-screen="16"]')).toBeVisible();
  expect(await page.evaluate(() => scrollY)).toBe(0);
});

test("flip has a 3D transition and audio cue; playback and microphone animations follow activity", async ({ page }, info) => {
  await page.emulateMedia({ reducedMotion: "no-preference" });
  await page.addInitScript(() => {
    const target = window as Window & { vocabularyNotes: number };
    target.vocabularyNotes = 0;
    const start = OscillatorNode.prototype.start;
    OscillatorNode.prototype.start = function(when?: number) { target.vocabularyNotes++; return start.call(this, when); };
  });
  // A real decodable four-second WAV, not a made-up success flag or a provider call.
  const wav = Buffer.alloc(44 + 16000 * 4 * 2);
  wav.write("RIFF"); wav.writeUInt32LE(wav.length - 8, 4); wav.write("WAVEfmt ", 8);
  wav.writeUInt32LE(16, 16); wav.writeUInt16LE(1, 20); wav.writeUInt16LE(1, 22);
  wav.writeUInt32LE(16000, 24); wav.writeUInt32LE(32000, 28); wav.writeUInt16LE(2, 32); wav.writeUInt16LE(16, 34);
  wav.write("data", 36); wav.writeUInt32LE(wav.length - 44, 40);
  for (let i = 0; i < 64000; i++) wav.writeInt16LE(Math.round(Math.sin(i * 2 * Math.PI * 440 / 16000) * 500), 44 + i * 2);
  await page.route("**/api/speaking/word/*", route => route.fulfill({ json: { word: "mother", ipa: "/ˈmʌðə/", audioBase64: wav.toString("base64"), tipUz: "Tinglab, takrorlang." } }));
  await openCards(page);
  const notes = await page.evaluate(() => (window as Window & { vocabularyNotes: number }).vocabularyNotes);
  await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
  await expect.poll(() => page.evaluate(() => (window as Window & { vocabularyNotes: number }).vocabularyNotes)).toBeGreaterThan(notes);
  await expect(page.locator(".vocabulary-flashcard__rotor")).toHaveCSS("animation-duration", "0.56s");
  await expect.poll(() => page.locator(".vocabulary-flashcard__rotor").evaluate(el => getComputedStyle(el).transform)).toBe("matrix3d(-1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1)");
  await page.getByRole("button", { name: "mother — tinglash", exact: true }).click();
  await expect(page.locator(".vocabulary-flashcard__sound")).toHaveClass(/is-playing/);
  await page.getByRole("button", { name: "Talaffuzni mashq qilish", exact: true }).click();
  await page.getByRole("button", { name: "Namunani tinglash", exact: true }).click();
  await expect(page.locator(".vocabulary-voice")).toHaveAttribute("data-playback-state", "playing");
  await expect(page.locator(".vocabulary-voice__wave > span").first()).toHaveCSS("animation-name", "vocabulary-wave");
  await page.waitForTimeout(2700);
  await expect(page.locator(".vocabulary-voice")).toHaveAttribute("data-playback-state", "playing");
  await page.getByRole("button", { name: "Namunani to‘xtatish", exact: true }).click();
  await expect(page.locator(".vocabulary-voice")).toHaveAttribute("data-playback-state", "idle");
  await expect(page.locator(".vocabulary-voice__wave > span").first()).toHaveCSS("animation-name", "none");
  await page.getByRole("button", { name: "Namunani tinglash", exact: true }).click();
  await expect(page.locator(".vocabulary-voice")).toHaveAttribute("data-playback-state", "playing");
  await expect(page.locator(".vocabulary-voice")).toHaveAttribute("data-playback-state", "idle", { timeout: 6000 });
  await page.getByRole("button", { name: "So‘zni aytish", exact: true }).click();
  await expect(page.locator(".vocabulary-pronunciation__empty")).toHaveClass(/is-recording/);
  await expect.poll(() => page.locator(".vocabulary-mic-wave > span").evaluateAll(elements => elements.some(el => Number.parseFloat((el as HTMLElement).style.transform.slice(7)) > .08))).toBe(true);
  await page.screenshot({ path: info.outputPath("live-microphone-animation.png") });
  await page.getByRole("button", { name: "Namunani tinglash", exact: true }).isDisabled().then(disabled => expect(disabled).toBe(true));
  await page.getByRole("button", { name: "Yozishni tugatish", exact: true }).click();
  await expect(page.locator(".vocabulary-mic-wave")).toHaveCount(0);
  await expect(page.getByText("82 / 100", { exact: true })).toBeVisible();
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.getByRole("button", { name: "Namunani tinglash", exact: true }).click();
  await expect(page.locator(".vocabulary-voice")).toHaveAttribute("data-playback-state", "playing");
  await expect(page.locator(".vocabulary-voice__wave > span").first()).toHaveCSS("animation-name", "none");
});

test("short landscape/keyboard viewports keep content reachable rather than clipping it", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await openCards(page);
  await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBe(320);
  await page.getByRole("button", { name: "Talaffuzni mashq qilish", exact: true }).click();
  await expect(page.getByRole("button", { name: "So‘zni aytish", exact: true })).toBeVisible();
  await page.setViewportSize({ width: 700, height: 360 });
  await page.getByRole("button", { name: "So‘zga qaytish", exact: true }).click();
  await expect(page.getByRole("button", { name: "Aylantirish", exact: true })).toBeVisible();
});

test("real-topic length examples stay readable on compact phones", async ({ page }, info) => {
  // Regression content from the reported My Family topic (no learner/account data).
  const family = {
    ...topic, title: "My Family", titleUz: "Mening oilam",
    words: topic.words.map(word => ({ ...word, exampleSentence: "When we talk about my family, the word morning helps us explain our ideas clearly." })),
  };
  await page.route(`**/api/vocabulary/topic/${topicId}`, route => route.fulfill({ json: family }));
  for (const width of [320, 360, 390]) {
    await page.setViewportSize({ width, height: 640 });
    await openCards(page);
    await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
    await fitsViewport(page, info, `long-example-${width}`);
    await expect(page.locator(".vocabulary-flashcard__example")).toContainText("explain our ideas clearly.");
  }
});

for (const viewport of [
  { width: 1440, height: 900 }, { width: 1024, height: 768 },
  { width: 390, height: 844 }, { width: 320, height: 640 },
]) {
  test(`physical flashcard deck ${viewport.width}x${viewport.height}: 19 layers and a real 3D flip`, async ({ page }, info) => {
    await page.setViewportSize(viewport);
    await openCards(page);
    await page.emulateMedia({ reducedMotion: "no-preference" });
    const stack = page.locator(".vocabulary-card-stack");
    const layers = page.locator(".vocabulary-card-stack__card");
    const rotor = page.locator(".vocabulary-flashcard__rotor");
    await expect(stack).toHaveAttribute("data-remaining-cards", "19");
    await expect(layers).toHaveCount(19);
    await expect(page.locator(".vocabulary-card-stack__layers")).toHaveAttribute("aria-hidden", "true");
    await expect(page.locator(".vocabulary-card-stack__layers")).toHaveCSS("z-index", "0");
    await expect(page.locator(".vocabulary-flashcard")).toHaveCSS("z-index", "1");
    await expect(page.getByRole("button", { name: "Kartani aylantirish", exact: true })).toHaveCount(1);
    const bounds = await layers.evaluateAll(elements => elements.map(el => {
      const rect = el.getBoundingClientRect();
      return { x: rect.x, right: rect.right, bottom: rect.bottom };
    }));
    expect(new Set(bounds.map(rect => rect.x)).size).toBe(19);
    const stackBox = (await stack.boundingBox())!;
    for (const rect of bounds) {
      expect(rect.right).toBeLessThanOrEqual(stackBox.x + stackBox.width + 1);
      expect(rect.bottom).toBeLessThanOrEqual(stackBox.y + stackBox.height + 1);
    }
    await fitsViewport(page, info, "deck-01-front-19-behind");
    await expect(rotor).toHaveCSS("transform-style", "preserve-3d");
    await expect(page.locator(".vocabulary-flashcard__side--back")).toHaveCSS("backface-visibility", "hidden");
    await page.getByRole("button", { name: "Kartani aylantirish", exact: true }).click();
    const midpoint = await rotor.evaluate(el => {
      const animation = el.getAnimations()[0];
      if (!animation) throw new Error("Card click must produce a 3D animation");
      animation.pause();
      const duration = Number(animation.effect!.getComputedTiming().duration);
      const stack = el.closest(".vocabulary-card-stack")!.getBoundingClientRect();
      const clipped: number[] = [];
      for (const progress of [.08, .18, .3, .42, .58, .72, .85, .94]) {
        animation.currentTime = duration * progress;
        for (const face of el.children) {
          const box = face.getBoundingClientRect();
          if (box.left < stack.left - 1 || box.right > stack.right + 1 || box.top < stack.top - 1 || box.bottom > stack.bottom + 1) clipped.push(progress);
        }
      }
      animation.currentTime = duration * .3;
      const matrix = new DOMMatrix(getComputedStyle(el).transform);
      return { depth: matrix.m13, scaleX: matrix.m11, clipped };
    });
    expect(midpoint.clipped).toEqual([]);
    expect(Math.abs(midpoint.depth)).toBeGreaterThan(.2);
    expect(Math.abs(midpoint.scaleX)).toBeLessThan(.95);
    await page.screenshot({ path: info.outputPath("deck-mid-3d-flip.png") });
    await rotor.evaluate(el => el.getAnimations().forEach(animation => animation.finish()));
    await fitsViewport(page, info, "deck-01-back-19-behind");
    await expect(layers).toHaveCount(19);
    await page.getByRole("button", { name: "O‘rgandim →", exact: true }).click();
    await expect(layers).toHaveCount(18);
    await fitsViewport(page, info, "deck-02-front-18-behind");
    await page.getByRole("button", { name: /20\. minnatdor/ }).click();
    await expect(layers).toHaveCount(0);
    await fitsViewport(page, info, "deck-20-no-hidden-cards");
    await page.getByRole("button", { name: "Oldingi karta", exact: true }).click();
    await expect(layers).toHaveCount(1);
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.getByRole("button", { name: "Kartani aylantirish", exact: true }).click();
    await expect(rotor).toHaveCSS("animation-duration", "0.56s");
    await rotor.evaluate(el => Promise.all(el.getAnimations().map(animation => animation.finished)));
    await expect(page.getByRole("button", { name: "Kartani aylantirish", exact: true })).toHaveAttribute("data-card-face", "back");
  });
}

for (const reducedMotion of ["reduce", "no-preference"] as const) {
  for (const viewport of [{ width: 1366, height: 768 }, { width: 390, height: 844 }]) {
    test(`user-triggered flip runs in real time with ${reducedMotion} at ${viewport.width}px`, async ({ page }, info) => {
      await page.setViewportSize(viewport);
      await page.emulateMedia({ reducedMotion });
      await openCards(page);
      expect(await page.evaluate(() => matchMedia("(prefers-reduced-motion: reduce)").matches)).toBe(reducedMotion === "reduce");
      const rotor = page.locator(".vocabulary-flashcard__rotor");
      await expect(rotor).toHaveCSS("animation-name", "none");

      async function observeLiveTurn(name: string) {
        // Do NOT pause, finish, seek or change CSS here. Measure a real click's
        // advancing animation, including the OS reduced-motion configuration.
        const motion = await rotor.evaluate(async el => {
          const animation = el.getAnimations()[0];
          if (!animation) throw new Error("User click did not start a visible animation");
          await animation.ready;
          const duration = Number(animation.effect!.getComputedTiming().duration);
          await new Promise(resolve => setTimeout(resolve, 70));
          const first = { time: Number(animation.currentTime), transform: getComputedStyle(el).transform };
          await new Promise(resolve => setTimeout(resolve, 100));
          const second = { time: Number(animation.currentTime), transform: getComputedStyle(el).transform };
          return { duration, first, second, playState: animation.playState };
        });
        expect(motion.duration).toBe(560);
        expect(motion.playState).toBe("running");
        expect(motion.second.time).toBeGreaterThan(motion.first.time);
        expect(motion.second.transform).not.toBe(motion.first.transform);
        expect(motion.second.transform).toContain("matrix3d");
        await page.screenshot({ path: info.outputPath(`${name}-running.png`) });
        await rotor.evaluate(el => Promise.all(el.getAnimations().map(animation => animation.finished)));
      }

      await page.getByRole("button", { name: "Kartani aylantirish", exact: true }).click();
      await observeLiveTurn("card-click-to-back");
      await expect(page.getByRole("button", { name: "Kartani aylantirish", exact: true })).toHaveAttribute("data-card-face", "back");
      await page.getByRole("button", { name: "Kartani aylantirish", exact: true }).press("Enter");
      await observeLiveTurn("keyboard-to-front");
      await expect(page.getByRole("button", { name: "Kartani aylantirish", exact: true })).toHaveAttribute("data-card-face", "front");
      await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
      await observeLiveTurn("button-to-back");
      await expect(page.locator(".vocabulary-card-stack__card")).toHaveCount(19);
      await page.getByRole("button", { name: "Keyingi karta", exact: true }).click();
      await expect(rotor).toHaveCSS("animation-name", "none");
      await expect(page.locator(".vocabulary-card-stack__card")).toHaveCount(18);
    });
  }
}

for (const viewport of [
  { width: 1440, height: 900 }, { width: 1280, height: 720 }, { width: 768, height: 1024 },
  { width: 390, height: 844 }, { width: 360, height: 640 }, { width: 320, height: 640 },
]) {
  test(`quiz visual states ${viewport.width}x${viewport.height}: Pen option anatomy and feedback`, async ({ page }, info) => {
    await page.setViewportSize(viewport);
    await page.route(`**/api/vocabulary/topic/${topicId}`, route => route.fulfill({ json: { ...topic, words: topic.words.slice(0, 4) } }));
    await openCards(page);
    for (let i = 0; i < 4; i++) {
      await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
      await page.getByRole("button", { name: "O‘rgandim →", exact: true }).click();
    }
    await page.getByRole("button", { name: "Testga o‘tish", exact: true }).click();
    const cards = page.locator(".vocabulary-quiz-option");
    await expect(cards).toHaveCount(4);
    const compact = viewport.width <= 700 && viewport.height <= 700;
    await expect(cards.first()).toHaveCSS("border-top-width", "2px");
    await expect(cards.first()).toHaveCSS("border-radius", compact ? "14px" : "18px");
    await expect(cards.first().locator(".vocabulary-quiz-option__key")).toHaveCSS("width", compact ? "26px" : "32px");
    await expect(cards.first().locator(".vocabulary-quiz-option__label")).toHaveCSS("font-weight", "600");
    await expect(cards.first().locator(".vocabulary-quiz-option__state")).toBeVisible();
    expect((await cards.first().boundingBox())!.height).toBeGreaterThanOrEqual(compact ? 44 : 64);
    await page.mouse.move(0, 0);
    await verifyQuizScreen(page, info, "quiz-idle");
    await cards.first().focus();
    await page.keyboard.press("Tab");
    await page.keyboard.press("Shift+Tab");
    await expect(cards.first()).toHaveCSS("outline-color", "rgb(117, 69, 232)");
    await page.screenshot({ path: info.outputPath("quiz-focus.png") });
    await page.locator(".vocabulary-topic__test-options").getByRole("button", { name: /ona/ }).click();
    await expect(page.locator('.vocabulary-quiz-option[data-answer-state="correct"]')).toHaveCSS("background-color", "rgb(233, 244, 215)");
    await expect(page.getByText("mother — ona", { exact: true })).toBeVisible();
    await verifyQuizScreen(page, info, "quiz-correct");
    await page.getByRole("button", { name: "Keyingi", exact: true }).click();
    await page.getByRole("textbox", { name: "Javobingiz" }).fill("father");
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await expect(page.locator(".vocabulary-answer-field input")).toHaveCSS("background-color", "rgb(233, 244, 215)");
    await verifyQuizScreen(page, info, "quiz-typed-correct");
    await page.getByRole("button", { name: "Keyingi", exact: true }).click();
    await page.locator(".vocabulary-quiz-option").filter({ hasNotText: "oila" }).first().click();
    await expect(page.locator('.vocabulary-quiz-option[data-answer-state="wrong"]')).toHaveCSS("background-color", "rgb(255, 228, 213)");
    await expect(page.locator('.vocabulary-quiz-option[data-answer-state="wrong"] .lucide-circle-x')).toBeVisible();
    await expect(page.locator('.vocabulary-quiz-option[data-answer-state="correct"]')).toHaveCount(1);
    await verifyQuizScreen(page, info, "quiz-wrong");
    await page.getByRole("button", { name: "Keyingi", exact: true }).click();
    await page.getByRole("textbox", { name: "Javobingiz" }).fill("suport");
    await page.getByRole("button", { name: "Tekshirish", exact: true }).click();
    await verifyQuizScreen(page, info, "quiz-correction");
  });
}
