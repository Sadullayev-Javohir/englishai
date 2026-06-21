import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { VoiceLiveSession, VoiceLiveTokenError } from "./voiceLiveSession";

// Every exit path has to tell the server the session ended, otherwise the up-front reservation
// stands and the learner is billed for time they never used. These tests pin that contract and
// the "exactly once" property that stops a double refund.

const TOKEN_RESPONSE = {
  webSocketUrl: "wss://example.test/voice-live/realtime?api-version=x",
  authorizationQueryParameter: "authorization",
  authorizationValue: "Bearer token",
  expiresAt: new Date(Date.now() + 60_000).toISOString(),
  sessionId: "0123456789abcdef0123456789abcdef",
  maxSessionSeconds: 600,
  session: {
    voiceName: "en-GB-Voice",
    inputAudioFormat: "pcm16",
    outputAudioFormat: "pcm16",
    inputSamplingRate: 24_000,
    silenceDurationMs: 500,
    turnDetectionType: "server_vad",
  },
};

let fetchMock: ReturnType<typeof vi.fn>;

function completionCalls() {
  return fetchMock.mock.calls.filter(([url]) => String(url).endsWith("/voice-live/complete"));
}

/** Boots a session far enough that a reservation exists on the server. */
async function startSession(): Promise<VoiceLiveSession> {
  const session = new VoiceLiveSession("british", {});
  await session.start();
  return session;
}

beforeEach(() => {
  fetchMock = vi.fn(async (url: string | URL) =>
    String(url).endsWith("/voice-live/complete")
      ? new Response(JSON.stringify({ billedSeconds: 12, alreadySettled: false }), { status: 200 })
      : new Response(JSON.stringify(TOKEN_RESPONSE), { status: 200 }),
  );
  vi.stubGlobal("fetch", fetchMock);

  // Minimal Web Audio + mic + socket stubs: this suite is about the completion report, not audio.
  vi.stubGlobal("AudioContext", class {
    currentTime = 0;
    destination = {};
    resume = vi.fn(async () => undefined);
    close = vi.fn(async () => undefined);
    createMediaStreamSource = vi.fn(() => ({ connect: vi.fn(), disconnect: vi.fn() }));
    createScriptProcessor = vi.fn(() => ({ connect: vi.fn(), disconnect: vi.fn(), onaudioprocess: null }));
    createBuffer = vi.fn();
    createBufferSource = vi.fn();
  });
  vi.stubGlobal("navigator", {
    mediaDevices: { getUserMedia: vi.fn(async () => ({ getTracks: () => [], getAudioTracks: () => [] })) },
  });
  vi.stubGlobal("WebSocket", class {
    static OPEN = 1;
    readyState = 0;
    onopen: (() => void) | null = null;
    onmessage: ((event: MessageEvent) => void) | null = null;
    onerror: (() => void) | null = null;
    onclose: (() => void) | null = null;
    send = vi.fn();
    close = vi.fn();
  });
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("VoiceLiveSession completion reporting", () => {
  it("reports completion when the session stops", async () => {
    const session = await startSession();

    session.stop();

    expect(completionCalls()).toHaveLength(1);
    const [, init] = completionCalls()[0] as [string, RequestInit];
    expect(init.method).toBe("POST");
    // keepalive (not sendBeacon): the native build is cross-origin and needs credentials.
    expect(init.keepalive).toBe(true);
    expect(init.credentials).toBe("include");
    const body = JSON.parse(String(init.body)) as { sessionId: string; durationSeconds: number };
    expect(body.sessionId).toBe(TOKEN_RESPONSE.sessionId);
    expect(body.durationSeconds).toBeGreaterThanOrEqual(0);
  });

  it("reports exactly once even when stopped repeatedly", async () => {
    const session = await startSession();

    session.stop();
    session.stop();

    expect(completionCalls()).toHaveLength(1);
  });

  it("reports when the tab goes away without the app unmounting", async () => {
    await startSession();

    window.dispatchEvent(new Event("pagehide"));

    expect(completionCalls()).toHaveLength(1);
  });

  it("does not report twice when pagehide is followed by stop", async () => {
    const session = await startSession();

    window.dispatchEvent(new Event("pagehide"));
    session.stop();

    expect(completionCalls()).toHaveLength(1);
  });

  it("never reports a session that was refused, because nothing was reserved", async () => {
    fetchMock.mockImplementation(async () =>
      new Response(JSON.stringify({ code: "voice_live_daily_limit" }), { status: 429 }),
    );
    const session = new VoiceLiveSession("british", {});

    await expect(session.start()).rejects.toBeInstanceOf(VoiceLiveTokenError);
    session.stop();

    expect(completionCalls()).toHaveLength(0);
  });

  it("surfaces the server's refusal code so the page can explain why", async () => {
    fetchMock.mockImplementation(async () =>
      new Response(JSON.stringify({ code: "budget_exhausted" }), { status: 429 }),
    );

    await expect(new VoiceLiveSession("british", {}).start()).rejects.toMatchObject({
      code: "budget_exhausted",
      status: 429,
    });
  });

  it("falls back to a generic code when the error body is not JSON", async () => {
    fetchMock.mockImplementation(async () => new Response("upstream exploded", { status: 503 }));

    await expect(new VoiceLiveSession("british", {}).start()).rejects.toMatchObject({
      code: "voice_live_unavailable",
      status: 503,
    });
  });
});
