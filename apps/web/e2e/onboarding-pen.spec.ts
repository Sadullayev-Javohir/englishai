import { test, expect, type Page } from "@playwright/test";
import { PLACEMENT_PREVIEWS } from "../src/pages/onboarding/placementPreviewItems";

// Recording tests use synthetic input, never the user's physical microphone.
test.use({ launchOptions: { args: ["--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream"] } });

// Deliberately isolated from real users, Google accounts and placement sessions.
// These fixture values are the reference states in englishai.pen WEB 02–09.
const referenceUser = {
  id: "mock-learner-0001", email: "pen-reference@example.test",
  displayName: "Javohir", preferredName: "Javohir", username: "javohir",
  pictureUrl: null, hasOnboarded: true, learningGoal: 1,
  birthDate: "2002-08-15", gender: 1, acquisitionSource: 1,
  acquisitionSourceOther: null, hasCompletedDemographics: true,
};

async function prepare(page: Page, signedOut = false) {
  await page.setViewportSize({ width: 1440, height: 1080 });
  await page.route("**/api/auth/me", route => route.fulfill({
    status: signedOut ? 401 : 200, json: signedOut ? {} : referenceUser,
  }));
  await page.route("**/api/auth/config", route => route.fulfill({ json: { googleClientId: "pen-test-client" } }));
  await page.route("**/api/auth/dev-login", route => route.fulfill({ status: 403, json: {} }));
  await page.route("**/accounts.google.com/**", route => route.abort());
  await page.route("**/api/auth/username-available?*", route => route.fulfill({ json: { isValidFormat: true, isAvailable: true } }));
  await page.addInitScript(() => {
    // Test double for the Google-owned control, not an alternative OAuth implementation.
    window.google = { accounts: { id: {
      initialize: () => {},
      renderButton: (parent: HTMLElement) => {
        const control = document.createElement("button");
        control.type = "button";
        control.setAttribute("aria-label", "Google bilan kirish");
        control.innerHTML = '<img src="/assets/play/google-g.svg" alt="" width="24" height="24"><span>Google bilan kirish</span>';
        control.style.cssText = "display:flex;align-items:center;justify-content:center;gap:14px;width:100%;font-size:15px;font-weight:800";
        parent.append(control);
      },
    } } } as typeof window.google;
  });
}

const screens = [
  { name: "02 login", path: "login", selector: ".play-login", width: 1280, x: 80, y: 124, titleSize: 44 },
  { name: "03 username", path: "username", selector: ".play-username__form", width: 470, x: 720, y: 124, titleSize: 44 },
  { name: "04 profile", path: "profile-details", selector: ".play-profile__form", width: 630, x: 606, y: 124, titleSize: 42 },
  { name: "05 goal", path: "goal", selector: ".play-goal", width: 1000, x: 220, y: 124, titleSize: 44 },
  { name: "06 welcome", path: "welcome", selector: ".play-welcome__journey", width: 460, x: 203, y: 124, titleSize: 46 },
  { name: "07 assessment", path: "assessment", selector: ".play-assessment", width: 928, x: 256, y: 124, titleSize: 46 },
  { name: "08 placement", path: "board", selector: ".play-placement", width: 920, x: 260, y: 124, titleSize: 42 },
  { name: "09 result", path: "result", selector: ".play-result", width: 1020, x: 210, y: 124, titleSize: 42 },
];

