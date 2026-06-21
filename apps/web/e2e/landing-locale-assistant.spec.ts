import { expect, test } from "@playwright/test";

test.use({ viewport: { width: 1440, height: 1000 } });

test.beforeEach(async ({ page }) => {
  await page.route("**/api/auth/me", route => route.fulfill({ status: 401, body: "{}" }));
  await page.route("**/api/auth/dev-login", route => route.fulfill({ status: 403, body: "{}" }));
});

test("landing language links preserve the explicit URL on navigation and reload", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));

  await page.goto("/LANDING");
  await expect(page.getByRole("link", { name: "Switch to English" })).toHaveAttribute("href", "/LANDING/eng");
  await page.getByRole("link", { name: "Switch to English" }).click();
  await expect(page).toHaveURL(/\/LANDING\/eng$/);
  await expect(page.getByRole("heading", { level: 1 })).toHaveText("Knowing is good.Speaking is better.");
  await expect(page.locator("html")).toHaveAttribute("lang", "en");

  await page.reload();
  await expect(page.getByRole("heading", { level: 1 })).toHaveText("Knowing is good.Speaking is better.");
  await page.getByRole("link", { name: "Switch to Uzbek" }).click();
  await expect(page).toHaveURL(/\/LANDING$/);
  await expect(page.locator("html")).toHaveAttribute("lang", "uz");
  await page.getByRole("link", { name: "English", exact: true }).click();
  await expect(page).toHaveURL(/\/LANDING\/eng$/);
  await page.getByRole("link", { name: "O‘zbekcha", exact: true }).click();
  await expect(page).toHaveURL(/\/LANDING$/);
  expect(errors).toEqual([]);
});

const englishSuggestionReplies = [
  ["Why is speaking harder than studying English?", "The difficulty may be in your learning method, not in you."],
  ["How does the AI tutor work?", "guides conversations based on your level"],
  ["How are the six skills connected?", "One topic connects all six skills."],
  ["How is the learning path organised?", "Each CEFR level has 50 ordered lessons."],
  ["What can I practise with Books and Video?", "interactive transcripts"],
  ["How do I contact support?", "For EnglishAI support and official updates"],
] as const;

for (const [question, expectedReply] of englishSuggestionReplies) {
  test(`English suggestion requests English: ${question}`, async ({ page }, info) => {
    // Opt in to the real local backend when validating end-to-end locale propagation.
    if (process.env.PW_LIVE_ASSISTANT !== "1") {
      await page.route("**/api/assistant/project", async route => {
        expect(route.request().postDataJSON().locale).toBe("en");
        await route.fulfill({ json: { reply: expectedReply } });
      });
    }
    await page.goto("/LANDING/eng");
    await page.getByRole("button", { name: "Open the EnglishAI assistant" }).click();
    const dialog = page.getByRole("dialog", { name: "AI assistant" });
    const requestPromise = page.waitForRequest(request => request.url().endsWith("/api/assistant/project") && request.method() === "POST");
    await dialog.getByRole("button", { name: question, exact: true }).click();
    const request = await requestPromise;
    expect(request.postDataJSON()).toMatchObject({ question, locale: "en" });
    await expect(dialog.locator(".pl-assistant-message--assistant").last()).toContainText(expectedReply);
    await expect(dialog.locator(".pl-assistant-composer")).toBeInViewport({ ratio: 1 });
    if (question === "How does the AI tutor work?") {
      await page.screenshot({ path: info.outputPath("english-assistant-answer.png") });
    }
  });
}

test("typed questions follow the selected page language, not the question language", async ({ page }) => {
  if (process.env.PW_LIVE_ASSISTANT !== "1") {
    await page.route("**/api/assistant/project", route => route.fulfill({
      json: { reply: route.request().postDataJSON().locale === "en"
        ? "The AI tutor guides conversations based on your level."
        : "AI tutor darajangiz va tanlangan mavzuga mos suhbatni boshqaradi." },
    }));
  }
  for (const step of [
    { path: "/LANDING/eng", open: "Open the EnglishAI assistant", title: "AI assistant", input: "Your question about EnglishAI", send: "Send question", question: "24/7 AI tutor qanday ishlaydi?", locale: "en", expected: "guides conversations based on your level" },
    { path: "/LANDING", open: "EnglishAI loyiha yordamchisini ochish", title: "AI yordamchi", input: "EnglishAI haqida savol", send: "Savolni yuborish", question: "How does the AI tutor work?", locale: "uz", expected: "darajangiz va tanlangan mavzuga mos" },
  ]) {
    await page.goto(step.path);
    await page.getByRole("button", { name: step.open }).click();
    const dialog = page.getByRole("dialog", { name: step.title });
    await dialog.getByRole("textbox", { name: step.input }).fill(step.question);
    const requestPromise = page.waitForRequest(request => request.url().endsWith("/api/assistant/project") && request.method() === "POST");
    await dialog.getByRole("button", { name: step.send, exact: true }).click();
    expect((await requestPromise).postDataJSON()).toMatchObject({ question: step.question, locale: step.locale });
    await expect(dialog.locator(".pl-assistant-message--assistant").last()).toContainText(step.expected);
  }
});

