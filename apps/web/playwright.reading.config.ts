import { defineConfig, devices } from "@playwright/test";

// Own both servers so another route suite cannot interrupt a Reading lesson.
export default defineConfig({
  testDir: "./e2e",
  testMatch: "reading-viewport.spec.ts",
  outputDir: ".artifacts/reading-viewport/results",
  timeout: 120_000,
  expect: { timeout: 10_000 },
  workers: 1,
  retries: 0,
  reporter: "list",
  use: {
    baseURL: process.env.READING_BASE_URL ?? "http://127.0.0.1:5186",
    contextOptions: { reducedMotion: "reduce" },
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  webServer: [
    {
      command: "PORT=5075 node mock-server.mjs",
      url: "http://127.0.0.1:5075/api/auth/config",
      reuseExistingServer: false,
      timeout: 120_000,
    },
    ...(process.env.READING_BASE_URL ? [] : [{
      command: `${process.env.READING_REUSE_BUILD ? "" : "VITE_API_TARGET=http://127.0.0.1:5075 npx vite build --outDir .artifacts/reading-viewport/site --emptyOutDir && "}VITE_API_TARGET=http://127.0.0.1:5075 npx vite preview --outDir .artifacts/reading-viewport/site --host 127.0.0.1 --port 5186 --strictPort`,
      url: "http://127.0.0.1:5186",
      reuseExistingServer: false,
      timeout: 180_000,
    }]),
  ],
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
