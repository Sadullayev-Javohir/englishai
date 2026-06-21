import { useLayoutEffect } from "react";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { LiveTutorPending } from "@/components/LiveTutorPending";

export function AppPending() {
  useLayoutEffect(() => {
    delete document.documentElement.dataset.appBooting;
  }, []);
  if (window.location.pathname === "/home") return null;
  if (window.location.pathname.startsWith("/app/speaking/live-tutor/")) return <LiveTutorPending />;
  return <LoadingSkeleton variant="page" className="ea-app-pending" />;
}
