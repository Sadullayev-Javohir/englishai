import { test, expect } from "@playwright/test";

test("native app keeps the mark-only identity even at tablet widths", async ({ page }) => {
  await page.route("**/api/auth/me", route => route.fulfill({ status: 401, body: "{}" }));
  await page.route("**/api/auth/dev-login", route => route.fulfill({ status: 403, body: "{}" }));
  await page.setViewportSize({ width: 1024, height: 900 });
  await page.goto("/");
  const brand = page.locator("header [data-englishai-brand]").first();
  await expect(brand.locator("[data-englishai-wordmark]")).toBeVisible();
  await page.evaluate(() => document.documentElement.setAttribute("data-native-app", ""));
  await expect(brand.locator("[data-englishai-wordmark]")).toBeHidden();
  await expect(brand.getByRole("img", { name: "EnglishAI" })).toBeVisible();
});

for (const width of [390, 700, 701, 1440]) {
  for (const path of ["/", "/login", "/home", "/video"]) {
    test(`Dialog branding ${width}px ${path}`, async ({ page }, info) => {
      const publicRoute = path === "/" || path === "/login";
      await page.route("**/api/auth/me", route => publicRoute
        ? route.fulfill({ status: 401, body: "{}" })
        : route.continue({ headers: { ...route.request().headers(), "x-englishai-audit-auth": "learner" } }));
      if (publicRoute) await page.route("**/api/auth/dev-login", route => route.fulfill({ status: 403, body: "{}" }));
      await page.addInitScript(() => {
        localStorage.setItem("englishai.learnerId", "mock-learner-0001");
        localStorage.setItem("englishai.level.mock-learner-0001", "1");
        localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");
      });
      // Branding coverage is independent of live/stale video catalog fixtures.
      await page.route("**/api/video/search**", route => route.fulfill({ json: { items: [], nextCursor: null } }));
      await page.setViewportSize({ width, height: 900 });
      await page.goto(path);
      const header = page.locator("header").filter({ has: page.locator('[data-englishai-brand="dialog"]') }).first();
      const logo = header.locator('[data-englishai-brand="dialog"]').first();
      await expect(logo.locator("img")).toBeVisible();
      await expect(logo.locator("img")).toHaveAttribute("src", "/assets/brand/dialog.svg");
      await expect.poll(() => logo.locator("img").evaluate((img: HTMLImageElement) => img.complete && img.naturalWidth > 0)).toBe(true);
      if (width <= 700) await expect(logo.locator("[data-englishai-wordmark]")).toBeHidden();
      else await expect(logo.locator("[data-englishai-wordmark]")).toBeVisible();
      await expect(logo.locator("[data-englishai-wordmark]")).toHaveText("EnglishAI");
      await expect(page.locator('link[rel="icon"][type="image/svg+xml"]')).toHaveAttribute("href", "/favicon.svg?v=dialog-v1");
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
      await header.screenshot({ path: info.outputPath("brand-header.png") });
    });
  }
}
