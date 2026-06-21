import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { formatDate } from "@/lib/labels";
import { Icon } from "@/components/ui/Icon";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import {
  TrendAreaChart,
  TrendBarChart,
  RetentionBarChart,
  PlanDoughnutChart,
  ActivationFunnelChart,
  ConversionFunnelChart,
  ProductHealthRadarChart,
  GoalSegmentPieChart,
  GoalConversionChart,
  DemographicDoughnutChart,
  DemographicBarChart,
} from "@/components/charts/DashboardCharts";
import type { ActivationFunnelStepDto, RetentionPointDto, VariableCostAttributionSnapshotDto } from "@/api/types";
import "./FounderDashboardPage.css";

type Tone = "lime" | "blue" | "purple" | "orange" | "rose" | "ink";
type Range = "7" | "30" | "90" | "custom";

const todayIso = () => new Date().toISOString().slice(0, 10);

export function FounderDashboardPage() {
  const [range, setRange] = useState<Range>("30");
  const [customFrom, setCustomFrom] = useState(() => {
    const from = new Date();
    from.setUTCDate(from.getUTCDate() - 29);
    return from.toISOString().slice(0, 10);
  });
  const [customTo, setCustomTo] = useState(todayIso);
  const requestRange = useMemo(() => {
    if (range === "custom") return { from: customFrom, to: customTo };
    const days = Number(range);
    const to = new Date();
    const from = new Date(to);
    from.setUTCDate(from.getUTCDate() - (days - 1));
    return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) };
  }, [range, customFrom, customTo]);
  const { data: initialData, loading, error } = useAsync(
    () => api.admin.metrics(requestRange.from, requestRange.to),
    [requestRange.from, requestRange.to]
  );
  const [data, setData] = useState<typeof initialData>(null);
  const [streamState, setStreamState] = useState<"connecting" | "live" | "reconnecting">("connecting");
  const [segment, setSegment] = useState("all");
  const [lastUpdatedAt, setLastUpdatedAt] = useState(() => new Date());

  useEffect(() => { if (initialData) setData(initialData); }, [initialData]);

  useEffect(() => {
    let disposed = false;
    let attempt = 0;
    let reconnectTimer: number | undefined;
    let controller: AbortController | undefined;
    const connect = () => {
      controller = new AbortController();
      setStreamState(attempt === 0 ? "connecting" : "reconnecting");
      void api.admin.metricsStream(requestRange.from, requestRange.to, (payload) => {
        if (disposed) return;
        attempt = 0;
        setData(payload);
        setStreamState("live");
      }, controller.signal).catch(() => {
        if (disposed || controller?.signal.aborted) return;
        attempt += 1;
        reconnectTimer = window.setTimeout(connect, Math.min(30_000, 1_000 * 2 ** attempt));
      });
    };
    connect();
    return () => {
      disposed = true;
      controller?.abort();
      if (reconnectTimer) window.clearTimeout(reconnectTimer);
    };
  }, [requestRange.from, requestRange.to]);

  useEffect(() => {
    if (data) setLastUpdatedAt(new Date());
  }, [data]);

  const activeTrend = useMemo(() => {
    if (!data) return [];
    return range === "7" || range === "30" || range === "90"
      ? data.activeUsersTrend.slice(-Number(range))
      : data.activeUsersTrend;
  }, [data, range]);
  const signupsTrend = useMemo(() => {
    if (!data) return [];
    return range === "7" || range === "30" || range === "90"
      ? data.signupsTrend.slice(-Number(range))
      : data.signupsTrend;
  }, [data, range]);
  const rangeLabel = range === "custom" ? `${formatDate(customFrom)} — ${formatDate(customTo)}` : `${range} kun`;
  const visibleSegments = useMemo(
    () => data?.goalSegments.filter((item) => segment === "all" || item.goal === segment) ?? [],
    [data, segment]
  );

  if (loading && !data) {
    return <LoadingSkeleton variant="dashboard" label={uz.founder.loading} />;
  }

  if ((error && !data) || !data) {
    const forbidden = (error as { status?: number } | null)?.status === 403;
    return (
      <div className="founder-dashboard founder-dashboard--state">
        <div className="founder-state-card founder-state-card--error" role="alert">
          <span className="founder-state-card__icon"><Icon name={forbidden ? "lock" : "error"} filled /></span>
          <div>
            <strong>{forbidden ? uz.founder.forbiddenTitle : uz.founder.loadErrorTitle}</strong>
            <p>{forbidden ? uz.founder.forbidden : uz.founder.loadError}</p>
          </div>
        </div>
      </div>
    );
  }

  const { overview, retention, funnel, activationFunnel, goalSegments, planBreakdown } = data;

  return (
    <div className="founder-dashboard">
      <motion.header className="founder-hero" initial={{ opacity: 0, y: 14 }} animate={{ opacity: 1, y: 0 }}>
        <div className="founder-hero__copy">
          <span className="founder-hero__eyebrow"><Icon name="monitoring" filled /> Founder intelligence</span>
          <h1>{uz.founder.title}</h1>
          <p>{uz.founder.subtitle}</p>
          <span className="founder-hero__date">{uz.founder.asOf(formatDate(data.asOf))}</span>
        </div>
        <div className="founder-hero__actions">
          <Link to="/admin" className="founder-button founder-button--back">
            <Icon name="arrow_back" /> Orqaga
          </Link>
          <span className="founder-live" title="API ma'lumotlari har 15 soniyada yangilanadi">
            <i /> {streamState === "live" ? "Live SSE" : "Ulanmoqda"} · {lastUpdatedAt.toLocaleTimeString("uz-UZ", { hour: "2-digit", minute: "2-digit", second: "2-digit" })}
          </span>
        </div>
      </motion.header>

      <section className="founder-filters" aria-label="Metrics filters">
        <div className="founder-filter-group">
          <span className="founder-filter-label">Davr</span>
          <div className="founder-range" role="group" aria-label="Sana oralig'i">
            {(["7", "30", "90"] as Range[]).map((days) => (
              <button key={days} type="button" className={range === days ? "is-active" : ""} onClick={() => setRange(days)}>
                {days} kun
              </button>
            ))}
            <button type="button" className={range === "custom" ? "is-active" : ""} onClick={() => setRange("custom")}>Custom</button>
          </div>
        </div>
        <label className="founder-select founder-select--date">
          <span>Boshlanish</span>
          <input type="date" value={customFrom} max={customTo} onChange={(event) => { setCustomFrom(event.target.value); setRange("custom"); }} />
        </label>
        <label className="founder-select founder-select--date">
          <span>Tugash</span>
          <input type="date" value={customTo} min={customFrom} max={todayIso()} onChange={(event) => { setCustomTo(event.target.value); setRange("custom"); }} />
        </label>
        <label className="founder-select">
          <span>Maqsad segmenti</span>
          <select value={segment} onChange={(event) => setSegment(event.target.value)}>
            <option value="all">Barcha segmentlar</option>
            {goalSegments.map((item) => <option key={item.goal} value={item.goal}>{goalSegmentLabel(item.goal)}</option>)}
          </select>
        </label>
        <div className="founder-filter-summary">
          <Icon name="date_range" />
          <span>{range === "custom" ? rangeLabel : `Oxirgi ${range} kunlik trend`}</span>
        </div>
      </section>

      {data.variableCosts && <CostSection costs={data.variableCosts} canManage={data.canManageVariableCostBudget} />}

      <section className="founder-command-grid" aria-label="Asosiy analitika ko'rinishi">
        <ChartCard title="Mahsulot salomatligi" tone="purple" meta="0–100 indeks">
          <ProductHealthRadarChart
            labels={["Faollik", "Stickiness", "D1 retention", "D7 retention", "Onboarding", "Konversiya"]}
            values={[overview.mau > 0 ? overview.dau / overview.mau * 100 : 0, overview.stickinessPct, retention.d1.ratePct, retention.d7.ratePct, overview.totalUsers > 0 ? overview.onboardedUsers / overview.totalUsers * 100 : 0, overview.conversionRatePct]}
          />
        </ChartCard>
        <div className="founder-command-summary">
          <span className="founder-command-summary__eyebrow">Executive snapshot</span>
          <strong>{pct(overview.stickinessPct)}</strong><p>DAU / MAU stickiness</p>
          <div className="founder-command-summary__rail"><i style={{ width: `${Math.min(100, overview.stickinessPct)}%` }} /></div>
          <dl>
            <div><dt>Onboarding</dt><dd>{pct(overview.totalUsers > 0 ? overview.onboardedUsers / overview.totalUsers * 100 : 0)}</dd></div>
            <div><dt>D7 retention</dt><dd>{pct(retention.d7.ratePct)}</dd></div>
            <div><dt>Premium CR</dt><dd>{pct(overview.conversionRatePct)}</dd></div>
            <div><dt>ARPPU</dt><dd>{uzs(overview.arppuUzs)}</dd></div>
          </dl>
        </div>
      </section>

      <Section title={uz.founder.sectionEngagement} icon="bolt" tone="lime" description="Mahsulotga qaytish chastotasi va faol auditoriya hajmi.">
        <div className="founder-stat-grid">
          <StatCard icon="local_fire_department" label={uz.founder.dau} value={overview.dau} tone="rose" />
          <StatCard icon="calendar_view_week" label={uz.founder.wau} value={overview.wau} tone="blue" />
          <StatCard icon="calendar_month" label={uz.founder.mau} value={overview.mau} tone="purple" />
          <StatCard icon="repeat" label={uz.founder.stickiness} value={pct(overview.stickinessPct)} tone="lime" />
        </div>
        <div className="founder-chart-grid">
          <ChartCard title={`${uz.founder.activeTrend} · ${rangeLabel}`} tone="blue">
            <TrendAreaChart points={activeTrend} color="#FBBF24" tooltipLabel={(n) => `${n} ${uz.founder.chartUsers}`} emptyLabel={uz.founder.noData} />
          </ChartCard>
          <ChartCard title={`${uz.founder.signupsTrend} · ${rangeLabel}`} tone="orange">
            <TrendBarChart points={signupsTrend} color="#FBBF24" tooltipLabel={(n) => `${n} ${uz.founder.chartSignups}`} emptyLabel={uz.founder.noData} />
          </ChartCard>
        </div>
      </Section>

      <Section title={uz.founder.sectionGrowth} icon="trending_up" tone="blue" description="Ro'yxatdan o'tish, onboarding va umumiy baza o'sishi.">
        <div className="founder-stat-grid">
          <StatCard icon="group" label={uz.founder.totalUsers} value={overview.totalUsers} tone="ink" />
          <StatCard icon="rocket_launch" label={uz.founder.onboarded} value={overview.onboardedUsers} tone="purple" />
          <StatCard icon="person_add" label={uz.founder.newToday} value={overview.newUsersToday} tone="orange" />
          <StatCard icon="event_available" label={uz.founder.new7d} value={overview.newUsers7d} tone="lime" />
        </div>
      </Section>

      <Section title={uz.founder.sectionActivationFunnel} icon="conversion_path" tone="purple" description={uz.founder.activationHint}>
        <ChartCard title="Activation yo'li" tone="purple">
          <ActivationPitch steps={activationFunnel} />
          {activationFunnel.length === 0 ? <EmptyState /> : (
            <>
              <ActivationFunnelChart labels={activationFunnel.map((step) => activationStepLabel(step.code))} counts={activationFunnel.map((step) => step.count)} tooltipLines={(index) => {
                const step = activationFunnel[index];
                if (!step) return [];
                const lines = [`${step.count} ${uz.founder.chartUsers}`, uz.founder.activationFromStart(pct(step.rateFromRegisteredPct))];
                if (index > 0) lines.push(uz.founder.activationFromPrevious(pct(step.rateFromPreviousPct)));
                return lines;
              }} />
              <div className="founder-table founder-table--activation">
                <div className="founder-table__head"><span>#</span><span>{uz.founder.activationColStep}</span><span>{uz.founder.activationColCount}</span><span>{uz.founder.activationColStart}</span><span>{uz.founder.activationColDropoff}</span></div>
                {activationFunnel.map((step, index) => <ActivationDropoffRow key={step.code} step={step} index={index} />)}
              </div>
            </>
          )}
        </ChartCard>
      </Section>

      <Section title={uz.founder.sectionRetention} icon="restart_alt" tone="orange" description={uz.founder.retentionHint}>
        <div className="founder-retention-grid">
          <RetentionCard label={uz.founder.d1} point={retention.d1} tone="rose" />
          <RetentionCard label={uz.founder.d7} point={retention.d7} tone="purple" />
          <RetentionCard label={uz.founder.d30} point={retention.d30} tone="blue" />
        </div>
        <ChartCard title={uz.founder.retentionChart} tone="orange">
          <RetentionBarChart labels={[uz.founder.d1, uz.founder.d7, uz.founder.d30]} rates={[retention.d1.ratePct, retention.d7.ratePct, retention.d30.ratePct]} tooltipLabel={(n) => `${n}%`} />
        </ChartCard>
      </Section>

      <Section title={uz.founder.sectionConversion} icon="payments" tone="ink" description="Premium o'tishlar, daromad va tariflar kesimi.">
        <div className="founder-stat-grid">
          <StatCard icon="workspace_premium" label={uz.founder.premium} value={overview.premiumUsers} tone="purple" />
          <StatCard icon="person" label={uz.founder.free} value={overview.freeUsers} tone="blue" />
          <StatCard icon="conversion_path" label={uz.founder.conversion} value={pct(overview.conversionRatePct)} tone="lime" />
          <StatCard icon="savings" label={uz.founder.mrr} value={uzs(overview.mrrUzs)} tone="orange" />
        </div>
        <div className="founder-chart-grid founder-chart-grid--revenue">
          <ChartCard title={uz.founder.sectionFunnel} tone="purple">
            <ConversionFunnelChart labels={[uz.founder.funnelRegistered, uz.founder.funnelOnboarded, uz.founder.funnelActive, uz.founder.funnelPremium]} counts={[funnel.registered, funnel.onboarded, funnel.active7d, funnel.premium]} tooltipLines={(index) => {
              const counts = [funnel.registered, funnel.onboarded, funnel.active7d, funnel.premium];
              const share = funnel.registered > 0 ? Math.round((counts[index] / funnel.registered) * 1000) / 10 : 0;
              return [`${counts[index]} ${uz.founder.chartUsers}`, uz.founder.activationFromStart(pct(share))];
            }} />
          </ChartCard>
          <ChartCard title={uz.founder.sectionPlans} tone="blue" meta={`${uz.founder.arppu}: ${uzs(overview.arppuUzs)}`}>
            {planBreakdown.length === 0 ? <EmptyState label={uz.founder.noPlans} /> : <PlanDoughnutChart labels={planBreakdown.map((item) => planLabel(item.plan))} values={planBreakdown.map((item) => item.count)} centerLabel={uz.founder.planUsers} centerValue={planBreakdown.reduce((sum, item) => sum + item.count, 0)} tooltipLabel={(label, count) => `${label}: ${count}`} />}
          </ChartCard>
        </div>
        {planBreakdown.length > 0 && <div className="founder-table-wrap" tabIndex={0} aria-label="Tariflar jadvali"><div className="founder-table founder-table--plans"><div className="founder-table__head"><span>{uz.founder.planName}</span><span>{uz.founder.planUsers}</span><span>{uz.founder.planMrr}</span></div>{planBreakdown.map((item) => <div className="founder-table__row" key={item.plan}><strong>{planLabel(item.plan)}</strong><span>{item.count}</span><span>{uzs(item.mrrUzs)}</span></div>)}</div></div>}
      </Section>

      <Section title={uz.founder.sectionGoals} icon="flag" tone="rose" description={uz.founder.goalsHint}>
        {visibleSegments.length === 0 ? <EmptyState /> : <>
          <div className="founder-chart-grid">
            <ChartCard title="Auditoriya taqsimoti" tone="purple" meta="Pie chart"><GoalSegmentPieChart labels={visibleSegments.map((item) => goalSegmentLabel(item.goal))} values={visibleSegments.map((item) => item.users)} /></ChartCard>
            <ChartCard title="Segment konversiyasi" tone="lime" meta="Premium ulushi"><GoalConversionChart labels={visibleSegments.map((item) => goalSegmentLabel(item.goal))} values={visibleSegments.map((item) => item.conversionRatePct)} /></ChartCard>
          </div>
          <div className="founder-table-wrap" tabIndex={0} aria-label="Maqsadlar jadvali"><div className="founder-table founder-table--goals"><div className="founder-table__head"><span>{uz.founder.goalColGoal}</span><span>{uz.founder.goalColUsers}</span><span>{uz.founder.goalColPremium}</span><span>{uz.founder.goalColConversion}</span><span>{uz.founder.goalColMrr}</span></div>{visibleSegments.map((item) => <div className="founder-table__row" key={item.goal}><strong>{goalSegmentLabel(item.goal)}</strong><span>{item.users}</span><span>{item.premiumUsers}</span><span>{pct(item.conversionRatePct)}</span><span>{uzs(item.mrrUzs)}</span></div>)}</div></div>
        </>}
      </Section>

      <Section title="Auditoriya demografiyasi" icon="groups" tone="blue" description="Profil savollarini to‘ldirish va auditoriya tarkibini kuzating.">
        <div className="founder-stat-grid">
          <StatCard icon="task_alt" label="Profil to‘ldirilgan" value={data.demographics.completed} tone="lime" />
          <StatCard icon="pending" label="To‘ldirilmagan" value={data.demographics.missing} tone="orange" />
          <StatCard icon="analytics" label="To‘ldirish darajasi" value={pct(data.demographics.completionRatePct)} tone="blue" />
        </div>
        <div className="founder-chart-grid">
          <ChartCard title="Jins taqsimoti" tone="purple"><DemographicDoughnutChart ariaLabel="Jins taqsimoti" labels={data.demographics.gender.map(item => demographicLabel(item.label))} values={data.demographics.gender.map(item => item.count)} /></ChartCard>
          <ChartCard title="Yosh guruhlari" tone="lime"><DemographicBarChart ariaLabel="Yosh guruhlari" labels={data.demographics.ageGroups.map(item => item.label)} values={data.demographics.ageGroups.map(item => item.count)} /></ChartCard>
          <ChartCard title="Qayerdan eshitgan" tone="orange"><DemographicDoughnutChart ariaLabel="Eshitish manbalari" labels={data.demographics.acquisitionSources.map(item => demographicLabel(item.label))} values={data.demographics.acquisitionSources.map(item => item.count)} /></ChartCard>
        </div>
      </Section>
    </div>
  );
}

