import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { api, ApiError } from "@/api/client";
import { ProductEventType, type AuthenticatedUserDto } from "@/api/types";
import { clearAuthToken, loadStoredToken } from "@/api/nativeAuth";
import { clearSession, setLearnerId } from "./session";
import { AppPending } from "@/components/AppPending";

type AuthStatus = "loading" | "authenticated" | "unauthenticated";

interface AuthContextValue {
  status: AuthStatus;
  user: AuthenticatedUserDto | null;
  /** Completes sign-in with a Google ID token (from Google Identity Services). Resolves with the authenticated user so callers can route by profile state (e.g. to /username). */
  signInWithGoogle: (idToken: string) => Promise<AuthenticatedUserDto>;
  signOut: () => Promise<void>;
  /** Replaces the cached user after a profile change so gates/UI reflect it immediately. */
  applyUser: (user: AuthenticatedUserDto) => void;
}

// Exported so the build-time prerender entry can supply a static (signed-out) value
// without mounting the real AuthProvider, whose effects fetch /auth/me.
export const AuthContext = createContext<AuthContextValue | null>(null);
export type { AuthContextValue };

/**
 * Holds the signed-in user and bridges it to the legacy learner-id call sites: whenever a
 * user is known, their account id is written as the learner id so getLearnerId() keeps
 * working everywhere. Restores the session on load from the HttpOnly cookie via /auth/me.
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [user, setUser] = useState<AuthenticatedUserDto | null>(null);

  const adopt = useCallback((u: AuthenticatedUserDto) => {
    setLearnerId(u.id);
    setUser(u);
    setStatus("authenticated");

    // One idempotent event per UTC day: captures "returned next day" even when the HttpOnly cookie
    // restores the session without a fresh Google OAuth round-trip.
    const day = new Date().toISOString().slice(0, 10);
    void api.analytics
      .track(u.id, ProductEventType.SignedIn, `app-open:${day}`)
      .catch(() => undefined);
  }, []);

  useEffect(() => {
    let cancelled = false;
    // Native: warm the persisted Bearer token before /me so the restored session authenticates.
    // Web: loadStoredToken is a no-op and /me rides the cookie.
    loadStoredToken()
      .then(() => api.auth.me())
      .then((u) => {
        if (!cancelled) adopt(u);
      })
      .catch((err) => {
        // 401 simply means "not signed in yet"; anything else we also treat as logged-out
        // so the user lands on the sign-in screen rather than a broken app.
        if (!(err instanceof ApiError)) throw err;
        if (!cancelled) {
          // DEV PREVIEW: if there's no session at all, auto-establish the local dev account so
          // every screen renders real data immediately (the preview bypass mode lets you open any
          // route without a manual sign-in). Production never reaches this branch.
          if (import.meta.env.DEV) {
            api.auth
              .dev()
              .then((u) => {
                if (!cancelled) adopt(u);
              })
              .catch(() => setStatus("unauthenticated"));
          } else {
            setStatus("unauthenticated");
          }
        }
      });
    return () => {
      cancelled = true;
    };
  }, [adopt]);

  // A protected call returning 401 asks us to reconsider the session. It must NOT immediately sign
  // the learner out: a single stale or racing background poll (e.g. a leftover request from another
  // tab, or one that lost the cookie to a transient CORS hiccup) would otherwise tear down the auth
  // context and unmount live surfaces — the Speaking tutor gets destroyed and recreated mid-turn, so
  // its hub reconnects and the tutor reply never reaches the page. Re-verify with /api/auth/me (which
  // the api client never lets re-dispatch auth:unauthorized) and only sign out if the session really
  // is gone. RequireAuth then routes to /login. Dispatched by the api client (decoupled).
  useEffect(() => {
    let verifying = false;
    const onUnauthorized = () => {
      if (verifying) return;
      verifying = true;
      api.auth
        .me()
        .then((u) => adopt(u)) // session is still valid — the 401 was transient, keep the learner in
        .catch(() => {
          void clearAuthToken();
          clearSession();
          setUser(null);
          setStatus("unauthenticated");
        })
        .finally(() => {
          verifying = false;
        });
    };
    window.addEventListener("auth:unauthorized", onUnauthorized);
    return () =>
      window.removeEventListener("auth:unauthorized", onUnauthorized);
  }, [adopt]);

  const signInWithGoogle = useCallback(
    async (idToken: string) => {
      const u = await api.auth.google(idToken);
      adopt(u);
      return u;
    },
    [adopt]
  );

  const signOut = useCallback(async () => {
    try {
      await api.auth.logout();
    } finally {
      window.google?.accounts.id.disableAutoSelect();
      clearSession();
      setUser(null);
      setStatus("unauthenticated");
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ status, user, signInWithGoogle, signOut, applyUser: adopt }),
    [status, user, signInWithGoogle, signOut, adopt]
  );

  // Don't mount protected routes until the session is resolved (including the DEV auto-login
  // that sets the session cookie). Otherwise pages fire their API calls before the cookie
  // exists, get 401, and render blank. Show a neutral full-screen placeholder while resolving.
  if (status === "loading") {
    return <AppPending />;
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
