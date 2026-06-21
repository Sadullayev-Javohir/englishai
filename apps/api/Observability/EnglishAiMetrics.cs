using System.Diagnostics.Metrics;
using Application.Ai;
using Application.Video.Ports;
using Hangfire;
using Infrastructure.Llm;

namespace Web.Observability;

public sealed class EnglishAiMetrics
{
    public EnglishAiMetrics(
        IServiceProvider services,
        ILogger<EnglishAiMetrics> logger)
    {
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.ai.active", () => ReadAi(services, snapshot => snapshot.Active));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.ai.queued", () => ReadAi(services, snapshot => snapshot.Queued));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.ai.rejected", () => ReadAi(services, snapshot => snapshot.Rejected));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.ai.failed", () => ReadAi(services, snapshot => snapshot.Failed));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.ai.circuit.open", () => ReadAi(services, snapshot => snapshot.CircuitOpen ? 1L : 0L));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.ai.admission.active", () => ReadAdmission(services, snapshot => snapshot.Active));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.ai.admission.queued", () => ReadAdmission(services, snapshot => snapshot.Queued));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.ai.admission.rejected", () => ReadAdmission(services, snapshot => snapshot.Rejected));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.ai.requests", () => ReadAdmission(services, snapshot => snapshot.Requests));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.ai.input_tokens.estimated", () => ReadAdmission(services, snapshot => snapshot.EstimatedInputTokens));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.ai.output_tokens.estimated", () => ReadAdmission(services, snapshot => snapshot.EstimatedOutputTokens));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.ai.daily_budget.used_usd", () => ReadAdmissionDouble(services, snapshot => snapshot.DailyBudgetUsedUsd));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.ai.daily_budget.warning", () => ReadAdmission(services, snapshot => snapshot.DailyBudgetWarning ? 1 : 0));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.ai.daily_budget.free_shed", () => ReadAdmission(services, snapshot => snapshot.FreeTierShed ? 1 : 0));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.variable_cost.daily.used_usd", () => ReadCost(services, snapshot => snapshot.DailyBudgetUsedUsd));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.variable_cost.daily.limit_usd", () => ReadCost(services, snapshot => snapshot.DailyBudgetLimitUsd));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.variable_cost.pricing_configured", () => ReadCostLong(services, snapshot => snapshot.PricingConfigured ? 1 : 0));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.variable_cost.category.cost_usd", () => ReadCostCategories(
                services,
                item => new Measurement<double>(item.EstimatedCostUsd, new KeyValuePair<string, object?>("category", item.Category))));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.variable_cost.category.units", () => ReadCostCategories(
                services,
                item => new Measurement<double>(
                    item.Units,
                    new KeyValuePair<string, object?>("category", item.Category),
                    new KeyValuePair<string, object?>("unit", item.Unit))));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.variable_cost.category.requests", () => ReadCostCategories(
                services,
                item => new Measurement<double>(item.Requests, new KeyValuePair<string, object?>("category", item.Category))));

        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.video_ai.active", () => ReadVideo(services, snapshot => snapshot.Active));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.video_ai.queued", () => ReadVideo(services, snapshot => snapshot.Queued));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.video_ai.rejected", () => ReadVideo(services, snapshot => snapshot.Rejected));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.video_ai.failed", () => ReadVideo(services, snapshot => snapshot.Failed));
        EnglishAiTelemetry.Meter.CreateObservableCounter(
            "englishai.video_ai.circuit_rejected", () => ReadVideo(services, snapshot => snapshot.CircuitOpenRejections));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.video_ai.circuit.open", () => ReadVideo(services, snapshot => snapshot.CircuitOpen ? 1L : 0L));

        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.hangfire.enqueued", () => ReadHangfire(logger, monitoring => monitoring.EnqueuedCount("default")));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.hangfire.processing", () => ReadHangfire(logger, monitoring => monitoring.ProcessingCount()));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.hangfire.scheduled", () => ReadHangfire(logger, monitoring => monitoring.ScheduledCount()));
        EnglishAiTelemetry.Meter.CreateObservableGauge(
            "englishai.hangfire.failed", () => ReadHangfire(logger, monitoring => monitoring.FailedCount()));
    }

    private static long ReadAi(IServiceProvider services, Func<HermesGatewaySnapshot, long> select)
    {
        var gateway = services.GetService<ResilientHermesGatewayLlmCompletion>();
        return gateway is null ? 0 : select(gateway.Snapshot());
    }

    private static long ReadVideo(IServiceProvider services, Func<VideoExplainSnapshot, long> select)
    {
        var coordinator = services.GetService<IVideoExplainCoordinator>();
        return coordinator is null ? 0 : select(coordinator.Snapshot());
    }

    private static long ReadAdmission(IServiceProvider services, Func<AiAdmissionSnapshot, long> select)
    {
        var admission = services.GetService<IAiAdmissionControl>();
        return admission is null ? 0 : select(admission.Snapshot());
    }

    private static double ReadAdmissionDouble(IServiceProvider services, Func<AiAdmissionSnapshot, double> select)
    {
        var admission = services.GetService<IAiAdmissionControl>();
        return admission is null ? 0 : select(admission.Snapshot());
    }

    private static double ReadCost(IServiceProvider services, Func<VariableCostSnapshot, double> select)
    {
        var costs = services.GetService<IVariableCostMeter>();
        return costs is null ? 0 : select(costs.Snapshot());
    }

    private static long ReadCostLong(IServiceProvider services, Func<VariableCostSnapshot, long> select)
    {
        var costs = services.GetService<IVariableCostMeter>();
        return costs is null ? 0 : select(costs.Snapshot());
    }

    private static IEnumerable<Measurement<double>> ReadCostCategories(
        IServiceProvider services,
        Func<VariableCostCategorySnapshot, Measurement<double>> select)
    {
        var costs = services.GetService<IVariableCostMeter>();
        return costs is null
            ? Array.Empty<Measurement<double>>()
            : costs.Snapshot().Categories.Select(select).ToArray();
    }

    private static long ReadHangfire(ILogger logger, Func<Hangfire.Storage.IMonitoringApi, long> select)
    {
        try
        {
            return select(JobStorage.Current.GetMonitoringApi());
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Hangfire metrics snapshot unavailable.");
            return 0;
        }
    }
}