function Section({ title, icon, tone, description, children }: { title: string; icon: string; tone: Tone; description: string; children: React.ReactNode }) {
  return <motion.section className={`founder-section founder-tone--${tone}`} initial={{ opacity: 0, y: 14 }} animate={{ opacity: 1, y: 0 }}><div className="founder-section__heading"><span className="founder-section__icon"><Icon name={icon} filled /></span><div><h2>{title}</h2><p>{description}</p></div></div><div className="founder-section__body">{children}</div></motion.section>;
}

function ChartCard({ title, tone, meta, children }: { title: string; tone: Tone; meta?: string; children: React.ReactNode }) {
  return <div className={`founder-chart-card founder-tone--${tone}`}><div className="founder-chart-card__heading"><h3>{title}</h3>{meta && <span>{meta}</span>}</div><div className="founder-chart-card__canvas">{children}</div></div>;
}

function StatCard({ icon, label, value, tone }: { icon: string; label: string; value: string | number; tone: Tone }) {
  return <motion.article className={`founder-stat-card founder-tone--${tone}`} initial={{ opacity: 0, scale: .98 }} animate={{ opacity: 1, scale: 1 }}><span className="founder-stat-card__icon"><Icon name={icon} filled /></span><div><strong>{value}</strong><span>{label}</span></div></motion.article>;
}

