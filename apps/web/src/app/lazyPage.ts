import { lazy, type ComponentType, type LazyExoticComponent } from "react";

const CHUNK_RELOAD_KEY = "englishai:chunk-reload";
let reloadPage = () => window.location.reload();
const CHUNK_ERROR_PATTERNS = [
  /Failed to fetch dynamically imported module/i,
  /Importing a module script failed/i,
  /error loading dynamically imported module/i,
  /Loading chunk .* failed/i,
  /ChunkLoadError/i,
];

function isChunkLoadError(error: unknown) {
  const message = error instanceof Error ? error.message : String(error);
  return CHUNK_ERROR_PATTERNS.some((pattern) => pattern.test(message));
}

async function loadWithChunkRecovery(loader: () => Promise<object>) {
  try {
    const module = await loader();
    sessionStorage.removeItem(CHUNK_RELOAD_KEY);
    return module;
  } catch (error) {
    if (
      isChunkLoadError(error) &&
      sessionStorage.getItem(CHUNK_RELOAD_KEY) !== window.location.href
    ) {
      sessionStorage.setItem(CHUNK_RELOAD_KEY, window.location.href);
      reloadPage();
      return new Promise<object>(() => undefined);
    }

    sessionStorage.removeItem(CHUNK_RELOAD_KEY);
    throw error;
  }
}

export function setChunkReloadForTests(reload: (() => void) | null) {
  reloadPage = reload ?? (() => window.location.reload());
}

export function lazyPage(
  loader: () => Promise<object>,
  exportName: string
): LazyExoticComponent<ComponentType> {
  return lazy(async () => {
    const module = await loadWithChunkRecovery(loader);
    return { default: (module as Record<string, ComponentType>)[exportName] };
  });
}
