import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const inventoryApiOrigin = env.VITE_INVENTORY_API_ORIGIN ?? "http://localhost:62950";
  const inventoryApiProxy = {
    target: inventoryApiOrigin,
    changeOrigin: true,
    secure: false
  };

  return {
    plugins: [react()],
    server: {
      port: 5173,
      host: "0.0.0.0",
      proxy: {
        "/api": inventoryApiProxy
      }
    },
    preview: {
      port: 4173,
      host: "0.0.0.0",
      proxy: {
        "/api": inventoryApiProxy
      }
    }
  };
});
