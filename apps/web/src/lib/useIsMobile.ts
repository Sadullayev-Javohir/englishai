import { useEffect, useState } from "react";

/**
 * True while the viewport is in the mobile shell range (<768px, matching the Tailwind `md`
 * breakpoint and the native-app shell). Use this only where a size must be a JS number that CSS
 * media queries can't reach (e.g. the ProgressRing `size` prop); prefer Tailwind `md:` utilities
 * for everything stylable in CSS. Re-renders on viewport changes (rotation / window resize).
 */
const MOBILE_QUERY = "(max-width: 767px)";

export function useIsMobile(): boolean {
  const [isMobile, setIsMobile] = useState(
    () => typeof window !== "undefined" && window.matchMedia(MOBILE_QUERY).matches,
  );

  useEffect(() => {
    const mql = window.matchMedia(MOBILE_QUERY);
    const onChange = () => setIsMobile(mql.matches);
    mql.addEventListener("change", onChange);
    return () => mql.removeEventListener("change", onChange);
  }, []);

  return isMobile;
}
