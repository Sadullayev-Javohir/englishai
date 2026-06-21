import { isNativePlatform } from "./nativeAuth";

export interface NativeCapabilities {
  video: boolean;
  payments: boolean;
  premiumAccess: boolean;
  googleOnlyAuth: boolean;
  widgets: boolean;
  push: boolean;
}

const WEB_CAPABILITIES: NativeCapabilities = {
  video: true,
  payments: true,
  premiumAccess: false,
  googleOnlyAuth: false,
  widgets: false,
  push: false,
};

const ANDROID_CAPABILITIES: NativeCapabilities = {
  video: false,
  payments: false,
  premiumAccess: true,
  googleOnlyAuth: true,
  widgets: true,
  push: true,
};

export function getNativeCapabilities(): NativeCapabilities {
  return isNativePlatform() ? ANDROID_CAPABILITIES : WEB_CAPABILITIES;
}

export function nativeRouteAllowed(pathname: string): boolean {
  const capabilities = getNativeCapabilities();
  if (!capabilities.video && pathname.startsWith("/video")) return false;
  if (!capabilities.payments && pathname === "/pricing") return false;
  return true;
}
