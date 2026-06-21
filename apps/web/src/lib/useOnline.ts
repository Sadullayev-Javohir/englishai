import { useCallback, useEffect, useRef, useState } from "react";
import { apiUrl } from "@/api/config";

// Connectivity state for the global offline dialog (design: englishai.pen, screen 81).
//
// `navigator.onLine` only reports whether the device has *a* network interface, so it
// stays true on a captive-carrier Wi-Fi or a dead uplink. It is therefore used as the
// cheap trigger, while "Qayta urinish" runs a real request against /health before the
// learner is told the connection is back.

/** Status shown inside the dialog while the learner retries. */
export type ConnectionProbe = "idle" | "checking" | "offline" | "online";

/** How long the probe waits before declaring the network unreachable. */
const PROBE_TIMEOUT_MS = 6000;
/** How long the "Ulanish tiklandi" confirmation stays up before the dialog closes. */
const RECONNECTED_LINGER_MS = 1200;

function readNavigatorOnline(): boolean {
  // SSR (entry-prerender.tsx) has no navigator: prerender the online state so the
  // dialog is never baked into the static HTML.
  if (typeof navigator === "undefined") return true;
  return navigator.onLine !== false;
}

/** Single uncached round-trip to the backend. Resolves false on any network failure. */
async function probeConnection(): Promise<boolean> {
  if (typeof fetch === "undefined") return true;
  const controller = typeof AbortController === "undefined" ? null : new AbortController();
  const timer = controller ? setTimeout(() => controller.abort(), PROBE_TIMEOUT_MS) : undefined;
  try {
    const response = await fetch(apiUrl("/health"), {
      method: "GET",
      cache: "no-store",
      signal: controller?.signal,
    });
    // Any answered request proves the link is up, even a 503 from a degraded backend:
    // that is a server problem, not the learner's internet.
    return response.type === "opaque" || response.status > 0;
  } catch {
    return false;
  } finally {
    if (timer !== undefined) clearTimeout(timer);
  }
}

export interface ConnectionStatus {
  /** True while the browser reports no connection, or a retry probe failed. */
  offline: boolean;
  /** Retry feedback rendered inside the dialog. */
  probe: ConnectionProbe;
  /** Runs the real connectivity probe behind the "Qayta urinish" button. */
  retry: () => void;
}

/**
 * Tracks connectivity for the offline dialog: browser `online`/`offline` events for the
 * automatic trigger, plus a manual probe for the retry button.
 */
export function useOnline(): ConnectionStatus {
  const [offline, setOffline] = useState(() => !readNavigatorOnline());
  const [probe, setProbe] = useState<ConnectionProbe>("idle");
  const probingRef = useRef(false);

  useEffect(() => {
    const goOffline = () => {
      setOffline(true);
      setProbe("idle");
    };
    const goOnline = () => {
      setOffline(false);
      setProbe("idle");
    };
    window.addEventListener("offline", goOffline);
    window.addEventListener("online", goOnline);
    return () => {
      window.removeEventListener("offline", goOffline);
      window.removeEventListener("online", goOnline);
    };
  }, []);

  // Clear the "Ulanish tiklandi" confirmation once it has been read.
  useEffect(() => {
    if (probe !== "online") return;
    const timer = setTimeout(() => {
      setOffline(false);
      setProbe("idle");
    }, RECONNECTED_LINGER_MS);
    return () => clearTimeout(timer);
  }, [probe]);

  const retry = useCallback(() => {
    if (probingRef.current) return;
    probingRef.current = true;
    setProbe("checking");
    void probeConnection().then((reachable) => {
      probingRef.current = false;
      setProbe(reachable ? "online" : "offline");
    });
  }, []);

  return { offline, probe, retry };
}
