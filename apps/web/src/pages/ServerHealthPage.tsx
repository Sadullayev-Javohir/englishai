import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { Bar, Line } from "react-chartjs-2";
import type { ChartOptions } from "chart.js";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { formatDateTime, formatDuration } from "@/lib/labels";
import { Icon } from "@/components/ui/Icon";
import {
  AppButton,
  AppIconButton,
  ChartCard,
  DesignCard,
  DesignConfirm,
  DesignState,
  PageHeader,
  StatCard,
} from "@/components/design";
import { cn } from "@/lib/cn";
import { chart, darkChart, ensureChartsRegistered, withAlpha } from "@/lib/charts";
import { useTheme } from "@/lib/theme";
import type {
  DependencyHealthDto,
  ExternalServiceDto,
  LogEntryDto,
  ServerDiagnosticsDto,
  ServerTelemetryPointDto,
} from "@/api/types";
import "./ServerHealthPage.css";

const t = uz.admin.server;
type OverallStatus = "Healthy" | "Degraded" | "Unhealthy";
type HealthHistoryPoint = {
  timestamp: string;
  managedMemoryMb: number;
  workingSetMb: number;
  threadCount: number;
  gcTotal: number;
  warningCount: number;
  errorCount: number;
  latencies: Record<string, number | null>;
};
type StreamState = "connecting" | "live" | "reconnecting" | "fallback";

ensureChartsRegistered();

export function ServerHealthPage() {
  const navigate = useNavigate();
  const { data: initialData, loading, error, reload } = useAsync(
    () => api.admin.server(),
    []
  );
  const [data, setData] = useState<ServerDiagnosticsDto | null>(null);
  const [history, setHistory] = useState<HealthHistoryPoint[]>([]);
  const [streamState, setStreamState] = useState<StreamState>("connecting");
  const [sampleIntervalSeconds, setSampleIntervalSeconds] = useState(5);
  const [retentionMinutes, setRetentionMinutes] = useState(60);

  useEffect(() => {
    if (initialData) setData((current) => current ?? initialData);
  }, [initialData]);

  useEffect(() => {
    let disposed = false;
    let attempt = 0;
    let fallbackTimer: number | undefined;
    let reconnectTimer: number | undefined;
    let controller: AbortController | undefined;

    const connect = () => {
      if (disposed) return;
      controller = new AbortController();
      setStreamState(attempt === 0 ? "connecting" : "reconnecting");
      void api.admin.serverStream(
        (payload) => {
          if (disposed) return;
          attempt = 0;
          setData(payload.current);
          setHistory(payload.history.map(toHistoryPoint));
          setSampleIntervalSeconds(payload.sampleIntervalSeconds);
          setRetentionMinutes(payload.retentionMinutes);
          setStreamState("live");
          if (fallbackTimer) window.clearInterval(fallbackTimer);
        },
        (snapshot) => {
          if (disposed) return;
          setData(snapshot);
          setHistory((current) => appendHistory(current, toHistoryPoint(snapshot)));
          setStreamState("live");
        },
        controller.signal,
      ).catch(() => {
        if (disposed || controller?.signal.aborted) return;
        attempt += 1;
        if (attempt >= 3 && !fallbackTimer) {
          setStreamState("fallback");
          fallbackTimer = window.setInterval(() => void reload(), 15_000);
        }
        reconnectTimer = window.setTimeout(connect, Math.min(30_000, 1_000 * 2 ** attempt));
      });
    };
    connect();
    return () => {
      disposed = true;
      controller?.abort();
      if (fallbackTimer) window.clearInterval(fallbackTimer);
      if (reconnectTimer) window.clearTimeout(reconnectTimer);
    };
  }, [reload]);

  useEffect(() => {
    if (streamState === "fallback" && initialData) {
      setData(initialData);
      setHistory((current) => appendHistory(current, toHistoryPoint(initialData)));
    }
  }, [initialData, streamState]);

  return (
    <main className="server-health-page">
      <nav className="ea-admin-hub__breadcrumb" aria-label="Breadcrumb">
        <button type="button" onClick={() => navigate("/admin")}>{uz.admin.title}</button>
        <Icon name="chevron_right" />
        <span>{t.title}</span>
      </nav>

      {loading && !data ? (
        <DesignState icon="progress_activity" title={uz.common.loading} description={t.autoRefresh} />
      ) : error && !data ? (
        <ErrorCard error={error} />
      ) : data ? (
        <div className="server-health-page__content">
          <PageHeader
            eyebrow="Live infrastructure"
            title={t.title}
            description={t.subtitle}
            actions={
              <>
                <AppButton type="button" tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>{uz.common.back}</AppButton>
                <AppButton type="button" tone="primary" leadingIcon="refresh" loading={loading} onClick={() => reload()}>{t.refresh}</AppButton>
              </>
            }
          />
          <StatusBanner data={data} />
          <RealtimeVisuals
            data={data}
            history={history}
            streamState={streamState}
            sampleIntervalSeconds={sampleIntervalSeconds}
            retentionMinutes={retentionMinutes}
          />
          <RuntimeSection data={data} />
          <DependenciesSection dependencies={data.dependencies} />
          <ServicesSection services={data.services} />
          <LogsSection logs={data.logs} onChanged={reload} />
          <p className="server-health-page__updated">
            {t.updatedAt(formatDateTime(data.generatedAtUtc))} · {t.autoRefresh}
          </p>
        </div>
      ) : null}
    </main>
  );
}

