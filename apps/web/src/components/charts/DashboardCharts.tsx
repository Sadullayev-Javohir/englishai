import { useMemo } from "react";
import { Line, Bar, Doughnut, Pie, Radar } from "react-chartjs-2";
import type { ChartOptions, Plugin } from "chart.js";
import { ensureChartsRegistered, chart, darkChart, verticalFill, funnelRamp, timeSeriesOptions, horizontalBarOptions, type ChartPalette } from "@/lib/charts";
import { formatDate } from "@/lib/labels";
import { useTheme } from "@/lib/theme";
import type { DailyCountDto } from "@/api/types";

ensureChartsRegistered();

/** Short axis label for a daily point: "dd.mm" (year is implicit across a 21-day window). */
const shortDay = (iso: string) => formatDate(iso).slice(0, 5);
const goalChartColors = ["#4F46E5", "#06B6D4", "#F59E0B", "#EC4899", "#10B981", "#8B5CF6"] as const;

function useChartPalette() {
  const { effective } = useTheme();
  return effective === "dark" ? darkChart : chart;
}

/**
 * Area line for a daily count (e.g. DAU over 30 days). Single series → brand green, gradient
 * fill, smooth curve, hover crosshair via the shared tooltip. Falls back to an empty note.
 */
export function TrendAreaChart({
  points,
  color,
  tooltipLabel,
  emptyLabel,
}: {
  points: DailyCountDto[];
  color?: string;
  tooltipLabel: (n: number) => string;
  emptyLabel: string;
}) {
  const palette = useChartPalette();
  const resolvedColor = color ?? palette.primary;
  const hasData = points.some((p) => p.count > 0);

  const data = useMemo(
    () => ({
      labels: points.map((p) => shortDay(p.day)),
      datasets: [
        {
          data: points.map((p) => p.count),
          borderColor: resolvedColor,
          borderWidth: 2,
          backgroundColor: verticalFill(resolvedColor),
          fill: true,
          tension: 0.35,
          pointRadius: 0,
          pointHoverRadius: 5,
          pointHoverBackgroundColor: resolvedColor,
          pointHoverBorderColor: palette.surface,
          pointHoverBorderWidth: 2,
        },
      ],
    }),
    [points, resolvedColor, palette.surface],
  );

  const options = useMemo(() => {
    const base = timeSeriesOptions(tooltipLabel, palette) as ChartOptions<"line">;
    return {
      ...base,
      plugins: {
        ...base.plugins,
        tooltip: {
          ...base.plugins?.tooltip,
          callbacks: {
            title: (items) => fullDate(points, items[0]?.dataIndex),
            label: (item) => tooltipLabel(item.parsed.y ?? 0),
          },
        },
      },
    } as ChartOptions<"line">;
  }, [tooltipLabel, points, palette]);

  if (!hasData) return <EmptyPlot label={emptyLabel} />;
  return (
    <div className="h-56">
      <Line aria-label="Faol foydalanuvchilar trendi" role="img" data={data} options={options} />
    </div>
  );
}

/**
 * Rounded bars for a daily count (e.g. signups over 30 days). Single series → one brand hue,
 * 4px rounded data-ends anchored to the baseline (dataviz mark spec).
 */
export function TrendBarChart({
  points,
  color,
  tooltipLabel,
  emptyLabel,
}: {
  points: DailyCountDto[];
  color?: string;
  tooltipLabel: (n: number) => string;
  emptyLabel: string;
}) {
  const palette = useChartPalette();
  const resolvedColor = color ?? palette.accent;
  const hasData = points.some((p) => p.count > 0);

  const data = useMemo(
    () => ({
      labels: points.map((p) => shortDay(p.day)),
      datasets: [
        {
          data: points.map((p) => p.count),
          backgroundColor: resolvedColor,
          hoverBackgroundColor: resolvedColor,
          borderColor: resolvedColor,
          borderWidth: 1,
          borderRadius: 4,
          borderSkipped: false as const,
          maxBarThickness: 22,
        },
      ],
    }),
    [points, resolvedColor],
  );

  const options = useMemo(() => {
    const base = timeSeriesOptions(tooltipLabel, palette) as ChartOptions<"bar">;
    return {
      ...base,
      plugins: {
        ...base.plugins,
        tooltip: {
          ...base.plugins?.tooltip,
          callbacks: {
            title: (items) => fullDate(points, items[0]?.dataIndex),
            label: (item) => tooltipLabel(item.parsed.y ?? 0),
          },
        },
      },
    } as ChartOptions<"bar">;
  }, [tooltipLabel, points, palette]);

  if (!hasData) return <EmptyPlot label={emptyLabel} />;
  return (
    <div className="h-56">
      <Bar aria-label="Ro‘yxatdan o‘tishlar trendi" role="img" data={data} options={options} />
    </div>
  );
}