function ActivationPitch({ steps }: { steps: ActivationFunnelStepDto[] }) {
  const count = (code: string) => steps.find((step) => step.code === code)?.count ?? 0;
  return <div className="founder-pitch"><Icon name="lightbulb" filled /><p>{uz.founder.activationPitch(count("registered"), count("placementCompleted"), count("firstSpeakingSessionCompleted"), count("day7Active"))}</p></div>;
}

function ActivationDropoffRow({ step, index }: { step: ActivationFunnelStepDto; index: number }) {
  const dropoff = index === 0 ? 0 : Math.max(0, 100 - step.rateFromPreviousPct);
  const dropoffLabel = dropoff === 0 ? pct(0) : `-${pct(dropoff)}`;
  return <div className="founder-table__row"><span className="founder-step">{index + 1}</span><strong>{activationStepLabel(step.code)}</strong><span>{step.count}</span><span>{pct(step.rateFromRegisteredPct)}</span><span className={dropoff >= 40 ? "is-risk" : ""}>{index === 0 ? "—" : dropoffLabel}</span></div>;
}

function RetentionCard({ label, point, tone }: { label: string; point: RetentionPointDto; tone: Tone }) {
  const measurable = point.cohortSize > 0;
  return <article className={`founder-retention-card founder-tone--${tone}`}><div><strong>{label}</strong><span>{uz.founder.cohort(point.cohortSize)}</span></div>{measurable ? <><b>{pct(point.ratePct)}</b><div className="founder-progress"><i style={{ width: `${Math.min(100, point.ratePct)}%` }} /></div><p>{uz.founder.retainedOf(point.retained, point.cohortSize)}</p></> : <p>{uz.founder.noCohort}</p>}</article>;
}

