import { afterEach, describe, expect, it } from "vitest";
import { acquireRouteLoadingLock } from "./routeLoadingLock";

describe("route loading lock", () => {
  afterEach(() => {
    delete document.documentElement.dataset.routeLoading;
    delete document.documentElement.dataset.routeLoadingLockCount;
    delete document.documentElement.dataset.appBooting;
  });

  it("keeps scrolling locked until every loader releases", () => {
    document.documentElement.dataset.appBooting = "true";

    const releaseFirst = acquireRouteLoadingLock();
    const releaseSecond = acquireRouteLoadingLock();

    expect(document.documentElement.dataset.routeLoading).toBe("true");
    expect(document.documentElement.dataset.routeLoadingLockCount).toBe("2");
    expect(document.documentElement.dataset.appBooting).toBeUndefined();

    releaseFirst();

    expect(document.documentElement.dataset.routeLoading).toBe("true");
    expect(document.documentElement.dataset.routeLoadingLockCount).toBe("1");

    releaseSecond();

    expect(document.documentElement.dataset.routeLoading).toBeUndefined();
    expect(document.documentElement.dataset.routeLoadingLockCount).toBeUndefined();
  });

  it("ignores repeated releases", () => {
    const release = acquireRouteLoadingLock();

    release();
    release();

    expect(document.documentElement.dataset.routeLoading).toBeUndefined();
    expect(document.documentElement.dataset.routeLoadingLockCount).toBeUndefined();
  });
});
