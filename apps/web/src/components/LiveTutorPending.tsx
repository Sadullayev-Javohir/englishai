import { useLayoutEffect } from "react";
import { Icon } from "@/components/ui/Icon";
import { acquireRouteLoadingLock } from "@/lib/routeLoadingLock";
import "@/pages/LiveAccentTutorPage.css";

export function LiveTutorPending() {
  useLayoutEffect(() => acquireRouteLoadingLock(), []);
  return (
    <div className="live-room__starting" role="status" aria-label="Tayyorlanmoqda..." aria-busy="true">
      <Icon name="progress_activity" />
      <span>Tayyorlanmoqda...</span>
    </div>
  );
}
