import { describe, expect, it, vi } from "vitest";
import { navigateFromProgress, resolveLearningRoute, routeReturnTarget } from "./routeReturn";

describe("routeReturn", () => {
  it("resolves only the supported progress return target", () => {
    expect(routeReturnTarget({ state: { returnTo: "/progress" } })).toBe("/progress");
    expect(routeReturnTarget({ state: { returnTo: "/admin" } })).toBe("/home");
    expect(routeReturnTarget({ state: null }, "/app/speaking")).toBe("/app/speaking");
  });

  it("navigates with progress as the return target", () => {
    const navigate = vi.fn();

    navigateFromProgress(navigate, "/app/grammar");

    expect(navigate).toHaveBeenCalledWith("/app/grammar", { state: { returnTo: "/progress" } });
  });

  it("maps legacy public learning paths to their real in-app page", () => {
    expect(resolveLearningRoute("/vocabulary")).toBe("/app/vocabulary/topics");
    expect(resolveLearningRoute("/vocabulary/")).toBe("/app/vocabulary/topics");
    expect(resolveLearningRoute("/grammar")).toBe("/app/grammar");
    expect(resolveLearningRoute("/speaking")).toBe("/app/speaking");
  });

  it("leaves already-correct app routes untouched", () => {
    expect(resolveLearningRoute("/app/vocabulary/review")).toBe("/app/vocabulary/review");
    expect(resolveLearningRoute("/listening")).toBe("/listening");
    expect(resolveLearningRoute("")).toBe("/home");
  });

  it("routes a legacy target through navigation", () => {
    const navigate = vi.fn();

    navigateFromProgress(navigate, "/vocabulary");

    expect(navigate).toHaveBeenCalledWith("/app/vocabulary/topics", { state: { returnTo: "/progress" } });
  });
});
