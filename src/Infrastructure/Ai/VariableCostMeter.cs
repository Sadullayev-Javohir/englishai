using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Application.Ai;
using Infrastructure.Redis;
using Infrastructure.Speaking;
using StackExchange.Redis;

namespace Infrastructure.Ai;

public sealed class VariableCostMeter : IVariableCostMeter
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(8);
    private const int AttributionLimit = 10;
    private const string DailyBudgetOverrideSuffix = "daily-budget-usd";
    private readonly AiAdmissionOptions _options;
    private readonly TimeProvider _clock;
    private readonly IRedisConnectionProvider? _redis;
    private readonly AzureSpeechOptions? _speech;
    private readonly ConcurrentDictionary<string, CategoryState> _localCategories = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, AttributionState> _localCallers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, AttributionState> _localEndpoints = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private DateOnly _localDay;
    private double? _localDailyBudgetOverride;

    public VariableCostMeter(
        AiAdmissionOptions options,
        TimeProvider clock,
        IRedisConnectionProvider? redis = null,
        AzureSpeechOptions? speech = null)
    {
        _options = options;
        _clock = clock;
        _redis = redis;
        _speech = speech;
        _localDay = Today;
    }

    public VariableCostSnapshot Snapshot()
    {
        var day = Today;
        var state = _redis is null ? ReadLocal() : ReadRedis(day);
        // Reconciled reservations subtract, so floating-point residue could leave the sum a hair
        // below zero. The budget must never read as "negative spend".
        var used = Math.Max(0, state.Categories.Sum(item => item.EstimatedCostUsd));
        var limit = ResolveDailyBudgetLimit();
        return new VariableCostSnapshot(
            day,
            used,
            limit,
            limit > 0 && used >= limit * Math.Clamp(_options.BudgetWarningRatio, 0.01, 1),
            limit > 0 && used >= limit,
            _options.PricingConfigured && (_speech is null || _speech.PricingConfigured),
            state.Categories,
            state.Callers,
            state.Endpoints);
    }

    public async Task SetDailyBudgetAsync(double dailyBudgetUsd, CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(dailyBudgetUsd) || dailyBudgetUsd <= 0 || dailyBudgetUsd > 10_000)
            throw new ArgumentOutOfRangeException(nameof(dailyBudgetUsd));

        if (_redis is not null)
        {
            var db = _redis.Get(RedisWorkload.Critical).GetDatabase();
            await db.StringSetAsync(DailyBudgetOverrideKey(), dailyBudgetUsd.ToString("R", CultureInfo.InvariantCulture))
                .WaitAsync(cancellationToken);
        }

        lock (_gate)
            _localDailyBudgetOverride = dailyBudgetUsd;
    }

    public void EnsureAllowed(AiSubscriptionTier tier, AiFeature feature, string? callerKey = null)
    {
        // Translation is exempt: it is a small, cheap call that a learner reaches mid-lesson, and
        // shedding it strands them on a word they cannot read.
        if (feature == AiFeature.Translation)
            return;

        // Checked for EVERY tier, and before the global budget. The global budget is all-or-nothing:
        // one runaway account can drain it and shed every other free learner for the rest of the
        // day. Bounding the individual first means the group never pays for one account's usage.
        EnsurePerLearnerBudget(tier, callerKey);

        if (tier == AiSubscriptionTier.Pro)
            return;

        var snapshot = Snapshot();
        if (!snapshot.FreeTierShed)
            return;

        throw new AiAdmissionException(
            "budget_exhausted",
            "Daily AI and speech budget is exhausted for free accounts.",
            SecondsUntilTomorrow(),
            429);
    }

    private void EnsurePerLearnerBudget(AiSubscriptionTier tier, string? callerKey)
    {
        var limit = _options.DailyBudgetPerLearnerUsd(tier);
        if (limit <= 0 || string.IsNullOrWhiteSpace(callerKey))
            return;

        var spent = CallerCostUsd(Normalize(callerKey, "anonymous", 96));
        if (spent < limit)
            return;

        throw new AiAdmissionException(
            "learner_budget_exhausted",
            "Today's AI and speech allowance for this account is used up.",
            SecondsUntilTomorrow(),
            429);
    }

    /// <summary>
    /// What this caller has spent today. Read directly rather than from
    /// <see cref="Snapshot"/>, whose attribution lists are truncated to the top spenders - a learner
    /// outside that top-N would otherwise read as having spent nothing.
    /// </summary>
    private double CallerCostUsd(string caller)
    {
        var day = Today;
        if (_redis is not null)
        {
            try
            {
                var score = _redis.Get(RedisWorkload.Critical).GetDatabase()
                    .SortedSetScore(CallersKey(day), caller);
                if (score.HasValue)
                    return Math.Max(0, score.Value);
            }
            catch
            {
                // Fall through to the local view rather than failing the learner's request.
            }
        }

        lock (_gate)
        {
            ResetLocalIfNeeded();
            return _localCallers.TryGetValue(caller, out var state) ? Math.Max(0, state.CostUsd) : 0;
        }
    }

    public void Record(
        string category,
        double units,
        string unit,
        double estimatedCostUsd,
        string? callerKey = null,
        AiFeature feature = AiFeature.Other,
        string? requestPath = null)
    {
        var normalizedCategory = Normalize(category, "other", 64);
        var normalizedUnit = Normalize(unit, "unit", 32);
        var normalizedCaller = Normalize(callerKey, "anonymous", 96);
        var normalizedEndpoint = Normalize(requestPath, feature.ToString(), 160);
        var safeUnits = Math.Max(0, units);
        var safeCost = Math.Max(0, estimatedCostUsd);

        Write(normalizedCategory, normalizedUnit, normalizedCaller, normalizedEndpoint, safeUnits, safeCost, countRequest: true);
    }

    public void RecordAdjustment(
        string category,
        double unitsDelta,
        string unit,
        double estimatedCostDeltaUsd,
        string? callerKey = null,
        AiFeature feature = AiFeature.Other,
        string? requestPath = null)
    {
        // Deltas are deliberately not clamped to zero: reconciling a reservation means subtracting.
        // Non-finite values would poison the running totals irrecoverably, so they are dropped.
        if (!double.IsFinite(unitsDelta) || !double.IsFinite(estimatedCostDeltaUsd))
            return;
        if (unitsDelta == 0 && estimatedCostDeltaUsd == 0)
            return;

        Write(
            Normalize(category, "other", 64),
            Normalize(unit, "unit", 32),
            Normalize(callerKey, "anonymous", 96),
            Normalize(requestPath, feature.ToString(), 160),
            unitsDelta,
            estimatedCostDeltaUsd,
            countRequest: false);
    }

    private void Write(
        string category,
        string unit,
        string caller,
        string endpoint,
        double units,
        double cost,
        bool countRequest)
    {
        if (_redis is not null)
        {
            try
            {
                RecordRedis(Today, category, unit, caller, endpoint, units, cost, countRequest);
                RecordLocal(category, unit, caller, endpoint, units, cost, countRequest);
                return;
            }
            catch
            {
            }
        }

        RecordLocal(category, unit, caller, endpoint, units, cost, countRequest);
    }

    private void RecordRedis(
        DateOnly day,
        string category,
        string unit,
        string caller,
        string endpoint,
        double units,
        double cost,
        bool countRequest)
    {
        var db = _redis!.Get(RedisWorkload.Critical).GetDatabase();
        var categoryKey = CategoryKey(day);
        var callersKey = CallersKey(day);
        var callerRequestsKey = CallerRequestsKey(day);
        var endpointsKey = EndpointsKey(day);
        var endpointRequestsKey = EndpointRequestsKey(day);
        var batch = db.CreateBatch();
        var tasks = new List<Task>
        {
            batch.HashIncrementAsync(categoryKey, $"{category}:units", units),
            batch.HashIncrementAsync(categoryKey, $"{category}:cost", cost),
            batch.HashSetAsync(categoryKey, $"{category}:unit", unit),
            batch.SetAddAsync(CategoriesKey(day), category),
            batch.SortedSetIncrementAsync(callersKey, caller, cost),
            batch.SortedSetIncrementAsync(endpointsKey, endpoint, cost),
            batch.KeyExpireAsync(categoryKey, Ttl),
            batch.KeyExpireAsync(CategoriesKey(day), Ttl),
            batch.KeyExpireAsync(callersKey, Ttl),
            batch.KeyExpireAsync(callerRequestsKey, Ttl),
            batch.KeyExpireAsync(endpointsKey, Ttl),
            batch.KeyExpireAsync(endpointRequestsKey, Ttl),
        };
        if (countRequest)
        {
            tasks.Add(batch.HashIncrementAsync(categoryKey, $"{category}:requests", 1));
            tasks.Add(batch.HashIncrementAsync(callerRequestsKey, caller, 1));
            tasks.Add(batch.HashIncrementAsync(endpointRequestsKey, endpoint, 1));
        }
        batch.Execute();
        Task.WhenAll(tasks).GetAwaiter().GetResult();
    }

    private void RecordLocal(
        string category,
        string unit,
        string caller,
        string endpoint,
        double units,
        double cost,
        bool countRequest)
    {
        var requestDelta = countRequest ? 1 : 0;
        lock (_gate)
        {
            ResetLocalIfNeeded();
            _localCategories.AddOrUpdate(
                category,
                _ => new CategoryState(requestDelta, units, unit, cost),
                (_, current) => current with
                {
                    Requests = current.Requests + requestDelta,
                    Units = current.Units + units,
                    Unit = unit,
                    CostUsd = current.CostUsd + cost,
                });
            _localCallers.AddOrUpdate(
                caller,
                _ => new AttributionState(requestDelta, cost),
                (_, current) => current with
                {
                    Requests = current.Requests + requestDelta,
                    CostUsd = current.CostUsd + cost,
                });
            _localEndpoints.AddOrUpdate(
                endpoint,
                _ => new AttributionState(requestDelta, cost),
                (_, current) => current with
                {
                    Requests = current.Requests + requestDelta,
                    CostUsd = current.CostUsd + cost,
                });
        }
    }

    private SnapshotState ReadRedis(DateOnly day)
    {
        try
        {
            var db = _redis!.Get(RedisWorkload.Critical).GetDatabase();
            var categories = db.SetMembers(CategoriesKey(day))
                .Select(value => value.ToString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Order(StringComparer.Ordinal)
                .ToArray();
            var categoryKey = CategoryKey(day);
            var categorySnapshots = categories.Select(category =>
            {
                var values = db.HashGet(categoryKey, new RedisValue[]
                {
                    $"{category}:requests",
                    $"{category}:units",
                    $"{category}:unit",
                    $"{category}:cost",
                });
                return new VariableCostCategorySnapshot(
                    category,
                    ParseLong(values[0]),
                    ParseDouble(values[1]),
                    values[2].HasValue ? values[2].ToString() : "unit",
                    ParseDouble(values[3]));
            }).ToArray();

            var redisState = new SnapshotState(
                categorySnapshots,
                ReadAttribution(db, CallersKey(day), CallerRequestsKey(day)),
                ReadAttribution(db, EndpointsKey(day), EndpointRequestsKey(day)));
            return HasData(redisState) ? redisState : ReadLocal();
        }
        catch
        {
            return ReadLocal();
        }
    }

    private SnapshotState ReadLocal()
    {
        lock (_gate)
        {
            ResetLocalIfNeeded();
            return new SnapshotState(
                _localCategories
                    .OrderBy(item => item.Key, StringComparer.Ordinal)
                    .Select(item => new VariableCostCategorySnapshot(
                        item.Key,
                        item.Value.Requests,
                        item.Value.Units,
                        item.Value.Unit,
                        item.Value.CostUsd))
                    .ToArray(),
                ToAttribution(_localCallers),
                ToAttribution(_localEndpoints));
        }
    }

    private static IReadOnlyList<VariableCostAttributionSnapshot> ReadAttribution(
        IDatabase db,
        RedisKey sortedSetKey,
        RedisKey requestsKey)
    {
        var entries = db.SortedSetRangeByRankWithScores(
            sortedSetKey,
            0,
            AttributionLimit - 1,
            Order.Descending);
        if (entries.Length == 0)
            return Array.Empty<VariableCostAttributionSnapshot>();

        var requestValues = db.HashGet(requestsKey, entries.Select(entry => entry.Element).ToArray());
        return entries.Select((entry, index) => new VariableCostAttributionSnapshot(
            entry.Element.ToString(),
            ParseLong(requestValues[index]),
            Math.Max(0, entry.Score)))
            .ToArray();
    }

    private static IReadOnlyList<VariableCostAttributionSnapshot> ToAttribution(
        IEnumerable<KeyValuePair<string, AttributionState>> values) => values
        .OrderByDescending(item => item.Value.CostUsd)
        .ThenByDescending(item => item.Value.Requests)
        .Take(AttributionLimit)
        .Select(item => new VariableCostAttributionSnapshot(
            item.Key,
            item.Value.Requests,
            item.Value.CostUsd))
        .ToArray();

    private static bool HasData(SnapshotState state) =>
        state.Categories.Count > 0 || state.Callers.Count > 0 || state.Endpoints.Count > 0;

    private string CategoryKey(DateOnly day) =>
        _redis!.Key(RedisWorkload.Critical, "cost", $"daily:{day:yyyy-MM-dd}:categories-data");

    private string CategoriesKey(DateOnly day) =>
        _redis!.Key(RedisWorkload.Critical, "cost", $"daily:{day:yyyy-MM-dd}:categories");

    private string CallersKey(DateOnly day) =>
        _redis!.Key(RedisWorkload.Critical, "cost", $"daily:{day:yyyy-MM-dd}:callers");

    private string CallerRequestsKey(DateOnly day) =>
        _redis!.Key(RedisWorkload.Critical, "cost", $"daily:{day:yyyy-MM-dd}:caller-requests");

    private string EndpointsKey(DateOnly day) =>
        _redis!.Key(RedisWorkload.Critical, "cost", $"daily:{day:yyyy-MM-dd}:endpoints");

    private string EndpointRequestsKey(DateOnly day) =>
        _redis!.Key(RedisWorkload.Critical, "cost", $"daily:{day:yyyy-MM-dd}:endpoint-requests");

    private string DailyBudgetOverrideKey() =>
        _redis!.Key(RedisWorkload.Critical, "cost", DailyBudgetOverrideSuffix);

    private double ResolveDailyBudgetLimit()
    {
        if (_redis is not null)
        {
            try
            {
                var raw = _redis.Get(RedisWorkload.Critical).GetDatabase().StringGet(DailyBudgetOverrideKey());
                var configured = ParseDouble(raw);
                if (configured is > 0 and <= 10_000)
                {
                    lock (_gate)
                        _localDailyBudgetOverride = configured;
                    return configured;
                }
            }
            catch
            {
            }
        }

        lock (_gate)
            return Math.Max(0, _localDailyBudgetOverride ?? _options.DailyBudgetUsd);
    }

    private DateOnly Today => DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

    private void ResetLocalIfNeeded()
    {
        var today = Today;
        if (today == _localDay)
            return;
        _localDay = today;
        _localCategories.Clear();
        _localCallers.Clear();
        _localEndpoints.Clear();
    }

    private int SecondsUntilTomorrow()
    {
        var now = _clock.GetUtcNow();
        return Math.Max(1, (int)Math.Ceiling((now.Date.AddDays(1) - now).TotalSeconds));
    }

    private static string Normalize(string? value, string fallback, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        var trimmed = value.Trim();
        var builder = new StringBuilder(Math.Min(trimmed.Length, maximumLength));
        foreach (var character in trimmed)
        {
            if (builder.Length >= maximumLength)
                break;
            builder.Append(char.IsControl(character) ? '_' : character);
        }
        return builder.Length == 0 ? fallback : builder.ToString();
    }

    private static long ParseLong(RedisValue value) =>
        long.TryParse(value.ToString(), out var parsed) ? parsed : 0;

    private static double ParseDouble(RedisValue value) =>
        double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private sealed record CategoryState(long Requests, double Units, string Unit, double CostUsd);
    private sealed record AttributionState(long Requests, double CostUsd);
    private sealed record SnapshotState(
        IReadOnlyList<VariableCostCategorySnapshot> Categories,
        IReadOnlyList<VariableCostAttributionSnapshot> Callers,
        IReadOnlyList<VariableCostAttributionSnapshot> Endpoints);
}
