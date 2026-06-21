import { App } from "@capacitor/app";
import { isNativePlatform } from "@/api/nativeAuth";
import { openExternalUrl } from "@/lib/openExternal";
import { resolveNotificationTarget } from "@/lib/notificationTarget";

let started = false;

function openDeepLink(url: string): void {
  if (url.startsWith("uz.englishai.app://")) {
    try {
      const parsed = new URL(url);
      const route = `/${parsed.host}${parsed.pathname}${parsed.search}${parsed.hash}`.replace(/\/+/g, "/");
      window.location.assign(route);
    } catch {
      // Ignore malformed custom-scheme links.
    }
    return;
  }
  const target = resolveNotificationTarget(url);
  if (!target) return;
  if (target.kind === "internal") {
    window.location.assign(target.path);
    return;
  }
  openExternalUrl(target.url);
}

/** Routes verified HTTPS and uz.englishai.app:// Android intents into the native SPA. */
export async function initNativeDeepLinks(): Promise<void> {
  if (!isNativePlatform() || started) return;
  started = true;
  try {
    await App.addListener("appUrlOpen", ({ url }) => openDeepLink(url));
    const launch = await App.getLaunchUrl();
    if (launch?.url) openDeepLink(launch.url);
  } catch {
    started = false;
  }
}
