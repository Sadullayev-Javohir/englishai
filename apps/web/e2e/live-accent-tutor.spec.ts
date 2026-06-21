import { expect, test } from "@playwright/test";

async function prepareLearner(
  page: import("@playwright/test").Page,
  theme: "light" | "dark",
  voiceLiveEvents?: Array<Record<string, unknown>>,
) {
  // Playwright creates an isolated browser context for every test. A module-level flag outlives
  // that context and incorrectly skips login for later tests, leaving them on the login page.
  const login = await page.request.post("/api/auth/dev-login", { data: {} });
  expect(login.ok()).toBe(true);
  await page.route(/\/api\/speaking\/accent-tutors\/[^/]+\/start$/, async (route) => route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify({ text: "Hey there! What have you been up to today?", tutorAudioBase64: "", isNaturalVoice: false, wordTimings: [] }),
  }));
  await page.route(/\/api\/vocabulary\/[^/]+\/due$/, async (route) => route.fulfill({
    status: 200,
    contentType: "application/json",
    body: "[]",
  }));
  await page.route(/\/api\/subscription\/[^/]+$/, async (route) => route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify({
      learnerId: "mock-learner-0001",
      status: 0,
      plan: null,
      expiresAt: null,
      daysUntilExpiry: null,
      isPremiumActive: false,
      isTrialActive: false,
      trialExpiresAt: null,
      trialDaysUntilExpiry: null,
    }),
  }));
  await page.route(/\/api\/speaking\/accent-tutors\/[^/]+\/voice-live\/token$/, async (route) => route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify({
      webSocketUrl: "ws://voice-live.test/realtime?api-version=2025-10-01",
      authorizationQueryParameter: "authorization",
      authorizationValue: "mock-token",
      expiresAt: "2027-08-15T00:00:00Z",
      session: {
        voiceName: "en-US-AvaMultilingualNeural",
        inputAudioFormat: "pcm16",
        outputAudioFormat: "pcm16",
        inputSamplingRate: 24000,
        silenceDurationMs: 700,
        turnDetectionType: "server_vad",
      },
    }),
  }));
  await page.addInitScript(({ selectedTheme, voiceLiveEvents }) => {
    localStorage.setItem("englishai-theme", selectedTheme);
    (window as typeof window & { __voiceLiveScenario?: Array<Record<string, unknown>> }).__voiceLiveScenario = voiceLiveEvents;
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    localStorage.setItem("englishai.level.mock-learner-0001", "1");
    localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");

    const track = { readyState: "live", stop() {}, addEventListener() {} };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: { getUserMedia: async () => ({ getAudioTracks: () => [track], getTracks: () => [track] }) },
    });
    class FakeRecorder {
      static isTypeSupported() { return true; }
      state = "inactive";
      mimeType = "audio/webm";
      ondataavailable: ((event: { data: Blob }) => void) | null = null;
      onerror = null;
      onstop: (() => void) | null = null;
      constructor(public stream: unknown) {}
      start() { this.state = "recording"; }
      pause() { this.state = "paused"; }
      resume() { this.state = "recording"; }
      stop() { this.state = "inactive"; this.onstop?.(); }
      requestData() { this.ondataavailable?.({ data: new Blob(["voice"], { type: this.mimeType }) }); }
    }
    class FakeAudioContext {
      state = "running";
      private readonly startedAt = performance.now();
      get currentTime() { return (performance.now() - this.startedAt) / 1000; }
      destination = {};
      createMediaStreamSource() { return { connect() {}, disconnect() {} }; }
      createScriptProcessor() {
        return { connect() {}, disconnect() {}, onaudioprocess: null };
      }
      createAnalyser() { return { fftSize: 1024, getFloatTimeDomainData(samples: Float32Array) { samples.fill((window as typeof window & { __micVolume?: number }).__micVolume ?? 0); } }; }
      createBuffer(_channels: number, length: number, sampleRate: number) {
        const minimumCaptionDuration = 2;
        return {
          duration: Math.max(length / sampleRate, minimumCaptionDuration),
          getChannelData: () => new Float32Array(length),
        };
      }
      createBufferSource() {
        const source: {
          buffer: ReturnType<FakeAudioContext["createBuffer"]> | null;
          onended: (() => void) | null;
          connect: () => void;
          start: () => void;
          stop: () => void;
        } = {
          buffer: null,
          onended: null,
          connect() {},
          start() {},
          stop() {},
        };
        return source;
      }
      async resume() {}
      async close() {}
      async decodeAudioData() { return { numberOfChannels: 1, length: 1, sampleRate: 16000, getChannelData: () => new Float32Array(1) }; }
    }
    class FakeWebSocket {
      static OPEN = 1;
      static instances: FakeWebSocket[] = [];
      readyState = 0;
      onopen: (() => void) | null = null;
      onmessage: ((event: MessageEvent<string>) => void) | null = null;
      onerror: (() => void) | null = null;
      onclose: (() => void) | null = null;
      constructor(public url: string) {
        FakeWebSocket.instances.push(this);
        setTimeout(() => {
          this.readyState = FakeWebSocket.OPEN;
          this.onopen?.();
          this.emit({ type: "session.created" });
        }, 0);
      }
      emit(event: Record<string, unknown>) {
        (window as typeof window & { __voiceLiveEmitted?: string[] }).__voiceLiveEmitted ??= [];
        (window as typeof window & { __voiceLiveEmitted?: string[] }).__voiceLiveEmitted?.push(String(event.type));
        this.onmessage?.({ data: JSON.stringify(event) } as MessageEvent<string>);
      }
      send() {}
      close() {
        this.readyState = 3;
        this.onclose?.();
      }
    }
    Object.defineProperty(window, "MediaRecorder", { configurable: true, value: FakeRecorder });
    Object.defineProperty(window, "AudioContext", { configurable: true, value: FakeAudioContext });
    Object.defineProperty(window, "WebSocket", { configurable: true, value: FakeWebSocket });
    Object.defineProperty(window, "__emitVoiceLiveScenario", {
      configurable: true,
      value: (events: Array<Record<string, unknown>>) => {
        const socket = FakeWebSocket.instances.at(-1);
        events.filter((event) => event.type !== "session.created").forEach((event, index) => {
          setTimeout(() => socket?.emit(event), index * 100);
        });
        return events.length;
      },
    });
    Object.defineProperty(window, "__voiceLiveSocketCount", {
      configurable: true,
      value: () => FakeWebSocket.instances.length,
    });
    Object.defineProperty(window, "speechSynthesis", { configurable: true, value: { speak(utterance: SpeechSynthesisUtterance) { setTimeout(() => utterance.onend?.(new Event("end") as SpeechSynthesisEvent), 0); }, cancel() {} } });
    Object.defineProperty(window, "SpeechSynthesisUtterance", { configurable: true, value: class { lang = ""; onend: ((event: Event) => void) | null = null; onerror: ((event: Event) => void) | null = null; constructor(public text: string) {} } });
  }, { selectedTheme: theme, voiceLiveEvents });
}

