import { expect, test, type Page } from "@playwright/test";

const LESSON_ID = "a6111111-1111-4111-8111-111111111111";
const playlist = {
  id: "PL-puss",
  title: "Puss in Boots — full movie collection",
  channel: "DreamWorks English",
  thumbnailUrl: "https://img.example/puss-playlist.jpg",
  totalDurationSeconds: 5_400,
  isSingleVideoCollection: false,
  items: [
    {
      youTubeVideoId: "PussBoots01",
      title: "Puss in Boots — Part 1",
      channel: "DreamWorks English",
      durationSeconds: 2_700,
      thumbnailUrl: "https://img.example/puss-one.jpg",
    },
    {
      youTubeVideoId: "PussBoots02",
      title: "Puss in Boots — Part 2",
      channel: "DreamWorks English",
      durationSeconds: 2_700,
      thumbnailUrl: "https://img.example/puss-two.jpg",
    },
    ...Array.from({ length: 6 }, (_, index) => ({
      youTubeVideoId: `PussBoots${String(index + 3).padStart(2, "0")}`,
      title: `Puss in Boots — Part ${index + 3}`,
      channel: "DreamWorks English",
      durationSeconds: 2_700,
      thumbnailUrl: `https://img.example/puss-${index + 3}.jpg`,
    })),
  ],
};
const catalogVideos = Array.from({ length: 4 }, (_, index) => ({
  lessonId: null,
  youTubeVideoId: `CatalogVid${String(index).padStart(2, "0")}`,
  title: `English learning video ${index + 1}`,
  channel: "EnglishAI catalog",
  channelAvatarUrl: null,
  durationSeconds: 300 + index,
  topic: "english",
  level: 2,
  hasClosedCaptions: true,
}));
const playlistVariants = Array.from({ length: 4 }, (_, index) => ({
  ...playlist,
  id: index === 0 ? playlist.id : `${playlist.id}-${index + 1}`,
  title: [
    "Puss in Boots — full movie collection",
    "Inside Out — full movie collection",
    "Kung Fu Panda — full episode collection",
    "How to Train Your Dragon — full episode collection",
  ][index],
}));
const lesson = {
  id: LESSON_ID,
  youTubeVideoId: playlist.items[0].youTubeVideoId,
  title: playlist.items[0].title,
  channel: playlist.channel,
  durationSeconds: playlist.items[0].durationSeconds,
  topic: "full-playlist",
  level: 2,
  status: 1,
  transcriptStatus: 1,
  transcript: [
    { startSeconds: 4, endSeconds: 7, englishText: "The project subtitle starts independently from YouTube captions.", uzbekTranslation: "Loyiha subtitri YouTube captionidan mustaqil boshlanadi.", words: [] },
    { startSeconds: 7, endSeconds: 10, englishText: "The rest of the transcript can continue in the background.", uzbekTranslation: "Qolgan transkript fon rejimida davom etadi.", words: [] },
  ],
  glossary: [],
  questions: [],
};

