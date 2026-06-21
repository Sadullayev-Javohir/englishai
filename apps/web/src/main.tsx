import { StrictMode, Suspense } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router-dom";
import { router } from "./app/router";
import { AuthProvider } from "./app/auth";
import { initNativeUi } from "./api/nativeUi";
import { captureReferralFromUrl } from "./lib/referral";
import { initializeThemeRuntime } from "./lib/theme";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { AppPending } from "./components/AppPending";
import { OfflineModal } from "./components/OfflineModal";
import "./index.css";

document.documentElement.dataset.appBooting = "true";
initializeThemeRuntime();

// Configure the native status bar (no-op on web); fire-and-forget so it never delays render.
void initNativeUi();

// Stash any ?ref= referral code before sign-in so it survives the Google OAuth round-trip.
captureReferralFromUrl();

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {/* Global render-error boundary - outermost so it also catches errors thrown by
        AuthProvider itself, not just the routed pages. Without this, any uncaught
        render error anywhere in the app produced a blank white screen. */}
    <ErrorBoundary>
      <AuthProvider>
        <Suspense fallback={<AppPending />}>
          <RouterProvider router={router} />
        </Suspense>
        {/* Mounted above the router so losing the connection is reported on every
            surface - learner shell, admin shell, login and the public pages alike. */}
        <OfflineModal />
      </AuthProvider>
    </ErrorBoundary>
  </StrictMode>,
);
