import { beforeEach, expect, it } from "vitest";
import { initializeThemeRuntime, useTheme } from "./theme";
beforeEach(() => { localStorage.clear(); document.documentElement.className = ""; });
it.each(["dark", "system", "light"])("retires the stored %s preference before rendering Play", (old) => {
  localStorage.setItem("englishai-theme", old);
  document.documentElement.classList.add("dark");
  initializeThemeRuntime();
  expect(document.documentElement.dataset.theme).toBe("light");
  expect(document.documentElement.style.colorScheme).toBe("light");
  expect(document.documentElement.classList.contains("dark")).toBe(false);
  expect(localStorage.getItem("englishai-theme")).toBeNull();
  expect(useTheme().effective).toBe("light");
});
