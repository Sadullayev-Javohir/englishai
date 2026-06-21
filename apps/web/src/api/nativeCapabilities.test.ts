import { beforeEach, describe, expect, it, vi } from "vitest";

const isNativePlatform = vi.fn();

vi.mock("./nativeAuth", () => ({ isNativePlatform }));

describe("nativeCapabilities", () => {
  beforeEach(() => isNativePlatform.mockReset());

  it("keeps video and payments available on web", async () => {
    isNativePlatform.mockReturnValue(false);
    const { getNativeCapabilities, nativeRouteAllowed } = await import("./nativeCapabilities");
    expect(getNativeCapabilities()).toMatchObject({ video: true, payments: true, premiumAccess: false });
    expect(nativeRouteAllowed("/video")).toBe(true);
    expect(nativeRouteAllowed("/pricing")).toBe(true);
  });

  it("removes video and payments from the native app", async () => {
    isNativePlatform.mockReturnValue(true);
    const { getNativeCapabilities, nativeRouteAllowed } = await import("./nativeCapabilities");
    expect(getNativeCapabilities()).toMatchObject({ video: false, payments: false, premiumAccess: true });
    expect(nativeRouteAllowed("/video/lesson/play")).toBe(false);
    expect(nativeRouteAllowed("/pricing")).toBe(false);
    expect(nativeRouteAllowed("/home")).toBe(true);
  });
});
