import { test, expect, type Page } from "@playwright/test";
import { PLACEMENT_PREVIEWS } from "../src/pages/onboarding/placementPreviewItems";

const user = { id: "placement-recovery-user", email: "recovery@example.test", displayName: "Recovery",
  preferredName: "Recovery", username: "recovery", pictureUrl: null, hasOnboarded: false,
  learningGoal: 1, hasCompletedDemographics: true, birthDate: "2000-01-01", gender: 1,
  acquisitionSource: 3, acquisitionSourceOther: null };
const result = { overallLevel: 3, overallScore: 72, stageResults: [1, 2, 3, 4, 5, 6].map(stage => ({ stage, level: 3, score: 72 })) };
const key = `englishai.placement.session.${user.id}`;
async function prepare(page: Page) {
  await page.setViewportSize({ width: 1440, height: 1080 });
  await page.route("**/api/auth/me", route => route.fulfill({ json: user }));
  await page.route("**/api/auth/dev-login", route => route.fulfill({ status: 403, json: {} }));
}

test("completed session survives a lost final result and an already-onboarded reload", async ({ page }) => {
  await prepare(page);
  let onboarded = false, finalizeCalls = 0, startCalls = 0;
  await page.route("**/api/auth/me", route => route.fulfill({ json: { ...user, hasOnboarded: onboarded } }));
  await page.route("**/api/placement/start", route => { startCalls++; return route.fulfill({ json: { sessionId: "recovery-session", currentStage: 2, firstItem: PLACEMENT_PREVIEWS.grammar } }); });
  await page.route("**/api/placement/answer", route => route.fulfill({ json: { isTestCompleted: true, nextItem: null } }));
  await page.route("**/api/placement/finalize", route => {
    finalizeCalls++; onboarded = true;
    return route.fulfill(finalizeCalls === 1 ? { status: 503, json: { error: "response lost" } } : { json: result });
  });
  await page.route("**/api/placement/session/recovery-session", route => route.fulfill({ json: { sessionId: "recovery-session", isCompleted: true, currentItem: null } }));
  await page.goto("/placement");
  await page.getByRole("button", { name: "Testni boshlash", exact: true }).click();
  await page.getByRole("button", { name: "A She don’t like coffee." }).click();
  await page.getByRole("button", { name: "Javobni yuborish" }).click();
  await expect(page.getByRole("button", { name: "Natijani qayta olish" })).toBeVisible();
  await page.reload();
  await expect(page).toHaveURL(/\/placement$/);
  await page.getByRole("button", { name: "Testni davom ettirish" }).click();
  await expect(page).toHaveURL(/\/placement\/result$/);
  await expect(page.getByRole("article")).toHaveCount(6);
  await page.reload();
  await expect(page.getByLabel("Aniqlangan daraja: B1")).toBeVisible();
  expect(startCalls).toBe(1);
  expect(finalizeCalls).toBe(2);
});

test("temporary resume failure retains session and can retry without starting a new test", async ({ page }) => {
  await prepare(page);
  await page.addInitScript(({ key }) => { localStorage.setItem(key, "offline-session"); }, { key });
  let starts = 0, resumes = 0;
  await page.route("**/api/placement/start", route => { starts++; return route.fulfill({ status: 500, json: {} }); });
  await page.route("**/api/placement/session/offline-session", route => {
    resumes++;
    return route.fulfill(resumes === 1 ? { status: 503, json: {} } : { json: { sessionId: "offline-session", isCompleted: false, currentItem: PLACEMENT_PREVIEWS.grammar } });
  });
  await page.goto("/placement");
  await page.getByRole("button", { name: "Testni davom ettirish" }).click();
  await expect(page.getByRole("alert")).toContainText("Mavjud sessiya saqlandi");
  await page.getByRole("button", { name: "Qayta urinish", exact: true }).click();
  await expect(page.locator(".play-placement")).toHaveAttribute("data-skill", "Grammar");
  expect(starts).toBe(0);
  expect(resumes).toBe(2);
});

test("expired session has an explicit restart rather than silently discarding progress", async ({ page }) => {
  await prepare(page);
  await page.addInitScript(({ key }) => { sessionStorage.setItem(key, "expired-session"); }, { key });
  let starts = 0;
  await page.route("**/api/placement/start", route => { starts++; return route.fulfill({ status: 500, json: {} }); });
  await page.route("**/api/placement/session/expired-session", route => route.fulfill({ status: 410, json: { code: "placement_session_expired" } }));
  await page.goto("/placement");
  await page.getByRole("button", { name: "Testni davom ettirish" }).click();
  await expect(page.getByRole("heading", { name: "Test sessiyasi tugagan" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Testni qayta boshlash" })).toBeVisible();
  expect(starts).toBe(0);
});
