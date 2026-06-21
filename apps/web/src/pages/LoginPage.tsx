import { useCallback, useEffect, useRef, useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { useAuth } from "@/app/auth";
import { api, ApiError } from "@/api/client";
import { Spinner } from "@/components/ui/Spinner";
import { isNativePlatform } from "@/api/nativeAuth";
import { NativeGoogleSignInError, nativeGoogleSignIn } from "@/api/nativeGoogleSignIn";
import { getStoredReferralCode, setStoredReferralCode } from "@/lib/referral";
import { Icon } from "@/components/ui/Icon";
import { Gift, Plus, ShieldCheck } from "lucide-react";
import { OnboardingChrome, PlayChip } from "./onboarding/OnboardingChrome";
import { returnTargetOr } from "@/app/returnTarget";
import "./LoginPage.css";

const BUILD_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID?.trim() ?? "";
const IS_NATIVE = isNativePlatform();
const GOOGLE_SCRIPT_TIMEOUT_MS = 8_000;
const GOOGLE_SCRIPT_POLL_MS = 100;

/**
 * The single entry point to the app: Google-only sign-in. On the web it renders the official
 * Google Identity Services button; inside the native shell it shows a button that drives the
 * platform Google sign-in. Either way the resulting Google ID token is exchanged with the
 * backend for a session (cookie on web, Bearer token on native) via the auth provider.
 *
 * UI follows englishai.pen frame 02. Google retains ownership of the production web
 * sign-in control; native and local development use their existing explicit handlers.
 */
export function LoginPage() {
  const navigate = useNavigate();
  const { status, signInWithGoogle, applyUser } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [clientId, setClientId] = useState(BUILD_CLIENT_ID);
  const [configLoading, setConfigLoading] = useState(!BUILD_CLIENT_ID);
  const [googleLoadAttempt, setGoogleLoadAttempt] = useState(0);
  const [googleReady, setGoogleReady] = useState(false);
  const googleButtonRef = useRef<HTMLDivElement>(null);

  // Activate the global animation system even though this page sits outside the AppShell.
  useEffect(() => {
    document.documentElement.classList.add("anim-on");
    document.documentElement.classList.add("login-page-active");
    window.dispatchEvent(new CustomEvent("englishai:background-video", { detail: false }));
    return () => {
      document.documentElement.classList.remove("login-page-active");
      window.dispatchEvent(new CustomEvent("englishai:background-video", { detail: true }));
    };
  }, []);

  useEffect(() => {
    if (BUILD_CLIENT_ID) return;

    let cancelled = false;
    api.auth.config()
      .then((config) => {
        if (!cancelled) setClientId(config.googleClientId.trim());
      })
      .catch(() => {
        if (!cancelled) setClientId("");
      })
      .finally(() => {
        if (!cancelled) setConfigLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const describeSignInError = (err: unknown) => {
    if (err instanceof NativeGoogleSignInError) {
      if (err.reason === "cancelled") return uz.auth.googleCancelled;
      if (err.reason === "timeout") return uz.auth.googleTimeout;
      if (err.reason === "state") return uz.auth.googleSecurityError;
      if (err.reason === "exchange") return uz.auth.googleExchangeError;
      return uz.auth.googleRejected;
    }
    // Native Credential Manager failures (for example an APK signing SHA-1 mismatch) happen
    // before any HTTP request. Calling those an internet problem sends the user in the wrong
    // direction; they are Google account/configuration failures instead.
    if (!(err instanceof ApiError)) return IS_NATIVE ? uz.auth.googleRejected : uz.auth.networkError;
    if (err.status === 409) return uz.auth.emailTaken;
    if (err.status === 429) return uz.auth.tooManyAttempts;
    if (err.status === 401) return uz.auth.googleRejected;
    return uz.auth.error;
  };

  // Route a freshly signed-in user: brand-new accounts (no username yet) go to the handle
  // setup screen; everyone else lands in the app.
  const routeAfterSignIn = useCallback(
    (u: { username: string | null; hasCompletedDemographics: boolean }) => {
      navigate(!u.username ? "/username" : !u.hasCompletedDemographics ? "/onboarding/profile-details" : returnTargetOr("/home"), { replace: true });
    },
    [navigate],
  );

  const signInWithGoogle_dev = useCallback(async () => {
    const u = await api.auth.dev();
    applyUser(u);
    return u;
  }, [applyUser]);

  // Dev-only local sign-in (no Google OAuth). Only shown under Vite's dev build; in production
  // import.meta.env.DEV is false and the button never renders nor calls the endpoint.
  const handleDevSignIn = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const u = await signInWithGoogle_dev();
      routeAfterSignIn(u);
    } catch {
      setError(uz.auth.error);
      setBusy(false);
    }
  }, [routeAfterSignIn, signInWithGoogle_dev]);

  // Native: drive the platform Google sign-in on tap, then exchange the ID token like the web.
  const handleNativeSignIn = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const idToken = await nativeGoogleSignIn(clientId);
      const u = await signInWithGoogle(idToken);
      routeAfterSignIn(u);
    } catch (err) {
      setError(describeSignInError(err));
      setBusy(false);
    }
  }, [clientId, routeAfterSignIn, signInWithGoogle]);

  useEffect(() => {
    // Web only - GIS cannot render inside the native WebView. The explicit Google-rendered
    // button is intentionally primary: One Tap/FedCM prompt() may be hidden by browser privacy
    // settings (notably Brave Shields) and must never be the only way to sign in.
    if (IS_NATIVE || !clientId || busy) return;

    let cancelled = false;
    const startedAt = Date.now();
    setGoogleReady(false);
    setError(null);

    const tryInit = () => {
      if (cancelled) return;

      const google = window.google;
      const parent = googleButtonRef.current;
      if (!google || !parent) {
        if (Date.now() - startedAt >= GOOGLE_SCRIPT_TIMEOUT_MS) {
          setError(uz.auth.googleBlocked);
          return;
        }
        window.setTimeout(tryInit, GOOGLE_SCRIPT_POLL_MS);
        return;
      }

      google.accounts.id.initialize({
        client_id: clientId,
        callback: async (response) => {
          setBusy(true);
          setError(null);
          try {
            const u = await signInWithGoogle(response.credential);
            routeAfterSignIn(u);
          } catch (err) {
            setError(describeSignInError(err));
            setBusy(false);
          }
        },
      });

      parent.replaceChildren();
      google.accounts.id.renderButton(parent, {
        type: "standard",
        theme: "outline",
        size: "large",
        text: "continue_with",
        shape: "pill",
        width: Math.min(320, Math.max(220, window.innerWidth - 96)),
        logo_alignment: "left",
      });
      setGoogleReady(true);
    };

    tryInit();
    return () => {
      cancelled = true;
    };
  }, [busy, clientId, googleLoadAttempt, routeAfterSignIn, signInWithGoogle]);

  const retryGoogleLoad = () => {
    setError(null);

    // A browser extension or Brave Shields may have blocked the original index.html request.
    // After the user permits it, create a fresh request rather than polling a script that will
    // never resume by itself.
    if (!window.google) {
      document.querySelectorAll<HTMLScriptElement>('script[src="https://accounts.google.com/gsi/client"]')
        .forEach((script) => script.remove());
      const script = document.createElement("script");
      script.src = "https://accounts.google.com/gsi/client";
      script.async = true;
      document.head.appendChild(script);
    }

    setGoogleLoadAttempt((attempt) => attempt + 1);
  };

  // DEV-only: the onboarding preview harness (`/dev/onboarding/login`) must render the
  // sign-in screen even when a dev session is already authenticated. No effect in prod.
  const isDevPreview =
    import.meta.env.DEV && typeof window !== "undefined" && window.location.pathname.startsWith("/dev/onboarding");
  if (status === "authenticated" && !isDevPreview) return <Navigate to={returnTargetOr("/home")} replace />;
  if (status === "loading" || configLoading) {
    return (
      <div className="onboarding-play play-login__loading">
        <Spinner />
      </div>
    );
  }

  return (
    <OnboardingChrome className="login-play-page" testId="login-viewport">
      <div className="play-login">
        <section className="play-login__welcome">
          <PlayChip tone="white">Yangi imkoniyatlar sizni kutmoqda</PlayChip>
          <h2 className="onboarding-play__title">Yana bir<br />yaxshi boshlanish.</h2>
          <img src="/assets/play/parrot.svg" alt="EnglishAI bilan yangi boshlanish" />
          <p>Let’s do this together!</p>
        </section>
        <section className="play-login__signin">
          <div className="play-login__heading">
            <h1 className="onboarding-play__title">Salom, do‘stim!</h1>
            <p className="onboarding-play__lede">Google hisobingiz bilan davom eting.</p>
          </div>
          <div className="login06__auth-control">
            {busy ? (
              <div className="login06__busy">
                <Spinner />
                <span>{uz.auth.signingIn}</span>
              </div>
            ) : IS_NATIVE && clientId ? (
              <button type="button" className="play-login__google-button" onClick={handleNativeSignIn}>
                <img src="/assets/play/google-g.svg" alt="" width={24} height={24} />Google bilan kirish
              </button>
            ) : import.meta.env.DEV && !clientId ? (
              <button type="button" className="play-login__google-button" onClick={handleDevSignIn} disabled={busy}>
                <Icon name="login" />Lokal kirish
              </button>
            ) : clientId ? (
              // Web: Google owns this control and opens its supported user-initiated account
              // chooser. This remains available when One Tap/FedCM prompts are suppressed.
              <div
                ref={googleButtonRef}
                className={`login06__google ${googleReady ? "is-ready" : ""}`}
                aria-label={uz.auth.googleAccount}
              />
            ) : (
              // No Google client id configured: dev-only local sign-in so the app stays usable
              // locally. Replace with the real Google button once VITE_GOOGLE_CLIENT_ID is set.
              <p className="onboarding-play__alert">{uz.auth.notConfigured}</p>
            )}
            {!busy && !IS_NATIVE && clientId && !googleReady && !error && (
              <div className="login06__google-loading" aria-label={uz.common.loading}>
                <Spinner />
              </div>
            )}
          </div>

          {error && (
            <div className="login06__error">
              <p role="alert" className="onboarding-play__alert"><Icon name="error" filled />{error}</p>
              {!IS_NATIVE && clientId && error === uz.auth.googleBlocked && (
                <button type="button" onClick={retryGoogleLoad} className="onboarding-play__quiet">
                  {uz.common.retry}
                </button>
              )}
            </div>
          )}

          <p className="play-login__security"><ShieldCheck size={16} aria-hidden />Parol kerak emas. Xavfsiz kirish.</p>
          <ReferralCodeEntry />
          <p className="play-login__new">Birinchi marta kirdingizmi?<br />Hisobingiz avtomatik yaratiladi.</p>
          <p className="play-login__privacy">Davom etish orqali Shartlar va<br /><a href="mailto:support@englishai.uz?subject=EnglishAI%20maxfiylik">Maxfiylik siyosatiga</a> rozilik bildirasiz.</p>
        </section>
      </div>
    </OnboardingChrome>
  );
}

