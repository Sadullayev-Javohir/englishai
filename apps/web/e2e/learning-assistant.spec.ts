import { expect, test } from "@playwright/test";

// The mock preview only: never modify the real development account.
test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    localStorage.setItem("englishai.level.mock-learner-0001", "1");
    localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");
    const homeEntryDay = new Intl.DateTimeFormat("en-CA", {
      timeZone: "Asia/Tashkent",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    }).format(new Date());
    localStorage.setItem(
      `englishai.home-entry-modal.suppressed.mock-learner-0001.${homeEntryDay}`,
      "1"
    );
  });
  await page.route("**/api/video/search?*", (route) =>
    route.fulfill({ json: { items: [], nextCursor: null } })
  );
  await page.route("**/api/assistant/sessions**", async (route) => {
    await route.fulfill({
      json:
        route.request().method() === "POST"
          ? {
              id: "assistant-test",
              skill: "general",
              title: "EnglishAI",
              resourceType: "page",
              resourceId: null,
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
              expiresAt: new Date(Date.now() + 86400000).toISOString(),
              messages: [],
              sources: [],
            }
          : { items: [], nextCursor: null },
    });
  });
});

for (const width of [390, 1440]) {
  for (const path of [
    "/home",
    "/levels",
    "/progress",
    "/leaderboard",
    "/profile",
    "/support",
    "/app/vocabulary/saved",
    "/app/vocabulary/topics",
    "/app/grammar",
    "/reading",
    "/listening",
    "/writing",
    "/books",
    "/video",
    "/writing/task/travel",
    "/app/speaking/topic/my-family",
  ]) {
    test(`single fixed bottom-right assistant: ${path} at ${width}px`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height: 900 });
      await page.goto(path, { waitUntil: "domcontentloaded" });
      const trigger = page.getByRole("button", {
        name: "AI Yordamchi",
        exact: true,
      });
      await expect(trigger).toHaveCount(1);
      await expect(trigger).toBeVisible();
      await expect(trigger).toHaveCSS("position", "fixed");
      await expect(trigger).toHaveText("");
      await expect(trigger).toHaveCSS("right", "16px");
      await expect(trigger).toHaveCSS("width", "56px");
      await expect(trigger).toHaveCSS("height", "56px");
      await expect(trigger).toHaveCSS("transform", "none");
      const nav = page.getByRole("navigation", { name: "Mobil navigatsiya" });
      if (await nav.isVisible()) {
        const navBox = (await nav.boundingBox())!;
        await expect
          .poll(async () => {
            const box = (await trigger.boundingBox())!;
            return Math.round(navBox.y - box.y - box.height);
          })
          .toBe(16);
      } else {
        await expect(trigger).toHaveCSS("bottom", "16px");
      }
      const before = (await trigger.boundingBox())!;
      expect(width - before.x - before.width).toBeCloseTo(16);
      await page.evaluate(() =>
        window.scrollTo(0, document.documentElement.scrollHeight)
      );
      const after = (await trigger.boundingBox())!;
      expect(after.x).toBeCloseTo(before.x);
      expect(after.y).toBeCloseTo(before.y);
      await expect(
        page.getByRole("banner").getByRole("button", { name: /AI yordamchi/i })
      ).toHaveCount(0);
    });
  }
}

for (const viewport of [
  { width: 390, height: 1000 },
  { width: 1440, height: 1080 },
]) {
  test(`Pen 67 compact assistant modal at ${viewport.width}×${viewport.height}`, async ({
    page,
  }, info) => {
    await page.setViewportSize(viewport);
    await page.goto("/home");
    const trigger = page.getByRole("button", {
      name: "AI Yordamchi",
      exact: true,
    });
    await trigger.click();
    const dialog = page.getByRole("dialog", { name: "AI Yordamchi" });
    await expect(dialog).toBeVisible();
    await expect(dialog).toHaveCSS("transform", "none");
    await expect(dialog.getByText("Salom, Javohir!")).toBeVisible();
    await expect(dialog.getByText("MY MOTHER · VOCABULARY")).toBeVisible();
    await expect(dialog.getByText("My mother is very kind.")).toBeVisible();
    await expect(
      dialog.getByRole("button", { name: "Misollar keltir" })
    ).toBeVisible();
    await expect(
      dialog.getByRole("button", { name: "Mashq ber" })
    ).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Full size" })).toHaveCount(
      0
    );
    await expect(dialog.getByTitle("Yangi chat")).toHaveCount(0);
    await expect(dialog.getByTitle("Chatlar tarixi")).toHaveCount(0);

    const compact = (await dialog.boundingBox())!;
    expect(compact.height).toBeLessThan(viewport.height);
    expect(compact.width).toBeCloseTo(viewport.width <= 700 ? 358 : 680, 0);
    expect(compact.x + compact.width / 2).toBeCloseTo(viewport.width / 2, 0);
    expect(compact.y + compact.height / 2).toBeCloseTo(viewport.height / 2, 0);
    await page.screenshot({ path: info.outputPath("assistant-pen-67.png") });
    await dialog.getByRole("button", { name: "Yopish", exact: true }).click();
    await expect(dialog).toHaveCount(0);
    await expect(page.locator(".ea-assistant-overlay")).toHaveCount(0);
    await expect(trigger).toBeVisible();
    await expect(trigger).toBeFocused();
    await expect(page).toHaveURL(/\/home$/);
  });
}
