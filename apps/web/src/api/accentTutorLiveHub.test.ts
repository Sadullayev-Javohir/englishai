import { beforeEach, describe, expect, it, vi } from "vitest";

const handlers = new Map<string, (payload?: unknown) => void>();
let reconnectingHandler: (() => void) | undefined;
let reconnectedHandler: (() => Promise<void> | void) | undefined;
const connection = {
  state: "Disconnected",
  serverTimeoutInMilliseconds: 0,
  keepAliveIntervalInMilliseconds: 0,
  on: vi.fn((event: string, handler: (payload?: unknown) => void) => handlers.set(event, handler)),
  onclose: vi.fn(),
  onreconnecting: vi.fn((handler: () => void) => { reconnectingHandler = handler; }),
  onreconnected: vi.fn((handler: () => Promise<void> | void) => { reconnectedHandler = handler; }),
  start: vi.fn(async () => { connection.state = "Connected"; }),
  stop: vi.fn(async () => { connection.state = "Disconnected"; }),
  invoke: vi.fn().mockResolvedValue(undefined),
};
const builder = {
  withUrl: vi.fn(() => builder),
  withAutomaticReconnect: vi.fn(() => builder),
  configureLogging: vi.fn(() => builder),
  build: vi.fn(() => connection),
};

vi.mock("@microsoft/signalr", () => ({
  HttpTransportType: { WebSockets: 1 },
  HubConnectionState: { Disconnected: "Disconnected", Connected: "Connected" },
  LogLevel: { Warning: 3 },
  HubConnectionBuilder: vi.fn(() => builder),
}));
vi.mock("./nativeAuth", () => ({ getAuthToken: () => null, isNativePlatform: () => false }));

import { AccentTutorLiveHubClient } from "./accentTutorLiveHub";

describe("AccentTutorLiveHubClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    handlers.clear();
    reconnectingHandler = undefined;
    reconnectedHandler = undefined;
    connection.state = "Disconnected";
  });

  it("re-joins the session after an automatic reconnect so turns keep working", async () => {
    const client = new AccentTutorLiveHubClient({
      onRecognized: vi.fn(),
      onTutor: vi.fn(),
      onPronunciation: vi.fn(),
      onAudio: vi.fn(),
      onRecoverableError: vi.fn(),
    });
    await client.join("american");
    await client.startTurn([], false);

    // Reconnect: the socket drops (any in-flight turn is abandoned) and comes back on a fresh
    // server connection that has no session state.
    reconnectingHandler?.();
    expect(client.hasActiveTurn).toBe(false);

    connection.invoke.mockClear();
    await reconnectedHandler?.();

    // Without the re-join the server would reject the next StartTurn with
    // "Join the accent tutor session before starting a turn" and the learner would get no reply.
    expect(connection.invoke).toHaveBeenCalledWith("JoinSession", "american");
    connection.state = "Connected";
    await expect(client.startTurn([], false)).resolves.toEqual(expect.any(String));
  });

  it("clears the active turn and reports a discarded noise turn", async () => {
    const discarded = vi.fn();
    const client = new AccentTutorLiveHubClient({
      onRecognized: vi.fn(),
      onTutor: vi.fn(),
      onPronunciation: vi.fn(),
      onAudio: vi.fn(),
      onRecoverableError: vi.fn(),
      onTurnDiscarded: discarded,
    });
    await client.join("american");
    await client.startTurn([], false);
    expect(client.hasActiveTurn).toBe(true);

    handlers.get("TurnDiscarded")?.({
      tutorId: "american",
      turnId: "turn-1",
      sequence: 1,
      reason: "no_recognized_speech",
      serverTimestamp: "2026-08-13T00:00:00Z",
    });

    expect(client.hasActiveTurn).toBe(false);
    expect(discarded).toHaveBeenCalledWith(expect.objectContaining({ reason: "no_recognized_speech" }));
  });

  it("keeps the committed turn reserved until TurnCompleted and then allows the next turn", async () => {
    const completed = vi.fn();
    const client = new AccentTutorLiveHubClient({
      onRecognized: vi.fn(),
      onTutor: vi.fn(),
      onPronunciation: vi.fn(),
      onAudio: vi.fn(),
      onRecoverableError: vi.fn(),
      onTurnCompleted: completed,
    });
    await client.join("american");
    await client.startTurn([], false);

    handlers.get("TurnCommitted")?.({
      tutorId: "american",
      turnId: "turn-1",
      sequence: 1,
      serverTimestamp: "2026-08-13T00:00:00Z",
    });
    handlers.get("FinalTranscript")?.({
      tutorId: "american",
      turnId: "turn-1",
      sequence: 1,
      transcript: "Hello",
      serverTimestamp: "2026-08-13T00:00:01Z",
    });

    expect(client.hasActiveTurn).toBe(true);
    await expect(client.startTurn([], false)).rejects.toThrow("still committing");

    handlers.get("TurnCompleted")?.({
      tutorId: "american",
      turnId: "turn-1",
      sequence: 1,
      serverTimestamp: "2026-08-13T00:00:02Z",
    });
    expect(client.hasActiveTurn).toBe(false);
    expect(completed).toHaveBeenCalledOnce();
    await expect(client.startTurn([], false)).resolves.toEqual(expect.any(String));
  });

  it("treats recoverable errors as a terminal turn event", async () => {
    const client = new AccentTutorLiveHubClient({
      onRecognized: vi.fn(),
      onTutor: vi.fn(),
      onPronunciation: vi.fn(),
      onAudio: vi.fn(),
      onRecoverableError: vi.fn(),
    });
    await client.join("american");
    await client.startTurn([], false);

    handlers.get("TurnCommitted")?.({
      tutorId: "american",
      turnId: "turn-1",
      sequence: 1,
      serverTimestamp: "2026-08-13T00:00:00Z",
    });
    handlers.get("RecoverableError")?.({
      tutorId: "american",
      turnId: "turn-1",
      sequence: 1,
      code: "accent_tutor_failed",
      message: "failed",
      recoverable: true,
      serverTimestamp: "2026-08-13T00:00:02Z",
    });

    expect(client.hasActiveTurn).toBe(false);
  });
});
