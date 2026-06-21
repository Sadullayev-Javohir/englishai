import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// In dev the SPA is served by Vite and the .NET backend runs separately. To avoid
// CORS entirely, Vite proxies /api and /hubs to the backend (default 5045, override with
// VITE_API_TARGET). The web build then uses same-origin (relative) requests.
const API_TARGET = process.env.VITE_API_TARGET ?? "http://localhost:5045";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { "@": path.resolve(__dirname, "src") },
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      "/api": { target: API_TARGET, changeOrigin: true },
      "/hubs": { target: API_TARGET, changeOrigin: true, ws: true },
    },
  },
});
