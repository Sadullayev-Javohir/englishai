const PENDING_TOPIC_KEY = "englishai:home-recommendation:pending-topic";
const LAST_ACTIVE_TOPIC_KEY = "englishai:home-recommendation:last-active-topic";
const NEXT_FALLBACK_KEY = "englishai:home-recommendation:next-fallback";

export type HomeFallbackKind = "books" | "video";

function learnerKey(key: string, learnerId: string): string {
  return `${key}:${learnerId}`;
}

export function rememberActiveHomeTopic(learnerId: string, topicId: string | null): void {
  if (!topicId) return;
  localStorage.setItem(learnerKey(LAST_ACTIVE_TOPIC_KEY, learnerId), topicId);
}

export function markHomeTopicCompleted(learnerId: string, topicId: string): void {
  localStorage.setItem(learnerKey(PENDING_TOPIC_KEY, learnerId), topicId);
}

export function ensureCompletedHomeTopicPending(
  learnerId: string,
  activeTopicId: string | null,
  hasMasteredTopics: boolean,
): void {
  const lastActiveTopic = localStorage.getItem(learnerKey(LAST_ACTIVE_TOPIC_KEY, learnerId));
  if (hasMasteredTopics && lastActiveTopic && lastActiveTopic !== activeTopicId) {
    markHomeTopicCompleted(learnerId, lastActiveTopic);
  }
  rememberActiveHomeTopic(learnerId, activeTopicId);
}

export function pendingHomeFallback(learnerId: string): HomeFallbackKind | null {
  const pendingTopic = localStorage.getItem(learnerKey(PENDING_TOPIC_KEY, learnerId));
  if (!pendingTopic) return null;
  return localStorage.getItem(learnerKey(NEXT_FALLBACK_KEY, learnerId)) === "video" ? "video" : "books";
}

export function consumeHomeFallback(learnerId: string): void {
  const current = pendingHomeFallback(learnerId);
  if (!current) return;
  localStorage.removeItem(learnerKey(PENDING_TOPIC_KEY, learnerId));
  localStorage.setItem(learnerKey(NEXT_FALLBACK_KEY, learnerId), current === "books" ? "video" : "books");
}
