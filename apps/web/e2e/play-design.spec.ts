import { test, expect } from "@playwright/test";
import { AUDIT_ROUTES } from "../src/app/routeAuditManifest";

for (const width of [360, 390, 412, 820, 1440]) {
  for (const path of ["/", "/login", "/home", "/app/vocabulary/topics", "/app/grammar", "/app/vocabulary/saved", "/progress", "/admin", "/app/vocabulary/topic/:topicId", "/app/grammar/topic/:topicId", "/reading/topic/:topicId", "/listening/topic/:topicId", "/writing/task/:topicId", "/app/speaking", "/levels", "/leaderboard", "/profile", "/books", "/video"]) {
    test(`Play ${width} ${path}`, async ({ page }, info) => {
      test.setTimeout(60_000);
      const entry = AUDIT_ROUTES.find(r => r.pattern === path)!;
      const audience = entry.audience === "public" ? "signed-out" : entry.audience;
      await page.route("**/api/auth/me", route => audience === "signed-out" ? route.fulfill({ status:401, body:"{}" }) : route.continue({ headers: { ...route.request().headers(), "x-englishai-audit-auth": audience } }));
      await page.route("**/api/subscription/mock-learner-0001", async route => { const response = await route.fetch(); const data = await response.json(); await route.fulfill({response,json:{...data,isTrialActive:false}}); });
      if (audience === "signed-out") await page.route("**/api/auth/dev-login", route => route.fulfill({status:403,body:"{}"}));
      await page.addInitScript(() => {
        localStorage.setItem("englishai.learnerId", "mock-learner-0001");
        localStorage.setItem("englishai.level.mock-learner-0001", "1");
        localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");
        localStorage.setItem("englishai-theme", "dark");
      });
      const errors: string[] = [];
      page.on("pageerror", error => errors.push(error.message));
      await page.setViewportSize({ width, height: 950 });
      await page.goto(entry.path);
      await expect(page).toHaveURL(new RegExp(entry.path.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + "$"));
      await expect(page.getByRole("heading").first()).toBeVisible();
      await expect(page).toHaveURL(new RegExp(entry.path.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + "$"));
      await page.evaluate(() => document.fonts.ready);
      for (let i = 0; i < 3 && await page.getByRole("dialog").count(); i++) await page.keyboard.press("Escape");
      await expect(page.locator("html")).toHaveAttribute("data-theme", "light");
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)).toBeLessThanOrEqual(1);
      await expect(page.locator('button[aria-label*="qorong‘i"]')).toHaveCount(0);
      expect(errors).toEqual([]);
      await page.screenshot({ path: info.outputPath("play-screen.png"), fullPage: true });
    });
  }
}

test("landing first word is interactive and its CTA opens sign-in", async ({ page }) => {
  await page.route("**/api/auth/me", route => route.fulfill({ status: 401, body: '{}' }));
  await page.route("**/api/auth/dev-login", route => route.fulfill({status:403,body:"{}"}));
  await page.goto("/");
  await page.getByRole("button", { name: "Rahmat", exact: true }).click();
  await expect(page.getByRole("status").filter({ hasText: "Yana bir bor" })).toBeVisible();
  await page.getByRole("button", { name: "Salom", exact: true }).click();
  await expect(page.getByRole("status").filter({ hasText: "To‘ppa-to‘g‘ri" })).toBeVisible();
  await page.getByRole("button", { name: "Bepul boshlash" }).first().click();
  await expect(page).toHaveURL(/\/login$/);
});
