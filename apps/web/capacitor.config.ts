import type { CapacitorConfig } from "@capacitor/cli";

// Native Android/iOS shell configuration. The web app (Vite `dist/`) is bundled into the
// native app; all backend traffic goes to the absolute API origin baked in at build time
// via VITE_API_BASE_URL (see src/api/config.ts). No live-reload server config here: we ship
// the built assets, not a dev URL.
const config: CapacitorConfig = {
  appId: "uz.englishai.app",
  appName: "EnglishAI.uz",
  webDir: "dist",
  // Android cleartext stays off; the production API must be HTTPS.
  android: {
    allowMixedContent: false,
    backgroundColor: "#FFFFFF",
  },
  plugins: {
    App: {
      disableBackButtonHandler: true,
    },
    Keyboard: {
      resizeOnFullScreen: true,
    },
    StatusBar: {
      overlaysWebView: false,
      style: "DARK",
      backgroundColor: "#FFFFFF",
    },
    PushNotifications: {
      presentationOptions: ["badge", "sound", "alert"],
    },
  },
};

export default config;