async function emitVoiceLiveScenario(
  page: import("@playwright/test").Page,
  events: Array<Record<string, unknown>>,
) {
  await page.evaluate(async (scenario) => {
    const testWindow = window as typeof window & {
      __emitVoiceLiveScenario?: (events: Array<Record<string, unknown>>) => number;
      __voiceLiveSocketCount?: () => number;
    };
    const startedAt = performance.now();
    while ((testWindow.__voiceLiveSocketCount?.() ?? 0) === 0
      || document.querySelector('[role="status"]')?.textContent?.includes("MIKROFON TAYYOR") !== true) {
      if (performance.now() - startedAt > 5_000) throw new Error("Voice Live WebSocket was not created");
      await new Promise((resolve) => setTimeout(resolve, 25));
    }
    testWindow.__emitVoiceLiveScenario?.(scenario);
    const expected = scenario.filter((event) => event.type !== "session.created").length;
    while (((window as typeof window & { __voiceLiveEmitted?: string[] }).__voiceLiveEmitted?.length ?? 0) < expected + 1) {
      if (performance.now() - startedAt > 5_000) throw new Error("Voice Live scenario was not fully emitted");
      await new Promise((resolve) => setTimeout(resolve, 25));
    }
  }, events);
}

test("learner opens American live tutor from Speaking and sees the active Voice Live room", async ({ page }) => {
  await prepareLearner(page, "light");
  await page.goto("/app/speaking");
  await expect(page.getByRole("heading", { name: "Accent tutor bilan jonli suhbat" })).toBeVisible();
  await page.getByRole("button", { name: /The Casual Slang & Idiom Tutor/ }).click();
  await expect(page).toHaveURL(/live-tutor\/american/);
  await expect(page.getByText("SUHBAT MATNI")).toBeVisible();
  await expect(page.getByText("LIVE", { exact: true })).toBeVisible();
  await expect(page.getByText("MIKROFON TAYYOR — GAPIRING", { exact: true })).toBeVisible();
  await expect(page.locator(".voice-core__state-icon--ear svg")).toBeVisible();
});

