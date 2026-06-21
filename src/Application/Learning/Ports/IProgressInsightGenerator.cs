using Application.Learning.Dtos;

namespace Application.Learning.Ports;

public interface IProgressInsightGenerator
{
    Task<GeneratedProgressInsight> GenerateAsync(ProgressSnapshotDto snapshot, CancellationToken cancellationToken);
}

public sealed record GeneratedProgressInsight(
    string OverallCode,
    IReadOnlyList<string> AchievementCodes,
    IReadOnlyList<GeneratedSkillInsight> SkillsToStrengthen,
    IReadOnlyList<GeneratedErrorInsight> RecurringErrors,
    string HabitCode,
    string NextActionCode,
    string TargetRoute,
    ProgressInsightSource Source,
    bool IsFallback);

public sealed record GeneratedSkillInsight(Domain.Learning.SkillType Skill, int Priority, string EvidenceCode, string ActionCode);
public sealed record GeneratedErrorInsight(Domain.Learning.ErrorCategory Category, int Priority, string ActionCode);
