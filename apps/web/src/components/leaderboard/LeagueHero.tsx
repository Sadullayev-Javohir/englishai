import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { DesignCard } from "@/components/design";
import { leagueProgressForXp } from "./useLeagueTier";

export function LeagueHero({ lifetimeXp, rank }: { lifetimeXp: number; rank: number | undefined }) {
  const progress = leagueProgressForXp(lifetimeXp);
  const meta = progress.current;

  return (
    <motion.div initial={{ opacity: 0, y: 18 }} animate={{ opacity: 1, y: 0 }} transition={{ type: "spring", stiffness: 220, damping: 22 }} className="leaderboard-page__league-hero relative min-w-0">
      <div className="flex min-w-0 items-center gap-3 sm:gap-4">
        <div className="grid h-16 w-16 shrink-0 place-items-center rounded-[20px] border border-ea-primary/25 bg-ea-primary/10 sm:h-20 sm:w-20 sm:rounded-[24px]" aria-hidden>
          <Icon name={meta.icon} filled className="text-[34px] text-ea-primary sm:text-[42px]" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="font-duo text-[12px] font-extrabold uppercase tracking-[0.14em] text-ea-purple-600">{uz.league.title}</p>
          <div className="mt-1 flex flex-wrap items-baseline gap-x-2 gap-y-1">
            <h2 className="font-duo text-[21px] font-extrabold leading-tight text-ea-text sm:text-[25px]">{meta.label}</h2>
            <span className="font-duo text-sm font-extrabold text-ea-purple-800">{progress.xp.toLocaleString("uz-UZ")} XP</span>
          </div>
          {rank != null && <p className="mt-1 font-caption text-[12px] text-ea-muted">{uz.leaderboard.overallRank(rank)}</p>}
        </div>
      </div>

      <DesignCard padding="sm" className="leaderboard-page__league-progress mt-4 sm:mt-5">
        <div className="flex min-w-0 items-center justify-between gap-4 font-duo text-[10px] font-extrabold uppercase tracking-[0.06em] text-ea-muted-deep sm:text-[11px] sm:tracking-[0.09em]">
          <span className="min-w-0 truncate">{progress.current.label}</span>
          <span className="min-w-0 truncate text-right">{progress.next?.label ?? "Eng yuqori liga"}</span>
        </div>
        <div className="mt-3 h-3 overflow-hidden rounded-full bg-ea-border" role="progressbar" aria-label="Liga jarayoni" aria-valuemin={0} aria-valuemax={100} aria-valuenow={progress.progressPercent}>
          <motion.div initial={{ width: 0 }} animate={{ width: `${progress.progressPercent}%` }} transition={{ duration: 0.7, ease: "easeOut" }} className="h-full rounded-full bg-ea-primary" />
        </div>
        <p className="mt-3 font-caption text-[12px] font-semibold leading-5 text-ea-muted">
          {progress.next
            ? `${progress.next.label} ligasi uchun yana ${progress.xpToNext.toLocaleString("uz-UZ")} XP kerak.`
            : "Siz eng yuqori Olmos ligasidasiz."}
        </p>
      </DesignCard>
    </motion.div>
  );
}