function EmptyState({ label = uz.founder.noData }: { label?: string }) {
  return <div className="founder-empty"><Icon name="inbox" /><span>{label}</span></div>;
}

function CostSection({ costs: initialCosts, canManage }: { costs: NonNullable<import("@/api/types").FounderMetricsDto["variableCosts"]>; canManage: boolean }) {
  const [costs, setCosts] = useState(initialCosts);
  const [budgetInput, setBudgetInput] = useState(() => String(initialCosts.dailyBudgetLimitUsd));
  const [budgetState, setBudgetState] = useState<"idle" | "saving" | "saved" | "error">("idle");
  useEffect(() => {
    setCosts(initialCosts);
    setBudgetInput(String(initialCosts.dailyBudgetLimitUsd));
    setBudgetState("idle");
  }, [initialCosts]);
  const limit = Math.max(0, costs.dailyBudgetLimitUsd);
  const usagePct = limit > 0 ? Math.min(100, costs.dailyBudgetUsedUsd / limit * 100) : 0;
  const category = (key: string) => costs.categories.find((item) => item.category === key)?.estimatedCostUsd ?? 0;

  const saveBudget = async (event: React.FormEvent) => {
    event.preventDefault();
    const nextBudget = Number(budgetInput);
    if (!Number.isFinite(nextBudget) || nextBudget <= 0 || nextBudget > 10_000) {
      setBudgetState("error");
      return;
    }
    setBudgetState("saving");
    try {
      const updated = await api.admin.setVariableCostBudget(nextBudget);
      setCosts(updated);
      setBudgetInput(String(updated.dailyBudgetLimitUsd));
      setBudgetState("saved");
    } catch {
      setBudgetState("error");
    }
  };

  return <Section title="AI va Speech xarajatlari" icon="payments" tone="orange" description="Bugungi o‘zgaruvchan provider xarajati, budget va qaysi oqim ko‘p sarflayotganini kuzating.">
    {!costs.pricingConfigured && <div className="founder-cost-alert" role="alert"><Icon name="warning" filled /><div><strong>Narxlar to‘liq sozlanmagan</strong><span>Ko‘rsatilgan jami real hisobdan past bo‘lishi mumkin. AI va Azure Speech pricing qiymatlarini tekshiring.</span></div></div>}
    {canManage && <form className="founder-cost-editor" onSubmit={saveBudget}>
      <div><strong>Kunlik xarajat limiti</strong><span>UTC bo‘yicha har kuni yangilanadigan umumiy AI va Speech budget.</span></div>
      <label><span>USD</span><input aria-label="Kunlik AI budgeti" type="number" min="0.01" max="10000" step="0.01" value={budgetInput} onChange={(event) => { setBudgetInput(event.target.value); setBudgetState("idle"); }} /></label>
      <button type="submit" disabled={budgetState === "saving"}>{budgetState === "saving" ? "Saqlanmoqda..." : "Limitni saqlash"}</button>
      {budgetState === "saved" && <small className="is-success">Yangi limit saqlandi.</small>}
      {budgetState === "error" && <small className="is-error">$0.01–$10,000 oralig‘idagi qiymat kiriting.</small>}
    </form>}
    <div className="founder-stat-grid founder-stat-grid--costs">
      <StatCard icon="today" label="Bugungi jami" value={usd(costs.dailyBudgetUsedUsd)} tone="orange" />
      <StatCard icon="savings" label="Kunlik limit" value={limit > 0 ? usd(limit) : "Cheklanmagan"} tone="blue" />
      <StatCard icon="smart_toy" label="AI tokenlar" value={usd(category("ai"))} tone="purple" />
      {/* Voice Live belongs in the speech total: it is the largest per-minute Azure cost, and this
          card is the one place the spend is meant to be visible. */}
      <StatCard icon="record_voice_over" label="Speech jami" value={usd(category("speech_to_text") + category("text_to_speech") + category("pronunciation") + category("voice_live"))} tone="rose" />
    </div>
    <div className={`founder-cost-budget${costs.freeTierShed ? " is-exhausted" : costs.dailyBudgetWarning ? " is-warning" : ""}`}>
      <div><strong>Budget ishlatilishi</strong><span>{limit > 0 ? `${pct(usagePct)} · ${usd(costs.dailyBudgetUsedUsd)} / ${usd(limit)}` : `${usd(costs.dailyBudgetUsedUsd)} · limit yo‘q`}</span></div>
      <div className="founder-cost-budget__bar" aria-label="Kunlik budget ishlatilishi"><i style={{ width: `${usagePct}%` }} /></div>
      {costs.freeTierShed && <p>Bepul foydalanuvchilar uchun AI va speech so‘rovlari ertangi UTC kungacha vaqtincha cheklangan.</p>}
    </div>
    <div className="founder-cost-category-grid">
      {costs.categories.length === 0 ? <EmptyState label="Bugun hali provider xarajati qayd etilmagan." /> : costs.categories.map((item) => <article className="founder-cost-category" key={item.category}>
        <div><strong>{costCategoryLabel(item.category)}</strong><span>{item.requests} so‘rov</span></div>
        <b>{usd(item.estimatedCostUsd)}</b>
        <p>{formatCostUnits(item.units, item.unit)}</p>
      </article>)}
    </div>
    <div className="founder-cost-lists">
      <CostAttributionList title="Top endpointlar" items={costs.topEndpoints} empty="Endpoint xarajati hali yo‘q." />
      <CostAttributionList title="Top foydalanuvchilar" items={costs.topCallers} empty="Foydalanuvchi xarajati hali yo‘q." />
    </div>
  </Section>;
}

