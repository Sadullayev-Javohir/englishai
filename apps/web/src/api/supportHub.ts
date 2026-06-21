import { HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import type { SupportConversationDto, SupportMessageDto } from "./types";
import { apiUrl } from "./config";
import { getAuthToken, isNativePlatform } from "./nativeAuth";

export interface SupportHubHandlers {
  onMessage?: (payload: SupportMessageDto | { conversationId: string; message: SupportMessageDto }) => void;
  onConversation?: (conversation: SupportConversationDto) => void;
  onTyping?: (payload: { conversationId: string; senderId: string; admin: boolean; typing: boolean }) => void;
  onRead?: (payload: { conversationId: string; byAdmin: boolean; readAt: string }) => void;
  onReconnecting?: () => void;
  onReconnected?: () => void | Promise<void>;
  onClosed?: () => void;
}

let supportHubStart: Promise<void> | null = null;
let supportHubConnection: HubConnection | null = null;

export class SupportHubClient {
  private readonly connection: HubConnection;
  constructor(handlers: SupportHubHandlers) {
    supportHubConnection ??= new HubConnectionBuilder()
        .withUrl(apiUrl("/hubs/support"), {
          accessTokenFactory: isNativePlatform() ? () => getAuthToken() ?? "" : undefined,
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
        })
        .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
        .configureLogging(LogLevel.Warning)
        .build();
    this.connection = supportHubConnection;
    if (handlers.onMessage) this.connection.on("SupportMessageCreated", handlers.onMessage);
    if (handlers.onConversation) this.connection.on("SupportConversationUpdated", handlers.onConversation);
    if (handlers.onTyping) this.connection.on("SupportTypingChanged", handlers.onTyping);
    if (handlers.onRead) this.connection.on("SupportMessagesRead", handlers.onRead);
    this.connection.onreconnecting(() => handlers.onReconnecting?.());
    this.connection.onreconnected(() => Promise.resolve(handlers.onReconnected?.()));
    this.connection.onclose(() => handlers.onClosed?.());
  }
  start() {
    if (this.connection.state !== HubConnectionState.Disconnected) return Promise.resolve();
    supportHubStart ??= this.connection.start().finally(() => { supportHubStart = null; });
    return supportHubStart;
  }
  async stop() {
    if (supportHubStart) {
      try { await supportHubStart; } catch { /* start failure already reaches the page handler */ }
    }
    if (this.connection.state !== HubConnectionState.Disconnected) await this.connection.stop();
  }
  join(conversationId: string, admin: boolean) { return this.connection.invoke("JoinConversation", conversationId, admin); }
  typing(conversationId: string, admin: boolean, typing: boolean) { return this.connection.invoke("SetTyping", conversationId, admin, typing); }
}