function toHistoryPoint(source: ServerTelemetryPointDto | ServerDiagnosticsDto): HealthHistoryPoint {
  if ("timestampUtc" in source) {
    return {
      timestamp: source.timestampUtc,
      managedMemoryMb: source.managedMemoryMb,
      workingSetMb: source.workingSetMb,
      threadCount: source.threadCount,
      gcTotal: source.gen0Collections + source.gen1Collections + source.gen2Collections,
      warningCount: source.warningCount,
      errorCount: source.errorCount,
      latencies: source.dependencyLatenciesMs,
    };
  }
  return {
    timestamp: source.generatedAtUtc,
    managedMemoryMb: source.runtime.managedMemoryMb,
    workingSetMb: source.runtime.workingSetMb,
    threadCount: source.runtime.threadCount,
    gcTotal: source.runtime.gen0Collections + source.runtime.gen1Collections + source.runtime.gen2Collections,
    warningCount: source.logs.warningCount,
    errorCount: source.logs.errorCount,
    latencies: Object.fromEntries(source.dependencies.map((item) => [item.name, item.latencyMs])),
  };
}

function appendHistory(current: HealthHistoryPoint[], point: HealthHistoryPoint) {
  const withoutDuplicate = current.filter((item) => item.timestamp !== point.timestamp);
  return [...withoutDuplicate, point]
    .sort((left, right) => left.timestamp.localeCompare(right.timestamp))
    .slice(-720);
}

function ErrorCard({ error }: { error: unknown }) {
  const forbidden = (error as { status?: number } | null)?.status === 403;
  return (
    <DesignState
      role="alert"
      className="ea-state--error"
      icon={forbidden ? "lock" : "error"}
      title={forbidden ? t.forbidden : t.loadError}
      description={t.subtitle}
    />
  );
}

function StatusBanner({ data }: { data: ServerDiagnosticsDto }) {
  const status: OverallStatus = ["Healthy", "Degraded", "Unhealthy"].includes(
    data.overallStatus
  )
    ? (data.overallStatus as OverallStatus)
    : "Unhealthy";
  const config = {
    Healthy: {
      icon: "check_circle",
      label: t.statusHealthy,
      hint: t.statusHealthyHint,
    },
    Degraded: {
      icon: "warning",
      label: t.statusDegraded,
      hint: t.statusDegradedHint,
    },
    Unhealthy: {
      icon: "error",
      label: t.statusUnhealthy,
      hint: t.statusUnhealthyHint,
    },
  }[status];
  return (
    <DesignCard
      as="section"
      tone={status === "Healthy" ? "success" : status === "Unhealthy" ? "danger" : "standard"}
      className={`server-health-page__status server-health-page__status--${status.toLowerCase()}`}
    >
      <span className="server-health-page__status-icon">
        <Icon name={config.icon} filled />
      </span>
      <div>
        <span className="server-health-page__eyebrow">Overall status</span>
        <h2>{config.label}</h2>
        <p>{config.hint}</p>
      </div>
    </DesignCard>
  );
}

