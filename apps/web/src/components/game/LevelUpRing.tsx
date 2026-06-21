import { motion } from "framer-motion";
import { DuoButton } from "./DuoButton";
import { ProgressRing } from "@/components/ui/ProgressRing";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { cn } from "@/lib/cn";
import { uz } from "@/content/uz";

export interface LevelUpRingProps {
  className?: string;
}

export function LevelUpRing({ className }: LevelUpRingProps) {
  const navigate = useNavigate();
  const learnerId = getLearnerId();

  const { data: points } = useAsync(() => api.gamification.points(learnerId), [learnerId]);

  const lifetimeXp = points?.lifetimeXp ?? 0;

  // XP thresholds per level (A1=1, A2=2, B1=3, B2=4, C1=5, C2=6)
  const thresholds: Record<number, number> = {
    1: 1000,
    2: 2500,
    3: 4500,
    4: 7000,
    5: 10000,
    6: 15000,
  };

  const currentLevel = Object.keys(thresholds)
    .map(Number)
    .sort((a, b) => a - b)
    .reduce((level, lv) => (lifetimeXp >= thresholds[lv] ? lv + 1 : level), 1);

  const nextLevel = Math.min(currentLevel + 1, 6);
  const nextThreshold = thresholds[nextLevel];
  const progress = nextThreshold > 0 ? (lifetimeXp % nextThreshold) / nextThreshold : 0;

  const delta = nextThreshold - (lifetimeXp % nextThreshold);

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ type: "spring", stiffness: 300, damping: 20 }}
      className={cn("flex flex-col items-center gap-4", className)}
    >
      <div className="text-center">
        <h3 className="font-label-md text-label-md font-bold text-text-primary mb-1">
          {uz.trophy.nextLevelTitle}
        </h3>
        <p className="font-caption text-caption text-text-secondary mb-4">
          {uz.trophy.nextLevelHint(delta)}
        </p>
      </div>

      <ProgressRing value={progress} size={120} strokeWidth={10}>
        <div className="text-center">
          <span className="block text-[20px] font-bold text-text-primary">
            {lifetimeXp % nextThreshold} / {nextThreshold}
          </span>
        </div>
      </ProgressRing>

      <DuoButton color="green" onClick={() => navigate("/home")}>
        {uz.trophy.practiceCta}
      </DuoButton>
    </motion.div>
  );
}
