import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import type { AccentTutorMessageDto, PronunciationResultDto, SpeechWordTimingDto } from "./types";
import { apiUrl } from "./config";
import { getAuthToken, isNativePlatform } from "./nativeAuth";

export interface AccentTutorLiveEventBase {
  tutorId: string;
  turnId: string;
  sequence: number;
  serverTimestamp: string;
}

export interface AccentTutorPartialTranscriptEvent extends AccentTutorLiveEventBase { text: string }
export interface AccentTutorTurnDiscardedEvent extends AccentTutorLiveEventBase { reason: string }
export interface AccentTutorFinalTranscriptEvent extends AccentTutorLiveEventBase { transcript: string }
export interface AccentTutorReplyEvent extends AccentTutorLiveEventBase { tutorText: string }
export interface AccentTutorPronunciationEvent extends AccentTutorLiveEventBase { pronunciation: PronunciationResultDto }
export interface AccentTutorAudioEvent extends AccentTutorLiveEventBase {
  tutorAudioBase64: string;
  isNaturalVoice: boolean;
  wordTimings: SpeechWordTimingDto[];
}
export interface AccentTutorLiveErrorEvent extends AccentTutorLiveEventBase {
  code: string;
  message: string;
  recoverable: boolean;
}

export interface AccentTutorLiveHandlers {
  onTurnStarted?: (event: AccentTutorLiveEventBase) => void;
  onAudioChunkAccepted?: (event: AccentTutorLiveEventBase) => void;
  onSpeechPauseCandidate?: (event: AccentTutorLiveEventBase) => void;
  onSpeechResumed?: (event: AccentTutorLiveEventBase) => void;
  onTurnCommitted?: (event: AccentTutorLiveEventBase) => void;
  onTurnCompleted?: (event: AccentTutorLiveEventBase) => void;
  onTurnDiscarded?: (event: AccentTutorTurnDiscardedEvent) => void;
  onPartialTranscript?: (event: AccentTutorPartialTranscriptEvent) => void;
  onRecognized: (event: AccentTutorFinalTranscriptEvent) => void;
  onTutor: (event: AccentTutorReplyEvent) => void;
  onPronunciation: (event: AccentTutorPronunciationEvent) => void;
  onAudio: (event: AccentTutorAudioEvent) => void;
  onRecoverableError: (event: AccentTutorLiveErrorEvent) => void;
  onClosed?: () => void;
}

interface ActiveTurn { turnId: string; sequence: number }

export class AccentTutorLiveHubClient {
  private readonly connection: HubConnection;
  private tutorId: string | null = null;
  private sequence = 0;
  private activeTurn: ActiveTurn | null = null;
  private committing = false;
  private sendQueue: Promise<void> = Promise.resolve();

