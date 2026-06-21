import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page, type TestInfo } from "@playwright/test";
import { AUDIT_ROUTES, type AuditAudience, type AuditRoute } from "../src/app/routeAuditManifest";

const VIEWPORTS = [
  { name: "mobile-360", width: 360, height: 800 },
  { name: "mobile-390", width: 390, height: 844 },
  { name: "mobile-412", width: 412, height: 915 },
  { name: "tablet", width: 820, height: 1180 },
  { name: "desktop", width: 1440, height: 900 },
] as const;

const VISUAL_ACCEPTANCE_PATTERNS = new Set([
  "/home",
  "/levels",
  "/profile",
  "/app/grammar",
  "/app/grammar/topic/:topicId",
  "/app/vocabulary/topics",
  "/app/vocabulary/topic/:topicId",
  "/app/vocabulary/saved",
  "/reading",
  "/writing",
  "/listening",
  "/app/speaking",
  "/pricing",
  "/video",
  "/video/:id/play",
  "/admin",
  "/admin/grammar",
  "/admin/users",
  "/admin/notifications",
]);

const SMOKE_ROUTE_PATTERNS = [
  "/",
  "/login",
  "/home",
  "/levels",
  "/app/vocabulary/topic/:topicId",
  "/app/vocabulary/saved/:topicId/practice",
  "/admin",
] as const;

const SMOKE_VIEWPORTS = [
  { name: "mobile", width: 390, height: 844 },
  { name: "desktop", width: 1440, height: 900 },
] as const;

const AUDIT_HEADER: Record<AuditAudience, string> = {
  public: "signed-out",
  "new-user": "new-user",
  onboarding: "onboarding",
  goal: "goal",
  learner: "learner",
  admin: "admin",
};

const IGNORED_CONSOLE = [
  /React Router Future Flag Warning/,
  /Download the React DevTools/,
  /Failed to load resource.*(favicon|englishailogo|logo|app-icon)/i,
  /The play\(\) request was interrupted/i,
  /WebSocket connection to .*\/hubs\/notifications/i,
  /WebSocket connection to .*\/hubs\/support/i,
  /WebSocket connection to .*\/voice-live/i,
  /Failed to start the connection.*stopped during negotiation/i,
  /Failed to start the connection: Error: WebSocket failed to connect/i,
  /Firefox can’t establish a connection.*\/hubs\/notifications/i,
  /Couldn't load preload assets/i,
  /GL Driver Message.*GPU stall due to ReadPixels/i,
  /Please ensure that the container has a non-static position/i,
];

function seedState({ audience, pattern, theme }: { audience: AuditAudience; pattern: string; theme: "light" | "dark" }) {
    localStorage.clear();
    sessionStorage.clear();
    localStorage.setItem("englishai-theme", theme);
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    if (audience === "learner" || audience === "admin" || audience === "goal") {
      localStorage.setItem("englishai.level.mock-learner-0001", "1");
    }
    if (audience === "learner" || audience === "admin") {
      localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");
    }
    if (pattern === "/placement/result") {
      sessionStorage.setItem("englishai.placement.result", JSON.stringify({
        overallLevel: 1,
        overallScore: 82,
        stageResults: [1, 2, 3, 4, 5, 6].map((stage) => ({ stage, level: 1, score: 82 })),
      }));
    }
}

function shouldIgnore(message: string) {
  return IGNORED_CONSOLE.some((pattern) => pattern.test(message));
}