async function playlistFixture(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    localStorage.setItem("englishai.level.mock-learner-0001", "2");
    const scope = window as unknown as {
      YT: unknown;
      __englishAiPlaylistTest?: { end: () => void; playCalls: () => number };
    };
    scope.YT = {
      Player: class {
        iframe: HTMLIFrameElement;
        playCount = 0;
        currentTime = 0;
        playerState = 2;
        constructor(target: string, options: {
          events?: {
            onReady?: (event: { target: unknown }) => void;
            onStateChange?: (event: { target: unknown; data: number }) => void;
          };
        }) {
          const node = document.getElementById(target);
          this.iframe = document.createElement("iframe");
          this.iframe.id = target;
          node?.replaceWith(this.iframe);
          scope.__englishAiPlaylistTest = {
            end: () => {
              this.playerState = 0;
              options.events?.onStateChange?.({ target: this, data: 0 });
            },
            playCalls: () => this.playCount,
          };
          setTimeout(() => options.events?.onReady?.({ target: this }), 0);
        }
        getIframe() { return this.iframe; }
        getCurrentTime() { return this.currentTime; }
        getDuration() { return 2700; }
        getPlayerState() { return this.playerState; }
        getPlaybackRate() { return 1; }
        getVolume() { return 80; }
        isMuted() { return false; }
        playVideo() { this.playCount += 1; this.playerState = 1; }
        pauseVideo() { this.playerState = 2; }
        seekTo(seconds: number) { this.currentTime = seconds; }
        destroy() { this.iframe.remove(); }
      },
    };
  });

  await page.route("**/api/auth/me", (route) => route.fulfill({ json: {
    id: "mock-learner-0001",
    email: "playlist@englishai.uz",
    displayName: "Playlist learner",
    username: "playlist_learner",
    hasOnboarded: true,
    hasCompletedDemographics: true,
    learningGoal: 1,
    isAdmin: false,
  } }));
  await page.route("**/api/gamification/**/points", (route) => route.fulfill({ json: { lifetimeXp: 360 } }));
  await page.route("**/api/gamification/**/energy", (route) => route.fulfill({
    json: { current: 5, maximum: 5, nextRefillAt: "2030-01-01T00:00:00Z" },
  }));
  await page.route("**/api/vocabulary/**/notifications", (route) => route.fulfill({ json: [] }));
  await page.route("https://img.example/**", (route) => route.fulfill({
    contentType: "image/svg+xml",
    body: '<svg xmlns="http://www.w3.org/2000/svg" width="640" height="360"><rect width="640" height="360" fill="#7545e8"/></svg>',
  }));

  await page.route("**/api/video/playlist/search?*", (route) => {
    const query = new URL(route.request().url()).searchParams.get("q")?.toLowerCase() ?? "";
    return route.fulfill({ json: { items: query.includes("unknown") ? [] : playlistVariants } });
  });
  await page.route("**/api/video/search?*", (route) => route.fulfill({
    json: { items: catalogVideos, nextCursor: null },
  }));
  await page.route(`**/api/video/playlist/${playlist.id}`, (route) => route.fulfill({ json: playlist }));
  await page.route("**/api/video/open", (route) => route.fulfill({
    json: lesson,
  }));
  await page.route(`**/api/video/${LESSON_ID}`, (route) => route.fulfill({ json: lesson }));
}

test.afterEach(async ({ page }) => {
  await page.unrouteAll({ behavior: "wait" });
});