test("live tutor uses the animated ear while listening", async ({ page }) => {
  await prepareLearner(page, "light");
  await page.goto("/app/speaking/live-tutor/american");
  await expect(page.locator(".voice-core__state-icon--ear svg")).toBeVisible();
  const listeningAnimation = await page.locator(".voice-core__state-icon--ear i").first().evaluate((element) =>
    getComputedStyle(element).animationName,
  );
  expect(listeningAnimation).not.toBe("none");
});

for (const tutorId of ["british", "american", "australian", "irish"]) {
  test(`${tutorId} tutor shows preparation until startup and microphone are ready`, async ({ page }) => {
    await prepareLearner(page, "light");
    let releaseStart!: () => void;
    const startReleased = new Promise<void>((resolve) => { releaseStart = resolve; });
    await page.route(new RegExp(`/api/speaking/accent-tutors/${tutorId}/start$`), async (route) => {
      await startReleased;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ text: "Ready to talk?", tutorAudioBase64: "", isNaturalVoice: false, wordTimings: [] }),
      });
    });

    await page.goto(`/app/speaking/live-tutor/${tutorId}`);
    await expect(page.getByRole("status", { name: "Tayyorlanmoqda..." })).toBeVisible();
    releaseStart();
    await expect(page.getByRole("status", { name: "Tayyorlanmoqda..." })).toHaveCount(0);
    await expect(page.getByText("MIKROFON TAYYOR — GAPIRING", { exact: true })).toBeVisible();
  });
}

test("live tutor renders its dark theme without horizontal overflow", async ({ page }) => {
  await prepareLearner(page, "dark");
  await page.goto("/app/speaking/live-tutor/british");
  await expect(page.getByText("The Strict IELTS Examiner")).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  const badgeColors = await page.getByTestId("tutor-identity-badge").evaluate((element) => {
    const style = getComputedStyle(element);
    return { color: style.color, background: style.backgroundColor };
  });
  expect(badgeColors.color).not.toBe(badgeColors.background);
  const profileColors = await page.locator(".live-room__identity strong, .live-room__identity small").evaluateAll((elements) =>
    elements.map((element) => getComputedStyle(element).color),
  );
  expect(profileColors).toEqual(["rgb(255, 255, 255)", "rgb(255, 255, 255)"]);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
  expect(overflow).toBe(false);
});

for (const viewport of [
  { name: "tablet", width: 768, height: 1024 },
  { name: "mobile", width: 390, height: 844 },
]) {
  test(`live tutor fits the ${viewport.name} viewport`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await prepareLearner(page, "light");
    await page.goto("/app/speaking/live-tutor/australian");
    await expect(page.getByText("The Active Fluency Builder")).toBeVisible();
    await expect(page.getByText("SUHBAT MATNI")).toBeVisible();
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth > document.documentElement.clientWidth,
    );
    expect(overflow).toBe(false);
  });
}