function RealtimeVisuals({
  data,
  history,
  streamState,
  sampleIntervalSeconds,
  retentionMinutes,
}: {
  data: ServerDiagnosticsDto;
  history: HealthHistoryPoint[];
  streamState: StreamState;
  sampleIntervalSeconds: number;
  retentionMinutes: number;
}) {
  const { effective } = useTheme();
  const palette = effective === "dark" ? darkChart : chart;
  const labels = useMemo(
    () =>
      history.map((point) =>
        new Date(point.timestamp).toLocaleTimeString("uz-UZ", {
          hour: "2-digit",
          minute: "2-digit",
          second: "2-digit",
        })
      ),
    [history]
  );
  const dependencyNames = useMemo(
    () =>
      Array.from(
        new Set(data.dependencies.map((dependency) => dependency.name))
      ),
    [data.dependencies]
  );
  const liveOptions = useMemo(
    () =>
      ({
        responsive: true,
        maintainAspectRatio: false,
        animation: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? false : { duration: 500 },
        interaction: { mode: "index", intersect: false },
        plugins: {
          legend: {
            position: "bottom",
            labels: { usePointStyle: true, boxWidth: 8, padding: 16, color: palette.textSecondary },
          },
          tooltip: {
            backgroundColor: palette.text,
            titleColor: palette.surface,
            bodyColor: palette.surface,
            padding: 10,
            cornerRadius: 10,
          },
        },
        scales: {
          x: { grid: { display: false }, ticks: { color: palette.textSecondary, maxTicksLimit: 6 } },
          y: { beginAtZero: true, grid: { color: palette.grid }, ticks: { color: palette.textSecondary } },
        },
      } satisfies ChartOptions<"line">),
    [palette]
  );
  const memoryData = useMemo(
    () => ({
      labels,
      datasets: [
        {
          label: "Managed memory (MB)",
          data: history.map((point) => point.managedMemoryMb),
          borderColor: palette.primary,
          backgroundColor: withAlpha(palette.primary, .14),
          fill: true,
          tension: 0.35,
          pointRadius: 0,
        },
        {
          label: "Working set (MB)",
          data: history.map((point) => point.workingSetMb),
          borderColor: palette.accent,
          backgroundColor: withAlpha(palette.accent, .08),
          fill: true,
          tension: 0.35,
          pointRadius: 0,
        },
      ],
    }),
    [history, labels, palette]
  );
  const processData = useMemo(
    () => ({
      labels,
      datasets: [
        {
          label: "Threads",
          data: history.map((point) => point.threadCount),
          borderColor: palette.gold,
          backgroundColor: withAlpha(palette.gold, .12),
          tension: 0.35,
          pointRadius: 0,
        },
        {
          label: "GC total",
          data: history.map((point) => point.gcTotal),
          borderColor: palette.primaryLine,
          backgroundColor: withAlpha(palette.primaryLine, .1),
          tension: 0.35,
          pointRadius: 0,
        },
      ],
    }),
    [history, labels, palette]
  );
  const latencyData = useMemo(
    () => ({
      labels,
      datasets: dependencyNames.map((name, index) => ({
        label: `${name} latency`,
        data: history.map((point) => point.latencies[name]),
        borderColor: [palette.success, palette.primary, palette.accent, palette.gold][index % 4],
        backgroundColor: "transparent",
        tension: 0.3,
        spanGaps: true,
        pointRadius: 0,
      })),
    }),
    [dependencyNames, history, labels, palette]
  );
  const logData = useMemo(
    () => ({
      labels,
      datasets: [
        {
          label: "Warnings",
          data: history.map((point) => point.warningCount),
          backgroundColor: palette.gold,
          borderRadius: 6,
        },
        {
          label: "Errors",
          data: history.map((point) => point.errorCount),
          backgroundColor: palette.accent,
          borderRadius: 6,
        },
      ],
    }),
    [history, labels, palette]
  );

  return (
    <OperatorSection title="Realtime monitoring" eyebrow="Live telemetry">
      <div className="server-health-page__live-head">
        <span className={cn("server-health-page__live-badge", `is-${streamState}`)}>
          <i /> {streamState === "live" ? "Live SSE" : streamState === "fallback" ? "REST fallback" : "Reconnecting"}
          · every {sampleIntervalSeconds}s
        </span>
        <span>
          {history.length} snapshots · last {retentionMinutes} min
        </span>
      </div>
      <div className="server-health-page__charts">
        <LiveChartCard
          title="Memory pressure"
          subtitle="Process memory usage in megabytes"
        >
          <Line aria-label="Xotira ishlatilishi" role="img" data={memoryData} options={liveOptions} />
        </LiveChartCard>
        <LiveChartCard
          title="Runtime activity"
          subtitle="Thread and garbage collection movement"
        >
          <Line aria-label="Runtime faolligi" role="img" data={processData} options={liveOptions} />
        </LiveChartCard>
        <LiveChartCard
          title="Dependency latency"
          subtitle="Live round-trip response time in milliseconds"
        >
          <Line aria-label="Bog‘liqliklar kechikishi" role="img" data={latencyData} options={liveOptions} />
        </LiveChartCard>
        <LiveChartCard
          title="Log activity"
          subtitle="Warning and error buffer movement"
        >
          <Bar aria-label="Loglar faolligi" role="img" data={logData} options={liveOptions as ChartOptions<"bar">} />
        </LiveChartCard>
      </div>
    </OperatorSection>
  );
}