async function installAuditHooks(page: Page, route: AuditRoute, theme: "light" | "dark" = "light") {
  await page.route("**/api/auth/me", async (requestRoute) => {
    await requestRoute.continue({
      headers: {
        ...requestRoute.request().headers(),
        "x-englishai-audit-auth": AUDIT_HEADER[route.audience],
      },
    });
  });
  if (route.audience === "public") {
    await page.route("**/api/auth/dev-login", requestRoute => requestRoute.fulfill({status:403,body:"{}"}));
  }
  await page.route(/https:\/\/fonts\.googleapis\.com\/.*/i, async (requestRoute) => {
    await requestRoute.fulfill({ status: 200, contentType: "text/css", body: "" });
  });
  await page.route(/https:\/\/fonts\.gstatic\.com\/.*/i, async (requestRoute) => {
    await requestRoute.fulfill({ status: 204, body: "" });
  });
  await page.route("**/hubs/notifications**", async (requestRoute) => {
    await requestRoute.abort("blockedbyclient");
  });
  await page.route(/https:\/\/(www\.)?(youtube\.com|youtube-nocookie\.com)\/.*/i, async (requestRoute) => {
    await requestRoute.fulfill({ status: 204, body: "" });
  });
  await page.addInitScript(seedState, { audience: route.audience, pattern: route.pattern, theme });
  await page.addInitScript(() => {
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: async () => ({ getTracks: () => [{ stop: () => undefined }] }),
      },
    });
    window.matchMedia ||= ((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    })) as typeof window.matchMedia;
    HTMLMediaElement.prototype.play = async () => undefined;
    HTMLMediaElement.prototype.pause = () => undefined;
  });
}

async function assertNoOverflow(page: Page) {
  const result = await page.evaluate(() => {
    const root = document.documentElement;
    const viewportWidth = root.clientWidth;
    const offenders: string[] = [];
    for (const element of document.querySelectorAll<HTMLElement>("body *")) {
      const rect = element.getBoundingClientRect();
      if (rect.width === 0 || rect.height === 0) continue;
      const style = getComputedStyle(element);
      if (style.position === "fixed") continue;
      if (rect.right <= viewportWidth + 1 && rect.left >= -1) continue;
      let parent = element.parentElement;
      let scoped = false;
      while (parent && parent !== document.body) {
        const overflowX = getComputedStyle(parent).overflowX;
        if (overflowX === "auto" || overflowX === "scroll") {
          scoped = true;
          break;
        }
        parent = parent.parentElement;
      }
      if (!scoped) offenders.push(`${element.tagName.toLowerCase()}.${String(element.className).slice(0, 80)}`);
    }
    return { overflow: root.scrollWidth > viewportWidth + 1, offenders: offenders.slice(0, 8) };
  });
  expect(result, `Horizontal overflow: ${result.offenders.join(", ")}`).toMatchObject({ overflow: false });
}

async function assertDialogWithinViewport(page: Page, name: string) {
  const dialog = page.getByRole("dialog", { name });
  await expect(dialog).toBeVisible();
  const box = await dialog.boundingBox();
  const viewport = page.viewportSize();
  expect(box, `Expected a bounding box for dialog "${name}"`).not.toBeNull();
  expect(viewport, "Expected the page viewport to be configured").not.toBeNull();
  if (!box || !viewport) return;
  expect(box.x).toBeGreaterThanOrEqual(0);
  expect(box.y).toBeGreaterThanOrEqual(0);
  expect(box.x + box.width).toBeLessThanOrEqual(viewport.width + 0.5);
  expect(box.y + box.height).toBeLessThanOrEqual(viewport.height + 0.5);
}

