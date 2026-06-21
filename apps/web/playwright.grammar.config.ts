import { defineConfig, devices } from "@playwright/test";

// Keep fixture traffic and builds isolated from the user's running application.
export default defineConfig({
  testDir: "./e2e",
  testMatch: "grammar-viewport.spec.ts",
  outputDir: ".artifacts/grammar-viewport/results",
  timeout: 60_000,
  expect: { timeout: 10_000 },
  workers: 1,
  retries: 0,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:5191",
    reducedMotion: "reduce",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  webServer: [
    { command: "PORT=5091 node mock-server.mjs", url: "http://127.0.0.1:5091/api/auth/config", reuseExistingServer: false, timeout: 120_000 },
    {
      // Opt in only for QA-only reruns against the unchanged, already-built snapshot.
      command: `${process.env.GRAMMAR_REUSE_BUILD === "1" ? "" : "npx vite build --outDir .artifacts/grammar-viewport/site --emptyOutDir && "}npx vite preview --outDir .artifacts/grammar-viewport/site --host 127.0.0.1 --port 5191 --strictPort`,
      url: "http://127.0.0.1:5191",
      reuseExistingServer: false,
      timeout: 180_000,
    },
  ],
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