function LiveChartCard({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle: string;
  children: ReactNode;
}) {
  return (
    <ChartCard
      title={title}
      description={subtitle}
      chartLabel={title}
      tone="standard"
      className="server-health-page__chart-card"
    >
      {children}
    </ChartCard>
  );
}

function RuntimeSection({ data }: { data: ServerDiagnosticsDto }) {
  const runtime = data.runtime;
  const items = [
    ["timer", t.uptime, formatDuration(runtime.uptimeSeconds)],
    ["dns", t.environment, runtime.environment],
    ["sell", t.version, runtime.version],
    ["memory", t.managedMemory, `${runtime.managedMemoryMb} MB`],
    ["developer_board", t.workingSet, `${runtime.workingSetMb} MB`],
    ["lan", t.threads, String(runtime.threadCount)],
    ["developer_mode", t.cpuCores, String(runtime.processorCount)],
    [
      "recycling",
      t.gc,
      `${runtime.gen0Collections} / ${runtime.gen1Collections} / ${runtime.gen2Collections}`,
    ],
    ["schedule", t.startedAt, formatDateTime(runtime.startedAtUtc)],
  ];
  return (
    <OperatorSection title={t.runtimeTitle} eyebrow="Runtime" count={items.length}>
      <div className="server-health-page__metric-grid" data-accent="server">
        {items.map(([icon, label, value]) => (
          <StatCard
            key={label}
            className="server-health-page__metric"
            icon={icon}
            label={label}
            value={value}
          />
        ))}
      </div>
      <p className="server-health-page__meta">
        {runtime.framework} · {runtime.operatingSystem} · {runtime.machineName}
      </p>
    </OperatorSection>
  );
}

function DependenciesSection({
  dependencies,
}: {
  dependencies: DependencyHealthDto[];
}) {
  return (
    <OperatorSection title={t.dependenciesTitle} eyebrow="Infrastructure" count={dependencies.length}>
      <div className="server-health-page__card-grid">
        {dependencies.map((dependency) => (
          <DependencyCard key={dependency.name} dependency={dependency} />
        ))}
      </div>
    </OperatorSection>
  );
}