for (const locale of [
  { code: "uz", path: "/LANDING", open: "EnglishAI loyiha yordamchisini ochish", title: "AI yordamchi", support: "Support uchun qanday bog'lanaman?" },
  { code: "en", path: "/LANDING/eng", open: "Open the EnglishAI assistant", title: "AI assistant", support: "How do I contact support?" },
]) {
  test(`${locale.code} assistant shows contacts only in a requested reply`, async ({ page }, info) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    let questions = 0;
    await page.route("**/api/assistant/project", async route => {
      questions++;
      expect(route.request().postDataJSON().question).toBe(locale.support);
      expect(route.request().postDataJSON().locale).toBe(locale.code);
      await route.fulfill({
        json: {
          reply: "**Telegram support:** https://t.me/englishaiuz\n\n**EnglishAI.uz LinkedIn:** https://www.linkedin.com/company/englishai-uz/\n\n**Javohir Sadullayev:** https://www.linkedin.com/in/javohir-sadullayev-b8737725a/",
        },
      });
    });

    await page.goto(locale.path);
    await page.getByRole("button", { name: locale.open }).click();
    const dialog = page.getByRole("dialog", { name: locale.title });
    await expect(dialog).toBeVisible();
    await expect(dialog.locator(".pl-assistant-contacts")).toHaveCount(0);
    for (const text of ["Official contact channels", "Rasmiy aloqa kanallari", "Telegram support", "EnglishAI.uz LinkedIn", "Javohir Sadullayev"]) {
      await expect(dialog.getByText(text, { exact: true })).toHaveCount(0);
    }
    await expect(dialog.getByRole("link")).toHaveCount(0);
    expect(questions).toBe(0);
    await page.screenshot({ path: info.outputPath("assistant-before-question.png") });

    await dialog.getByRole("button", { name: locale.support, exact: true }).click();
    await expect(dialog.getByText("Telegram support:", { exact: true })).toBeVisible();
    await expect(dialog.getByText("EnglishAI.uz LinkedIn:", { exact: true })).toBeVisible();
    await expect(dialog.getByText("Javohir Sadullayev:", { exact: true })).toBeVisible();
    expect(questions).toBe(1);
    await expect(dialog.locator(".pl-assistant-contacts")).toHaveCount(0);
    expect(errors).toEqual([]);
  });
}

for (const viewport of [
  { width: 1440, height: 900 },
  { width: 1366, height: 600 },
  { width: 1024, height: 500 },
]) {
  for (const locale of [
    { code: "uz", path: "/LANDING", open: "EnglishAI loyiha yordamchisini ochish", title: "AI yordamchi", input: "EnglishAI haqida savol", send: "Savolni yuborish" },
    { code: "en", path: "/LANDING/eng", open: "Open the EnglishAI assistant", title: "AI assistant", input: "Your question about EnglishAI", send: "Send question" },
  ]) {
    test(`assistant layout ${viewport.width}x${viewport.height} ${locale.code}: only the body scrolls`, async ({ page }, info) => {
      await page.setViewportSize(viewport);
      await page.route("**/api/assistant/project", route => route.fulfill({
        json: { reply: Array.from({ length: 45 }, (_, index) => `Paragraph ${index + 1}: EnglishAI connects vocabulary, grammar, reading, writing, speaking and listening practice.`).join("\n\n") },
      }));
      await page.goto(locale.path);
      await page.getByRole("button", { name: locale.open }).click();
      const dialog = page.getByRole("dialog", { name: locale.title });
      const header = dialog.locator(".ea-overlay__header");
      const body = dialog.locator(".ea-overlay__body");
      const footer = dialog.locator(".ea-overlay__footer");
      const input = dialog.getByRole("textbox", { name: locale.input });
      const send = dialog.getByRole("button", { name: locale.send, exact: true });

      await expect(dialog).toBeVisible();
      await page.screenshot({ path: info.outputPath("assistant-layout-open.png") });
      await expect(footer.locator("form")).toHaveCount(1);
      await expect(body.locator("form")).toHaveCount(0);
      await expect.poll(() => body.evaluate(element => element.scrollTop)).toBe(0);

      async function expectControlsInsidePanel() {
        const panelBounds = (await dialog.boundingBox())!;
        expect(panelBounds.y).toBeGreaterThanOrEqual(0);
        expect(panelBounds.y + panelBounds.height).toBeLessThanOrEqual(page.viewportSize()!.height + 1);
        for (const control of [header, footer, input, send]) {
          await expect(control).toBeInViewport({ ratio: 1 });
          const bounds = (await control.boundingBox())!;
          expect(bounds.y).toBeGreaterThanOrEqual(panelBounds.y);
          expect(bounds.y + bounds.height).toBeLessThanOrEqual(panelBounds.y + panelBounds.height);
        }
        expect(await body.evaluate(element => element.clientHeight)).toBeGreaterThan(40);
      }

      await expectControlsInsidePanel();
      await input.fill("How does EnglishAI help me practise?");
      await send.click();
      await expect(body.getByText(/^Paragraph 45:/)).toBeVisible();
      await expect.poll(() => body.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
      await expectControlsInsidePanel();

      await body.evaluate(element => { element.scrollTop = 0; });
      const headerBefore = await header.boundingBox();
      const footerBefore = await footer.boundingBox();
      const pageScrollBefore = await page.evaluate(() => window.scrollY);
      await body.hover();
      await page.mouse.wheel(0, 600);
      await expect.poll(() => body.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
      expect(await header.boundingBox()).toEqual(headerBefore);
      expect(await footer.boundingBox()).toEqual(footerBefore);
      expect(await dialog.evaluate(element => element.scrollTop)).toBe(0);
      expect(await page.evaluate(() => window.scrollY)).toBe(pageScrollBefore);
      await expectControlsInsidePanel();

      // The composer must also fit after a user enlarges the multiline input.
      await input.evaluate(element => { element.style.height = "130px"; });
      await expectControlsInsidePanel();
      await page.setViewportSize({ width: viewport.width, height: viewport.height - 140 });
      await expectControlsInsidePanel();
      await page.screenshot({ path: info.outputPath("assistant-layout-scrolled.png") });
    });
  }
}
