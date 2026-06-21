import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api, ApiError } from "./client";
import { ProductEventType } from "./types";

function pendingFetch() {
  return vi.fn((_input: RequestInfo | URL, init?: RequestInit) =>
    new Promise<Response>((_resolve, reject) => {
      init?.signal?.addEventListener(
        "abort",
        () => reject(init.signal?.reason ?? new DOMException("Aborted", "AbortError")),
        { once: true },
      );
    }),
  );
}

describe("API request timeouts", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.stubGlobal("fetch", pendingFetch());
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("times out regular requests after 15 seconds", async () => {
    const request = api.health();
    const result = request.catch((error: unknown) => error);

    await vi.advanceTimersByTimeAsync(15_000);

    await expect(result).resolves.toMatchObject({
      status: 408,
      body: { code: "request_timeout" },
    });
  });

  it("allows long speech requests up to 90 seconds", async () => {
    const request = api.speaking.assessSegment("Hello", "audio");
    const result = request.catch((error: unknown) => error);

    await vi.advanceTimersByTimeAsync(15_000);
    expect(fetch).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(75_000);
    await expect(result).resolves.toMatchObject({ status: 408 });
  });

  it("allows conversation startup up to 90 seconds", async () => {
    const request = api.speaking.start("learner-1", 2, undefined, "topic-1");
    const result = request.catch((error: unknown) => error);

    await vi.advanceTimersByTimeAsync(15_000);
    expect(fetch).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(75_000);
    await expect(result).resolves.toMatchObject({ status: 408 });
  });

  it("allows roleplay startup up to 90 seconds", async () => {
    const request = api.speaking.roleplayStart("learner-1", 2, "airport");
    const result = request.catch((error: unknown) => error);

    await vi.advanceTimersByTimeAsync(15_000);
    expect(fetch).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(75_000);
    await expect(result).resolves.toMatchObject({ status: 408 });
  });

  it("allows lazy book section generation up to 90 seconds", async () => {
    const request = api.books.section("book-1", "section-1", "learner-1");
    const result = request.catch((error: unknown) => error);

    await vi.advanceTimersByTimeAsync(15_000);
    expect(fetch).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(75_000);
    await expect(result).resolves.toMatchObject({ status: 408 });
  });

  it("allows the public project assistant up to 90 seconds", async () => {
    const request = api.assistant.askProject("EnglishAI nima?", []);
    const result = request.catch((error: unknown) => error);

    await vi.advanceTimersByTimeAsync(15_000);
    expect(fetch).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(75_000);
    await expect(result).resolves.toMatchObject({ status: 408 });
  });

  it("preserves caller cancellation instead of reporting a timeout", async () => {
    const controller = new AbortController();
    const request = api.speaking.assessSegment("Hello", "audio", controller.signal);
    const result = request.catch((error: unknown) => error);

    controller.abort(new DOMException("Cancelled", "AbortError"));

    await expect(result).resolves.toMatchObject({ name: "AbortError" });
    expect(await result).not.toBeInstanceOf(ApiError);
  });
});

describe("public project assistant locale", () => {
  afterEach(() => vi.unstubAllGlobals());

  it.each(["uz", "en"] as const)("sends the selected %s locale with questions and history", async (locale) => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ reply: "Assistant reply" }), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);
    const history = [{ role: "assistant" as const, text: "Previous answer" }];

    await api.assistant.askProject("How does EnglishAI work?", history, locale);
    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/api/assistant/project"), expect.objectContaining({
      body: JSON.stringify({ question: "How does EnglishAI work?", history, locale }),
    }));
  });

  it("defaults older callers to Uzbek", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ reply: "Javob" }), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);
    await api.assistant.askProject("EnglishAI nima?", []);
    expect(fetchMock).toHaveBeenCalledWith(expect.any(String), expect.objectContaining({
      body: JSON.stringify({ question: "EnglishAI nima?", history: [], locale: "uz" }),
    }));
  });
});

