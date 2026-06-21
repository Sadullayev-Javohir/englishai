// Build-time prerender entry. Vite builds this as an SSR bundle (`vite build --ssr`),
// then scripts/prerender.mjs imports `render()` and injects the resulting HTML into
// dist/index.html's #root. This gives crawlers the full marketing page in the initial
// HTML response, without a headless browser - the client still hydrates via main.tsx
// (createRoot cleanly re-renders over the server markup).
import { renderToString } from "react-dom/server";
import { StaticRouter } from "react-router-dom/server";
import { AuthContext, type AuthContextValue } from "./app/auth";
import { LandingPage } from "./pages/LandingPage";
import { PublicContentPage } from "./pages/PublicContentPage";
import { PublicSeoPage } from "./pages/PublicSeoPage";
import { getPublicPage, publicPaths as editorialPaths } from "./content/publicContent";
import publicSeoData from "./content/publicSeo.json";

// A signed-out, no-op auth value: the public landing page only reads `status`, and its
// navigate()/sign-in handlers never fire during a static render.
const guestAuth: AuthContextValue = {
  status: "unauthenticated",
  user: null,
  signInWithGoogle: async () => {
    throw new Error("Sign-in is unavailable during static prerendering.");
  },
  signOut: async () => {},
  applyUser: () => {},
};

const seoRoutes = publicSeoData.filter((route) => route.path !== "/");
export const publicPaths = [...seoRoutes.map((route) => route.path), ...editorialPaths];

export function render(path = "/"): string {
  const seoRoute = seoRoutes.some((route) => route.path === path);
  return renderToString(
    <AuthContext.Provider value={guestAuth}>
      <StaticRouter location={path}>
        {path === "/" ? <LandingPage /> : path === "/en/" ? <LandingPage locale="en" /> : seoRoute ? <PublicSeoPage /> : <PublicContentPage />}
      </StaticRouter>
    </AuthContext.Provider>,
  );
}

export function metadata(path: string) {
  const seoRoute = seoRoutes.find((route) => route.path === path);
  return seoRoute ? { ...seoRoute, published: true, seoRoute: true } : getPublicPage(path);
}
