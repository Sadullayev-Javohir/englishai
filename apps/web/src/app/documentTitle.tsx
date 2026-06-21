import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useLocation } from "react-router-dom";
import { uz } from "@/content/uz";
import { getRouteMetadata } from "./routeMetadata";

const BRAND = uz.brand;

interface DocumentTitleContextValue {
  setOverride: (title: string | null) => void;
}

const DocumentTitleContext = createContext<DocumentTitleContextValue | null>(null);

export function formatDocumentTitle(title?: string | null, section?: string): string {
  const cleanTitle = title?.trim();
  const cleanSection = section?.trim();
  const parts = [cleanTitle, cleanSection].filter(
    (part, index, values): part is string => Boolean(part) && values.indexOf(part) === index && part !== BRAND,
  );
  return parts.length ? `${parts.join(" — ")} | ${BRAND}` : BRAND;
}

export function DocumentTitleProvider({ children }: { children: ReactNode }) {
  const { pathname } = useLocation();
  const [override, setOverride] = useState<string | null>(null);
  const previousPathname = useRef(pathname);
  const routeTitle = getRouteMetadata(pathname).documentTitle;
  const updateOverride = useCallback((title: string | null) => setOverride(title), []);
  const value = useMemo(() => ({ setOverride: updateOverride }), [updateOverride]);

  if (previousPathname.current !== pathname) {
    previousPathname.current = pathname;
    setOverride(null);
    document.title = formatDocumentTitle(routeTitle);
  }

  useEffect(() => {
    document.title = formatDocumentTitle(override ?? routeTitle);
  }, [override, routeTitle]);

  return <DocumentTitleContext.Provider value={value}>{children}</DocumentTitleContext.Provider>;
}

export function useDocumentTitle(title?: string | null, section?: string): void {
  const context = useContext(DocumentTitleContext);
  const formattedOverride = title?.trim()
    ? [title.trim(), section?.trim()].filter(Boolean).join(" — ")
    : null;

  useEffect(() => {
    if (!context || !formattedOverride) return;
    context.setOverride(formattedOverride);
    return () => context.setOverride(null);
  }, [context, formattedOverride]);
}