test("film search discovers a duration-checked Puss in Boots playlist and opens its dedicated playlist episode page", async ({ page }) => {
  await playlistFixture(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/video/search?q=Puss%20in%20Boots");

  await expect(page.getByRole("heading", { name: "Topilgan playlistlar" })).toBeVisible();
  await expect(page.getByRole("button", { name: /Puss in Boots — full movie collection/ })).toBeVisible();
  await page.getByRole("button", { name: /Puss in Boots — full movie collection/ }).click();
  await expect(page).toHaveURL(`/video/playlists/${playlist.id}`);
  await expect(page.getByRole("heading", { name: playlist.title })).toBeVisible();
  await expect(page.getByRole("button", { name: "Puss in Boots — Part 1 — ijro" })).toBeVisible();

  await page.getByRole("button", { name: "Puss in Boots — Part 1 — ijro" }).click();
  await expect(page).toHaveURL(`/video/playlists/${playlist.id}/01`);
  await expect(page.getByRole("heading", { name: "01 · Puss in Boots — Part 1" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Playlist" })).toBeVisible();
});

test("Multfilm category opens a full-episode shelf for older learners", async ({ page }) => {
  await playlistFixture(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/video");

  await page.getByRole("button", { name: "Multfilm" }).click();
  await expect(page.getByRole("heading", { name: "10+ yosh uchun multfilm playlistlari" })).toBeVisible();
  await expect(page.getByText("Puss in Boots — full movie collection")).toBeVisible();
  await page.getByRole("button", { name: /Puss in Boots — full movie collection/ }).click();
  await expect(page).toHaveURL(`/video/playlists/${playlist.id}`);
});

test("desktop catalog and full-episode shelves use four cards per row", async ({ page }) => {
  await playlistFixture(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/video");

  await expect(page.locator(".video-catalog__card")).toHaveCount(4);
  expect(await page.locator(".video-catalog__grid").evaluate((element) =>
    getComputedStyle(element).gridTemplateColumns.split(" ").length,
  )).toBe(4);

  await page.getByRole("button", { name: "Multfilm" }).click();
  await expect(page.locator(".video-catalog__playlist-card")).toHaveCount(4);
  expect(await page.locator(".video-catalog__playlist-grid").evaluate((element) =>
    getComputedStyle(element).gridTemplateColumns.split(" ").length,
  )).toBe(4);
});

test("playlist player keeps YouTube visible while a pending transcript becomes partial", async ({ page }) => {
  await playlistFixture(page);
  const pending = { ...lesson, transcriptStatus: 0, transcript: [] };
  const partial = { ...lesson, transcriptStatus: 3, transcript: [lesson.transcript[0]] };
  await page.route("**/api/video/open", route => route.fulfill({ json: pending }));
  let lessonReads = 0;
  await page.unroute(`**/api/video/${LESSON_ID}`);
  await page.route(`**/api/video/${LESSON_ID}`, (route) => {
    lessonReads += 1;
    return route.fulfill({ json: lessonReads === 1 ? pending : partial });
  });

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/video/playlists/${playlist.id}/01`);

  await expect(page.locator(".video-playlist-player__frame iframe")).toBeVisible();
  await expect(page.locator(".video-playlist-player__caption-list").getByText(
    "The project subtitle starts independently from YouTube captions.",
  )).toBeVisible();
  expect(lessonReads).toBeGreaterThanOrEqual(2);
});

test("a pending playlist transcript explains loading instead of leaving a frozen icon", async ({ page }) => {
  await playlistFixture(page);
  const pending = { ...lesson, transcriptStatus: 0, transcript: [] };
  await page.route("**/api/video/open", route => route.fulfill({ json: pending }));
  await page.unroute(`**/api/video/${LESSON_ID}`);
  await page.route(`**/api/video/${LESSON_ID}`, (route) => route.fulfill({ json: pending }));
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/video/playlists/${playlist.id}/01`);

  await expect(page.locator(".video-playlist-player__caption-pending")).toBeVisible();
  await expect(page.locator(".video-playlist-player__caption-pending")).toHaveAttribute(
    "aria-label",
    "Subtitrlar yuklanmoqda…",
  );
  await expect(page.getByText("Subtitrlar yuklanmoqda…")).toBeVisible();
  await expect(page.locator(".video-playlist-player__caption-pending")).toHaveAttribute("aria-busy", "true");
});

test("a completed playlist episode opens and starts the next item, but the final item stays put", async ({ page }) => {
  await playlistFixture(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/video/playlists/${playlist.id}/01`);
  await expect(page.getByRole("button", { name: "Puss in Boots — Part 1" })).toHaveAttribute("aria-current", "true");
  await expect(page.getByText("Hozir ijro etilmoqda")).toBeVisible();
  await expect(page.locator(".video-playlist-player__frame iframe")).toBeVisible();

  await page.evaluate(() => {
    const testWindow = window as typeof window & { __englishAiPlaylistTest?: { end: () => void } };
    testWindow.__englishAiPlaylistTest?.end();
  });

  await expect(page).toHaveURL(`/video/playlists/${playlist.id}/02`);
  await expect(page.getByRole("button", { name: "Puss in Boots — Part 2" })).toHaveAttribute("aria-current", "true");
  await expect.poll(() => page.evaluate(() => {
    const testWindow = window as typeof window & { __englishAiPlaylistTest?: { playCalls: () => number } };
    return testWindow.__englishAiPlaylistTest?.playCalls() ?? 0;
  })).toBeGreaterThan(0);

  await page.goto(`/video/playlists/${playlist.id}/08`);
  await expect(page.getByRole("button", { name: "Puss in Boots — Part 8" })).toHaveAttribute("aria-current", "true");
  await page.evaluate(() => {
    const testWindow = window as typeof window & { __englishAiPlaylistTest?: { end: () => void } };
    testWindow.__englishAiPlaylistTest?.end();
  });
  await page.waitForTimeout(250);
  await expect(page).toHaveURL(`/video/playlists/${playlist.id}/08`);
});

test("every visible subtitle has an AI icon that immediately sends its own text", async ({ page }) => {
  await playlistFixture(page);
  let submitted: { userMessage?: string } | null = null;
  await page.route("**/api/video/explain/stream", (route) => {
    submitted = JSON.parse(route.request().postData() ?? "{}") as { userMessage?: string };
    return route.fulfill({
      contentType: "text/event-stream",
      body: 'event: chunk\ndata: {"text":"AI javobi"}\n\nevent: done\ndata: {}\n\n',
    });
  });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/video/playlists/${playlist.id}/01`);

  const text = "The project subtitle starts independently from YouTube captions.";
  await page.getByRole("button", { name: `"${text}" ni AI bilan tushunish` }).click();
  await expect.poll(() => submitted?.userMessage).toBe(text);
});

for (const { width, height } of [
  { width: 1440, height: 900 },
  { width: 390, height: 844 },
]) {
  test(`screen 57P keeps the native playlist player and queue usable at ${width}px`, async ({ page }, info) => {
    await playlistFixture(page);
    await page.setViewportSize({ width, height });
    await page.goto(`/video/playlists/${playlist.id}/01`);

    await expect(page.getByRole("heading", { name: "01 · Puss in Boots — Part 1" })).toBeVisible();
    await expect(page.locator(".video-playlist-player__frame iframe")).toHaveAttribute("allowfullscreen", "");
    await expect(page.locator(".video-playlist-player__caption-list").getByText("The project subtitle starts independently from YouTube captions.")).toBeVisible();
    await expect(page.getByRole("button", {
      name: /The project subtitle starts independently from YouTube captions.*AI bilan tushunish/,
    })).toBeEnabled();
    await expect(page.getByRole("heading", { name: "Playlist" })).toBeVisible();
    if (width <= 700) await page.getByRole("button", { name: /^Playlist/ }).click();
    await expect(page.getByRole("button", { name: "Puss in Boots — Part 2" })).toBeVisible();
    const queueList = page.locator(".video-playlist-player__queue-list");
    await expect(queueList.getByRole("button")).toHaveCount(playlist.items.length);
    const geometry = await page.evaluate(() => {
      const box = (selector: string) => {
        const rect = document.querySelector(selector)!.getBoundingClientRect();
        return {
          x: rect.x,
          y: rect.y,
          width: rect.width,
          height: rect.height,
          bottom: rect.bottom,
          right: rect.right,
        };
      };
      return {
        frame: box(".video-playlist-player__frame"),
        queue: box(".video-playlist-player__queue"),
        navigation: box(window.innerWidth > 700 ? ".video-playlist-player__episode-nav" : ".video-playlist-player__queue-mobile-nav"),
        pageWidth: document.documentElement.scrollWidth,
      };
    });
    expect(geometry.pageWidth).toBeLessThanOrEqual(width);
    if (width > 700) {
      const caption = await page.locator(".video-playlist-player__captions").boundingBox();
      expect(geometry.queue.x).toBeLessThan(geometry.frame.x);
      expect(caption?.x).toBeCloseTo(geometry.queue.x, 0);
      expect(geometry.queue.y).toBeGreaterThanOrEqual(caption ? caption.y + caption.height : 0);
      expect(geometry.frame.width).toBeGreaterThanOrEqual(760);
      expect(geometry.frame.height).toBeGreaterThanOrEqual(420);
      expect(geometry.navigation.bottom).toBeLessThanOrEqual(height);
      expect(await page.evaluate(() => document.documentElement.scrollHeight <= document.documentElement.clientHeight)).toBe(true);
      const finalQueueScroll = await queueList.evaluate((node) => {
        node.scrollTop = node.scrollHeight;
        return node.scrollTop;
      });
      expect(finalQueueScroll).toBeGreaterThan(0);
      await expect(queueList.getByRole("button", { name: "Puss in Boots — Part 8" })).toBeVisible();
    } else {
      expect(geometry.queue.y).toBeGreaterThanOrEqual(geometry.frame.bottom);
      expect(geometry.frame.width).toBeCloseTo(width, 0);
    }
    await page.screenshot({ path: info.outputPath(`video-57p-${width}.png`), fullPage: true });
  });
}

for (const { width, height } of [
  { width: 1440, height: 900 },
  { width: 390, height: 844 },
]) {
  test(`screen 56D keeps the full-video empty state usable at ${width}px`, async ({ page }, info) => {
    await playlistFixture(page);
    await page.setViewportSize({ width, height });
    await page.goto("/video/search?q=Unknown%20film");

    await expect(page.getByRole("heading", { name: "To‘liq video topilmadi" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Boshqa nom bilan sinab ko‘ring" })).toBeVisible();
    await expect(page.getByText(/Qisqa parchalarni “full” deb ko‘rsatmaymiz/)).toBeVisible();
    await expect(page.getByRole("textbox", { name: "Qidiruvni o‘zgartirish" })).toHaveValue("Unknown film");
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
    await page.screenshot({ path: info.outputPath(`video-56d-${width}.png`), fullPage: true });
  });
}

async function pen72Fixture(page: Page) {
  await playlistFixture(page);
  await page.route(`**/api/video/${LESSON_ID}`, route => route.fulfill({ json: {
    ...lesson,
    transcript: [
      { startSeconds: 0, endSeconds: 6, englishText: "Hello, what's your name?", uzbekTranslation: "Salom, ismingiz nima?", words: [] },
      { startSeconds: 6, endSeconds: 9, englishText: "Hi, I'm Tim.", uzbekTranslation: "Salom, men Timman.", words: [] },
      { startSeconds: 9, endSeconds: 18, englishText: "Hello, I'm Sian.", uzbekTranslation: "Salom, men Sianman.", words: [] },
      { startSeconds: 18, endSeconds: 25, englishText: "It's nice to meet you.", uzbekTranslation: "Siz bilan tanishganimdan xursandman.", words: [] },
      ...Array.from({ length: 30 }, (_, index) => ({ startSeconds: 25 + index * 5, endSeconds: 30 + index * 5, englishText: `Another sentence ${index + 1}.`, uzbekTranslation: `Keyingi jumla ${index + 1}.`, words: [] })),
    ],
  } }));
  await page.route("**/api/gamification/**/energy", route => route.fulfill({ json: { current: 4, maximum: 5, consumed: true, nextRefillAt: "2030-01-01T00:00:00Z" } }));
  await page.route("**/api/video/explain/stream", route => route.fulfill({ contentType: "text/event-stream", body: 'event: chunk\ndata: {"text":"Bu ibora ilk tanishuvda aytiladi. Javob: Nice to meet you too."}\n\nevent: done\ndata: {}\n\n' }));
}

async function expectPlaylistViewport(page: Page) {
  const geometry = await page.evaluate(() => ({
    width: innerWidth, height: innerHeight, scrollX: scrollX, scrollY: scrollY,
    documentWidth: document.documentElement.scrollWidth, documentHeight: document.documentElement.scrollHeight,
    bodyHeight: document.body.scrollHeight,
    sections: [...document.querySelectorAll<HTMLElement>(".video-playlist-player__captions, .video-playlist-player__queue, .video-playlist-player__frame, .video-playlist-player__ai")]
      .filter(element => element.getBoundingClientRect().height > 0)
      .map(element => ({ name: element.className, bottom: element.getBoundingClientRect().bottom, right: element.getBoundingClientRect().right, top: element.getBoundingClientRect().top })),
  }));
  expect(geometry.documentWidth).toBeLessThanOrEqual(geometry.width);
  expect(geometry.documentHeight).toBeLessThanOrEqual(geometry.height + 1);
  expect(geometry.bodyHeight).toBeLessThanOrEqual(geometry.height + 1);
  expect(geometry.scrollX).toBe(0);
  expect(geometry.scrollY, JSON.stringify(geometry)).toBe(0);
  for (const section of geometry.sections) {
    expect(section.bottom, `${section.name} below viewport`).toBeLessThanOrEqual(geometry.height + 1);
    expect(section.right, `${section.name} beyond viewport`).toBeLessThanOrEqual(geometry.width + 1);
    expect(section.top).toBeGreaterThanOrEqual(0);
  }
  const frame = await page.locator(".video-playlist-player__frame").boundingBox();
  expect(frame!.width / frame!.height).toBeCloseTo(16 / 9, 2);
}

for (const [width, height] of [[1440, 900], [1366, 768], [1024, 768], [768, 1024], [701, 800], [700, 800], [390, 844], [375, 667], [360, 800], [320, 568]]) {
  test(`Pen 72 playlist, transcript and inline AI fit ${width}x${height}`, async ({ page }, info) => {
    await pen72Fixture(page);
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.setViewportSize({ width, height });
    await page.goto(`/video/playlists/${playlist.id}/05`);
    await expect(page.getByRole("heading", { name: "Transcript", exact: true })).toBeVisible();
    await expect(page.locator(".video-playlist-player__caption-row.is-current")).toBeVisible();
    await expect(page.getByRole("link", { name: "Energiya: 4 / 5" })).toBeVisible();
    await expect(page.locator(".video-playlist-player__energy")).toHaveCount(0);
    await expect(page.getByText("Video ochildi. Yaxshi tomosha!")).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Tanlangan gapni takrorlash", includeHidden: true })).toHaveCount(0);
    await expectPlaylistViewport(page);
    await page.mouse.move(width - 5, height / 2);
    await page.mouse.wheel(0, 500);
    await expectPlaylistViewport(page);
    await page.screenshot({ path: info.outputPath(`pen72-playlist-${width}-default.png`) });
    if (width <= 700) {
      const toggle = page.getByRole("button", { name: /^Playlist/ });
      await expect(toggle).toHaveAttribute("aria-expanded", "false");
      await toggle.click();
      await expect(toggle).toHaveAttribute("aria-expanded", "true");
      await expect(page.getByRole("button", { name: "Puss in Boots — Part 5", exact: true })).toHaveAttribute("aria-current", "true");
      await expectPlaylistViewport(page);
      const queue = page.locator(".video-playlist-player__queue-list");
      await queue.evaluate(element => { element.scrollTop = element.scrollHeight; });
      await expect(queue.getByRole("button", { name: "Puss in Boots — Part 8", exact: true })).toBeInViewport();
      await page.screenshot({ path: info.outputPath(`pen72-playlist-${width}-queue.png`) });
      await toggle.click();
    }
    await page.getByRole("button", { name: '"Hello, what\'s your name?" ni AI bilan tushunish' }).click();
    await expect(page.locator(".video-ai-contextual__messages")).toContainText("Bu ibora ilk tanishuvda aytiladi.");
    await expect(page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" })).toBeInViewport();
    await expect(page.locator(".video-playlist-player__frame iframe")).toBeInViewport();
    await expectPlaylistViewport(page);
    await page.screenshot({ path: info.outputPath(`pen72-playlist-${width}-ai.png`) });
    expect(errors).toEqual([]);
  });
}

test("Pen 72 translation toggles inline and transcript paging returns to playback", async ({ page }) => {
  await pen72Fixture(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/video/playlists/${playlist.id}/05`);
  await expect(page.locator(".video-playlist-player__translation").filter({ hasText: "Salom, ismingiz nima?" })).toBeVisible();
  await page.getByRole("button", { name: "Tarjimani yashirish" }).click();
  await expect(page.locator(".video-playlist-player__translation").filter({ hasText: "Salom, ismingiz nima?" })).toBeHidden();
  await page.getByRole("button", { name: "Tarjimani ko‘rsatish" }).click();
  await expect(page.locator(".video-playlist-player__translation").filter({ hasText: "Salom, ismingiz nima?" })).toBeVisible();
  await page.getByRole("button", { name: "Keyingi jumlalar" }).click();
  await expect(page.getByRole("button", { name: "Videoga mos kuzatish" })).toHaveAttribute("aria-pressed", "false");
  await page.getByRole("button", { name: "Videoga mos kuzatish" }).click();
  await expect(page.locator(".video-playlist-player__translation").filter({ hasText: "Salom, ismingiz nima?" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Tanlangan gapni takrorlash", includeHidden: true })).toHaveCount(0);
  const quiz = page.getByRole("button", { name: "Video testiga o‘tish" });
  await expect(quiz).toBeEnabled();
  expect(await quiz.evaluate(element => getComputedStyle(element).backgroundColor)).toBe("rgba(0, 0, 0, 0)");
  await quiz.click();
  await expect(page).toHaveURL(`/video/${LESSON_ID}/quiz`);
});

for (const [width, height] of [[1440, 900], [375, 667]]) {
  test(`Pen 72 oversized transcript stays internally scrollable at ${width}px`, async ({ page }) => {
    await pen72Fixture(page);
    const longText = "This is a deliberately long subtitle for checking accessible overflow. ".repeat(65);
    await page.route(`**/api/video/${LESSON_ID}`, route => route.fulfill({ json: {
      ...lesson, transcript: Array.from({ length: 2300 }, (_, index) => ({ startSeconds: index * 5, endSeconds: index * 5 + 5, englishText: index === 0 ? longText : `Transcript line ${index}.`, uzbekTranslation: index === 0 ? "Uzun jumlaning tarjimasi." : null, words: [] })),
    } }));
    await page.setViewportSize({ width, height });
    await page.goto(`/video/playlists/${playlist.id}/05`);
    const current = page.locator(".video-playlist-player__caption-row.is-current");
    await expect(current).toHaveAttribute("aria-setsize", "2300");
    await expect(page.locator(".video-playlist-player__caption-row")).toHaveCount(1);
    expect(await current.evaluate(element => {
      element.scrollTop = element.scrollHeight;
      return element.scrollTop;
    })).toBeGreaterThan(0);
    await expect(current.locator(".video-playlist-player__translation")).toBeInViewport();
    // Mobile keeps the AI icon beside the timestamp. Both ends of an oversized sentence
    // must remain reachable rather than requiring the entire paragraph to fit at once.
    if (width <= 700) await current.evaluate(element => { element.scrollTop = 0; });
    await expect(current.getByRole("button", { name: /AI bilan tushunish/ })).toBeInViewport();
    await expectPlaylistViewport(page);
    await page.getByRole("button", { name: "Keyingi jumlalar" }).click();
    await expect(page.locator('[data-caption-index="1"]')).toBeVisible();
  });
}

test("Pen 72 mobile queue handles 41 episodes and a long film title", async ({ page }, info) => {
  await pen72Fixture(page);
  const longPlaylist = {
    ...playlist,
    items: Array.from({ length: 41 }, (_, index) => ({ ...playlist.items[0], youTubeVideoId: `FilmTest${String(index).padStart(3, "0")}`, title: `${index + 1} · The keeper of keys | Harry Potter and the Philosopher’s Stone` })),
  };
  await page.route(`**/api/video/playlist/${playlist.id}`, route => route.fulfill({ json: longPlaylist }));
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/video/playlists/${playlist.id}/05`);
  await expect(page.locator(".video-playlist-player__caption-row.is-current")).toBeVisible();
  await expectPlaylistViewport(page);
  await page.getByRole("button", { name: /^Playlist/ }).click();
  await expect(page.locator(".video-playlist-player__queue-list > button")).toHaveCount(41);
  await page.locator(".video-playlist-player__queue-list").evaluate(element => { element.scrollTop = element.scrollHeight; });
  const last = page.getByRole("button", { name: longPlaylist.items[40].title, exact: true });
  await expect(last).toBeInViewport();
  await last.click();
  await expect(page).toHaveURL(`/video/playlists/${playlist.id}/41`);
  await expect(page.getByRole("button", { name: "Keyingi qism" })).toBeDisabled();
  await expectPlaylistViewport(page);
  await page.screenshot({ path: info.outputPath("pen72-mobile-long-film.png") });
});

test("Pen 72 simulated mobile keyboard keeps video and the AI composer reachable", async ({ page }) => {
  await pen72Fixture(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/video/playlists/${playlist.id}/05`);
  const input = page.getByRole("textbox", { name: "Shu jumla haqida savol yozing" });
  await expect(input).toBeEnabled();
  await input.focus();
  await page.evaluate(() => {
    Object.defineProperty(window.visualViewport, "height", { configurable: true, value: 430 });
    window.visualViewport?.dispatchEvent(new Event("resize"));
  });
  await expect(page.locator(".video-playlist-player")).toHaveAttribute("data-keyboard", "true");
  expect((await input.boundingBox())!.y + (await input.boundingBox())!.height).toBeLessThanOrEqual(430);
  await expect(page.locator(".video-playlist-player__frame iframe")).toBeInViewport();
  await input.fill("Bu jumla nimani anglatadi?");
  await input.press("Enter");
  await expect(page.locator(".video-ai-contextual__messages")).toContainText("Bu ibora ilk tanishuvda aytiladi.");
  await page.evaluate(() => {
    Object.defineProperty(window.visualViewport, "height", { configurable: true, value: 844 });
    window.visualViewport?.dispatchEvent(new Event("resize"));
  });
  await expectPlaylistViewport(page);
});

for (const width of [1440, 390]) {
  test(`transcript loader animates until real captions arrive at ${width}px`, async ({ page }, info) => {
    await playlistFixture(page);
    const pending = { ...lesson, transcriptStatus: 0, transcript: [] };
    let ready = false;
    await page.route("**/api/video/open", route => route.fulfill({ json: pending }));
    await page.route(`**/api/video/${LESSON_ID}`, route => route.fulfill({ json: ready ? lesson : pending }));
    await page.emulateMedia({ reducedMotion: "no-preference" });
    await page.setViewportSize({ width, height: 844 });
    await page.goto(`/video/playlists/${playlist.id}/01`);
    const spinner = page.locator(".video-playlist-player__spinner");
    await expect(spinner).toBeVisible();
    expect(await spinner.evaluate(element => getComputedStyle(element).animationName)).toBe("playlist-spin");
    const firstTransform = await spinner.evaluate(element => getComputedStyle(element).transform);
    await expect.poll(() => spinner.evaluate(element => getComputedStyle(element).transform)).not.toBe(firstTransform);
    await expect(page.getByText("Subtitrlar yuklanmoqda…")).toBeVisible();
    await expect(page.locator(".video-playlist-player__loading-lines i")).toHaveCount(3);
    await page.screenshot({ path: info.outputPath(`transcript-loading-${width}.png`) });
    ready = true;
    await expect(page.locator(".video-playlist-player__caption-row.is-current")).toBeVisible();
    await expect(spinner).toHaveCount(0);
    await expect(page.locator(".video-playlist-player__caption-select").first()).toContainText(lesson.transcript[0].englishText);
    await expectPlaylistViewport(page);
    await page.screenshot({ path: info.outputPath(`transcript-loaded-${width}.png`) });
  });
}

test("captions from open render even if the next lesson read fails", async ({ page }) => {
  await playlistFixture(page);
  await page.route(`**/api/video/${LESSON_ID}`, route => route.fulfill({ status: 503, json: { error: "temporary unavailable" } }));
  await page.goto(`/video/playlists/${playlist.id}/01`);
  await expect(page.locator(".video-playlist-player__caption-select").first()).toContainText(lesson.transcript[0].englishText);
  await expect(page.locator(".video-playlist-player__caption-pending")).toHaveCount(0);
});

test("failed transcript reads expose retry and recover without reloading YouTube", async ({ page }) => {
  await playlistFixture(page);
  const pending = { ...lesson, transcriptStatus: 0, transcript: [] };
  let recovering = false;
  await page.route("**/api/video/open", route => route.fulfill({ json: pending }));
  await page.route(`**/api/video/${LESSON_ID}`, route => recovering ? route.fulfill({ json: lesson }) : route.fulfill({ status: 503, json: { error: "unavailable" } }));
  await page.goto(`/video/playlists/${playlist.id}/01`);
  await expect(page.getByText("Subtitrlarni yuklab bo‘lmadi. Qayta urinib ko‘ring.")).toBeVisible();
  await expect(page.locator(".video-playlist-player__spinner")).toHaveCount(0);
  await page.locator(".video-playlist-player__frame iframe").evaluate(element => element.setAttribute("data-existing-player", "true"));
  recovering = true;
  await page.getByRole("button", { name: "Qayta urinish" }).click();
  await expect(page.locator(".video-playlist-player__caption-row.is-current")).toBeVisible();
  await expect(page.locator(".video-playlist-player__frame iframe")).toHaveAttribute("data-existing-player", "true");
});

test("pending transcript times out honestly and can retry", async ({ page }) => {
  await playlistFixture(page);
  const pending = { ...lesson, transcriptStatus: 0, transcript: [] };
  await page.route("**/api/video/open", route => route.fulfill({ json: pending }));
  await page.route(`**/api/video/${LESSON_ID}`, route => route.fulfill({ json: pending }));
  await page.clock.install();
  await page.goto(`/video/playlists/${playlist.id}/01`);
  await expect(page.getByText("Subtitrlar yuklanmoqda…")).toBeVisible();
  await page.clock.fastForward(31_000);
  await expect(page.getByText("YouTube subtitrlarini olish kutilganidan uzoqroq davom etmoqda…")).toBeVisible();
  await page.clock.fastForward(60_000);
  await expect(page.getByText("Subtitrlarni yuklab bo‘lmadi. Qayta urinib ko‘ring.")).toBeVisible();
  await expect(page.locator(".video-playlist-player__caption-pending")).toHaveAttribute("aria-busy", "false");
  await expect(page.getByRole("button", { name: "Qayta urinish" })).toBeEnabled();
});
