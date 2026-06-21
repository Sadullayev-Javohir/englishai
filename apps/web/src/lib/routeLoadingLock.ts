const ROUTE_LOADING_LOCK_COUNT = "routeLoadingLockCount";

function readLockCount(root: HTMLElement) {
  const count = Number.parseInt(root.dataset[ROUTE_LOADING_LOCK_COUNT] ?? "0", 10);
  return Number.isFinite(count) ? count : 0;
}

export function acquireRouteLoadingLock() {
  const root = document.documentElement;
  const nextCount = readLockCount(root) + 1;

  root.dataset[ROUTE_LOADING_LOCK_COUNT] = String(nextCount);
  root.dataset.routeLoading = "true";
  delete root.dataset.appBooting;

  let released = false;

  return () => {
    if (released) return;
    released = true;

    const remainingCount = Math.max(0, readLockCount(root) - 1);

    if (remainingCount > 0) {
      root.dataset[ROUTE_LOADING_LOCK_COUNT] = String(remainingCount);
      return;
    }

    delete root.dataset[ROUTE_LOADING_LOCK_COUNT];
    delete root.dataset.routeLoading;
  };
}
