import { useSyncExternalStore } from "react";
import type { NotificationDto } from "@/api/types";

interface NotificationsFeedSnapshot {
  learnerId: string | null;
  data: NotificationDto[] | null;
  loading: boolean;
  error: unknown;
}

let snapshot: NotificationsFeedSnapshot = {
  learnerId: null,
  data: null,
  loading: false,
  error: null,
};
const listeners = new Set<() => void>();
const requests = new Map<string, Promise<NotificationDto[]>>();

function emit(next: NotificationsFeedSnapshot) {
  snapshot = next;
  listeners.forEach((listener) => listener());
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function getSnapshot(): NotificationsFeedSnapshot {
  return snapshot;
}

export function useNotificationsFeedSnapshot(): NotificationsFeedSnapshot {
  return useSyncExternalStore(subscribe, getSnapshot, getSnapshot);
}

export function loadNotificationsFeed(
  learnerId: string,
  loader: () => Promise<{ items: NotificationDto[] }>,
  force = false,
): Promise<NotificationDto[]> {
  if (!force && snapshot.learnerId === learnerId && snapshot.data) {
    return Promise.resolve(snapshot.data);
  }

  const existing = requests.get(learnerId);
  if (existing) return existing;

  emit({
    learnerId,
    data: snapshot.learnerId === learnerId ? snapshot.data : null,
    loading: true,
    error: null,
  });

  const request = loader()
    .then((page) => {
      const data = page.items;
      emit({ learnerId, data, loading: false, error: null });
      return data;
    })
    .catch((error) => {
      emit({
        learnerId,
        data: snapshot.learnerId === learnerId ? snapshot.data : null,
        loading: false,
        error,
      });
      throw error;
    })
    .finally(() => requests.delete(learnerId));

  requests.set(learnerId, request);
  return request;
}

export function resetNotificationsFeedForTests(): void {
  requests.clear();
  snapshot = { learnerId: null, data: null, loading: false, error: null };
  listeners.forEach((listener) => listener());
}