async function assertThemeContract(page: Page, theme: "light" | "dark") {
  // Legacy persisted dark preferences must resolve to the approved Play light palette.
  theme = "light";
  const result = await page.evaluate((expectedTheme) => {
    const root = document.documentElement;
    const parseColor = (value: string) => {
      const match = value.match(/rgba?\((\d+)[, ]+(\d+)[, ]+(\d+)(?:[, /]+([\d.]+))?/);
      if (!match) return null;
      return {
        red: Number(match[1]),
        green: Number(match[2]),
        blue: Number(match[3]),
        alpha: match[4] === undefined ? 1 : Number(match[4]),
      };
    };
    const tokenRgb = (name: string) => getComputedStyle(root)
      .getPropertyValue(name)
      .trim()
      .replace(/\s+/g, " ");
    const rgbKey = (color: { red: number; green: number; blue: number }) => `${color.red} ${color.green} ${color.blue}`;
    const primaryBackgrounds = new Set([
      "--ea-primary-rgb",
      "--ea-primary-end-rgb",
      "--ea-purple-600-rgb",
      "--ea-blue-600-rgb",
      "--ea-orange-500-rgb",
      "--ea-orange-600-rgb",
    ].map(tokenRgb));
    const onPrimary = tokenRgb("--ea-on-primary-rgb");
    const whiteForegroundViolations = expectedTheme === "dark"
      ? Array.from(document.querySelectorAll<HTMLElement>('[class~="text-white"], [class*="text-white/"]'))
          .filter((element) => {
            const rect = element.getBoundingClientRect();
            const style = getComputedStyle(element);
            return rect.width > 0 && rect.height > 0 && style.visibility !== "hidden" && style.display !== "none";
          })
          .filter((element) => {
            const foreground = parseColor(getComputedStyle(element).color);
            return foreground === null
              || foreground.red !== 255
              || foreground.green !== 255
              || foreground.blue !== 255;
          })
          .slice(0, 8)
          .map((element) => `${element.tagName.toLowerCase()}.${String(element.className).slice(0, 90)}`)
      : [];
    const whiteCurrentColorIconViolations = expectedTheme === "dark"
      ? Array.from(document.querySelectorAll<SVGElement>(
          '[class~="text-white"] svg, [class*="text-white/"] svg, svg[class~="text-white"], svg[class*="text-white/"]',
        ))
          .filter((element) => {
            const rect = element.getBoundingClientRect();
            const style = getComputedStyle(element);
            return rect.width > 0 && rect.height > 0 && style.visibility !== "hidden" && style.display !== "none";
          })
          .filter((element) => {
            const style = getComputedStyle(element);
            const currentColor = parseColor(style.color);
            const fill = parseColor(style.fill);
            const stroke = parseColor(style.stroke);
            const usesCurrentColor = Array.from(element.querySelectorAll("[fill], [stroke]"))
              .some((node) => node.getAttribute("fill") === "currentColor" || node.getAttribute("stroke") === "currentColor")
              || element.getAttribute("fill") === "currentColor"
              || element.getAttribute("stroke") === "currentColor";
            if (!usesCurrentColor) return false;
            return currentColor === null
              || currentColor.red !== 255
              || currentColor.green !== 255
              || currentColor.blue !== 255
              || (fill !== null && fill.alpha > 0 && (fill.red !== 255 || fill.green !== 255 || fill.blue !== 255))
              || (stroke !== null && stroke.alpha > 0 && (stroke.red !== 255 || stroke.green !== 255 || stroke.blue !== 255));
          })
          .slice(0, 8)
          .map((element) => `svg.${String(element.className.baseVal).slice(0, 90)}`)
      : [];
    const visibleElements = Array.from(document.querySelectorAll<HTMLElement>("body *"))
      .filter((element) => {
        const rect = element.getBoundingClientRect();
        const style = getComputedStyle(element);
        return rect.width > 0 && rect.height > 0 && style.visibility !== "hidden" && style.display !== "none";
      });
    const darkSurfaceViolations = expectedTheme === "dark"
      ? visibleElements
          .filter((element) => {
            if (!element.textContent?.trim() && element.children.length === 0) return false;
            if (element.closest('[data-theme-white-allowed="true"]')) return false;
            const style = getComputedStyle(element);
            const color = parseColor(style.backgroundColor);
            return color !== null
              && color.alpha >= 0.78
              && color.red > 245
              && color.green > 245
              && color.blue > 245;
          })
          .slice(0, 8)
          .map((element) => `${element.tagName.toLowerCase()}.${String(element.className).slice(0, 90)}`)
      : [];
    const primaryForegroundViolations = visibleElements
      .filter((element) => Array.from(element.childNodes).some((node) => node.nodeType === Node.TEXT_NODE && node.textContent?.trim()))
      .filter((element) => {
        let current: HTMLElement | null = element;
        while (current && current !== document.body) {
          const background = parseColor(getComputedStyle(current).backgroundColor);
          if (background && background.alpha > 0.05) {
            if (!primaryBackgrounds.has(rgbKey(background))) return false;
            const foreground = parseColor(getComputedStyle(element).color);
            return foreground === null || rgbKey(foreground) !== onPrimary;
          }
          current = current.parentElement;
        }
        return false;
      })
      .slice(0, 8)
      .map((element) => `${element.tagName.toLowerCase()}.${String(element.className).slice(0, 90)}:${element.textContent?.trim().slice(0, 80) ?? ""}`);
    return {
      marker: root.dataset.designSystem,
      theme: root.dataset.theme,
      primary: tokenRgb("--ea-primary-rgb"),
      onPrimary,
      darkSurfaceViolations,
      whiteForegroundViolations,
      whiteCurrentColorIconViolations,
      primaryForegroundViolations,
    };
  }, theme);

  expect(result.marker).toBe("englishai-play");
  expect(result.theme).toBe(theme);
  expect(result.primary).toBe("117 69 232");
  expect(result.onPrimary).toBe("255 255 255");
  expect(result.whiteForegroundViolations, `Non-white text-white foregrounds in dark mode: ${result.whiteForegroundViolations.join(", ")}`).toEqual([]);
  expect(result.whiteCurrentColorIconViolations, `Non-white currentColor icons in dark mode: ${result.whiteCurrentColorIconViolations.join(", ")}`).toEqual([]);
  expect(result.primaryForegroundViolations, `Wrong foregrounds on primary surfaces: ${result.primaryForegroundViolations.join(", ")}`).toEqual([]);
  expect(result.darkSurfaceViolations, `White canonical surfaces in dark mode: ${result.darkSurfaceViolations.join(", ")}`).toEqual([]);
}

async function assertThemeRoute(page: Page, route: AuditRoute, theme: "light" | "dark") {
  await installAuditHooks(page, route, theme);
  await page.goto(route.path, { waitUntil: "domcontentloaded" });
  // Admin dashboards keep SSE connections open, so networkidle may never arrive.
  // Bound this best-effort wait and rely on the rendered/loading-state assertions below.
  await page.waitForLoadState("networkidle", { timeout: 5_000 }).catch(() => undefined);
  await expect(page.locator("#root")).not.toBeEmpty();
  await expect.poll(async () => page.locator('[aria-busy="true"]:not(button):not([role="button"])').count(), {
    message: `Loading state did not settle on ${route.path}`,
    timeout: 15_000,
  }).toBe(0);
  await assertThemeContract(page, theme);
}

async function assertHealthyPage(page: Page, route: AuditRoute, info: TestInfo) {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const failedRequests: string[] = [];
  const errorResponses: string[] = [];

  page.on("console", (message) => {
    if (route.audience === "public" && message.type() === "error" && /(?:401 \(Unauthorized\)|403 \(Forbidden\))/i.test(message.text())) return;
    if ((message.type() === "error" || message.type() === "warning") && !shouldIgnore(message.text())) {
      consoleErrors.push(`${message.type()}: ${message.text()}`);
    }
  });
  page.on("pageerror", (error) => pageErrors.push(error.message));
  page.on("requestfailed", (request) => {
    const url = request.url();
    const failure = request.failure()?.errorText ?? "";
    if (/\/api\/learning\/[^/]+\/events(?:\?|$)/.test(url) && /ERR_ABORTED/i.test(failure)) return;
    if (/\/api\/admin\/metrics\/stream(?:\?|$)/.test(url) && /ERR_ABORTED/i.test(failure)) return;
    if (/\/api\/admin\/server\/stream(?:\?|$)/.test(url) && /ERR_ABORTED/i.test(failure)) return;
    if (!/fonts\.(googleapis|gstatic)\.com/i.test(url) && !/\.(png|jpe?g|gif|webp|svg|mp3|wav|mp4)(\?|$)/i.test(url)) {
      failedRequests.push(`${request.method()} ${url}: ${failure || "failed"}`);
    }
  });
  page.on("response", (response) => {
    const url = response.url();
    if (route.audience === "public" && response.status() === 403 && /\/api\/auth\/dev-login$/.test(url)) return;
    if (route.audience === "public" && response.status() === 401 && /\/api\/auth\/me(?:\?|$)/.test(url)) return;
    if (url.includes("/api/") && response.status() >= 400) {
      errorResponses.push(`${response.status()} ${response.request().method()} ${url}`);
    }
  });

  await page.goto(route.path, { waitUntil: "domcontentloaded" });
  await page.waitForLoadState("networkidle", { timeout: 5_000 }).catch(() => undefined);
  await expect(page.locator("#root")).not.toBeEmpty();
  await expect(page.locator("body")).not.toContainText("Ilova xatosi");
  await expect(page.locator("body")).not.toContainText("Something went wrong");
  await expect(page.locator("body")).not.toContainText("Yuklashda xatolik");

  await expect.poll(async () => page.locator('[aria-busy="true"]:not(button):not([role="button"])').count(), {
    message: `Loading state did not settle on ${route.path}`,
    timeout: 15_000,
  }).toBe(0);
  await expect.poll(async () => page.locator("#root").evaluate((root) => root.textContent?.trim().length ?? 0), {
    message: `Route rendered no visible content on ${route.path}`,
    timeout: 15_000,
  }).toBeGreaterThan(0);
  if (route.expectedText) {
    const matches = page.getByText(route.expectedText, { exact: false });
    await expect.poll(async () => {
      const count = await matches.count();
      for (let index = 0; index < count; index += 1) {
        if (await matches.nth(index).isVisible()) return true;
      }
      return false;
    }, {
      message: `Expected populated-data marker "${route.expectedText}" on ${route.path}`,
      timeout: 8_000,
    }).toBe(true);
  }

  if (route.kind !== "redirect") {
    await assertNoOverflow(page);
  }

  if (route.audience !== "public" && route.kind !== "redirect") {
    const results = await new AxeBuilder({ page })
      .disableRules(["color-contrast"])
      .analyze();
    const serious = results.violations.filter((violation) => violation.impact === "serious" || violation.impact === "critical");
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  }

  expect(pageErrors, `Page errors on ${route.path}`).toEqual([]);
  expect(failedRequests, `Failed requests on ${route.path}`).toEqual([]);
  expect(errorResponses, `HTTP error responses on ${route.path}`).toEqual([]);
  expect(consoleErrors, `Console problems on ${route.path}`).toEqual([]);

  await info.attach("route", { body: route.path, contentType: "text/plain" });
}

async function assertVisualAcceptancePage(
  page: Page,
  route: AuditRoute,
  info: TestInfo,
  theme: "light" | "dark",
) {
  await installAuditHooks(page, route, theme);
  await assertHealthyPage(page, route, info);
  await assertThemeContract(page, theme);
}

test.describe("@smoke critical journeys", () => {
  for (const viewport of SMOKE_VIEWPORTS) {
    for (const pattern of SMOKE_ROUTE_PATTERNS) {
      test(`${viewport.name} ${pattern}`, async ({ page }, info) => {
        test.setTimeout(60_000);
        const route = AUDIT_ROUTES.find((candidate) => candidate.pattern === pattern);
        expect(route, `Missing smoke route in audit manifest: ${pattern}`).toBeDefined();
        if (!route) return;

        await installAuditHooks(page, route);
        await page.setViewportSize(viewport);
        await assertHealthyPage(page, route, info);
      });
    }
  }
});

for (const route of AUDIT_ROUTES) {
  test(`route ${route.pattern}`, async ({ page }, info) => {
    test.setTimeout(180_000);
    await installAuditHooks(page, route);
    await page.setViewportSize({ width: 390, height: 844 });
    await assertHealthyPage(page, route, info);
  });
}

{
  const routes = AUDIT_ROUTES.filter((route) => route.kind !== "redirect");
  const batchCount = 6;
  const batches = Array.from({ length: batchCount }, (_, batchIndex) =>
    routes.filter((_, index) => index % batchCount === batchIndex),
  );
  batches.forEach((batch, batchIndex) => test(`legacy theme preference migrates to Play ${batchIndex + 1}`, async ({ page }) => {
    test.setTimeout(300_000);
    await page.setViewportSize({ width: 390, height: 844 });
    for (const route of batch) await assertThemeRoute(page, route, "dark");
  }));
}

for (const viewport of VIEWPORTS) {
  const routes = AUDIT_ROUTES.filter((route) => route.audience === "learner" && route.kind !== "redirect");
  // Keep each test comfortably below its timeout on the resource-limited self-hosted runner.
  // Two mobile batches put ~20 routes (including axe scans) into one 180s test, so Playwright
  // closed the page mid-route and Vite subsequently reported misleading ECONNRESET warnings.
  const batchCount = 6;
  const batches = Array.from({ length: batchCount }, (_, batchIndex) =>
    routes.filter((_, index) => index % batchCount === batchIndex),
  );
  batches.forEach((batch, batchIndex) => test(`responsive learner matrix ${viewport.name} ${batchIndex + 1}`, async ({ page }, info) => {
    test.setTimeout(180_000);
    await page.setViewportSize(viewport);
    for (const route of batch) {
      await installAuditHooks(page, route);
      await assertHealthyPage(page, route, info);
    }
  }));
}

for (const theme of ["light", "dark"] as const) {
  for (const viewport of VIEWPORTS) {
    const routes = AUDIT_ROUTES.filter((route) => VISUAL_ACCEPTANCE_PATTERNS.has(route.pattern));
    const batchCount = 3;
    const batches = Array.from({ length: batchCount }, (_, batchIndex) =>
      routes.filter((_, index) => index % batchCount === batchIndex),
    );
    batches.forEach((batch, batchIndex) => test(`visual acceptance matrix ${theme} ${viewport.name} ${batchIndex + 1}`, async ({ page }, info) => {
      test.setTimeout(240_000);
      await page.setViewportSize(viewport);
      for (const route of batch) {
        await assertVisualAcceptancePage(page, route, info, theme);
      }
    }));
  }
}

test("keyboard navigation keeps visible focus", async ({ page }) => {
  test.setTimeout(180_000);
  const route = AUDIT_ROUTES.find((item) => item.pattern === "/home")!;
  await installAuditHooks(page, route);
  await page.goto(route.path);
  await page.keyboard.press("Tab");
  const focused = page.locator(":focus");
  await expect(focused).toBeVisible();
  const outline = await focused.evaluate((element) => {
    const style = getComputedStyle(element);
    return { outline: style.outlineStyle, boxShadow: style.boxShadow };
  });
  expect(outline.outline !== "none" || outline.boxShadow !== "none").toBe(true);
});

test("mobile admin user overlays stay within viewport", async ({ page }) => {
  test.setTimeout(180_000);
  const route = AUDIT_ROUTES.find((item) => item.pattern === "/admin/users")!;
  await installAuditHooks(page, route);
  await page.route(/\/api\/admin\/users(?:\?.*)?$/, async (requestRoute) => {
    await requestRoute.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        viewerRole: 2,
        totalUsers: 2,
        adminCount: 1,
        onboardedCount: 2,
        premiumCount: 1,
        nextCursor: null,
        users: [
          {
            id: "mock-admin-0001",
            email: "admin@englishai.uz",
            displayName: "Demo Admin",
            username: "demo-admin",
            pictureUrl: null,
            role: 2,
            registeredAt: "2026-01-01T10:00:00Z",
            lastLoginAt: "2026-08-05T10:00:00Z",
            hasOnboarded: true,
            level: "B1",
            lastActivityAt: "2026-08-05T10:00:00Z",
            subscriptionStatus: "Premium",
            subscriptionPlan: "Monthly",
            subscriptionExpiresAt: "2026-09-01T00:00:00Z",
          },
          {
            id: "mock-learner-0002",
            email: "learner@englishai.uz",
            displayName: "Demo Learner",
            username: "demo-learner",
            pictureUrl: null,
            role: 0,
            registeredAt: "2026-02-01T10:00:00Z",
            lastLoginAt: "2026-08-06T10:00:00Z",
            hasOnboarded: true,
            level: "A2",
            lastActivityAt: "2026-08-06T10:00:00Z",
            subscriptionStatus: "Free",
            subscriptionPlan: null,
            subscriptionExpiresAt: null,
          },
        ],
      }),
    });
  });
  await page.setViewportSize({ width: 360, height: 800 });
  await page.goto(route.path);

  await page.getByRole("button", { name: /^Rol:/ }).click();
  await assertDialogWithinViewport(page, "Rol");
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog", { name: "Rol" })).toBeHidden();

  await page.getByRole("button", { name: "Admin qilish" }).click();
  await assertDialogWithinViewport(page, "Admin qilish");
});
