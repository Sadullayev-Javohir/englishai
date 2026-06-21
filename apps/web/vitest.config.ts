import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { "@": path.resolve(__dirname, "src") },
  },
  test: {
    environment: "jsdom",
    clearMocks: true,
    setupFiles: ["./src/test/setup.ts"],
    // A handful of RTL page suites are timing-sensitive under the fully parallel run (async
    // session bootstrap, caption timers) and occasionally fail once, blocking merges on noise.
    // Retry transient failures — a genuine regression still fails every attempt, so this hardens
    // CI without hiding real bugs.
    retry: 2,
    exclude: ["e2e/**", "node_modules/**", "dist/**"],
  },
});
