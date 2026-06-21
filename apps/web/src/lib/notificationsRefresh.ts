import { useSyncExternalStore } from "react";

// A tiny external store that lets any component re-fetch notifications when a realtime broadcast
// arrives. The SignalR client (started once in AppShell) calls bumpNotifications(); every hook that
// reads useNotificationsRefreshKey() then re-runs its fetch, so the bell badge and the Notifications
// feed update instantly - without polling or a page refresh.

let version = 0;
const listeners = new Set<() => void>();

/** Signals that notifications changed (a broadcast arrived); wakes every subscribed reader. */
export function bumpNotifications(): void {
  version += 1;
  listeners.forEach((l) => l());
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function getSnapshot(): number {
  return version;
}

/** A number that increments on every realtime notification event; use it as a fetch dependency. */
export function useNotificationsRefreshKey(): number {
  return useSyncExternalStore(subscribe, getSnapshot, getSnapshot);
}