function DependencyCard({
  dependency,
}: {
  dependency: DependencyHealthDto;
}) {
  const healthy = dependency.status === "Healthy";
  const notConfigured = dependency.status === "NotConfigured";
  const badge = healthy
    ? { icon: "check_circle", label: t.depHealthy, state: "healthy" }
    : notConfigured
    ? { icon: "warning", label: t.depNotConfigured, state: "warning" }
    : { icon: "error", label: t.depUnhealthy, state: "error" };
  return (
    <DesignCard as="article" data-state={badge.state} className="server-health-page__detail-card">
      <div className="server-health-page__detail-head">
        <span className="server-health-page__card-icon">
          <Icon name="storage" />
        </span>
        <span
          className={`server-health-page__badge server-health-page__badge--${badge.state}`}
        >
          <Icon name={badge.icon} filled />
          {badge.label}
        </span>
      </div>
      <h3>{dependency.name}</h3>
      <p>{dependency.detail}</p>
      {dependency.latencyMs !== null && (
        <span className="server-health-page__latency">
          <Icon name="bolt" />
          {t.latency(dependency.latencyMs)}
        </span>
      )}
    </DesignCard>
  );
}

function ServicesSection({ services }: { services: ExternalServiceDto[] }) {
  return (
    <OperatorSection title={t.servicesTitle} eyebrow="Integrations" count={services.length}>
      <div className="server-health-page__card-grid">
        {services.map((service) => (
          <DesignCard
            as="article"
            key={service.name}
            data-state={service.configured ? "healthy" : "warning"}
            className="server-health-page__service"
          >
            <span className="server-health-page__card-icon">
              <Icon name={service.configured ? "cloud_done" : "cloud_off"} />
            </span>
            <div>
              <h3>{service.name}</h3>
              <span
                className={cn(
                  "server-health-page__service-state",
                  !service.configured &&
                    "server-health-page__service-state--off"
                )}
              >
                {service.configured ? t.svcConfigured : t.svcNotConfigured}
              </span>
              <p>{service.detail}</p>
            </div>
          </DesignCard>
        ))}
      </div>
    </OperatorSection>
  );
}

function LogsSection({
  logs,
  onChanged,
}: {
  logs: ServerDiagnosticsDto["logs"];
  onChanged: () => void;
}) {
  const [clearing, setClearing] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [warningsOpen, setWarningsOpen] = useState(false);
  const errors = logs.recent.filter(
    (entry) => entry.level === "Error" || entry.level === "Fatal"
  );
  const warnings = logs.recent.filter(
    (entry) => entry.level !== "Error" && entry.level !== "Fatal"
  );
  const clearAll = async () => {
    setClearing(true);
    try {
      await api.admin.clearServerLogs();
      setConfirmOpen(false);
      onChanged();
    } catch {
      /* The next poll keeps backend state visible. */
    } finally {
      setClearing(false);
    }
  };
  return (
    <OperatorSection title={t.logsTitle} eyebrow="Diagnostics" count={logs.errorCount + logs.warningCount || undefined}>
      <div className="server-health-page__logs-head">
        {logs.recent.length > 0 && (
          <span>{t.logsSummary(logs.errorCount, logs.warningCount)}</span>
        )}
        {logs.recent.length > 0 && (
          <AppButton tone="danger" leadingIcon="delete_sweep" onClick={() => setConfirmOpen(true)} disabled={clearing}>{t.logsClear}</AppButton>
        )}
      </div>
      {logs.recent.length === 0 ? (
        <DesignState icon="check_circle" title={t.logsEmpty} />
      ) : (
        <div className="server-health-page__log-groups">
          {errors.length > 0 && (
            <DesignCard as="section" tone="danger" className="server-health-page__log-group server-health-page__log-group--danger">
              <div className="server-health-page__log-group-title">
                <Icon name="error" filled />
                <strong>{t.errorsButton(errors.length)}</strong>
              </div>
              <div className="server-health-page__logs">
                {errors.map((entry, index) => (
                  <LogRow
                    key={entry.id || `${entry.timestampUtc}-${index}`}
                    entry={entry}
                    onChanged={onChanged}
                  />
                ))}
              </div>
            </DesignCard>
          )}
          {warnings.length > 0 && (
            <DesignCard as="section" className="server-health-page__log-group server-health-page__log-group--warning">
              <AppButton
                type="button"
                tone="standard"
                fullWidth
                leadingIcon="warning"
                trailingIcon={warningsOpen ? "expand_less" : "expand_more"}
                className="server-health-page__warning-toggle"
                aria-expanded={warningsOpen}
                onClick={() => setWarningsOpen((open) => !open)}
              >
                {t.warningsButton(warnings.length)}
              </AppButton>
              {warningsOpen && (
                <div className="server-health-page__logs">
                  {warnings.map((entry, index) => (
                    <LogRow
                      key={entry.id || `${entry.timestampUtc}-${index}`}
                      entry={entry}
                      onChanged={onChanged}
                    />
                  ))}
                </div>
              )}
            </DesignCard>
          )}
        </div>
      )}
      <DesignConfirm
        open={confirmOpen}
        onClose={() => { if (!clearing) setConfirmOpen(false); }}
        title={t.logsClear}
        message={t.logsClearConfirm}
        confirmLabel={t.logsClear}
        destructive
        loading={clearing}
        closeOnBackdrop={!clearing}
        closeOnEscape={!clearing}
        onConfirm={() => void clearAll()}
      />
    </OperatorSection>
  );
}

