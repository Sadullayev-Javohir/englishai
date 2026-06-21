using Application.Learning.Dtos;
using Application.Learning.Ports;
using Domain.Learning;

namespace Infrastructure.Learning;

public sealed class LocalProgressInsightGenerator : IProgressInsightGenerator
{
    public Task<GeneratedProgressInsight> GenerateAsync(
        ProgressSnapshotDto snapshot, CancellationToken cancellationToken) =>
        Task.FromResult(Generate(snapshot));

    public static GeneratedProgressInsight Generate(ProgressSnapshotDto snapshot)
    {
        if (!snapshot.HasLearningProfile || snapshot.Skills.Count == 0)
            return Result("no_data", [], [], [], "no_study_data", "start_placement", "/assessment");

        var reliable = snapshot.Skills.Where(skill => skill.SampleCount > 0).ToList();
        var weakest = (reliable.Count > 0 ? reliable : snapshot.Skills.ToList())
            .OrderBy(skill => skill.Score).ThenBy(skill => skill.Skill).Take(3).ToList();
        var skills = weakest.Select((skill, index) => new GeneratedSkillInsight(
            skill.Skill,
            index + 1,
            skill.SampleCount == 0 ? "placement_baseline" :
                skill.EightWeekDelta < -2 ? "negative_trend" : "lowest_score",
            SkillAction(skill.Skill))).ToList();
        var errors = snapshot.ErrorsLast30Days.OrderByDescending(error => error.CountLast30Days)
            .ThenBy(error => error.Category).Take(3)
            .Select((error, index) => new GeneratedErrorInsight(
                error.Category, index + 1, ErrorAction(error.Category))).ToList();

        var achievements = new List<string>();
        if (snapshot.Skills.Any(skill => skill.SampleCount > 0 && skill.Score >= 80)) achievements.Add("strong_skill");
        if (snapshot.Skills.Any(skill => skill.SampleCount > 0 && skill.EightWeekDelta >= 3)) achievements.Add("positive_trend");
        if (snapshot.Gamification.IsGoalMet) achievements.Add("daily_goal_met");
        if (snapshot.Vocabulary.AddedLast7Days > 0) achievements.Add("vocabulary_growth");

        var overall = reliable.Count == 0 ? "getting_started" :
            reliable.Average(skill => skill.Score) >= 80 ? "strong_progress" :
            reliable.Average(skill => skill.Score) >= 60 ? "steady_progress" : "needs_focus";
        var habit = snapshot.StudyTime.TotalSeconds == 0 ? "start_habit" :
            snapshot.Gamification.IsStreakAtRisk ? "habit_at_risk" :
            snapshot.Gamification.CurrentStreak >= 7 ? "habit_consistent" : "habit_building";
        var nextAction = errors.Count > 0 && errors[0].Priority == 1
            ? errors[0].ActionCode
            : skills[0].ActionCode;

        return Result(overall, achievements.Take(3).ToList(), skills, errors, habit, nextAction,
            RouteForAction(nextAction));
    }

    internal static string SkillAction(SkillType skill) => $"practice_{skill.ToString().ToLowerInvariant()}";
    internal static string ErrorAction(ErrorCategory category) => $"drill_{ToSnakeCase(category.ToString())}";

    internal static string RouteForAction(string code)
    {
        foreach (var skill in Enum.GetValues<SkillType>())
            if (code == SkillAction(skill)) return $"/{skill.ToString().ToLowerInvariant()}";
        return code.StartsWith("drill_pronunciation", StringComparison.Ordinal) ? "/speaking" :
            code.StartsWith("drill_vocabulary", StringComparison.Ordinal) ||
            code.StartsWith("drill_spelling", StringComparison.Ordinal) ? "/vocabulary" : "/grammar";
    }

    private static GeneratedProgressInsight Result(
        string overall, IReadOnlyList<string> achievements, IReadOnlyList<GeneratedSkillInsight> skills,
        IReadOnlyList<GeneratedErrorInsight> errors, string habit, string action, string route) =>
        new(overall, achievements, skills, errors, habit, action, route, ProgressInsightSource.Local, true);

    private static string ToSnakeCase(string value) => string.Concat(value.Select((character, index) =>
        char.IsUpper(character) && index > 0 ? $"_{char.ToLowerInvariant(character)}" :
            char.ToLowerInvariant(character).ToString()));
}
