import { defineConfig, devices } from "@playwright/test";

const ci = Boolean(process.env.CI);

export default defineConfig({
  testDir: "./e2e",
  outputDir: ".artifacts/playwright-results",
  timeout: 30_000,
  expect: { timeout: 8_000 },
  fullyParallel: false,
  workers: ci ? 2 : 1,
  retries: ci ? 1 : 0,
  reporter: ci ? [["github"], ["html", { outputFolder: ".artifacts/playwright-report", open: "never" }]] : "list",
  use: {
    baseURL: process.env.PW_BASE_URL ?? "http://127.0.0.1:5173",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    reducedMotion: "reduce",
  },
  webServer: [
    ...(process.env.PLACEMENT_LIVE_ACCOUNT ? [] : [{
      command: "node mock-server.mjs",
      url: "http://127.0.0.1:5055/api/auth/config",
      reuseExistingServer: !ci,
      timeout: 120_000,
    }]),
    ...(process.env.PW_BASE_URL ? [] : [{
      command: "VITE_API_TARGET=http://127.0.0.1:5055 npm run dev -- --host 127.0.0.1",
      url: "http://127.0.0.1:5173",
      reuseExistingServer: !ci,
      timeout: 120_000,
    }]),
  ],
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    ...(process.env.PW_CROSS_BROWSER === "1"
      ? [
          { name: "firefox", use: { ...devices["Desktop Firefox"] } },
          { name: "webkit", use: { ...devices["Desktop Safari"] } },
        ]
      : []),
  ],
});
