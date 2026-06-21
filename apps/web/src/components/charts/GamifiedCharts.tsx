import { PopCard } from "@/components/game/PopCard";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import {
  GrowthAreaChart,
  HorizontalBarChart,
  SkillRadarChart,
  StudyTimeBarChart,
} from "./ProgressCharts";

export interface GamifiedChartsProps {
  children?: React.ReactNode;
  className?: string;
}

function Caption({ caption }: { caption: string }) {
  return (
    <div className="flex items-center gap-2 text-text-secondary font-caption text-caption mt-2">
      <Icon name="auto_awesome" filled className="text-ea-orange-600 text-[16px]" />
      <span>{caption}</span>
    </div>
  );
}

export function GamifiedCharts({ children, className }: GamifiedChartsProps) {
  return <div className={className}>{children}</div>;
}

// Wrapped chart components

export function StudyTimeBarChart7({ labels, fullLabels, seconds }: {
  labels: string[];
  fullLabels: string[];
  seconds: number[];
}) {
  return (
    <PopCard className="p-4">
      <StudyTimeBarChart labels={labels} fullLabels={fullLabels} seconds={seconds} formatValue={(s) => `${s / 60} min`} />
      <Caption caption={uz.trophy.capStudy7} />
    </PopCard>
  );
}

export function StudyTimeBarChart12({ labels, fullLabels, seconds }: {
  labels: string[];
  fullLabels: string[];
  seconds: number[];
}) {
  return (
    <PopCard className="p-4">
      <StudyTimeBarChart labels={labels} fullLabels={fullLabels} seconds={seconds} formatValue={(s) => `${s / 60} min`} />
      <Caption caption={uz.trophy.capStudy12} />
    </PopCard>
  );
}

export function HorizontalBarChartBySkill({ labels, values }: { labels: string[]; values: number[] }) {
  return (
    <PopCard className="p-4">
      <HorizontalBarChart labels={labels} values={values} formatValue={(s) => `${s / 60} min`} />
      <Caption caption={uz.trophy.capSkillTime} />
    </PopCard>
  );
}

export function SkillRadar({ labels, scores }: { labels: string[]; scores: number[] }) {
  return (
    <PopCard className="p-4">
      <SkillRadarChart labels={labels} scores={scores} />
      <Caption caption={uz.trophy.capRadar} />
    </PopCard>
  );
}

export function GrowthArea({ labels, values }: { labels: string[]; values: number[] }) {
  return (
    <PopCard className="p-4">
      <GrowthAreaChart labels={labels} values={values} formatValue={(v) => `${Math.round(v)}`} />
      <Caption caption={uz.trophy.capTrend} />
    </PopCard>
  );
}

export function HorizontalBarChartErrors({ labels, values }: { labels: string[]; values: number[] }) {
  return (
    <PopCard className="p-4">
      <HorizontalBarChart labels={labels} values={values} formatValue={(v) => `${v}`} color="#B85B35" />
      <Caption caption={uz.trophy.capErrors} />
    </PopCard>
  );
}

export function VocabularySize({ value }: { value: number }) {
  return (
    <PopCard className="p-4 flex items-center justify-center">
      <div className="text-center">
        <span className="block text-[56px] font-bold text-text-primary leading-none">{value}</span>
        <Caption caption={uz.trophy.capVocab} />
      </div>
    </PopCard>
  );
}
