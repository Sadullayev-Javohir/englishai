import { useCallback, useEffect, useState } from "react";

interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: unknown;
  reload: () => void;
  refresh: () => void;
}

/** Runs an async loader on mount (and when deps change); exposes data/loading/error.
 *  Pass `enabled = false` to skip the fetch until a precondition (e.g. auth) is met.
 *  Transient failures (network errors, 429 rate-limit, 5xx) are retried a few times so a
 *  brief blip (e.g. an auth race or a rate-limit window) self-heals instead of showing a
 *  hard error. */
export function useAsync<T>(
  loader: () => Promise<T>,
  deps: React.DependencyList = [],
  enabled = true,
): AsyncState<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(enabled);
  const [error, setError] = useState<unknown>(null);
  const [tick, setTick] = useState(0);
  const [backgroundReload, setBackgroundReload] = useState(false);

  const reload = useCallback(() => {
    setBackgroundReload(false);
    setTick((t) => t + 1);
  }, []);
  const refresh = useCallback(() => {
    setBackgroundReload(true);
    setTick((t) => t + 1);
  }, []);

  useEffect(() => {
    if (!enabled) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    let retryTimer: number | undefined;
    if (!backgroundReload) setLoading(true);
    setError(null);

    const attempt = async (triesLeft: number) => {
      try {
        const result = await loader();
        if (!cancelled) {
          setData(result);
          setLoading(false);
          setBackgroundReload(false);
        }
      } catch (err) {
        if (cancelled) return;
        if (isTransient(err) && triesLeft > 0) {
          retryTimer = window.setTimeout(() => void attempt(triesLeft - 1), 700);
          return;
        }
        setError(err);
        setLoading(false);
        setBackgroundReload(false);
      }
    };

    // One retry heals a brief transport/server blip without turning every failed screen request into
    // four backend calls. Higher-level AI providers already have their own bounded fallback chain.
    void attempt(1);
    return () => {
      cancelled = true;
      if (retryTimer !== undefined) window.clearTimeout(retryTimer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick, enabled]);

  return { data, loading, error, reload, refresh };
}

/** A failure worth retrying: network hiccup, or a server-side throttle / 5xx. Auth (401)
 *  and ownership (403) errors are NOT transient - they need a different fix, not a retry. */
function isTransient(err: unknown): boolean {
  if (err instanceof TypeError || err instanceof Error) {
    if (/Failed to fetch|NetworkError|Load failed/i.test(err.message)) return true;
  }
  // ApiError carries a status code on the instance (see api/client.ts).
  const status = (err as { status?: number } | null)?.status;
  if (status === 429 || (status !== undefined && status >= 500)) return true;
  return false;
}