for (const entry of screens) {
  test(`Pen desktop ${entry.name}`, async ({ page }, info) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await prepare(page, entry.path === "login");
    await page.goto(`/dev/onboarding/${entry.path}?clean=1`);
    await expect(page.locator(entry.selector)).toBeVisible();
    if (entry.path === "username") {
      await page.getByLabel("Foydalanuvchi nomi").fill("javohir");
      await expect(page.getByText("Bu nom bo‘sh. Sizniki bo‘lsin!")).toBeVisible();
      await page.getByRole("heading").first().click();
    }
    if (entry.path === "goal") await page.getByRole("button", { name: "Erkin gapirish" }).click();
    if (entry.path === "login") await expect(page.getByRole("button", { name: "Google bilan kirish" })).toBeVisible();
    await page.evaluate(() => document.fonts.ready);
    await expect(page.locator(".onboarding-play")).toHaveCSS("background-color", "rgb(255, 252, 247)");
    await expect(page.locator(".onboarding-play__nav")).toHaveCSS("height", "88px");
    await expect(page.locator(".onboarding-play__brand .ea-brand-name")).toHaveCSS("font-family", '"EnglishAI Manrope", sans-serif');
    await expect(page.locator("h1")).toHaveCSS("font-size", `${entry.titleSize}px`);
    await expect(page.locator("h1")).toHaveCSS("font-family", "Nunito, sans-serif");
    await expect(page.locator("h1")).toHaveCSS("font-weight", "900");
    if (entry.path !== "login") {
      await expect(page.locator(".onboarding-play__button").first()).toHaveCSS("height", "56px");
      await expect(page.locator(".onboarding-play__button").first()).toHaveCSS("border-radius", "18px");
      await expect(page.locator(".onboarding-play__button").first()).toHaveCSS("color", "rgb(255, 255, 255)");
    }
    await expect(page.getByRole("button", { name: "Ortga qaytish" })).toBeVisible();
    await expect(page.getByRole("link", { name: /Yordam/ })).toHaveCount(0);
    await expect(page.locator(".onboarding-play__footer")).toHaveText("EnglishAI · Bir qadam yaqinroq.");
    const bounds = await page.locator(entry.selector).boundingBox();
    expect(bounds?.width).toBeCloseTo(entry.width, 0);
    expect(bounds?.x).toBeCloseTo(entry.x, 0);
    expect(bounds?.y).toBeCloseTo(entry.y, 0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
    expect(errors).toEqual([]);
    await page.screenshot({ path: info.outputPath("screen.png"), fullPage: true });
  });
}

test("goal selection persists the selected enum, not an icon or display label", async ({ page }) => {
  await prepare(page);
  let savedGoal: unknown;
  await page.route("**/api/auth/learning-goal", async route => {
    savedGoal = route.request().postDataJSON();
    await route.fulfill({ json: { ...referenceUser, learningGoal: 5 } });
  });
  await page.goto("/dev/onboarding/goal?clean=1");
  await expect(page.getByRole("button", { name: "Maqsadim shu!" })).toBeDisabled();
  await page.getByRole("button", { name: "Erkin gapirish" }).click();
  await expect(page.getByRole("button", { name: "Erkin gapirish" })).toHaveAttribute("aria-pressed", "true");
  await page.getByRole("button", { name: "Maqsadim shu!" }).click();
  await expect.poll(() => savedGoal).toEqual({ goal: 5 });
});

test("placement selection is neutral and does not reveal correctness", async ({ page }) => {
  await prepare(page);
  await page.goto("/dev/onboarding/board?clean=1");
  await page.getByRole("button", { name: "A She don’t like coffee." }).click();
  await expect(page.getByRole("button", { name: "A She don’t like coffee." })).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByRole("button", { name: "C She doesn’t like coffee." })).toHaveAttribute("aria-pressed", "false");
  await expect(page.getByRole("button", { name: "Javobni yuborish" })).toBeEnabled();
  await expect(page.getByText("To'g'ri!", { exact: true })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "O‘tkazib yuborish" })).toBeDisabled();
});

test("microphone check reports denial without blocking the test", async ({ page }) => {
  await prepare(page);
  await page.addInitScript(() => {
    Object.defineProperty(navigator, "mediaDevices", { configurable: true, value: {
      getUserMedia: () => Promise.reject(new DOMException("Denied", "NotAllowedError")),
    } });
  });
  await page.goto("/dev/onboarding/assessment?clean=1");
  await page.getByRole("button", { name: "Mikrofonni tekshirish" }).click();
  await expect(page.getByRole("alert")).toContainText("mikrofon ruxsatini yoqing");
  await expect(page.getByRole("button", { name: "Tayyorman, boshlaymiz!" })).toBeEnabled();
});

test("preview walks from screen 02 to 09 without login or a real assessment", async ({ page }) => {
  await prepare(page, true);
  await page.goto("/dev/onboarding/login");
  const nav = page.getByRole("navigation", { name: "Onboarding previews" });
  await expect(nav.getByRole("link", { name: "02 · Kirish", exact: true })).toHaveAttribute("aria-current", "page");
  await expect(nav.getByRole("link", { name: /^Oldingi sahifa:/ })).toHaveCount(0);
  for (const entry of screens.slice(1)) {
    await nav.getByRole("link", { name: /^Keyingi sahifa:/ }).click();
    await expect(page).toHaveURL(new RegExp(`/dev/onboarding/${entry.path}$`));
    await expect(page.locator(entry.selector)).toBeVisible();
  }
  await expect(nav.getByRole("link", { name: "09 · B1 natija", exact: true })).toHaveAttribute("aria-current", "page");
  await expect(nav.getByRole("link", { name: /^Keyingi sahifa:/ })).toHaveCount(0);
  await nav.getByRole("link", { name: /^Oldingi sahifa:/ }).click();
  await expect(page.locator(".play-placement")).toBeVisible();
  await nav.getByRole("link", { name: "02 · Kirish", exact: true }).click();
  await expect(page.locator(".play-login")).toBeVisible();
});

