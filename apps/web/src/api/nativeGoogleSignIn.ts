import { SocialLogin } from "@capgo/capacitor-social-login";

export class NativeGoogleSignInError extends Error {
  constructor(public readonly reason: "cancelled" | "timeout" | "state" | "exchange" | "unavailable") {
    super(`Native Google sign-in failed: ${reason}`);
  }
}

let initializedClientId = "";

function isCancellation(error: unknown): boolean {
  const message = error instanceof Error ? error.message : String(error);
  return /cancel|canceled|cancelled|dismiss|12501/i.test(message);
}

/** Opens Android Credential Manager's native Google account chooser and returns its ID token. */
export async function nativeGoogleSignIn(webClientId: string): Promise<string> {
  if (!webClientId) throw new NativeGoogleSignInError("unavailable");

  try {
    if (initializedClientId !== webClientId) {
      await SocialLogin.initialize({ google: { webClientId, mode: "online" } });
      initializedClientId = webClientId;
    }

    const response = await SocialLogin.login({
      provider: "google",
      options: {
        scopes: ["email", "profile"],
        style: "bottom",
        // A first sign-in must show every Google account available on the phone.
        filterByAuthorizedAccounts: false,
      },
    });

    if (response.result.responseType !== "online" || !response.result.idToken) {
      throw new NativeGoogleSignInError("exchange");
    }
    return response.result.idToken;
  } catch (error) {
    if (error instanceof NativeGoogleSignInError) throw error;
    throw new NativeGoogleSignInError(isCancellation(error) ? "cancelled" : "unavailable");
  }
}