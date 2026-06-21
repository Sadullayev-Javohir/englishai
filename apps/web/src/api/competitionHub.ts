// SignalR client for the real-time competition hub ("Musobaqa"). Caller identity always comes
// from the authenticated session; reconnects restore the competition group from persisted state.
import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import type {
  CompetitionDto,
  CompetitionResultDto,
  ParticipantDto,
} from "./types";
import { apiUrl } from "./config";
import { getAuthToken, isNativePlatform } from "./nativeAuth";

export const ParticipantJoinedEvent = "ParticipantJoined";
export const CompetitionStartedEvent = "CompetitionStarted";
export const SlideAdvancedEvent = "SlideAdvanced";
export const TickEvent = "Tick";
export const ScoresUpdatedEvent = "ScoresUpdated";
export const CompetitionFinishedEvent = "CompetitionFinished";

const reconnectDelays = [0, 2_000, 5_000, 10_000, 30_000];

export interface CompetitionHubHandlers {
  onParticipantJoined: (participant: ParticipantDto) => void;
  onCompetitionStarted: (competition: CompetitionDto) => void;
  onSlideAdvanced: (competition: CompetitionDto) => void;
  onTick: (remainingSeconds: number) => void;
  onScoresUpdated: (participants: ParticipantDto[]) => void;
  onCompetitionFinished: (result: CompetitionResultDto) => void;
  onSnapshot?: (competition: CompetitionDto) => void;
  onReconnecting?: (error?: Error) => void;
  onReconnected?: (competition: CompetitionDto) => void;
  onClosed?: (error?: Error) => void;
}

export class CompetitionHubClient {
  private connection: HubConnection;
  private activeCompetitionId: string | null = null;

  constructor(handlers: CompetitionHubHandlers) {
    this.connection = new HubConnectionBuilder()
      .withUrl(apiUrl("/hubs/competition"), {
        accessTokenFactory: isNativePlatform()
          ? () => getAuthToken() ?? ""
          : undefined,
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect(reconnectDelays)
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on(ParticipantJoinedEvent, handlers.onParticipantJoined);
    this.connection.on(CompetitionStartedEvent, handlers.onCompetitionStarted);
    this.connection.on(SlideAdvancedEvent, handlers.onSlideAdvanced);
    this.connection.on(TickEvent, handlers.onTick);
    this.connection.on(ScoresUpdatedEvent, handlers.onScoresUpdated);
    this.connection.on(CompetitionFinishedEvent, handlers.onCompetitionFinished);

    this.connection.onreconnecting((error) => handlers.onReconnecting?.(error));
    this.connection.onreconnected(async () => {
      if (!this.activeCompetitionId) return;

      try {
        const snapshot = await this.resume(this.activeCompetitionId);
        handlers.onSnapshot?.(snapshot);
        handlers.onReconnected?.(snapshot);
      } catch (error) {
        console.error("[CompetitionHub] group resume failed:", error);
      }
    });
    this.connection.onclose((error) => handlers.onClosed?.(error));
  }

  connect(): Promise<void> {
    if (this.connection.state !== HubConnectionState.Disconnected) {
      return Promise.resolve();
    }
    return this.connection.start();
  }

  stop(): Promise<void> {
    this.activeCompetitionId = null;
    return this.connection.stop();
  }

  private invoke<T>(methodName: string, ...args: unknown[]): Promise<T> {
    return this.connection.invoke<T>(methodName, ...args).catch((error) => {
      console.error(`[CompetitionHub] ${methodName} failed:`, error);
      throw error;
    });
  }

  async join(
    competitionId: string,
    displayName: string,
    accessCode?: string
  ): Promise<void> {
    await this.invoke<void>(
      "JoinCompetition",
      competitionId,
      displayName,
      accessCode
    );
    this.activeCompetitionId = competitionId;
  }

  async resume(competitionId: string): Promise<CompetitionDto> {
    const snapshot = await this.invoke<CompetitionDto>(
      "ResumeCompetition",
      competitionId
    );
    this.activeCompetitionId = competitionId;
    return snapshot;
  }

  start(competitionId: string): Promise<void> {
    return this.invoke<void>("StartCompetition", competitionId);
  }

  submitAnswer(
    competitionId: string,
    selectedOptionIndex: number,
    timeRatioRemaining = 1.0
  ): Promise<void> {
    return this.invoke<void>(
      "SubmitAnswer",
      competitionId,
      selectedOptionIndex,
      timeRatioRemaining
    );
  }

  advance(competitionId: string): Promise<void> {
    return this.invoke<void>("AdvanceSlide", competitionId);
  }

  finish(competitionId: string): Promise<void> {
    return this.invoke<void>("FinishCompetition", competitionId);
  }

  async leave(competitionId: string): Promise<void> {
    await this.invoke<void>("LeaveCompetition", competitionId);
    if (this.activeCompetitionId === competitionId) {
      this.activeCompetitionId = null;
    }
  }
}
