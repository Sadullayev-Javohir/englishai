/** EnglishAI Play has one approved, light-only palette. */
export function initializeThemeRuntime(): void {
  if (typeof document === "undefined") return;
  const root = document.documentElement;
  root.classList.remove("dark");
  root.dataset.theme = "light";
  root.dataset.themeMode = "light";
  root.style.colorScheme = "light";
  document.querySelector('meta[name="theme-color"]')?.setAttribute("content", "#FFFCF7");
  document.querySelector('meta[name="color-scheme"]')?.setAttribute("content", "light");
  try { localStorage.removeItem("englishai-theme"); } catch { /* Storage can be unavailable. */ }
}

// Retains chart consumers' typed palette interface without a theme store or listeners.
export function useTheme(): { effective: "light" | "dark" } {
  return { effective: "light" };
}
