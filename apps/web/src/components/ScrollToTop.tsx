import { useLayoutEffect } from "react";
import { useLocation } from "react-router-dom";

/**
 * SPA navigatsiyada brauzer scroll pozitsiyasini o'zi tiklamaydi: yangi sahifa oldingi
 * sahifaning o'rtasidan yoki oxiridan ko'rinib qoladi (ayniqsa mobil/planshetdagi uzun
 * ro'yxatlarda seziladi). Har bir yo'l (pathname) o'zgarganda oynani eng yuqoriga
 * qaytaramiz - shunda har sahifa boshidan ochiladi. Query parametr (`?level=` kabi)
 * o'zgarsa scroll tiklanmaydi, chunki bog'liqlik faqat pathname'ga.
 */
export function ScrollToTop() {
  const { pathname } = useLocation();
  useLayoutEffect(() => {
    const previousRestoration = window.history.scrollRestoration;
    window.history.scrollRestoration = "manual";
    const reset = () => {
      window.scrollTo({ top: 0, left: 0, behavior: "auto" });
      document.documentElement.scrollTop = 0;
      document.body.scrollTop = 0;
    };
    reset();
    const frame = window.requestAnimationFrame(reset);
    return () => {
      window.cancelAnimationFrame(frame);
      window.history.scrollRestoration = previousRestoration;
    };
  }, [pathname]);
  return null;
}
