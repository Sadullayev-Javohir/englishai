import { defineConfig, devices } from "@playwright/test";

process.env.LISTENING_MOCK_API = "http://127.0.0.1:6135";
export default defineConfig({
  testDir: "./e2e", testMatch: "listening-pen.spec.ts",
  outputDir: ".artifacts/listening-pen/results", timeout: 60_000,
  expect: { timeout: 10_000 }, workers: 1, retries: 0, reporter: "list",
  use: { baseURL: "http://127.0.0.1:5335", trace: "retain-on-failure", screenshot: "only-on-failure", reducedMotion: "reduce" },
  webServer: [
    { command: "PORT=6135 node mock-server.mjs", url: "http://127.0.0.1:6135/api/auth/config", reuseExistingServer: false, timeout: 120_000 },
    { command: "npx vite build --outDir .artifacts/listening-pen/test-site --emptyOutDir && npx vite preview --outDir .artifacts/listening-pen/test-site --host 127.0.0.1 --port 5335 --strictPort", url: "http://127.0.0.1:5335", reuseExistingServer: false, timeout: 180_000 },
  ],
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