  constructor(handlers: AccentTutorLiveHandlers) {
    this.connection = new HubConnectionBuilder()
      .withUrl(apiUrl("/hubs/accent-tutor-live"), {
        accessTokenFactory: isNativePlatform() ? () => getAuthToken() ?? "" : undefined,
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect([0, 1_000, 3_000, 8_000])
      .configureLogging(LogLevel.Warning)
      .build();
    // A live turn is STT (up to ~8s) + LLM reply (up to ~25s) + TTS, during which the learner's
    // browser may receive no hub message for a while. The default 30s client serverTimeout tripped
    // mid-turn ("Server timeout elapsed without receiving a message from the server") and dropped the
    // socket. Give a generous 60s window (4x the 15s keep-alive) so a slow turn or brief network
    // hiccup no longer kills the session.
    this.connection.serverTimeoutInMilliseconds = 60_000;
    this.connection.keepAliveIntervalInMilliseconds = 15_000;
    this.connection.on("TurnStarted", handlers.onTurnStarted ?? (() => undefined));
    this.connection.on("AudioChunkAccepted", handlers.onAudioChunkAccepted ?? (() => undefined));
    this.connection.on("SpeechPauseCandidate", handlers.onSpeechPauseCandidate ?? (() => undefined));
    this.connection.on("SpeechResumed", handlers.onSpeechResumed ?? (() => undefined));
    this.connection.on("TurnCommitted", (event: AccentTutorLiveEventBase) => {
      this.committing = true;
      handlers.onTurnCommitted?.(event);
    });
    this.connection.on("TurnCompleted", (event: AccentTutorLiveEventBase) => {
      this.activeTurn = null;
      this.committing = false;
      handlers.onTurnCompleted?.(event);
    });
    this.connection.on("TurnDiscarded", (event: AccentTutorTurnDiscardedEvent) => {
      this.activeTurn = null;
      this.committing = false;
      handlers.onTurnDiscarded?.(event);
    });
    this.connection.on("PartialTranscript", handlers.onPartialTranscript ?? (() => undefined));
    this.connection.on("FinalTranscript", handlers.onRecognized);
    this.connection.on("TutorText", handlers.onTutor);
    this.connection.on("PronunciationReady", handlers.onPronunciation);
    this.connection.on("TutorAudio", handlers.onAudio);
    this.connection.on("RecoverableError", (event: AccentTutorLiveErrorEvent) => {
      this.activeTurn = null;
      this.committing = false;
      handlers.onRecoverableError(event);
    });
    this.connection.onreconnecting(() => {
      // The socket is gone; drop any in-flight turn so we don't append audio to a dead turn.
      this.activeTurn = null;
      this.committing = false;
    });
    this.connection.onreconnected(async () => {
      // Automatic reconnect gives a brand-new server connection with NO session state, so every
      // subsequent StartTurn/AppendAudioChunk/SpeechPaused was rejected with "Join the accent tutor
      // session before starting a turn" — the learner spoke on and never got a reply. Re-join here.
      this.activeTurn = null;
      this.committing = false;
      if (this.tutorId) {
        try { await this.connection.invoke("JoinSession", this.tutorId); }
        catch { /* if the rejoin itself fails, onclose fires and the page surfaces it */ }
      }
    });
    this.connection.onclose(() => {
      this.activeTurn = null;
      this.committing = false;
      handlers.onClosed?.();
    });
  }

  get connected(): boolean { return this.connection.state === HubConnectionState.Connected; }
  get hasActiveTurn(): boolean { return this.activeTurn !== null; }
  get activeTurnId(): string | null { return this.activeTurn?.turnId ?? null; }

  async join(tutorId: string): Promise<void> {
    if (this.connection.state === HubConnectionState.Disconnected) await this.connection.start();
    await this.connection.invoke("JoinSession", tutorId);
    this.tutorId = tutorId;
    this.committing = false;
  }

  startTurn(history: AccentTutorMessageDto[], isInterruption: boolean): Promise<string> {
    if (!this.tutorId) return Promise.reject(new Error("Accent tutor live session is not joined."));
    if (this.activeTurn) return this.committing
      ? Promise.reject(new Error("Accent tutor turn is still committing."))
      : Promise.resolve(this.activeTurn.turnId);
    const active = { turnId: crypto.randomUUID(), sequence: ++this.sequence };
    this.activeTurn = active;
    this.committing = false;
    return this.enqueue("StartTurn", {
      tutorId: this.tutorId,
      turnId: active.turnId,
      sequence: active.sequence,
      history,
      isInterruption,
    }).then(() => active.turnId);
  }

  appendAudioChunk(pcmBase64: string): Promise<void> {
    const request = this.turnRequest({ pcmBase64 });
    return this.enqueue("AppendAudioChunk", request);
  }

  speechPaused(): Promise<void> { return this.enqueue("SpeechPaused", this.turnRequest()); }
  resumeSpeech(): Promise<void> { return this.enqueue("ResumeSpeech", this.turnRequest()); }

  commitTurn(): Promise<void> {
    const request = this.turnRequest();
    this.committing = true;
    return this.enqueue("CommitTurn", request);
  }

  cancelTurn(): Promise<void> {
    if (!this.activeTurn) return Promise.resolve();
    const request = this.turnRequest();
    this.activeTurn = null;
    this.committing = false;
    return this.enqueue("CancelTurn", request);
  }

  async stop(): Promise<void> {
    try { await this.cancelTurn(); } catch { /* connection may already be gone */ }
    await this.sendQueue.catch(() => undefined);
    if (this.connected) await this.connection.invoke("LeaveSession");
    this.tutorId = null;
    this.committing = false;
    await this.connection.stop();
  }

  private turnRequest(extra: Record<string, unknown> = {}): Record<string, unknown> {
    if (!this.tutorId || !this.activeTurn) throw new Error("No active accent tutor turn.");
    return {
      tutorId: this.tutorId,
      turnId: this.activeTurn.turnId,
      sequence: this.activeTurn.sequence,
      ...extra,
    };
  }

  private enqueue(method: string, request: unknown): Promise<void> {
    const operation = this.sendQueue.then(() => this.connection.invoke(method, request));
    this.sendQueue = operation.catch(() => undefined);
    return operation;
  }
}
