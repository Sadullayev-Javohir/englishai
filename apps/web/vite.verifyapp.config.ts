import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig({
  plugins: [react()],
  resolve: { alias: { "@": path.resolve(__dirname, "src") } },
  build: {
    outDir: ".verifydist",
    logLevel: "error",
    rollupOptions: {
      input: path.resolve(__dirname, "verify-index.html"),
    },
  },
});