for (const viewport of [
  { name: "desktop", width: 1440, height: 900 },
  { name: "tablet", width: 768, height: 1024 },
  { name: "mobile", width: 390, height: 844 },
]) {
  test(`processing animation stays centered on ${viewport.name}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await prepareLearner(page, "light");
    let releaseToken!: () => void;
    const delayedToken = new Promise<void>((resolve) => { releaseToken = resolve; });
    await page.route(/\/api\/speaking\/accent-tutors\/american\/voice-live\/token$/, async (route) => {
      await delayedToken;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          webSocketUrl: "ws://voice-live.test/realtime?api-version=2025-10-01",
          authorizationQueryParameter: "authorization",
          authorizationValue: "mock-token",
          expiresAt: "2027-08-15T00:00:00Z",
          session: {
            voiceName: "en-US-AvaMultilingualNeural",
            inputAudioFormat: "pcm16",
            outputAudioFormat: "pcm16",
            inputSamplingRate: 24000,
            silenceDurationMs: 700,
            turnDetectionType: "server_vad",
          },
        }),
      });
    });
    await page.goto("/app/speaking/live-tutor/american");
    await expect(page.locator(".voice-core__processing")).toBeAttached();
    const offset = await page.locator(".voice-core__surface").evaluate((surface) => {
      const processing = surface.querySelector<HTMLElement>(".voice-core__processing")!;
      const surfaceBox = surface.getBoundingClientRect();
      const processingBox = processing.getBoundingClientRect();
      return {
        x: Math.abs((processingBox.left + processingBox.width / 2) - (surfaceBox.left + surfaceBox.width / 2)),
        y: Math.abs((processingBox.top + processingBox.height / 2) - (surfaceBox.top + surfaceBox.height / 2)),
        alignItems: getComputedStyle(processing).alignItems,
        justifyContent: getComputedStyle(processing).justifyContent,
      };
    });
    releaseToken();
    expect(offset.x).toBeLessThanOrEqual(1);
    expect(offset.y).toBeLessThanOrEqual(1);
    expect(offset.alignItems).toBe("center");
    expect(offset.justifyContent).toBe("center");
  });
}

test("live tutor retries startup without exposing technical errors", async ({ page }) => {
  await prepareLearner(page, "light");
  await page.route(/\/api\/speaking\/accent-tutors\/american\/start$/, async (route) => route.fulfill({
    status: 503,
    contentType: "application/json",
    body: JSON.stringify({ message: "Azure tutor is unavailable" }),
  }));
  await page.goto("/app/speaking/live-tutor/american");
  await expect(page.getByText("Suhbat davom etmadi")).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Qayta urinish" })).toHaveCount(0);
});

test("silent learner session pauses after the Voice Live idle timeout", async ({ page }) => {
  await page.clock.install();
  await prepareLearner(page, "light");
  await page.goto("/app/speaking/live-tutor/american");
  await expect(page.getByText("MIKROFON TAYYOR — GAPIRING", { exact: true })).toBeVisible();
  await page.clock.fastForward(45_500);
  await expect(page.getByText("Suhbat pauzada")).toBeVisible();
  await page.getByRole("button", { name: "Davom etish" }).click();
  await expect(page.getByText("Suhbat pauzada")).toHaveCount(0);
});

test("closing an accent tutor returns to the Speaking page", async ({ page }) => {
  await prepareLearner(page, "light");
  await page.goto("/app/speaking/live-tutor/american");
  await expect(page.getByText("MIKROFON TAYYOR — GAPIRING", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Suhbatdan chiqish" }).click();
  await expect(page).toHaveURL(/\/app\/speaking$/);
  await expect(page.getByRole("heading", { name: "Accent tutor bilan jonli suhbat" })).toBeVisible();
});

test("live tutor streams transcript, reply, and pronunciation feedback", async ({ page }) => {
  await prepareLearner(page, "light");
  await page.route(/\/api\/speaking\/accent-tutors\/american\/turn\/stream$/, async (route) => route.fulfill({
    status: 200,
    contentType: "text/event-stream",
    body: [
      'event: recognized\ndata: {"transcript":"I visited the library yesterday."}\n\n',
      'event: tutor\ndata: {"tutorText":"Nice! What did you read?"}\n\n',
      'event: pronunciation\ndata: {"pronunciation":{"overallScore":78,"accuracyScore":74,"fluencyScore":82,"completenessScore":100,"band":1,"isAuthentic":true,"words":[{"word":"library","accuracyScore":61,"errorType":1,"needsPractice":true,"phonemes":[],"spokenForm":null}]}}\n\n',
      'event: audio\ndata: {"tutorAudioBase64":"","isNaturalVoice":false,"wordTimings":[]}\n\n',
      "event: done\ndata: {}\n\n",
    ].join(""),
  }));
  await page.goto("/app/speaking/live-tutor/american");

  const result = await page.evaluate(async () => {
    const response = await fetch("/api/speaking/accent-tutors/american/turn/stream", {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "text/event-stream" },
      body: JSON.stringify({ audioContent: "AQID", history: [] }),
    });
    return { status: response.status, body: await response.text() };
  });

  expect(result.status).toBe(200);
  expect(result.body).toContain("event: recognized");
  expect(result.body).toContain("event: pronunciation");
});

test("AI caption keeps its position, reveals progressively, and stays after playback", async ({ page }) => {
  const events = [
    { type: "session.created" },
    { type: "response.audio_transcript.delta", delta: "One two three four" },
    { type: "response.audio.delta", delta: "AQIDBAUGBwg=" },
    { type: "response.audio_transcript.done", transcript: "One two three four" },
  ];
  await prepareLearner(page, "light");
  await page.goto("/app/speaking/live-tutor/american");
  await emitVoiceLiveScenario(page, events);
  const caption = page.getByTestId("live-caption");
  const initialBox = await caption.boundingBox();
  await expect(caption.locator("span.is-upcoming, span.is-active, span.is-spoken")).toHaveCount(4);
  const upcomingOpacity = await caption.locator("span.is-upcoming").first().evaluate((element) => getComputedStyle(element).opacity);
  expect(Number(upcomingOpacity)).toBeGreaterThan(0);
  expect(await caption.boundingBox()).toEqual(initialBox);
  await expect(caption).toHaveAttribute("aria-hidden", "false");
});

test("long AI captions remain complete and readable", async ({ page }) => {
  const words = "one two three four five six seven eight nine ten eleven twelve thirteen fourteen";
  const events = [
    { type: "session.created" },
    { type: "response.audio.delta", delta: "AQIDBAUGBwg=" },
    { type: "response.audio_transcript.done", transcript: words },
  ];
  await prepareLearner(page, "light");
  await page.goto("/app/speaking/live-tutor/american");
  await emitVoiceLiveScenario(page, events);
  const caption = page.getByTestId("live-caption");
  const box = await caption.boundingBox();
  await expect(caption).toContainText("one two three");
  await expect(caption).toContainText("thirteen");
  expect(await caption.boundingBox()).toEqual(box);
});

for (const viewport of [
  { name: "caption desktop", width: 1440, height: 900 },
  { name: "caption tablet", width: 768, height: 1024 },
  { name: "caption mobile", width: 390, height: 844 },
]) {
  test(`${viewport.name} keeps every revealed word inside its caption box`, async ({ page }) => {
    await page.setViewportSize(viewport);
    const sentence = "Extraordinary pronunciation practice feels clearer today";
    const events = [
      { type: "session.created" },
      { type: "response.audio.delta", delta: "AQIDBAUGBwg=" },
      { type: "response.audio_transcript.done", transcript: sentence },
    ];
    await prepareLearner(page, "light");
    await page.goto("/app/speaking/live-tutor/american");
    await emitVoiceLiveScenario(page, events);
    await expect(page.getByTestId("live-caption").locator("span.is-upcoming, span.is-active, span.is-spoken").first()).toBeVisible();
    const result = await page.getByTestId("live-caption").evaluate((caption) => {
      const box = caption.getBoundingClientRect();
      const words = [...caption.querySelectorAll<HTMLElement>("span.is-upcoming, span.is-active, span.is-spoken")];
      return {
        caption: { left: box.left, right: box.right, top: box.top, bottom: box.bottom },
        rows: [...caption.querySelectorAll<HTMLElement>(".live-room__caption-row")].map((row) => {
          const rect = row.getBoundingClientRect();
          return { left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom };
        }),
        words: words.map((word) => {
          const rect = word.getBoundingClientRect();
          return { left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom };
        }),
      };
    });
    expect(result.rows).toHaveLength(1);
    expect(result.words.length).toBeGreaterThan(0);
    for (const row of result.rows) {
      expect(row.left).toBeGreaterThanOrEqual(result.caption.left - 0.5);
      expect(row.right).toBeLessThanOrEqual(result.caption.right + 0.5);
      expect(row.top).toBeGreaterThanOrEqual(result.caption.top - 0.5);
      expect(row.bottom).toBeLessThanOrEqual(result.caption.bottom + 0.5);
    }
    for (const word of result.words) {
      expect(word.left).toBeGreaterThanOrEqual(result.caption.left - 0.5);
      expect(word.right).toBeLessThanOrEqual(result.caption.right + 0.5);
      expect(word.top).toBeGreaterThanOrEqual(result.caption.top - 0.5);
      expect(word.bottom).toBeLessThanOrEqual(result.caption.bottom + 0.5);
    }
  });
}

for (const viewport of [
  { name: "desktop", width: 1440, height: 900 },
  { name: "tablet", width: 768, height: 1024 },
  { name: "mobile", width: 390, height: 844 },
]) {
  test(`animated state icon stays centered on ${viewport.name}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await prepareLearner(page, "light");
    await page.goto("/app/speaking/live-tutor/american");
    await expect(page.getByRole("status", { name: "Tayyorlanmoqda..." })).toHaveCount(0);
    await expect(page.locator(".voice-core__state-icon")).toBeVisible();
    const offset = await page.locator(".voice-core__surface").evaluate((surface) => {
      const icon = surface.querySelector<HTMLElement>(".voice-core__state-icon");
      const surfaceBox = surface.getBoundingClientRect();
      const iconBox = icon!.getBoundingClientRect();
      return {
        x: Math.abs((iconBox.left + iconBox.width / 2) - (surfaceBox.left + surfaceBox.width / 2)),
        y: Math.abs((iconBox.top + iconBox.height / 2) - (surfaceBox.top + surfaceBox.height / 2)),
      };
    });
    expect(offset.x).toBeLessThanOrEqual(1);
    expect(offset.y).toBeLessThanOrEqual(1);
  });
}