for (const entry of [
  { path: "/login", selector: ".play-login", user: null },
  { path: "/username", selector: ".play-username", user: { ...referenceUser, username: null, hasOnboarded: false } },
  { path: "/onboarding/profile-details", selector: ".play-profile", user: { ...referenceUser, hasCompletedDemographics: false } },
  { path: "/onboarding/goal", selector: ".play-goal", user: { ...referenceUser, learningGoal: 0 } },
  { path: "/welcome", selector: ".play-welcome", user: { ...referenceUser, hasOnboarded: false } },
  { path: "/assessment", selector: ".play-assessment", user: { ...referenceUser, hasOnboarded: false } },
]) {
  test(`real route uses the Pen components: ${entry.path}`, async ({ page }) => {
    await prepare(page);
    await page.route("**/api/auth/me", route => route.fulfill({ status: entry.user ? 200 : 401, json: entry.user ?? {} }));
    await page.goto(entry.path);
    await expect(page.locator(entry.selector)).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`${entry.path}$`));
  });
}

test("real secure placement submits the chosen index and displays the server result", async ({ page }) => {
  await prepare(page);
  await page.route("**/api/auth/me", route => route.fulfill({ json: { ...referenceUser, hasOnboarded: false } }));
  await page.route("**/api/placement/start", route => route.fulfill({ json: {
    sessionId: "pen-placement-session", currentStage: 2,
    firstItem: {
      id: "pen-question", kind: 0, stage: 2, difficulty: 3,
      prompt: "Choose the\ncorrect sentence.",
      options: ["She don’t like coffee.", "She doesn’t likes coffee.", "She doesn’t like coffee.", "She not like coffee."],
      hasAudio: false, passageText: null, minWords: null, maxWords: null,
      stageNumber: 2, stageCount: 6, itemNumberInStage: 5, itemsInStage: 12, completedItems: 16, totalItems: 72,
    },
  } }));
  let answer: unknown;
  await page.route("**/api/placement/answer", route => {
    answer = route.request().postDataJSON();
    return route.fulfill({ json: { wasCorrect: true, isTestCompleted: true, nextItem: null } });
  });
  await page.route("**/api/placement/finalize", route => route.fulfill({ json: {
    overallLevel: 3, overallScore: 72,
    stageResults: [81, 68, 74, 79, 58, 70].map((score, index) => ({ stage: index + 1, level: 3, score })),
  } }));
  await page.goto("/placement");
  await page.getByRole("button", { name: "Testni boshlash", exact: true }).click();
  await expect(page.locator(".play-placement")).toBeVisible();
  await expect(page.getByRole("button", { name: "Javobni yuborish" })).toBeDisabled();
  await page.getByRole("button", { name: "C She doesn’t like coffee." }).click();
  await page.getByRole("button", { name: "Javobni yuborish" }).click();
  await expect(page.locator(".play-result")).toBeVisible();
  expect(answer).toEqual({ sessionId: "pen-placement-session", questionId: "pen-question", selectedOptionIndex: 2 });
  await expect(page).toHaveURL(/\/placement\/result$/);
  await expect(page.getByLabel("Aniqlangan daraja: B1")).toBeVisible();
  await expect(page.getByText("Umumiy natija: 72%")).toBeVisible();
  await expect(page.getByText("Writing’ni birga kuchaytiramiz.")).toBeVisible();
});

