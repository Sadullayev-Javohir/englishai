import { defineConfig, devices } from "@playwright/test";

// Dedicated servers: other route suites cannot stop the API underneath this verification.
process.env.VOCABULARY_MOCK_API = "http://127.0.0.1:5065";
process.env.VITE_API_TARGET = process.env.VOCABULARY_MOCK_API;

export default defineConfig({
  testDir: "./e2e",
  testMatch: "vocabulary-pen.spec.ts",
  outputDir: ".artifacts/vocabulary-pen/e2e-results",
  timeout: 60_000,
  expect: { timeout: 10_000 },
  workers: 1,
  retries: 0,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:5185",
    reducedMotion: "reduce",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  webServer: [
    { command: "PORT=5065 node mock-server.mjs", url: "http://127.0.0.1:5065/api/auth/config", reuseExistingServer: false, timeout: 120_000 },
    // Test an immutable production bundle: concurrent edits/HMR must not reset a lesson mid-test.
    { command: "npx vite build --outDir .artifacts/vocabulary-pen/site --emptyOutDir && npx vite preview --outDir .artifacts/vocabulary-pen/site --host 127.0.0.1 --port 5185 --strictPort", url: "http://127.0.0.1:5185", reuseExistingServer: false, timeout: 180_000 },
  ],
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"], reducedMotion: "reduce" } }],
});
