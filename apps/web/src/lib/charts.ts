/**
 * Central Chart.js setup + brand theme for the founder dashboard (and any future chart).
 *
 * One registration point (tree-shaken - only the pieces we actually draw) and one palette,
 * derived from the design tokens in tailwind.config.js so every chart reads as one system.
 * Rule 11: charts render only server numbers/dates; all labels come from the uz content store.
 */
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  RadialLinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Tooltip,
  Filler,
  Legend,
  type ChartOptions,
  type ScriptableContext,
} from "chart.js";

let registered = false;

/** Register the Chart.js primitives + apply global brand defaults exactly once. */
export function ensureChartsRegistered(): void {
  if (registered) return;
  ChartJS.register(
    CategoryScale,
    LinearScale,
    RadialLinearScale, // radar (skill profile)
    PointElement,
    LineElement,
    BarElement,
    ArcElement,
    Tooltip,
    Filler,
    Legend,
  );

  ChartJS.defaults.font.family = "'Plus Jakarta Sans', system-ui, -apple-system, sans-serif";
  ChartJS.defaults.font.size = 12;
  ChartJS.defaults.color = chart.textSecondary;
  registered = true;
}

/**
 * Chart palette from the design tokens. Single-series plots use the brand hues (identity is
 * decorative, no CVD constraint); the plan doughnut uses `categorical`, a validated
 * colorblind-safe ramp (fixed order - never cycled).
 */
export const chart = {
  primary: "#7545E8",
  primaryLine: "#5126B8",
  accent: "#818CF8",
  gold: "#D97706",
  success: "#568744",
  text: "#2C2144",
  textSecondary: "#7A718B",
  grid: "rgba(217, 218, 227, 0.72)",
  surface: "#FFFFFF",
  categorical: ["#7545E8", "#818CF8", "#A5B4FC", "#D97706"],
} as const;

/** Compatibility alias for older chart consumers; Play is always light. */
export const darkChart = chart;

export type ChartPalette = typeof chart | typeof darkChart;

/**
 * A vertical gradient fill (opaque-ish at the top, fading to transparent) for area charts.
 * Chart.js needs the live canvas context, so this is used as a scriptable `backgroundColor`.
 */
export function verticalFill(hex: string, topAlpha = 0.28) {
  return (ctx: ScriptableContext<"line">): CanvasGradient | string => {
    const { chart } = ctx;
    const { ctx: c, chartArea } = chart;
    if (!chartArea) return "transparent"; // first frame before layout is known
    const g = c.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
    g.addColorStop(0, withAlpha(hex, topAlpha));
    g.addColorStop(1, withAlpha(hex, 0));
    return g;
  };
}

/** Shared tooltip look for every chart (rounded, brand ink, generous padding). */
function tooltipFor(palette: ChartPalette) {
  return {
    backgroundColor: palette.text,
    titleColor: palette.surface,
    bodyColor: palette.surface,
    padding: 10,
    cornerRadius: 10,
    displayColors: false,
    titleFont: { weight: 700 as const, size: 12 },
    bodyFont: { size: 13 },
  };
}

/** Base options for a time-series (line or bar): recessive grid, no legend, tidy ticks. */
export function timeSeriesOptions(
  valueLabel: (n: number) => string,
  palette: ChartPalette = chart,
): ChartOptions<"line" | "bar"> {
  return {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: "index", intersect: false },
    plugins: {
      legend: { display: false },
      tooltip: {
        ...tooltipFor(palette),
        callbacks: { label: (item) => valueLabel(item.parsed.y ?? 0) },
      },
    },
    scales: {
      x: {
        grid: { display: false },
        border: { display: false },
        ticks: { color: palette.textSecondary, maxRotation: 0, autoSkip: true, maxTicksLimit: 6 },
      },
      y: {
        beginAtZero: true,
        grid: { color: palette.grid },
        border: { display: false },
        ticks: { color: palette.textSecondary, precision: 0, maxTicksLimit: 5 },
      },
    },
  };
}

/**
 * Base options for a horizontal magnitude bar (category on y, value on x): recessive vertical
 * grid, no legend, value labels via the tooltip. Ideal on phones - long category names get a
 * full row instead of a cramped rotated x-tick.
 */
export function horizontalBarOptions(
  valueLabel: (n: number) => string,
  palette: ChartPalette = chart,
): ChartOptions<"bar"> {
  return {
    indexAxis: "y",
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        ...tooltipFor(palette),
        callbacks: { label: (item) => valueLabel(item.parsed.x ?? 0) },
      },
    },
    scales: {
      x: {
        beginAtZero: true,
        grid: { color: palette.grid },
        border: { display: false },
        ticks: { display: false },
      },
      y: {
        grid: { display: false },
        border: { display: false },
        ticks: { color: palette.textSecondary, autoSkip: false },
      },
    },
  };
}

/** Base options for a single-series radar (skill profile), fixed 0–100 radial scale. */
export function radarOptions(palette: ChartPalette = chart): ChartOptions<"radar"> {
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        ...tooltipFor(palette),
        displayColors: false,
        callbacks: { label: (item) => `${Math.round(item.parsed.r ?? 0)}` },
      },
    },
    scales: {
      r: {
        min: 0,
        max: 100,
        angleLines: { color: palette.grid },
        grid: { color: palette.grid },
        pointLabels: { color: palette.textSecondary, font: { size: 12, weight: 600 } },
        ticks: {
          stepSize: 25,
          backdropColor: "transparent",
          color: palette.textSecondary,
          showLabelBackdrop: false,
          font: { size: 9 },
        },
      },
    },
  };
}

export function withAlpha(hex: string, alpha: number): string {
  const [r, g, b] = rgb(hex);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function rgb(hex: string): [number, number, number] {
  const h = hex.replace("#", "");
  return [
    parseInt(h.slice(0, 2), 16),
    parseInt(h.slice(2, 4), 16),
    parseInt(h.slice(4, 6), 16),
  ];
}

/**
 * Sequential funnel ramp inside the approved indigo family.
 */
export function funnelRamp(
  index: number,
  total: number,
  palette: ChartPalette = chart,
): string {
  const from = rgb(palette.primaryLine);
  const to = rgb(palette.accent);
  const t = total <= 1 ? 0 : index / (total - 1);
  const mix = from.map((f, i) => Math.round(f + (to[i] - f) * t));
  return `rgb(${mix[0]}, ${mix[1]}, ${mix[2]})`;
}
