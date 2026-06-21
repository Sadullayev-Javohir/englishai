// Native push-notification registration (@capacitor/push-notifications).
//
// Only runs inside the native shell - every entry point is guarded by isNativePlatform(), and
// every plugin call is fire-and-forget with swallowed errors, so a missing plugin / denied
// permission / web build never blocks startup. The web app keeps its in-app SignalR feed; this
// only adds the OS-level status-bar notification + app badge that arrive even when the app is closed.
import { PushNotifications } from "@capacitor/push-notifications";
import { Capacitor } from "@capacitor/core";
import { isNativePlatform } from "@/api/nativeAuth";
import { getLearnerId } from "@/app/session";
import { api } from "@/api/client";
import { openExternalUrl } from "@/lib/openExternal";
import { resolveNotificationTarget } from "@/lib/notificationTarget";

let listenersReady = false;
let registrationInFlight = false;

/**
 * Requests permission, registers with FCM/APNs, and sends the resulting device token to the backend
 * so this device becomes a push target for the signed-in learner. Call once after authentication.
 * Idempotent - repeated calls after the first are ignored.
 */
export async function initPushNotifications(): Promise<void> {
  if (!isNativePlatform() || registrationInFlight) return;

  try {
    // Ask for permission (Android 13+ and iOS require an explicit grant); register only if granted.
    let permission = await PushNotifications.checkPermissions();
    if (permission.receive === "prompt" || permission.receive === "prompt-with-rationale") {
      permission = await PushNotifications.requestPermissions();
    }
    if (permission.receive !== "granted") return;

    registrationInFlight = true;

    if (!listenersReady) {
      await PushNotifications.addListener("registration", (token) => {
        registrationInFlight = false;
        void sendToken(token.value);
      });

      await PushNotifications.addListener("registrationError", () => {
        registrationInFlight = false;
      });

      await PushNotifications.addListener("pushNotificationActionPerformed", (action) => {
        const link = action.notification.data?.link;
        if (typeof link !== "string") return;
        const target = resolveNotificationTarget(link);
        if (!target) return;
        if (target.kind === "internal") window.location.assign(target.path);
        else openExternalUrl(target.url);
      });
      listenersReady = true;
    }

    await PushNotifications.register();

  } catch {
    registrationInFlight = false;
    // Plugin unavailable or a platform quirk - never let push setup break app startup.
  }
}

async function sendToken(token: string): Promise<void> {
  try {
    await api.registerDevice(getLearnerId(), token, Capacitor.getPlatform());
  } catch {
    // Best-effort: a failed registration just means no push until the next successful call.
  }
}
