import { useMemo } from "react";
import { Bar, Line, Radar } from "react-chartjs-2";
import type { ChartOptions } from "chart.js";
import { useTheme } from "@/lib/theme";
import {
  chart,
  darkChart,
  ensureChartsRegistered,
  horizontalBarOptions,
  radarOptions,
  timeSeriesOptions,
  verticalFill,
  withAlpha,
} from "@/lib/charts";

ensureChartsRegistered();

function useChartPalette() {
  const { effective } = useTheme();
  return effective === "dark" ? darkChart : chart;
}

interface AccessibleChartProps {
  summary?: string;
}

function AccessibleDataList({ labels, values, formatValue, summary }: {
  labels: string[];
  values: number[];
  formatValue: (value: number) => string;
  summary?: string;
}) {
  return (
    <div className="sr-only" role="img" aria-label={summary || "Diagramma ma'lumotlari"}>
      {summary && <p>{summary}</p>}
      <ul>
        {labels.map((label, index) => (
          <li key={`${label}-${index}`}>{label}: {formatValue(values[index] ?? 0)}</li>
        ))}
      </ul>
    </div>
  );
}

/** Clean, rounded Chart.js bars for study time. */
export function StudyTimeBarChart({
  labels,
  seconds,
  fullLabels,
  formatValue,
  color,
  summary,
}: {
  labels: string[];
  seconds: number[];
  fullLabels: string[];
  formatValue: (seconds: number) => string;
  color?: string;
} & AccessibleChartProps) {
  const palette = useChartPalette();
  const resolvedColor = color ?? palette.primary;
  const data = useMemo(
    () => ({
      labels,
      datasets: [{
        data: seconds,
        backgroundColor: withAlpha(resolvedColor, 0.86),
        hoverBackgroundColor: resolvedColor,
        borderRadius: 8,
        borderSkipped: false as const,
        maxBarThickness: 34,
        minBarLength: 3,
      }],
    }),
    [labels, seconds, resolvedColor],
  );

  const options = useMemo(() => {
    const base = timeSeriesOptions(formatValue, palette) as ChartOptions<"bar">;
    return {
      ...base,
      plugins: {
        ...base.plugins,
        tooltip: {
          ...base.plugins?.tooltip,
          callbacks: {
            title: (items) => fullLabels[items[0]?.dataIndex ?? 0] ?? "",
            label: (item) => formatValue(item.parsed.y ?? 0),
          },
        },
      },
    } as ChartOptions<"bar">;
  }, [formatValue, fullLabels, palette]);

  return (
    <>
      <div className="max-w-full overflow-x-auto pb-1" aria-hidden="true">
        <div className="h-48 min-w-[420px] md:h-60 md:min-w-0">
          <Bar data={data} options={options} />
        </div>
      </div>
      <AccessibleDataList labels={fullLabels} values={seconds} formatValue={formatValue} summary={summary} />
    </>
  );
}

/** Horizontal Chart.js bars for ranked categories. */
export function HorizontalBarChart({
  labels,
  values,
  formatValue,
  color,
  summary,
}: {
  labels: string[];
  values: number[];
  formatValue: (value: number) => string;
  color?: string;
} & AccessibleChartProps) {
  const palette = useChartPalette();
  const resolvedColor = color ?? palette.primary;
  const data = useMemo(
    () => ({
      labels,
      datasets: [{
        data: values,
        backgroundColor: withAlpha(resolvedColor, 0.84),
        hoverBackgroundColor: resolvedColor,
        borderRadius: 8,
        borderSkipped: false as const,
        maxBarThickness: 26,
        minBarLength: 3,
      }],
    }),
    [labels, values, resolvedColor],
  );

  const options = useMemo(() => horizontalBarOptions(formatValue, palette), [formatValue, palette]);
  const height = Math.max(150, labels.length * 42);

  return (
    <>
      <div className="max-w-full overflow-x-auto pb-1" aria-hidden="true">
        <div className="min-w-[420px] sm:min-w-0" style={{ height }}>
          <Bar data={data} options={options} />
        </div>
      </div>
      <AccessibleDataList labels={labels} values={values} formatValue={formatValue} summary={summary} />
    </>
  );
}

/** Radar of the six skill scores, fixed to a 0–100 scale. */
export function SkillRadarChart({
  labels,
  scores,
  color,
  summary,
}: {
  labels: string[];
  scores: number[];
  color?: string;
} & AccessibleChartProps) {
  const palette = useChartPalette();
  const resolvedColor = color ?? palette.primary;
  const data = useMemo(
    () => ({
      labels,
      datasets: [{
        data: scores,
        backgroundColor: withAlpha(resolvedColor, 0.14),
        borderColor: resolvedColor,
        borderWidth: 2.5,
        pointBackgroundColor: palette.surface,
        pointBorderColor: resolvedColor,
        pointBorderWidth: 2,
        pointRadius: 3.5,
        pointHoverRadius: 5,
        pointHoverBackgroundColor: resolvedColor,
      }],
    }),
    [labels, scores, resolvedColor, palette.surface],
  );

  const options = useMemo(() => radarOptions(palette), [palette]);
  return (
    <>
      <div className="h-56 max-w-full sm:h-64 md:h-72" aria-hidden="true">
        <Radar data={data} options={options} />
      </div>
      <AccessibleDataList labels={labels} values={scores} formatValue={(value) => `${Math.round(value)} / 100`} summary={summary} />
    </>
  );
}

/** Weekly average-score trend as a restrained Chart.js area line. */
export function GrowthAreaChart({
  labels,
  values,
  formatValue,
  color,
  summary,
}: {
  labels: string[];
  values: number[];
  formatValue: (value: number) => string;
  color?: string;
} & AccessibleChartProps) {
  const palette = useChartPalette();
  const resolvedColor = color ?? palette.primary;
  const data = useMemo(
    () => ({
      labels,
      datasets: [{
        data: values,
        borderColor: resolvedColor,
        borderWidth: 2.5,
        backgroundColor: verticalFill(resolvedColor, 0.18),
        fill: true,
        tension: 0.34,
        pointRadius: values.length > 1 ? 0 : 4,
        pointHoverRadius: 5,
        pointHoverBackgroundColor: resolvedColor,
        pointHoverBorderColor: palette.surface,
        pointHoverBorderWidth: 2,
      }],
    }),
    [labels, values, resolvedColor, palette.surface],
  );

  const options = useMemo(() => {
    const base = timeSeriesOptions(formatValue, palette) as ChartOptions<"line">;
    return {
      ...base,
      scales: {
        ...base.scales,
        y: { ...base.scales?.y, min: 0, max: 100 },
      },
      plugins: {
        ...base.plugins,
        tooltip: {
          ...base.plugins?.tooltip,
          callbacks: { label: (item) => formatValue(item.parsed.y ?? 0) },
        },
      },
    } as ChartOptions<"line">;
  }, [formatValue, palette]);

  return (
    <>
      <div className="max-w-full overflow-x-auto pb-1" aria-hidden="true">
        <div className="h-48 min-w-[420px] md:h-60 md:min-w-0">
          <Line data={data} options={options} />
        </div>
      </div>
      <AccessibleDataList labels={labels} values={values} formatValue={formatValue} summary={summary} />
    </>
  );
}
