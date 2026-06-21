// SignalR client for real-time notification hints. The persisted notification feed is authoritative:
// reconnecting clients refetch it, which covers offline delivery and deduplicates multiple tabs/devices.
import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import type { NotificationDto } from "./types";
import { apiUrl } from "./config";
import { getAuthToken, isNativePlatform } from "./nativeAuth";

export const BroadcastReceivedEvent = "BroadcastReceived";
export const NotificationReceivedEvent = "NotificationReceived";
const reconnectDelays = [0, 2_000, 5_000, 10_000, 30_000];

export interface NotificationsHubHandlers {
  onBroadcast: (notification: NotificationDto) => void;
  onReconnecting?: (error?: Error) => void;
  onReconnected?: () => void | Promise<void>;
  onClosed?: (error?: Error) => void;
}

export class NotificationsHubClient {
  private connection: HubConnection;

  constructor(
    handlersOrBroadcast:
      | NotificationsHubHandlers
      | ((notification: NotificationDto) => void)
  ) {
    const handlers: NotificationsHubHandlers =
      typeof handlersOrBroadcast === "function"
        ? { onBroadcast: handlersOrBroadcast }
        : handlersOrBroadcast;

    this.connection = new HubConnectionBuilder()
      .withUrl(apiUrl("/hubs/notifications"), {
        accessTokenFactory: isNativePlatform() ? () => getAuthToken() ?? "" : undefined,
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect(reconnectDelays)
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on(BroadcastReceivedEvent, handlers.onBroadcast);
    this.connection.on(NotificationReceivedEvent, handlers.onBroadcast);
    this.connection.onreconnecting((error) => handlers.onReconnecting?.(error));
    this.connection.onreconnected(() => Promise.resolve(handlers.onReconnected?.()));
    this.connection.onclose((error) => handlers.onClosed?.(error));
  }

  start(): Promise<void> {
    if (this.connection.state !== HubConnectionState.Disconnected) {
      return Promise.resolve();
    }
    return this.connection.start();
  }

  stop(): Promise<void> {
    return this.connection.stop();
  }
}
