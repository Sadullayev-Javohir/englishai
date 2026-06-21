import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import type {
  PronunciationResultDto,
  SpeakingRejectionCode,
  SpeakingTranscriptAlternative,
  SpeakingTranscriptConfirmationOutcome,
  SpeechWordTimingDto,
  TopicCompletionDto,
  TopicSpeakingProgressDto,
  VisemeFrameDto,
} from "./types";
import { apiUrl } from "./config";
import { getAuthToken, isNativePlatform } from "./nativeAuth";

export interface SpeakingLiveEventBase {
  sessionId: string;
  turnId: string;
  sequence: number;
  serverTimestamp: string;
}

export interface SpeakingLiveSessionReady {
  sessionId: string;
  enabled: boolean;
  serverTimestamp: string;
}

export interface SpeakingLiveTranscriptEvent extends SpeakingLiveEventBase {
  text: string;
}

export interface SpeakingLiveTutorEvent extends SpeakingLiveTranscriptEvent {
  sessionLimitReached: boolean;
  namePrompt: boolean;
  topicProgress: TopicSpeakingProgressDto | null;
  completion: TopicCompletionDto | null;
  /** What is left of today's speaking budget, so the learner can watch it count down. */
  quota: SpeakingQuotaDto | null;
}

export interface SpeakingQuotaDto {
  limitMinutes: number;
  usedMinutes: number;
  remainingMinutes: number;
}

export interface SpeakingLivePronunciationEvent extends SpeakingLiveEventBase {
  pronunciation: PronunciationResultDto;
  feedbackUz: string | null;
  focusWord: string | null;
}

export interface SpeakingLiveAudioEvent extends SpeakingLiveEventBase {
  audioBase64: string;
  visemes: VisemeFrameDto[];
  visemeAnimation: string | null;
  isNaturalVoice: boolean;
  wordTimings: SpeechWordTimingDto[];
}

export interface SpeakingLiveUnrecognizedEvent extends SpeakingLiveEventBase {
  feedbackUz: string | null;
  rejectionCode: SpeakingRejectionCode;
}

export interface SpeakingLiveTranscriptConfirmationEvent extends SpeakingLiveEventBase {
  suggestedText: string;
  alternatives: SpeakingTranscriptAlternative[];
}

export interface SpeakingLiveErrorEvent extends SpeakingLiveEventBase {
  code: string;
  message: string;
  recoverable: boolean;
}

export interface SpeakingLiveHubHandlers {
  onPartialTranscript?: (event: SpeakingLiveTranscriptEvent) => void;
  onFinalTranscript: (event: SpeakingLiveTranscriptEvent) => void;
  onTutorText: (event: SpeakingLiveTutorEvent) => void;
  onPronunciation: (event: SpeakingLivePronunciationEvent) => void;
  onTutorAudio: (event: SpeakingLiveAudioEvent) => void;
  onUnrecognized: (event: SpeakingLiveUnrecognizedEvent) => void;
  onTranscriptConfirmationRequired?: (event: SpeakingLiveTranscriptConfirmationEvent) => void;
  onTurnCompleted?: (event: SpeakingLiveEventBase) => void;
  onRecoverableError: (event: SpeakingLiveErrorEvent) => void;
  onReconnecting?: () => void;
  onReconnected?: () => void;
  onClosed?: () => void;
}

const reconnectDelays = [0, 1_000, 3_000, 8_000, 15_000];

export class SpeakingLiveHubClient {
  private readonly connection: HubConnection;
  private sessionId: string | null = null;
  private sequence = 0;

  constructor(handlers: SpeakingLiveHubHandlers) {
    this.connection = new HubConnectionBuilder()
      .withUrl(apiUrl("/hubs/speaking-live"), {
        accessTokenFactory: isNativePlatform()
          ? () => getAuthToken() ?? ""
          : undefined,
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect(reconnectDelays)
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on("PartialTranscript", handlers.onPartialTranscript ?? (() => undefined));
    this.connection.on("FinalTranscript", handlers.onFinalTranscript);
    this.connection.on("TutorText", handlers.onTutorText);
    this.connection.on("PronunciationReady", handlers.onPronunciation);
    this.connection.on("TutorAudio", handlers.onTutorAudio);
    this.connection.on("Unrecognized", handlers.onUnrecognized);
    this.connection.on(
      "TranscriptConfirmationRequired",
      handlers.onTranscriptConfirmationRequired ?? (() => undefined),
    );
    this.connection.on("TurnCompleted", handlers.onTurnCompleted ?? (() => undefined));
    this.connection.on("RecoverableError", handlers.onRecoverableError);
    this.connection.onreconnecting(() => handlers.onReconnecting?.());
    this.connection.onreconnected(async () => {
      if (this.sessionId) await this.joinSession(this.sessionId);
      handlers.onReconnected?.();
    });
    this.connection.onclose(() => handlers.onClosed?.());
  }

  get connected(): boolean {
    return this.connection.state === HubConnectionState.Connected;
  }

  async start(): Promise<void> {
    if (this.connection.state !== HubConnectionState.Disconnected) return;
    await this.connection.start();
  }

  async joinSession(sessionId: string): Promise<SpeakingLiveSessionReady> {
    await this.start();
    const result = await this.connection.invoke<SpeakingLiveSessionReady>(
      "JoinSession",
      sessionId,
    );
    this.sessionId = sessionId;
    return result;
  }

  async submitTurn(
    audioBase64: string,
    confirmation?: { transcript: string; outcome: SpeakingTranscriptConfirmationOutcome; turnId: string },
  ): Promise<string> {
    if (!this.sessionId) throw new Error("Live speaking session is not joined.");
    const turnId = confirmation?.turnId ?? crypto.randomUUID();
    this.sequence += 1;
    await this.connection.invoke("SubmitTurn", {
      sessionId: this.sessionId,
      turnId,
      sequence: this.sequence,
      audioBase64,
      confirmedTranscript: confirmation?.transcript ?? null,
      confirmationOutcome: confirmation?.outcome ?? null,
    });
    return turnId;
  }

  async interruptTutor(): Promise<void> {
    if (!this.connected) return;
    await this.connection.invoke("InterruptTutor");
  }

  async stop(): Promise<void> {
    if (this.connected) await this.connection.invoke("LeaveSession");
    this.sessionId = null;
    await this.connection.stop();
  }
}