describe("API GET deduplication", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("shares duplicate GET requests inside the cache window", async () => {
    const fetchMock = vi.fn().mockImplementation(() =>
      Promise.resolve(
        new Response(JSON.stringify({ status: "ok" }), { status: 200 }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    const first = api.health();
    const second = api.health();

    await expect(Promise.all([first, second])).resolves.toEqual([
      { status: "ok" },
      { status: "ok" },
    ]);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    await api.analytics.track("cache-cleanup", ProductEventType.SignedIn);
  });

  it("invalidates cached GET requests before a mutation", async () => {
    const fetchMock = vi.fn().mockImplementation((_input, init?: RequestInit) =>
      Promise.resolve(
        init?.method === "POST"
          ? new Response(null, { status: 204 })
          : new Response(JSON.stringify({ status: "ok" }), { status: 200 }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await api.health();
    await api.analytics.track("learner-1", ProductEventType.SignedIn);
    await api.health();

    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});

describe("distributed rate limit errors", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("preserves the response contract without emitting a global UI event", async () => {
    const body = { code: "rate_limited", message: "Slow down", retryAfterSeconds: 42, correlationId: "corr-1" };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status: 429 })));
    const listener = vi.fn();
    window.addEventListener("api:rate-limit", listener);

    await expect(api.publicMetrics()).rejects.toMatchObject({ status: 429, body });
    expect(listener).not.toHaveBeenCalled();
    window.removeEventListener("api:rate-limit", listener);
  });
});

describe("Video AI stream fallback", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("falls back to the regular explain endpoint when streaming is unavailable", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(null, { status: 404 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ replyUz: "Fallback answer" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(api.video.explainStream("lesson", "line", "question", [], vi.fn()))
      .resolves.toEqual({ replyUz: "Fallback answer" });
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});

describe("Assistant session stream", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("reads the final done event without a trailing blank line", async () => {
    const message = {
      id: "answer-1",
      role: "assistant",
      text: "have/has + V3",
      status: "completed",
      source: "cache",
      clientRequestId: "request-1",
      latencyMs: 12,
      createdAt: new Date().toISOString(), sources: [],
    };
    const body = `event: done\ndata: ${JSON.stringify({ message })}`;
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(body, {
      status: 200,
      headers: { "Content-Type": "text/event-stream" },
    })));

    await expect(api.assistant.sessions.send("session-1", "Present Perfect", "Lesson context", "", "request-1"))
      .resolves.toEqual(message);
  });

  it("emits assistant chunks before the final persisted message", async () => {
    const message = {
      id: "answer-2",
      role: "assistant",
      text: "Use have or has plus V3.",
      status: "completed",
      source: "ai",
      clientRequestId: "request-2",
      latencyMs: 24,
      createdAt: new Date().toISOString(), sources: [],
    } as const;
    const body = [
      `event: chunk\ndata: ${JSON.stringify({ text: "Use have or has " })}`,
      `event: chunk\ndata: ${JSON.stringify({ text: "plus V3." })}`,
      `event: done\ndata: ${JSON.stringify({ message })}`,
      "",
    ].join("\n\n");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(body, {
      status: 200,
      headers: { "Content-Type": "text/event-stream" },
    })));
    const chunks: string[] = [];

    const result = await api.assistant.sessions.send(
      "session-1", "Present Perfect", "Lesson context", "", "request-2", undefined, undefined, (text) => chunks.push(text),
    );

    expect(chunks).toEqual(["Use have or has ", "plus V3."]);
    expect(result).toEqual(message);
  });
});

describe("Speaking utterance stream", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("surfaces a typed tutor unavailable event", async () => {
    const body = [
      `event: recognized\ndata: ${JSON.stringify({ text: "I like travel." })}`,
      `event: tutor-unavailable\ndata: ${JSON.stringify({ code: "timeout", message: "Speaking AI is temporarily unavailable.", retryable: true })}`,
      "event: done\ndata: {}",
      "",
    ].join("\n\n");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(body, {
      status: 200,
      headers: { "Content-Type": "text/event-stream" },
    })));
    const unavailable = vi.fn();

    await api.speaking.utteranceStream("session-1", "audio", {
      onRecognized: vi.fn(),
      onTutor: vi.fn(),
      onPronunciation: vi.fn(),
      onAudio: vi.fn(),
      onProgress: vi.fn(),
      onUnrecognized: vi.fn(),
      onTutorUnavailable: unavailable,
    });

    expect(unavailable).toHaveBeenCalledWith({
      code: "timeout",
      message: "Speaking AI is temporarily unavailable.",
      retryable: true,
    });
  });

  it("surfaces transcript confirmation candidates and sends the confirmed transcript", async () => {
    const body = [
      `event: transcript-confirmation-required\ndata: ${JSON.stringify({
        suggestedText: "I prefer tea in the morning.",
        alternatives: [
          { text: "I prefer tea in the morning.", confidence: 0.71 },
          { text: "I prefer tea in Jammu.", confidence: 0.70 },
        ],
      })}`,
      "event: done\ndata: {}",
      "",
    ].join("\n\n");
    const fetchMock = vi.fn().mockResolvedValue(new Response(body, {
      status: 200,
      headers: { "Content-Type": "text/event-stream" },
    }));
    vi.stubGlobal("fetch", fetchMock);
    const confirmation = vi.fn();

    await api.speaking.utteranceStream("session-1", "audio", {
      onRecognized: vi.fn(),
      onTutor: vi.fn(),
      onPronunciation: vi.fn(),
      onAudio: vi.fn(),
      onProgress: vi.fn(),
      onUnrecognized: vi.fn(),
      onTranscriptConfirmationRequired: confirmation,
      onTutorUnavailable: vi.fn(),
    }, {
      transcript: "I prefer tea in the morning.",
      outcome: "edited",
    });

    expect(confirmation).toHaveBeenCalledWith(expect.objectContaining({
      suggestedText: "I prefer tea in the morning.",
    }));
    expect(JSON.parse(fetchMock.mock.calls[0][1].body as string)).toMatchObject({
      confirmedTranscript: "I prefer tea in the morning.",
      confirmationOutcome: "edited",
    });
  });
});
