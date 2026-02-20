import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#14120f",
        sand: "#f4ece2",
        rust: "#c84f2f",
        teal: "#2f7d79",
        clay: "#e0c6a8",
      },
      fontFamily: {
        sans: ["Space Grotesk", "Avenir Next", "Trebuchet MS", "sans-serif"],
        mono: ["IBM Plex Mono", "Menlo", "Consolas", "monospace"],
      },
      boxShadow: {
        panel: "0 18px 40px rgba(20, 18, 15, 0.16)",
      },
    },
  },
  plugins: [],
} satisfies Config;
