import { beforeEach, describe, expect, it, vi } from "vitest";

const connection = {
  state: "Disconnected",
  start: vi.fn(() => Promise.resolve()),
  stop: vi.fn(() => Promise.resolve()),
  invoke: vi.fn(() => Promise.resolve()),
  on: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
  onclose: vi.fn(),
};

const builder = {
  withUrl: vi.fn(() => builder),
  withAutomaticReconnect: vi.fn(() => builder),
  configureLogging: vi.fn(() => builder),
  build: vi.fn(() => connection),
};

vi.mock("@microsoft/signalr", () => ({
  HttpTransportType: { WebSockets: 1 },
  HubConnectionState: { Disconnected: "Disconnected" },
  LogLevel: { Warning: 3 },
  HubConnectionBuilder: vi.fn(() => builder),
}));

vi.mock("./nativeAuth", () => ({
  getAuthToken: vi.fn(() => null),
  isNativePlatform: vi.fn(() => false),
}));

describe("realtime hub clients", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    connection.state = "Disconnected";
    connection.invoke.mockResolvedValue(undefined);
  });

  it("uses WebSockets without negotiation and restores competition membership", async () => {
    const { CompetitionHubClient } = await import("./competitionHub");
    const client = new CompetitionHubClient({
      onParticipantJoined: vi.fn(),
      onCompetitionStarted: vi.fn(),
      onSlideAdvanced: vi.fn(),
      onTick: vi.fn(),
      onScoresUpdated: vi.fn(),
      onCompetitionFinished: vi.fn(),
    });

    await client.join("competition-1", "Learner", "ABC123");

    expect(builder.withUrl).toHaveBeenCalledWith(
      "/hubs/competition",
      expect.objectContaining({ skipNegotiation: true, transport: 1 })
    );
    expect(connection.invoke).toHaveBeenCalledWith(
      "JoinCompetition",
      "competition-1",
      "Learner",
      "ABC123"
    );

    connection.invoke.mockImplementationOnce(() =>
      Promise.resolve({ id: "competition-1" }) as unknown as Promise<void>
    );
    const onReconnected = connection.onreconnected.mock.calls[0][0];
    await onReconnected();
    expect(connection.invoke).toHaveBeenCalledWith(
      "ResumeCompetition",
      "competition-1"
    );
  });

  it("refetches persisted notifications after reconnect", async () => {
    const refresh = vi.fn();
    const receive = vi.fn();
    const { NotificationsHubClient } = await import("./notificationsHub");
    new NotificationsHubClient({
      onBroadcast: receive,
      onReconnected: refresh,
    });

    expect(builder.withUrl).toHaveBeenCalledWith(
      "/hubs/notifications",
      expect.objectContaining({ skipNegotiation: true, transport: 1 })
    );
    expect(connection.on).toHaveBeenCalledWith("NotificationReceived", receive);

    const onReconnected = connection.onreconnected.mock.calls.at(-1)?.[0];
    await onReconnected();
    expect(refresh).toHaveBeenCalledOnce();
  });
});
