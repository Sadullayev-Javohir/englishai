module.exports = {
  root: true,
  env: { browser: true, es2020: true },
  extends: [
    "eslint:recommended",
    "plugin:@typescript-eslint/recommended",
    "plugin:react-hooks/recommended",
  ],
  // vite.config.{js,d.ts} are tsc -b build artifacts of vite.config.ts (composite project).
  ignorePatterns: ["dist", ".artifacts", "android/**/build", "android/app/src/main/assets", "hermes-webui", ".eslintrc.cjs", "vite.config.ts", "vite.config.js", "vite.config.d.ts"],
  parser: "@typescript-eslint/parser",
  plugins: ["react-refresh"],
  rules: {
    "react-refresh/only-export-components": "off",
  },
};
