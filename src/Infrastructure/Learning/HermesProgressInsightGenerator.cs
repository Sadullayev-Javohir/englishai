using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using Domain.Learning;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Learning;

public sealed class HermesProgressInsightGenerator : IProgressInsightGenerator
{
    internal const int MaxOutputTokens = 600;
    private const int CacheLimit = 256;
    private static readonly TimeSpan InteractiveTimeout = TimeSpan.FromSeconds(2);
    internal static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
    private static readonly HashSet<string> OverallCodes =
        ["no_data", "getting_started", "strong_progress", "steady_progress", "needs_focus"];
    private static readonly HashSet<string> AchievementCodes =
        ["strong_skill", "positive_trend", "daily_goal_met", "vocabulary_growth"];
    private static readonly HashSet<string> EvidenceCodes =
        ["placement_baseline", "negative_trend", "lowest_score"];
    private static readonly HashSet<string> HabitCodes =
        ["no_study_data", "start_habit", "habit_at_risk", "habit_consistent", "habit_building"];

    private readonly ILlmCompletion _llm;
    private readonly LocalProgressInsightGenerator _fallback;
    private readonly TimeProvider _clock;
    private readonly ILogger<HermesProgressInsightGenerator> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<GeneratedProgressInsight>>> _inflight = new();

    public HermesProgressInsightGenerator(
        ILlmCompletion llm,
        LocalProgressInsightGenerator fallback,
        TimeProvider clock,
        ILogger<HermesProgressInsightGenerator> logger)
    {
        _llm = llm;
        _fallback = fallback;
        _clock = clock;
        _logger = logger;
    }

    public async Task<GeneratedProgressInsight> GenerateAsync(
        ProgressSnapshotDto snapshot, CancellationToken cancellationToken)
    {
        var payload = BuildPayload(snapshot);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var now = _clock.GetUtcNow();
        if (_cache.TryGetValue(fingerprint, out var cached) && cached.ExpiresAt > now)
            return cached.Value;

        var generation = _inflight.GetOrAdd(
            fingerprint,
            _ => new Lazy<Task<GeneratedProgressInsight>>(
                () => GenerateAndCacheAsync(fingerprint, snapshot, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await generation.Value.WaitAsync(cancellationToken);
        }
        finally
        {
            if (generation.IsValueCreated && generation.Value.IsCompleted)
                _inflight.TryRemove(new KeyValuePair<string, Lazy<Task<GeneratedProgressInsight>>>(fingerprint, generation));
        }
    }

    private async Task<GeneratedProgressInsight> GenerateAndCacheAsync(
        string fingerprint,
        ProgressSnapshotDto snapshot,
        CancellationToken cancellationToken)
    {
        var payload = BuildPayload(snapshot);
        var now = _clock.GetUtcNow();

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Progress statistics are authoritative and the local analyzer derives its result from
            // the same snapshot. External AI is optional enrichment, so it must never consume the
            // browser's regular 15-second request budget and make the whole dashboard look broken.
            timeout.CancelAfter(InteractiveTimeout);
            var response = await _llm.CompleteAsync(SystemPrompt, payload, MaxOutputTokens, timeout.Token);
            if (TryParse(response, snapshot, out var result))
            {
                Store(fingerprint, result!, now);
                return result!;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Hermes progress insight generation timed out; using local fallback");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Hermes progress insight generation failed; using local fallback");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await _fallback.GenerateAsync(snapshot, cancellationToken);
    }

    private void Store(string key, GeneratedProgressInsight value, DateTimeOffset now)
    {
        if (_cache.Count >= CacheLimit)
        {
            foreach (var expired in _cache.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key))
                _cache.TryRemove(expired, out _);
            if (_cache.Count >= CacheLimit)
            {
                var oldest = _cache.OrderBy(pair => pair.Value.ExpiresAt).FirstOrDefault();
                if (!string.IsNullOrEmpty(oldest.Key)) _cache.TryRemove(oldest.Key, out _);
            }
        }
        _cache[key] = new CacheEntry(value, now.Add(CacheTtl));
    }

    internal static bool TryParse(
        string? response, ProgressSnapshotDto snapshot, out GeneratedProgressInsight? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(response) || response.Contains("```", StringComparison.Ordinal)) return false;
        HermesResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<HermesResponse>(response, JsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }
        if (parsed is null || !OverallCodes.Contains(parsed.OverallCode) ||
            !HabitCodes.Contains(parsed.HabitCode) || parsed.AchievementCodes is null ||
            parsed.AchievementCodes.Count > 3 || parsed.AchievementCodes.Any(code => !AchievementCodes.Contains(code)) ||
            parsed.SkillsToStrengthen is null || parsed.SkillsToStrengthen.Count > 3 ||
            parsed.RecurringErrors is null || parsed.RecurringErrors.Count > 3)
            return false;

        var availableSkills = snapshot.Skills.Select(skill => skill.Skill).ToHashSet();
        var availableErrors = snapshot.ErrorsLast30Days.Select(error => error.Category).ToHashSet();
        var skills = new List<GeneratedSkillInsight>();
        foreach (var item in parsed.SkillsToStrengthen)
        {
            if (!Enum.TryParse<SkillType>(item.Skill, true, out var skill) || !availableSkills.Contains(skill) ||
                item.Priority is < 1 or > 3 || !EvidenceCodes.Contains(item.EvidenceCode) ||
                item.ActionCode != LocalProgressInsightGenerator.SkillAction(skill) ||
                skills.Any(existing => existing.Skill == skill)) return false;
            skills.Add(new GeneratedSkillInsight(skill, item.Priority, item.EvidenceCode, item.ActionCode));
        }
        var errors = new List<GeneratedErrorInsight>();
        foreach (var item in parsed.RecurringErrors)
        {
            if (!Enum.TryParse<ErrorCategory>(item.Category, true, out var category) ||
                !availableErrors.Contains(category) || item.Priority is < 1 or > 3 ||
                item.ActionCode != LocalProgressInsightGenerator.ErrorAction(category) ||
                errors.Any(existing => existing.Category == category)) return false;
            errors.Add(new GeneratedErrorInsight(category, item.Priority, item.ActionCode));
        }

        var allowedActions = skills.Select(skill => skill.ActionCode)
            .Concat(errors.Select(error => error.ActionCode)).Append("start_placement").ToHashSet();
        if (!allowedActions.Contains(parsed.NextActionCode)) return false;
        var route = parsed.NextActionCode == "start_placement" ? "/assessment" :
            LocalProgressInsightGenerator.RouteForAction(parsed.NextActionCode);
        result = new GeneratedProgressInsight(parsed.OverallCode, parsed.AchievementCodes.Distinct().ToList(),
            skills.OrderBy(skill => skill.Priority).ToList(), errors.OrderBy(error => error.Priority).ToList(),
            parsed.HabitCode, parsed.NextActionCode, route, ProgressInsightSource.Hermes, false);
        return true;
    }

