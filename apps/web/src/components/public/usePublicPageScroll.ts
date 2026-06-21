import { useEffect } from "react";
import { useLocation } from "react-router-dom";

/** Public routes sit outside the app shell and its ScrollToTop component. */
export function usePublicPageScroll() {
  const { pathname, hash } = useLocation();
  useEffect(() => {
    const frame = window.requestAnimationFrame(() => {
      const target = hash ? document.getElementById(hash.slice(1)) : null;
      window.scrollTo({
        top: target ? target.getBoundingClientRect().top + window.scrollY - 24 : 0,
        behavior: "auto",
      });
    });
    return () => window.cancelAnimationFrame(frame);
  }, [hash, pathname]);
}