/**
 * Day-N retention as three vertical bars (0–100%). One series (magnitude), so a single brand
 * hue with a 0–100 y-axis makes the three brackets directly comparable at a glance.
 */
export function RetentionBarChart({
  labels,
  rates,
  tooltipLabel,
}: {
  labels: string[];
  rates: number[];
  tooltipLabel: (n: number) => string;
}) {
  const palette = useChartPalette();
  const data = useMemo(
    () => ({
      labels,
      datasets: [
        {
          data: rates,
          backgroundColor: palette.categorical.slice(0, 3),
          hoverBackgroundColor: palette.categorical.slice(0, 3),
          borderColor: palette.categorical.slice(0, 3),
          borderWidth: 1,
          borderRadius: 6,
          borderSkipped: false as const,
          maxBarThickness: 64,
        },
      ],
    }),
    [labels, rates, palette],
  );

  const options: ChartOptions<"bar"> = useMemo(
    () => ({
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: palette.text,
          titleColor: palette.surface,
          bodyColor: palette.surface,
          padding: 10,
          cornerRadius: 10,
          displayColors: false,
          callbacks: { label: (item) => tooltipLabel(item.parsed.y ?? 0) },
        },
      },
      scales: {
        x: { grid: { display: false }, border: { display: false } },
        y: {
          beginAtZero: true,
          max: 100,
          grid: { color: palette.grid },
          border: { display: false },
          ticks: { callback: (v) => `${v}%`, maxTicksLimit: 5 },
        },
      },
    }),
    [tooltipLabel, palette],
  );

  return (
    <div className="h-56">
      <Bar aria-label="Foydalanuvchilarni saqlab qolish ko‘rsatkichi" role="img" data={data} options={options} />
    </div>
  );
}

/**
 * Plan composition as a doughnut. Multiple categories → the validated CVD-safe categorical
 * ramp in fixed order; a legend + a centred total keeps identity off color-alone.
 */
export function PlanDoughnutChart({
  labels,
  values,
  centerLabel,
  centerValue,
  tooltipLabel,
}: {
  labels: string[];
  values: number[];
  centerLabel: string;
  centerValue: number;
  tooltipLabel: (label: string, value: number) => string;
}) {
  const palette = useChartPalette();
  const data = useMemo(
    () => ({
      labels,
      datasets: [
        {
          data: values,
          backgroundColor: labels.map((_, i) => palette.categorical[i % palette.categorical.length]),
          borderColor: palette.surface,
          borderWidth: 3, // 2px+ surface gap between segments (dataviz spacer spec)
          hoverOffset: 6,
        },
      ],
    }),
    [labels, values, palette],
  );

  // Center label drawn as a plugin so it sits exactly on the arc center (robust to the
  // bottom legend's dynamic height, unlike an absolutely-positioned overlay).
  const centerText = useMemo<Plugin<"doughnut">>(
    () => ({
      id: "planCenterText",
      afterDatasetsDraw(c) {
        const arc = c.getDatasetMeta(0).data[0] as unknown as { x: number; y: number } | undefined;
        if (!arc) return;
        const { ctx } = c;
        ctx.save();
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillStyle = palette.text;
        ctx.font = "700 22px 'Manrope', system-ui, sans-serif";
        ctx.fillText(String(centerValue), arc.x, arc.y - 8);
        ctx.fillStyle = palette.textSecondary;
        ctx.font = "12px 'Manrope', system-ui, sans-serif";
        ctx.fillText(centerLabel, arc.x, arc.y + 13);
        ctx.restore();
      },
    }),
    [centerValue, centerLabel, palette],
  );

  const options: ChartOptions<"doughnut"> = useMemo(
    () => ({
      responsive: true,
      maintainAspectRatio: false,
      cutout: "62%",
      plugins: {
        legend: {
          position: "bottom",
          labels: { boxWidth: 12, boxHeight: 12, padding: 14, color: palette.text, usePointStyle: true },
        },
        tooltip: {
          backgroundColor: palette.text,
          titleColor: palette.surface,
          bodyColor: palette.surface,
          padding: 10,
          cornerRadius: 10,
          displayColors: true,
          callbacks: { label: (item) => tooltipLabel(item.label, item.parsed) },
        },
      },
    }),
    [tooltipLabel, palette],
  );

  return (
    <div className="h-64">
      <Doughnut aria-label="Tariflar taqsimoti" role="img" data={data} options={options} plugins={[centerText]} />
    </div>
  );
}

