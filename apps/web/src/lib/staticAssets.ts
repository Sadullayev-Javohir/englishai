const publicBaseUrl = (import.meta.env.VITE_PUBLIC_ASSET_BASE_URL as string | undefined)?.replace(/\/$/, "");
const version = (import.meta.env.VITE_PUBLIC_ASSET_VERSION as string | undefined)?.replace(/^\/+|\/+$/g, "");

export function staticAsset(path: string) {
  const normalized = path.startsWith("/") ? path : `/${path}`;
  if (!publicBaseUrl || !version) return normalized;
  return `${publicBaseUrl}/${version}${normalized}`;
}
