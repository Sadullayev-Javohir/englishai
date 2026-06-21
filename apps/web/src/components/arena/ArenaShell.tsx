import { cn } from "@/lib/cn";

/**
 * Shared chrome for the two speaking surfaces (/speaking, /speaking/role-talk). The global looping background.mp4 (mounted once in main.tsx,
 * fixed z-0) shows through - we only layer a transparent scrim + gradient so the
 * video stays alive behind the colored 3D cards, matching the Home dashboard look.
 * The HUD is NOT rendered here (it lives in SpeakingHud) - this is purely chrome.
 */
export function ArenaShell({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className="relative min-h-full w-full overflow-x-hidden bg-transparent">
      {/* No scrim/glass overlay over the global background.mp4 - the video stays
          fully clear behind the colored 3D cards. */}
      <div
        className={cn(
          "relative mx-auto w-full max-w-[960px] px-3 sm:px-4",
          className,
        )}
      >
        {children}
      </div>
    </div>
  );
}
