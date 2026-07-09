import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  resolve: {
    // Deep three/examples imports must reuse the same three instance.
    dedupe: ["three"],
  },
});