/** Executive pulse: normalized product-health signals on one 0–100 radar. */
export function ProductHealthRadarChart({ labels, values }: { labels: string[]; values: number[] }) {
  const palette = useChartPalette();
  const data = useMemo(() => ({ labels, datasets: [{
    data: values.map((value) => Math.max(0, Math.min(100, value))),
    backgroundColor: `${palette.primary}2e`, borderColor: palette.primary, borderWidth: 2,
    pointBackgroundColor: palette.surface, pointBorderColor: palette.primary, pointBorderWidth: 2, pointRadius: 4,
  }] }), [labels, values, palette]);
  const options: ChartOptions<"radar"> = useMemo(() => ({
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { display: false }, tooltip: { backgroundColor: palette.text, titleColor: palette.surface, bodyColor: palette.surface, displayColors: false, padding: 12, cornerRadius: 12, callbacks: { label: (item) => `${Math.round(item.parsed.r)}%` } } },
    scales: { r: { min: 0, max: 100, beginAtZero: true, ticks: { display: false, stepSize: 25 }, grid: { color: palette.grid, circular: true }, angleLines: { color: palette.grid }, pointLabels: { color: palette.text, font: { size: 11, weight: 700 } } } },
  }), [palette]);
  return <div className="h-72"><Radar aria-label="Mahsulot salomatligi radar charti" role="img" data={data} options={options} /></div>;
}

/** Goal-segment audience composition as a true pie chart. */
export function GoalSegmentPieChart({ labels, values }: { labels: string[]; values: number[] }) {
  const palette = useChartPalette();
  const data = useMemo(() => ({ labels, datasets: [{ data: values, backgroundColor: labels.map((_, index) => goalChartColors[index % goalChartColors.length]), borderColor: palette.surface, borderWidth: 4, hoverOffset: 10 }] }), [labels, values, palette.surface]);
  const options: ChartOptions<"pie"> = useMemo(() => ({
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { position: "bottom", labels: { color: palette.text, usePointStyle: true, boxWidth: 9, padding: 15, font: { size: 11, weight: 600 } } }, tooltip: { backgroundColor: palette.text, titleColor: palette.surface, bodyColor: palette.surface, padding: 12, cornerRadius: 12, callbacks: { label: (item) => `${item.label}: ${item.parsed}` } } },
  }), [palette]);
  return <div className="h-72"><Pie aria-label="Maqsad segmentlari pie charti" role="img" data={data} options={options} /></div>;
}