function CostAttributionList({ title, items, empty }: { title: string; items: VariableCostAttributionSnapshotDto[]; empty: string }) {
  return <div className="founder-cost-list"><h3>{title}</h3>{items.length === 0 ? <p>{empty}</p> : <ol>{items.map((item) => <li key={item.key}><span title={item.key}>{item.key}</span><b>{usd(item.estimatedCostUsd)}</b><small>{item.requests} so‘rov</small></li>)}</ol>}</div>;
}

function activationStepLabel(code: string): string {
  switch (code) {
    case "registered": return uz.founder.activationRegistered;
    case "usernameSetupCompleted": return uz.founder.activationUsername;
    case "placementStarted": return uz.founder.activationPlacementStarted;
    case "placementCompleted": return uz.founder.activationPlacementCompleted;
    case "firstTopicOpened": return uz.founder.activationTopicOpened;
    case "firstSpeakingSessionCompleted": return uz.founder.activationSpeakingCompleted;
    case "firstPronunciationFeedbackViewed": return uz.founder.activationFeedbackViewed;
    case "day1Returned": return uz.founder.activationDay1Returned;
    case "day7Active": return uz.founder.activationDay7Active;
    case "monetizationIntent": return uz.founder.activationMonetization;
    default: return code;
  }
}

const pct = (number: number): string => `${Math.round(number * 10) / 10}%`;
const uzs = (number: number): string => `${Math.round(number).toLocaleString("ru-RU").replace(/,/g, " ")} so'm`;
const usd = (number: number): string => `$${number.toLocaleString("en-US", { minimumFractionDigits: number < 1 ? 4 : 2, maximumFractionDigits: number < 1 ? 4 : 2 })}`;

