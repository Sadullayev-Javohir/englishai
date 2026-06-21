import { beforeEach, describe, expect, it, vi } from "vitest";

const connection = {
  state: "Disconnected",
  on: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
  onclose: vi.fn(),
  start: vi.fn(async () => {
    connection.state = "Connected";
  }),
  stop: vi.fn(async () => {
    connection.state = "Disconnected";
  }),
  invoke: vi.fn(),
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

vi.mock("./nativeAuth", () => ({
  getAuthToken: () => null,
  isNativePlatform: () => false,
}));

import { SpeakingLiveHubClient } from "./speakingLiveHub";

describe("SpeakingLiveHubClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    connection.state = "Disconnected";
    connection.invoke.mockImplementation(async (method: string, payload?: unknown) => {
      if (method === "JoinSession") {
        return { sessionId: payload, enabled: true, serverTimestamp: "2026-07-30T00:00:00Z" };
      }
      return undefined;
    });
  });

  it("joins a session and submits uniquely identified turns", async () => {
    const client = new SpeakingLiveHubClient({
      onFinalTranscript: vi.fn(),
      onTutorText: vi.fn(),
      onPronunciation: vi.fn(),
      onTutorAudio: vi.fn(),
      onUnrecognized: vi.fn(),
      onRecoverableError: vi.fn(),
    });

    await client.joinSession("session-1");
    await client.submitTurn("audio-base64");

    expect(connection.start).toHaveBeenCalledOnce();
    expect(connection.invoke).toHaveBeenNthCalledWith(1, "JoinSession", "session-1");
    expect(connection.invoke).toHaveBeenNthCalledWith(
      2,
      "SubmitTurn",
      expect.objectContaining({
        sessionId: "session-1",
        sequence: 1,
        audioBase64: "audio-base64",
        turnId: expect.any(String),
      }),
    );
  });

  it("resubmits a confirmed transcript with the original turn id", async () => {
    const client = new SpeakingLiveHubClient({
      onFinalTranscript: vi.fn(),
      onTutorText: vi.fn(),
      onPronunciation: vi.fn(),
      onTutorAudio: vi.fn(),
      onUnrecognized: vi.fn(),
      onTranscriptConfirmationRequired: vi.fn(),
      onRecoverableError: vi.fn(),
    });

    await client.joinSession("session-1");
    await client.submitTurn("audio-base64", {
      transcript: "I prefer tea in the morning.",
      outcome: "candidate_selected",
      turnId: "turn-1",
    });

    expect(connection.invoke).toHaveBeenNthCalledWith(
      2,
      "SubmitTurn",
      expect.objectContaining({
        turnId: "turn-1",
        confirmedTranscript: "I prefer tea in the morning.",
        confirmationOutcome: "candidate_selected",
      }),
    );
  });
});
