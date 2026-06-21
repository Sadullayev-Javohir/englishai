import { motion } from "framer-motion";
import { ProgressDuo } from "./ProgressDuo";
import { Hearts, XpBits } from "./GamifiedBits";
import { useLessonSession } from "./lessonSession";
import { Icon } from "@/components/ui/Icon";

interface GlobalHudProps {
  /** 0..1 lesson progress. */
  progress: number;
  onExit: () => void;
}

/**
 * Global HUD (GlobalHud): the fixed top bar of the jungle lesson - spring-filled progress
 * bar on the left, hearts + XP pills on the right, and an "exit" ghost button. Reads hearts/XP
 * from the shared lesson-session store so they stay in sync across every step.
 */
export function GlobalHud({ progress, onExit }: GlobalHudProps) {
  const { hearts, xp } = useLessonSession();
  return (
    <div className="fixed top-0 inset-x-0 z-[62] px-md md:px-xl pt-md pb-sm">
      <div className="flex items-center gap-md">
        <button
          onClick={onExit}
          aria-label="Chiqish"
          className="shrink-0 text-white/85 hover:text-white transition-colors p-1"
        >
          <Icon name="close" className="text-[26px]" />
        </button>
        <ProgressDuo value={progress} className="flex-1" />
        <div className="flex items-center gap-sm shrink-0">
          <Hearts hearts={hearts} />
          <XpBits xp={xp} />
        </div>
      </div>
    </div>
  );
}

/** Tiny pulse used by the parrot wink in the MascotBubble. Re-exported for reuse. */
export const hudMotionProps = {
  transition: { type: "spring" as const, stiffness: 300, damping: 24 },
};

export { motion };
