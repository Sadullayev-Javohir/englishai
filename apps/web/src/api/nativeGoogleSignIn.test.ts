import { beforeEach, describe, expect, it, vi } from "vitest";

const initialize = vi.fn();
const login = vi.fn();

vi.mock("@capgo/capacitor-social-login", () => ({
  SocialLogin: { initialize, login },
}));

beforeEach(() => {
  vi.clearAllMocks();
  vi.resetModules();
  initialize.mockResolvedValue(undefined);
});

describe("nativeGoogleSignIn", () => {
  it("uses the native Google account chooser and returns its ID token", async () => {
    login.mockResolvedValue({
      provider: "google",
      result: {
        responseType: "online",
        idToken: "native-google-id-token",
        accessToken: null,
        profile: { email: "a@example.com", familyName: null, givenName: null, id: "1", name: "A", imageUrl: null },
      },
    });
    const { nativeGoogleSignIn } = await import("./nativeGoogleSignIn");
    await expect(nativeGoogleSignIn("web-client-id")).resolves.toBe("native-google-id-token");
    expect(initialize).toHaveBeenCalledWith({ google: { webClientId: "web-client-id", mode: "online" } });
    expect(login).toHaveBeenCalledWith({
      provider: "google",
      options: { scopes: ["email", "profile"], style: "bottom", filterByAuthorizedAccounts: false },
    });
  });

  it("rejects a native response without an ID token", async () => {
    login.mockResolvedValue({ provider: "google", result: { responseType: "offline", serverAuthCode: "code" } });
    const { nativeGoogleSignIn } = await import("./nativeGoogleSignIn");
    await expect(nativeGoogleSignIn("web-client-id")).rejects.toMatchObject({ reason: "exchange" });
  });
});
