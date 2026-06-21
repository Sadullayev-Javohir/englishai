import { useCallback, useEffect, useState } from "react";
import { getLearnerId } from "@/app/session";

const STORAGE_PREFIX = "englishai.lesson.progress.v1";

interface StoredProgress<T> {
  value: T;
  completed: boolean;
  updatedAt: number;
}

function storageKey(scope: string): string {
  return `${STORAGE_PREFIX}.${getLearnerId()}.${scope}`;
}

export function readLessonProgress<T>(scope: string, fallback: T): T {
  if (typeof window === "undefined") return fallback;
  try {
    const raw = window.localStorage.getItem(storageKey(scope));
    if (!raw) return fallback;
    const parsed = JSON.parse(raw) as Partial<StoredProgress<T>>;
    return parsed.value ?? fallback;
  } catch {
    return fallback;
  }
}

export function writeLessonProgress<T>(scope: string, value: T, completed = false): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(
      storageKey(scope),
      JSON.stringify({ value, completed, updatedAt: Date.now() } satisfies StoredProgress<T>),
    );
  } catch {
    // Progress persistence is best-effort when storage is unavailable.
  }
}

export function clearLessonProgress(scope: string): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.removeItem(storageKey(scope));
  } catch {
    // Storage may be unavailable.
  }
}

export function clearLessonProgressScope(scope: string): void {
  if (typeof window === "undefined") return;
  const prefix = storageKey(scope);
  try {
    const keys = Array.from({ length: window.localStorage.length }, (_, index) =>
      window.localStorage.key(index),
    ).filter((key): key is string => Boolean(key?.startsWith(prefix)));
    keys.forEach((key) => window.localStorage.removeItem(key));
  } catch {
    // Storage may be unavailable.
  }
}

export function useLessonProgress<T>(
  scope: string,
  fallback: T,
): [T, (next: T | ((current: T) => T)) => void] {
  const [value, setValue] = useState<T>(() => readLessonProgress(scope, fallback));

  useEffect(() => {
    setValue(readLessonProgress(scope, fallback));
  }, [scope]); // eslint-disable-line react-hooks/exhaustive-deps

  const update = useCallback((next: T | ((current: T) => T)) => {
    setValue((current) => {
      const resolved = typeof next === "function"
        ? (next as (current: T) => T)(current)
        : next;
      writeLessonProgress(scope, resolved);
      return resolved;
    });
  }, [scope]);

  return [value, update] as const;
}