for (const skill of ["vocabulary", "grammar", "listening", "reading", "writing", "speaking"]) {
  test(`six-skill Pen board: ${skill}`, async ({ page }, info) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await prepare(page);
    await page.goto(`/dev/onboarding/board?skill=${skill}&clean=1`);
    const item = PLACEMENT_PREVIEWS[skill];
    const board = page.locator(".play-placement");
    await expect(board).toBeVisible();
    await expect(board).toHaveAttribute("data-skill", new RegExp(skill, "i"));
    await expect(page.getByRole("progressbar", { name: "Test jarayoni" })).toHaveAttribute("aria-valuenow", `${item.stageNumber}`);
    await expect(page.locator(".play-placement__status")).toContainText(`${item.itemNumberInStage} / ${item.itemsInStage}`);
    await expect(page.locator("h1")).toHaveCSS("font-family", "Nunito, sans-serif");
    await expect(page.locator("h1")).toHaveCSS("font-size", "42px");
    await expect(page.locator(".onboarding-play__button")).toHaveCSS("border-radius", "18px");
    await expect(page.locator(".onboarding-play")).toHaveCSS("background-color", "rgb(255, 252, 247)");
    const bounds = await board.boundingBox();
    expect(bounds?.width).toBe(920);
    expect(bounds?.x).toBe(260);
    expect(bounds?.y).toBe(124);
    if (skill === "listening") {
      await expect(page.getByRole("button", { name: "Tinglash", exact: true })).toBeVisible();
      await expect(page.locator(".play-placement__passage")).toHaveCount(0);
    }
    if (skill === "reading") await expect(page.getByRole("region", { name: "Matnni o'qing" })).toBeVisible();
    if (skill === "writing") {
      await expect(page.getByRole("textbox", { name: "Javobingiz" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Yuborish va davom etish" })).toBeDisabled();
    }
    if (skill === "speaking") {
      await expect(page.getByRole("button", { name: "Yozishni boshlash" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Ovozli javobni yuborish" })).toBeDisabled();
    }
    expect(await page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
    expect(errors).toEqual([]);
    await page.screenshot({ path: info.outputPath(`${skill}.png`), fullPage: true });
  });
}

test("gallery changes skill without carrying over answers or calling scoring APIs", async ({ page }) => {
  await prepare(page);
  const submissions: string[] = [];
  page.on("request", request => { if (request.url().includes("/api/placement/answer")) submissions.push(request.url()); });
  await page.goto("/dev/onboarding/board");
  const skills = page.getByRole("group", { name: "Olti ko‘nikma namunasi" });
  for (const label of ["Vocabulary", "Grammar", "Listening", "Reading", "Writing", "Speaking"]) {
    await skills.getByRole("link", { name: label, exact: true }).click();
    await expect(page.locator(".play-placement")).toHaveAttribute("data-skill", label);
  }
  await skills.getByRole("link", { name: "Writing", exact: true }).click();
  const answer = Array.from({ length: 40 }, () => "word").join(" ");
  await page.getByRole("textbox").fill(answer);
  await page.getByRole("button", { name: "Yuborish va davom etish" }).click();
  await expect(page.getByRole("status")).toContainText("serverga yuborilmadi");
  await skills.getByRole("link", { name: "Grammar", exact: true }).click();
  await skills.getByRole("link", { name: "Writing", exact: true }).click();
  await expect(page.getByRole("textbox")).toHaveValue("");
  expect(submissions).toEqual([]);
});

test.describe("complete six-skill secure placement", () => {
  test("MCQ → listening → reading → writing → recorded speaking → six server scores", async ({ page }, info) => {
    test.setTimeout(120_000);
    await prepare(page);
    await page.route("**/api/auth/me", route => route.fulfill({ json: { ...referenceUser, hasOnboarded: false } }));
    const items = ["vocabulary", "grammar", "listening", "reading", "writing", "speaking"].map(skill => ({
      ...PLACEMENT_PREVIEWS[skill], id: `live-${skill}`,
    }));
    await page.route("**/api/placement/start", route => route.fulfill({ json: {
      sessionId: "six-skills-session", currentStage: 1, firstItem: items[0],
    } }));
    // A PCM tone exercises real HTML audio playback without claiming to be test speech.
    const samples = 16000 * 10;
    const wave = Buffer.alloc(44 + samples * 2);
    wave.write("RIFF"); wave.writeUInt32LE(36 + samples * 2, 4); wave.write("WAVEfmt ", 8);
    wave.writeUInt32LE(16, 16); wave.writeUInt16LE(1, 20); wave.writeUInt16LE(1, 22);
    wave.writeUInt32LE(16000, 24); wave.writeUInt32LE(32000, 28); wave.writeUInt16LE(2, 32); wave.writeUInt16LE(16, 34);
    wave.write("data", 36); wave.writeUInt32LE(samples * 2, 40);
    for (let index = 0; index < samples; index++) wave.writeInt16LE(Math.round(Math.sin(index / 16000 * 2 * Math.PI * 440) * 2000), 44 + index * 2);
    await page.route("**/api/placement/audio/**", route => route.fulfill({ contentType: "audio/wav", body: wave }));
    const answers: Record<string, unknown>[] = [];
    await page.route("**/api/placement/answer", route => {
      answers.push(route.request().postDataJSON());
      return route.fulfill({ json: { wasCorrect: true, isTestCompleted: false, nextItem: items[answers.length] } });
    });
    let written: Record<string, unknown> | undefined;
    await page.route("**/api/placement/answer/writing", route => {
      written = route.request().postDataJSON();
      return route.fulfill({ json: { score: 58, level: 2, isTestCompleted: false, nextItem: items[5] } });
    });
    const recordings: string[] = [];
    await page.route("**/api/placement/answer/speaking", route => {
      recordings.push(route.request().postDataJSON().audioContent);
      return route.fulfill({ json: recordings.length === 1
        ? { retryable: true, outcome: 5, isTestCompleted: false, nextItem: null }
        : { retryable: false, outcome: 0, isTestCompleted: true, nextItem: null } });
    });
    const scores = [81, 68, 74, 79, 58, 70];
    await page.route("**/api/placement/finalize", route => route.fulfill({ json: {
      overallLevel: 3, overallScore: 72, stageResults: scores.map((score, index) => ({ stage: index + 1, score, level: 3 })),
    } }));
    await page.goto("/placement");
    await expect(page.locator(".play-placement__state")).toBeVisible();
    await page.getByRole("button", { name: "Testni boshlash", exact: true }).click();
    for (const [index, skill] of ["Vocabulary", "Grammar", "Listening", "Reading"].entries()) {
      await expect(page.locator(".play-placement")).toHaveAttribute("data-skill", skill);
      if (skill === "Listening") {
        await page.getByRole("button", { name: "Tinglash", exact: true }).click();
        await expect(page.getByRole("button", { name: "To'xtatib turish" })).toBeVisible();
        await expect.poll(() => page.locator("audio").evaluate((audio: HTMLAudioElement) => audio.currentTime)).toBeGreaterThan(.1);
        await page.getByRole("button", { name: "To'xtatib turish" }).click();
        const pausedAt = await page.locator("audio").evaluate((audio: HTMLAudioElement) => audio.currentTime);
        await page.getByRole("button", { name: "Tinglashni davom ettirish" }).click();
        await expect.poll(() => page.locator("audio").evaluate((audio: HTMLAudioElement) => audio.currentTime)).toBeGreaterThan(pausedAt);
      }
      await page.getByRole("group", { name: "Javob variantlari" }).getByRole("button").nth(index % 4).click();
      await page.getByRole("button", { name: "Javobni yuborish" }).click();
    }
    await expect(page.locator(".play-placement")).toHaveAttribute("data-skill", "Writing");
    const writing = page.getByRole("textbox", { name: "Javobingiz" });
    await writing.fill("Too short.");
    await expect(page.getByRole("button", { name: "Yuborish va davom etish" })).toBeDisabled();
    const text = Array.from({ length: 40 }, () => "garden").join(" ");
    await writing.fill(text);
    await page.getByRole("button", { name: "Yuborish va davom etish" }).click();
    await expect(page.locator(".play-placement")).toHaveAttribute("data-skill", "Speaking");
    await page.getByRole("button", { name: "Yozishni boshlash" }).click();
    await expect(page.locator(".play-placement__timer")).toHaveText("0:03", { timeout: 10_000 });
    await page.getByRole("button", { name: "Yozishni to‘xtatish" }).click();
    await expect(page.getByRole("button", { name: "Ovozli javobni yuborish" })).toBeEnabled();
    await page.screenshot({ path: info.outputPath("speaking-recorded.png"), fullPage: true });
    await page.getByRole("button", { name: "Yozuvni tinglash" }).click();
    await expect(page.getByRole("button", { name: "To'xtatib turish" })).toBeVisible();
    await page.getByRole("button", { name: "Ovozli javobni yuborish" }).click();
    await expect(page.getByRole("alert")).toContainText("Yozuv saqlandi");
    await expect(page.locator(".play-placement")).toHaveAttribute("data-skill", "Speaking");
    await page.getByRole("button", { name: "Ovozli javobni yuborish" }).click();
    await expect(page).toHaveURL(/\/placement\/result$/);
    await expect(page.getByRole("article")).toHaveCount(6);
    for (const score of scores) await expect(page.getByText(`${score}%`, { exact: true })).toBeVisible();
    expect(answers.map(answer => answer.selectedOptionIndex)).toEqual([0, 1, 2, 3]);
    expect(written).toEqual({ sessionId: "six-skills-session", taskId: "live-writing", text });
    expect(recordings).toHaveLength(2);
    expect(recordings[0]).toBe(recordings[1]);
    const decoded = Buffer.from(recordings[0], "base64");
    expect(decoded.subarray(0, 4).toString()).toBe("RIFF");
    expect(decoded.readUInt32LE(24)).toBe(16000);
    expect(decoded.readUInt16LE(22)).toBe(1);
    await page.screenshot({ path: info.outputPath("six-skill-result.png"), fullPage: true });
  });
});