function costCategoryLabel(category: string): string {
  switch (category) {
    case "ai": return "AI tokenlar";
    case "speech_to_text": return "Speech-to-Text";
    case "text_to_speech": return "Text-to-Speech";
    case "pronunciation": return "Talaffuz tahlili";
    case "voice_live": return "Jonli tutor (Voice Live)";
    default: return category;
  }
}

function formatCostUnits(units: number, unit: string): string {
  if (unit === "audio_hour") return `${units.toFixed(4)} audio soat`;
  if (unit === "character") return `${Math.round(units).toLocaleString("uz-UZ")} belgi`;
  if (unit === "token") return `${Math.round(units).toLocaleString("uz-UZ")} token`;
  return `${Math.round(units * 1000) / 1000} ${unit}`;
}

function planLabel(plan: string): string {
  switch (plan) {
    case "Monthly": return uz.founder.planMonthly;
    case "Quarterly": return uz.founder.planQuarterly;
    case "SemiAnnual": return uz.founder.planSemiAnnual;
    case "Yearly": return uz.founder.planYearly;
    default: return plan;
  }
}

function goalSegmentLabel(goal: string): string {
  switch (goal) {
    case "IeltsCefr": return uz.onboardingGoal.labels.ieltsCefr;
    case "Work": return uz.onboardingGoal.labels.work;
    case "Migration": return uz.onboardingGoal.labels.migration;
    case "Travel": return uz.onboardingGoal.labels.travel;
    case "GeneralSpeaking": return uz.onboardingGoal.labels.generalSpeaking;
    case "School": return uz.onboardingGoal.labels.school;
    default: return uz.onboardingGoal.labels.unspecified;
  }
}

function demographicLabel(label: string): string {
  switch (label) {
    case "Male": return "Erkak";
    case "Female": return "Ayol";
    case "Telegram": return "Telegram";
    case "Instagram": return "Instagram";
    case "Google": return "Google";
    case "AiAssistants": return "AI yordamchilari";
    case "FriendReferral": return "Do‘st-tanish";
    case "Other": return "Boshqa";
    default: return label;
  }
}