/** Goal conversion ranking with color-per-segment and a fixed percentage scale. */
export function GoalConversionChart({ labels, values }: { labels: string[]; values: number[] }) {
  const palette = useChartPalette();
  const data = useMemo(() => ({ labels, datasets: [{ data: values, backgroundColor: labels.map((_, index) => goalChartColors[index % goalChartColors.length]), borderRadius: 9, borderSkipped: false as const, barThickness: 18 }] }), [labels, values]);
  const options: ChartOptions<"bar"> = useMemo(() => ({
    indexAxis: "y", responsive: true, maintainAspectRatio: false,
    plugins: { legend: { display: false }, tooltip: { backgroundColor: palette.text, titleColor: palette.surface, bodyColor: palette.surface, displayColors: false, padding: 12, cornerRadius: 12, callbacks: { label: (item) => `${item.parsed.x ?? 0}%` } } },
    scales: { x: { beginAtZero: true, max: 100, grid: { color: palette.grid }, border: { display: false }, ticks: { color: palette.textSecondary, callback: (value) => `${value}%`, maxTicksLimit: 5 } }, y: { grid: { display: false }, border: { display: false }, ticks: { color: palette.text, font: { size: 11, weight: 600 } } } },
  }), [palette]);
  return <div className="h-72"><Bar aria-label="Segmentlar konversiyasi charti" role="img" data={data} options={options} /></div>;
}

/**
 * Draws each bar's value at its end - inside the bar (white) when it is wide enough, otherwise just
 * past the tip (ink) so tiny funnel steps stay readable. Lets us hide the x-axis entirely for a
 * clean, label-on-mark look (dataviz: value labels beat an axis for a short magnitude ranking).
 */
function valueLabelsPlugin(id: string, palette: ChartPalette, weight: 700 | 800): Plugin<"bar"> {
  return {
    id,
    afterDatasetsDraw(chartInstance) {
      const meta = chartInstance.getDatasetMeta(0);
      const values = chartInstance.data.datasets[0].data as number[];
      const { ctx } = chartInstance;
      ctx.save();
      ctx.font = `${weight} 12px 'Manrope', system-ui, sans-serif`;
      ctx.textBaseline = "middle";
      meta.data.forEach((bar, index) => {
        const element = bar as unknown as { x: number; y: number; base: number };
        const fitsInside = element.x - element.base > 30;
        ctx.fillStyle = fitsInside ? palette.surface : palette.text;
        ctx.textAlign = fitsInside ? "right" : "left";
        ctx.fillText(String(values[index] ?? 0), fitsInside ? element.x - 8 : element.x + 6, element.y);
      });
      ctx.restore();
    },
  };
}

/** Shared horizontal-funnel options: bar-end value labels + a multi-line branded tooltip. */
function funnelOptions(
  labels: string[],
  tooltipLines: (index: number) => string[],
  palette: ChartPalette,
): ChartOptions<"bar"> {
  const base = horizontalBarOptions(() => "", palette);
  return {
    ...base,
    layout: { padding: { right: 28 } }, // room for value labels that spill past a full-width bar
    plugins: {
      ...base.plugins,
      tooltip: {
        ...base.plugins?.tooltip,
        callbacks: {
          title: (items) => labels[items[0]?.dataIndex ?? 0] ?? "",
          label: (item) => tooltipLines(item.dataIndex),
        },
      },
    },
    scales: {
      ...base.scales,
      y: {
        ...base.scales?.y,
        ticks: { autoSkip: false, color: palette.text, font: { size: 12, weight: 500 } },
      },
    },
  };
}

/**
 * Activation funnel as a sequential horizontal bar chart (registered → … → monetization). Bars
 * share one green→terracotta ramp so the narrowing reads as a single funnel; the tooltip carries
 * the drop-off math (from start / from previous). This is the accelerator "activation proof" visual.
 */
export function ActivationFunnelChart({
  labels,
  counts,
  tooltipLines,
}: {
  labels: string[];
  counts: number[];
  tooltipLines: (index: number) => string[];
}) {
  const palette = useChartPalette();
  const data = useMemo(
    () => ({
      labels,
      datasets: [
        {
          data: counts,
          backgroundColor: counts.map(() => palette.primary),
          hoverBackgroundColor: palette.primaryLine,
          borderRadius: 6,
          borderSkipped: false as const,
          barPercentage: 0.82,
          categoryPercentage: 0.86,
          minBarLength: 2,
        },
      ],
    }),
    [labels, counts, palette],
  );

  const options = useMemo(() => funnelOptions(labels, tooltipLines, palette), [labels, tooltipLines, palette]);
  const labelsPlugin = useMemo(() => valueLabelsPlugin("activationBarValueLabels", palette, 800), [palette]);

  const height = Math.max(300, labels.length * 34);
  return (
    <div style={{ height }}>
      <Bar aria-label="Faollashtirish bosqichlari" role="img" data={data} options={options} plugins={[labelsPlugin]} />
    </div>
  );
}

