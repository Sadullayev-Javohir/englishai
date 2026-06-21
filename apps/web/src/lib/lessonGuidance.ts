import { useSyncExternalStore } from "react";

const STORAGE_KEY = "englishai.lesson-guidance.visible.v1";
const listeners = new Set<() => void>();

export function getLessonGuidanceVisible(): boolean {
  if (typeof localStorage === "undefined") return true;
  return localStorage.getItem(STORAGE_KEY) !== "0";
}

export function setLessonGuidanceVisible(visible: boolean): void {
  try {
    if (typeof localStorage !== "undefined") {
      localStorage.setItem(STORAGE_KEY, visible ? "1" : "0");
    }
  } catch {
    // Guidance preference is best-effort when storage is unavailable.
  }
  for (const listener of listeners) listener();
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  if (listeners.size === 1 && typeof window !== "undefined") {
    window.addEventListener("storage", handleStorage);
  }
  return () => {
    listeners.delete(listener);
    if (listeners.size === 0 && typeof window !== "undefined") {
      window.removeEventListener("storage", handleStorage);
    }
  };
}

function handleStorage(event: StorageEvent): void {
  if (event.key !== STORAGE_KEY) return;
  for (const listener of listeners) listener();
}

export function useLessonGuidancePreference() {
  const visible = useSyncExternalStore(subscribe, getLessonGuidanceVisible, () => true);
  return { visible, setVisible: setLessonGuidanceVisible };
}