/**
 * Optional referral-code entry on the sign-in screen. Lets a brand-new user apply a friend's
 * code by hand - for when they received the code as plain text rather than a shareable `?ref=`
 * link. The applied code is stashed exactly like a captured link and attached to the Google
 * sign-in call, so the backend credits the referrer once the new account is created (and it is
 * silently ignored for an existing account or an unknown code).
 */
function ReferralCodeEntry() {
  // If a ?ref= link was already captured on load, start expanded and show it as applied.
  const initial = getStoredReferralCode();
  const [open, setOpen] = useState(initial !== null);
  const [value, setValue] = useState(initial ?? "");
  const [applied, setApplied] = useState<string | null>(initial);
  const [invalid, setInvalid] = useState(false);

  const apply = () => {
    if (setStoredReferralCode(value)) {
      setApplied(value.trim().toUpperCase());
      setInvalid(false);
    } else {
      setApplied(null);
      setInvalid(true);
    }
  };

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="login06__referral-toggle"
        aria-expanded={false}
      >
        <Gift size={20} aria-hidden /><span>Taklif kodi bormi?</span><Plus size={18} aria-hidden />
      </button>
    );
  }

  return (
    <div className="login06__referral">
      <label htmlFor="referralCode" className="login06__referral-label">
        {uz.auth.referralPrompt}
      </label>
      <div className="login06__referral-row">
        <input
          id="referralCode"
          type="text"
          value={value}
          onChange={(e) => {
            setValue(e.target.value);
            setApplied(null);
            setInvalid(false);
          }}
          placeholder={uz.auth.referralPlaceholder}
          autoCapitalize="characters"
          maxLength={12}
          className="login06__referral-input onboarding-play__input"
        />
        <button
          type="button"
          className="login06__referral-apply"
          onClick={apply}
          disabled={value.trim().length === 0}
        >
          {uz.auth.referralApply}
        </button>
      </div>
      {applied && (
        <p className="login06__referral-status login06__referral-status--success">
          <Icon name="check_circle" filled />
          {uz.auth.referralApplied}
        </p>
      )}
      {invalid && <p className="login06__referral-status login06__referral-status--error">{uz.auth.referralInvalid}</p>}
    </div>
  );
}
