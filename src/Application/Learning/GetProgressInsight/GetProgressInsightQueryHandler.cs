using Application.Analytics.Dtos;
using Application.Analytics.Ports;
using Application.Gamification;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using Application.Vocabulary.Ports;
using Domain.Analytics;
using Domain.Gamification;
using Domain.Learning;
using MediatR;

namespace Application.Learning.GetProgressInsight;

public sealed class GetProgressInsightQueryHandler : IRequestHandler<GetProgressInsightQuery, ProgressInsightDto>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly IStudyLogStore _studyLogs;
    private readonly IVocabularyStatsReader _vocabulary;
    private readonly ITopicCompletionStore _topics;
    private readonly IGamificationStore _gamification;
    private readonly IUserPreferencesStore _preferences;
    private readonly IProgressInsightGenerator _generator;
    private readonly TimeProvider _clock;

    public GetProgressInsightQueryHandler(
        ILearnerProfileRepository profiles,
        IStudyLogStore studyLogs,
        IVocabularyStatsReader vocabulary,
        ITopicCompletionStore topics,
        IGamificationStore gamification,
        IUserPreferencesStore preferences,
        IProgressInsightGenerator generator,
        TimeProvider clock)
    {
        _profiles = profiles;
        _studyLogs = studyLogs;
        _vocabulary = vocabulary;
        _topics = topics;
        _gamification = gamification;
        _preferences = preferences;
        _generator = generator;
        _clock = clock;
    }

    public async Task<ProgressInsightDto> Handle(GetProgressInsightQuery request, CancellationToken cancellationToken)
    {
        var generatedAt = _clock.GetUtcNow();
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        var studyStats = await _studyLogs.GetStatsAsync(request.LearnerId, request.Today, cancellationToken);
        var vocabulary = await _vocabulary.ReadAsync(request.LearnerId, generatedAt, cancellationToken);
        var topicRecords = await _topics.GetByLearnerAsync(request.LearnerId, cancellationToken);
        var goal = await DailyGoalPreference.ResolveAsync(_preferences, request.LearnerId, cancellationToken);
        var todayTasks = await _gamification.GetTaskCountAsync(request.LearnerId, request.Today, cancellationToken);
        var completedDays = await _gamification.GetCompletedDaysAsync(request.LearnerId, cancellationToken);
        var skillsToday = await _gamification.GetSkillsPracticedTodayAsync(
            request.LearnerId, request.Today, cancellationToken);

        var snapshot = new ProgressSnapshotDto(
            request.LearnerId,
            request.Today,
            profile?.OverallLevel,
            profile is null ? null : LevelStatusDto.FromDomain(profile.LevelStatus(generatedAt)),
            BuildSkills(profile, generatedAt),
            BuildErrors(profile, generatedAt),
            new TopicProgressSummaryDto(
                topicRecords.Count,
                topicRecords.Count(record => record.IsMastered),
                topicRecords.Sum(record => record.PassedModuleCount)),
            StudyStatsDto.FromDomain(studyStats),
            VocabularySummaryDto.FromDomain(vocabulary),
            GamificationStatusDto.From(
                todayTasks, goal, StreakCalculator.Compute(completedDays, request.Today), skillsToday),
            profile is not null);

        var generated = await _generator.GenerateAsync(snapshot, cancellationToken);
        var skillEvidence = snapshot.Skills.ToDictionary(skill => skill.Skill);
        var errorEvidence = snapshot.ErrorsLast30Days.ToDictionary(error => error.Category);
        var analysis = new ProgressInsightAnalysisDto(
            generated.OverallCode,
            generated.AchievementCodes,
            generated.SkillsToStrengthen.Select(item =>
            {
                var evidence = skillEvidence[item.Skill];
                return new ProgressSkillInsightDto(item.Skill, item.Priority, item.EvidenceCode, item.ActionCode,
                    evidence.Score, evidence.EightWeekDelta, evidence.SampleCount, evidence.Confidence);
            }).ToList(),
            generated.RecurringErrors.Select(item =>
                new ProgressErrorInsightDto(item.Category, item.Priority, item.ActionCode,
                    errorEvidence[item.Category].CountLast30Days)).ToList(),
            generated.HabitCode,
            generated.NextActionCode,
            generated.TargetRoute,
            generated.Source,
            generated.IsFallback);

        return new ProgressInsightDto(snapshot, analysis, generatedAt);
    }

    private static IReadOnlyList<ProgressSkillSnapshotDto> BuildSkills(
        LearnerProfile? profile, DateTimeOffset now)
    {
        if (profile is null)
            return Array.Empty<ProgressSkillSnapshotDto>();

        var growth = profile.GrowthHistory(now, 8).GroupBy(point => point.Skill)
            .ToDictionary(group => group.Key, group => group.OrderBy(point => point.WeekEnding).ToList());
        return profile.SkillScores(now).Select(score =>
        {
            var points = growth[score.Skill].Select(GrowthPointDto.FromDomain).ToList();
            return new ProgressSkillSnapshotDto(
                score.Skill, score.Score, score.SampleCount, Confidence(score.SampleCount),
                score.SampleCount == 0, Math.Round(points[^1].Score - points[0].Score, 1), points);
        }).ToList();
    }

    private static IReadOnlyList<ProgressErrorSnapshotDto> BuildErrors(
        LearnerProfile? profile, DateTimeOffset now)
    {
        if (profile is null)
            return Array.Empty<ProgressErrorSnapshotDto>();

        var cutoff = now.AddDays(-SkillScoreCalculator.WindowDays);
        return profile.ErrorHeatmap(now)
            .OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key)
            .Select(pair => new ProgressErrorSnapshotDto(
                pair.Key,
                pair.Value,
                profile.Errors
                    .Where(error => error.Category == pair.Key && error.OccurredAt >= cutoff)
                    .OrderByDescending(error => error.OccurredAt)
                    .Take(5)
                    .Select(error => new ProgressErrorDetailDto(
                        error.Id,
                        error.Category,
                        error.Skill,
                        error.OccurredAt,
                        error.Source,
                        error.SourceId,
                        error.Prompt,
                        error.LearnerAnswer,
                        error.ExpectedAnswer,
                        error.Explanation))
                    .ToList()))
            .ToList();
    }

    private static ProgressDataConfidence Confidence(int sampleCount) => sampleCount switch
    {
        0 => ProgressDataConfidence.Baseline,
        < 3 => ProgressDataConfidence.Low,
        < 8 => ProgressDataConfidence.Medium,
        _ => ProgressDataConfidence.High,
    };
}
