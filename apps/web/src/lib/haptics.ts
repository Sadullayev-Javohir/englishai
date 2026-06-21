// Lightweight haptic-feedback wrapper around @capacitor/haptics.
//
// Haptics only exist inside the native shell, so every call is a no-op on the web (guarded by
// isNativePlatform(), mirroring nativeAuth.ts). The promises are fire-and-forget - we never
// want a missing vibrator or a rejected plugin call to break a tap handler, so failures are
// swallowed. Use these from tab/CTA/sheet interactions for a native feel.
import { Haptics, ImpactStyle, NotificationType } from "@capacitor/haptics";
import { isNativePlatform } from "@/api/nativeAuth";

/** A light tap - primary navigation, tab switches, sheet open/close. */
export function tapLight(): void {
  if (!isNativePlatform()) return;
  void Haptics.impact({ style: ImpactStyle.Light }).catch(() => {});
}

/** A medium tap - confirming a primary action (submit, start). */
export function tapMedium(): void {
  if (!isNativePlatform()) return;
  void Haptics.impact({ style: ImpactStyle.Medium }).catch(() => {});
}

/** A success cue - correct answer, completed lesson. */
export function notifySuccess(): void {
  if (!isNativePlatform()) return;
  void Haptics.notification({ type: NotificationType.Success }).catch(() => {});
}