function LogRow({
  entry,
  onChanged,
}: {
  entry: LogEntryDto;
  onChanged: () => void;
}) {
  const [deleting, setDeleting] = useState(false);
  const [failed, setFailed] = useState(false);
  const isError = entry.level === "Error" || entry.level === "Fatal";
  const levelLabel =
    entry.level === "Fatal"
      ? t.logLevelFatal
      : isError
      ? t.logLevelError
      : t.logLevelWarning;
  const remove = async () => {
    setDeleting(true);
    setFailed(false);
    try {
      await api.admin.deleteServerLog(entry.id);
      onChanged();
    } catch {
      setFailed(true);
      setDeleting(false);
    }
  };
  return (
    <article
      className={cn(
        "server-health-page__log",
        isError && "server-health-page__log--error"
      )}
    >
      <div className="server-health-page__log-head">
        <span
          className={cn(
            "server-health-page__log-level",
            isError && "server-health-page__log-level--error"
          )}
        >
          {levelLabel}
        </span>
        <time>{formatDateTime(entry.timestampUtc)}</time>
        <AppIconButton
          icon="delete"
          label={t.logDelete}
          tone="danger"
          onClick={remove}
          disabled={deleting || !entry.id}
        />
      </div>
      <p>{entry.message}</p>
      {(entry.requestPath ||
        entry.sourceContext ||
        entry.correlationId ||
        entry.traceId) && (
        <dl className="server-health-page__log-meta">
          {entry.requestPath && (
            <div>
              <dt>Request</dt>
              <dd>{entry.requestPath}</dd>
            </div>
          )}
          {entry.sourceContext && (
            <div>
              <dt>Source</dt>
              <dd>{entry.sourceContext}</dd>
            </div>
          )}
          {entry.correlationId && (
            <div>
              <dt>Correlation</dt>
              <dd>{entry.correlationId}</dd>
            </div>
          )}
          {entry.traceId && (
            <div>
              <dt>Trace</dt>
              <dd>{entry.traceId}</dd>
            </div>
          )}
        </dl>
      )}
      {failed && (
        <span className="server-health-page__log-failed">
          {t.logDeleteError}
        </span>
      )}
      {entry.exception && (
        <details>
          <summary>{t.showException}</summary>
          <pre>{entry.exception}</pre>
        </details>
      )}
    </article>
  );
}

function OperatorSection({
  title,
  eyebrow,
  count,
  children,
}: {
  title: string;
  eyebrow?: string;
  count?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className="server-health-page__section">
      <header className="server-health-page__section-head">
        <div>
          {eyebrow && <span className="ea-eyebrow">{eyebrow}</span>}
          <h2>{title}</h2>
        </div>
        {count !== undefined && count !== null && <span className="ea-count-pill">{count}</span>}
      </header>
      {children}
    </section>
  );
}
