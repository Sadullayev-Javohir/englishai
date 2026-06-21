namespace Domain.Vocabulary;

public sealed record VocabularyStats(
    int Total, int Learning, int Mastered, int Due, int AddedLast7Days, int AddedLast30Days,
    int TotalFailCount, IReadOnlyDictionary<ReviewStage, int> StageBreakdown);

public static class VocabularyStatsCalculator
{
    public static VocabularyStats Compute(IEnumerable<VocabularyItem> items, DateTimeOffset now)
    {
        var all = items.ToList();
        var stages = Enum.GetValues<ReviewStage>()
            .ToDictionary(stage => stage, stage => all.Count(item => item.Schedule.Stage == stage));
        return new VocabularyStats(
            all.Count,
            all.Count(item => item.Schedule.Stage != ReviewStage.Mastered),
            stages[ReviewStage.Mastered],
            all.Count(item => item.IsDue(now)),
            all.Count(item => item.CreatedAt >= now.AddDays(-7)),
            all.Count(item => item.CreatedAt >= now.AddDays(-30)),
            all.Sum(item => item.Schedule.FailCount),
            stages);
    }
}