    internal static string BuildPayload(ProgressSnapshotDto snapshot) => JsonSerializer.Serialize(new
    {
        snapshot.AsOfDate,
        OverallLevel = snapshot.OverallLevel?.ToString(),
        snapshot.HasLearningProfile,
        Skills = snapshot.Skills.Select(skill => new
        {
            Skill = skill.Skill.ToString(), skill.Score, skill.SampleCount,
            Confidence = skill.Confidence.ToString(), skill.IsPlacementBaseline, skill.EightWeekDelta,
            WeeklyScores = skill.Last8Weeks.Select(point => point.Score)
        }),
        ErrorsLast30Days = snapshot.ErrorsLast30Days.Select(error => new
        {
            Category = error.Category.ToString(),
            error.CountLast30Days,
            RecentExamples = error.RecentExamples.Take(3).Select(example => new
            {
                Skill = example.Skill.ToString(),
                example.Source,
                example.Prompt,
                example.LearnerAnswer,
                example.ExpectedAnswer,
                example.Explanation
            })
        }),
        snapshot.TopicProgress,
        StudyTime = new
        {
            snapshot.StudyTime.TodaySeconds, snapshot.StudyTime.WeekSeconds,
            snapshot.StudyTime.MonthSeconds, snapshot.StudyTime.YearSeconds,
            snapshot.StudyTime.TotalSeconds, snapshot.StudyTime.ActiveDays
        },
        snapshot.Vocabulary,
        Gamification = new
        {
            snapshot.Gamification.TodayCompletedTasks, snapshot.Gamification.DailyGoalTarget,
            snapshot.Gamification.IsGoalMet, snapshot.Gamification.CurrentStreak,
            snapshot.Gamification.LongestStreak, snapshot.Gamification.IsStreakAtRisk
        }
    });

    internal const string SystemPrompt = """
You analyze a bounded English-learning progress snapshot. Return ONE raw JSON object only; no markdown and no prose.
Never calculate, repeat, or invent numeric facts. Never output Uzbek or any user-facing sentence. Use only these codes:
overallCode: no_data|getting_started|strong_progress|steady_progress|needs_focus
achievementCodes: strong_skill|positive_trend|daily_goal_met|vocabulary_growth (max 3)
evidenceCode: placement_baseline|negative_trend|lowest_score
habitCode: no_study_data|start_habit|habit_at_risk|habit_consistent|habit_building
actionCode for a skill: practice_<lowercase skill>; for an error: drill_<snake_case category>; or start_placement.
Only select skills and error categories present in the input. Max 3 skills and 3 errors. Priority is integer 1..3.
Schema: {"overallCode":"...","achievementCodes":[],"skillsToStrengthen":[{"skill":"Speaking","priority":1,"evidenceCode":"lowest_score","actionCode":"practice_speaking"}],"recurringErrors":[{"category":"Articles","priority":1,"actionCode":"drill_articles"}],"habitCode":"...","nextActionCode":"..."}
""";

    private sealed record CacheEntry(GeneratedProgressInsight Value, DateTimeOffset ExpiresAt);
    private sealed record HermesResponse(
        string OverallCode, List<string> AchievementCodes, List<HermesSkill> SkillsToStrengthen,
        List<HermesError> RecurringErrors, string HabitCode, string NextActionCode);
    private sealed record HermesSkill(string Skill, int Priority, string EvidenceCode, string ActionCode);
    private sealed record HermesError(string Category, int Priority, string ActionCode);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
}
