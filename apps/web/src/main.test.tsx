import { beforeEach, describe, expect, it, vi } from "vitest";

const render = vi.fn();

vi.mock("react-dom/client", () => ({ createRoot: () => ({ render }) }));
vi.mock("./app/router", () => ({ router: {} }));
vi.mock("./app/auth", () => ({ AuthProvider: ({ children }: { children: React.ReactNode }) => children }));
vi.mock("./api/nativeUi", () => ({ initNativeUi: vi.fn() }));
vi.mock("./lib/referral", () => ({ captureReferralFromUrl: vi.fn() }));
vi.mock("./components/ErrorBoundary", () => ({ ErrorBoundary: ({ children }: { children: React.ReactNode }) => children }));
vi.mock("./components/ui/Spinner", () => ({ Spinner: () => null }));
vi.mock("react-router-dom", () => ({ RouterProvider: () => null }));

beforeEach(() => {
  vi.resetModules();
  document.body.innerHTML = '<div id="root"></div>';
});

describe("application bootstrap", () => {
  it("mounts without a global splash loader", async () => {
    await import("./main");
    expect(render).toHaveBeenCalled();
    expect(document.getElementById("splash-loader")).toBeNull();
  });
});