/**
 * Conversion funnel (registered → onboarded → active → premium) as a compact horizontal bar chart.
 * One brand hue (a magnitude comparison, not a category split), value labels on the bar ends.
 */
export function ConversionFunnelChart({
  labels,
  counts,
  tooltipLines,
}: {
  labels: string[];
  counts: number[];
  tooltipLines: (index: number) => string[];
}) {
  const palette = useChartPalette();
  const data = useMemo(
    () => ({
      labels,
      datasets: [
        {
          data: counts,
          backgroundColor: counts.map((_, index) => index === 0 ? palette.primary : funnelRamp(index, counts.length, palette)),
          hoverBackgroundColor: palette.primaryLine,
          borderRadius: 6,
          borderSkipped: false as const,
          barPercentage: 0.7,
          categoryPercentage: 0.8,
          minBarLength: 2,
        },
      ],
    }),
    [labels, counts, palette],
  );

  const options = useMemo(() => funnelOptions(labels, tooltipLines, palette), [labels, tooltipLines, palette]);
  const labelsPlugin = useMemo(() => valueLabelsPlugin("barEndValueLabels", palette, 700), [palette]);

  const height = Math.max(180, labels.length * 46);
  return (
    <div style={{ height }}>
      <Bar aria-label="Konversiya bosqichlari" role="img" data={data} options={options} plugins={[labelsPlugin]} />
    </div>
  );
}

export function DemographicDoughnutChart({ labels, values, ariaLabel }: { labels: string[]; values: number[]; ariaLabel: string }) {
  const palette = useChartPalette();
  const colors = useMemo(() => [palette.primary, palette.accent, "#8B5CF6", "#EC4899", "#06B6D4", "#F59E0B"], [palette.accent, palette.primary]);
  const data = useMemo(() => ({ labels, datasets: [{ data: values, backgroundColor: labels.map((_, index) => colors[index % colors.length]), borderColor: palette.surface, borderWidth: 3 }] }), [colors, labels, values, palette.surface]);
  const options = useMemo(() => ({ responsive: true, maintainAspectRatio: false, plugins: { legend: { position: "bottom" as const, labels: { color: palette.text, usePointStyle: true, padding: 16 } }, tooltip: { callbacks: { label: (item: { label?: string; parsed: number }) => `${item.label}: ${item.parsed}` } } } }), [palette.text]);
  if (!values.some(Boolean)) return <EmptyPlot label="Hozircha ma’lumot yo‘q" />;
  return <div className="h-64"><Doughnut aria-label={ariaLabel} role="img" data={data} options={options} /></div>;
}

export function DemographicBarChart({ labels, values, ariaLabel }: { labels: string[]; values: number[]; ariaLabel: string }) {
  const palette = useChartPalette();
  const data = useMemo(() => ({ labels, datasets: [{ data: values, backgroundColor: palette.primary, borderRadius: 6, borderSkipped: false as const }] }), [labels, values, palette.primary]);
  const options = useMemo(() => ({ responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { x: { ticks: { color: palette.textSecondary }, grid: { display: false } }, y: { beginAtZero: true, ticks: { color: palette.textSecondary, precision: 0 }, grid: { color: palette.grid } } } }), [palette]);
  if (!values.some(Boolean)) return <EmptyPlot label="Hozircha ma’lumot yo‘q" />;
  return <div className="h-64"><Bar aria-label={ariaLabel} role="img" data={data} options={options} /></div>;
}

/** Full "dd.mm.yyyy" for a tooltip title, from the point index into the series. */
function fullDate(points: DailyCountDto[], index: number | undefined): string {
  if (index == null || !points[index]) return "";
  return formatDate(points[index].day);
}

function EmptyPlot({ label }: { label: string }) {
  return (
    <div className="h-56 flex items-center justify-center">
      <p className="font-body-md text-body-md text-text-secondary">{label}</p>
    </div>
  );
}
