import { motion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";
import { PopCard } from "./PopCard";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useAsync } from "@/lib/useAsync";
import { uz } from "@/content/uz";
import { cn } from "@/lib/cn";

export interface AchievementBadgesProps {
  className?: string;
}

export interface AchievementBadgeProps {
  id: string;
  icon: React.ReactNode;
  label: string;
  unlocked: boolean;
  className?: string;
}

function AchievementBadge({ icon, label, unlocked, className }: AchievementBadgeProps) {
  return (
    <PopCard
      className={cn(
        "w-24 h-24 flex flex-col items-center justify-center gap-1 p-2 text-center",
        !unlocked && "bg-ea-surface-soft/60 border border-ea-border/50",
        unlocked && "bg-ea-surface/95",
        className,
      )}
    >
      <motion.div
        animate={unlocked ? { scale: [1, 1.1, 1] } : {}}
        transition={{ duration: 2, repeat: Infinity, repeatType: "reverse" }}
        className={cn(
          unlocked ? "animate-pulse-ring" : "",
          "relative w-full h-full flex items-center justify-center",
        )}
      >
        {icon}
        {!unlocked && (
          <Icon
            name="lock"
            filled
            className="absolute -top-1 -right-1 text-ea-muted text-[16px] bg-ea-surface rounded-full p-0.5"
          />
        )}
      </motion.div>
      <span className="font-caption text-caption font-bold text-text-primary truncate w-full px-1">
        {label}
      </span>
    </PopCard>
  );
}

export function AchievementBadges({ className }: AchievementBadgesProps) {
  const learnerId = getLearnerId();

  const { data: status } = useAsync(() => api.gamification.status(learnerId), [learnerId]);
  const { data: overview } = useAsync(() => api.learning.overview(learnerId), [learnerId]);
  const { data: vocabulary } = useAsync(() => api.vocabulary.list(learnerId), [learnerId]);

  const wordCount = vocabulary?.length ?? 0;

  const skillsCompletedToday = status?.skillsCompletedToday ?? [];

  const achievements = [
    {
      id: "first-lesson",
      icon: <Icon name="trophy" filled className="text-[24px] text-ea-orange-600" />,
      label: uz.trophy.achievementFirstLesson,
      unlocked: skillsCompletedToday.includes("FirstLesson"),
    },
    {
      id: "week-streak",
      icon: <Icon name="local_fire_department" filled className="text-[24px] text-ea-orange-600" />,
      label: uz.trophy.achievementWeekStreak,
      unlocked: (status?.currentStreak ?? 0) >= 7,
    },
    {
      id: "month-streak",
      icon: <Icon name="local_fire_department" filled className="text-[24px] text-ea-orange-600" />,
      label: uz.trophy.achievementMonthStreak,
      unlocked: (status?.currentStreak ?? 0) >= 30,
    },
    {
      id: "level",
      icon: <span className="text-[24px] font-duo font-extrabold text-ea-primary">{overview?.overallLevel ?? 1}</span>,
      label: uz.trophy.achievementLevel(overview?.overallLevel ?? 1),
      unlocked: true,
    },
    {
      id: "vocab",
      icon: <Icon name="translate" filled className="text-[24px] text-ea-primary" />,
      label: uz.trophy.achievementVocab(wordCount),
      unlocked: wordCount >= 10,
    },
    {
      id: "skills",
      icon: <Icon name="insights" filled className="text-[24px] text-ea-green-600" />,
      label: uz.trophy.achievementSkills,
      unlocked: (overview?.skillScores ?? []).every((s) => s.score >= 50),
    },
    {
      id: "topics",
      icon: <Icon name="book" filled className="text-[24px] text-ea-primary" />,
      label: uz.trophy.achievementTopics,
      unlocked: true,
    },
    {
      id: "mastered",
      icon: <Icon name="check_circle" filled className="text-[24px] text-ea-green-600" />,
      label: uz.trophy.achievementMastered,
      unlocked: true,
    },
  ];

  return (
    <div className={cn("flex gap-3 overflow-x-auto pb-2 -mx-4 px-4", className)}
      style={{ WebkitOverflowScrolling: "touch" }}
    >
      {achievements.map((a) => (
        <AchievementBadge key={a.id} {...a} />
      ))}
    </div>
  );
}
