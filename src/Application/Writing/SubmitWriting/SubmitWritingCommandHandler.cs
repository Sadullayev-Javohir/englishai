using Application.Ai;
using Application.Common;
using Application.Gamification;
using Application.Learning.Ports;
using Application.Subscription.Access;
using Application.Subscription.Entitlements;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Application.Writing.Dtos;
using Application.Writing.Ports;
using Domain.Learning;
using Domain.Subscription;
using Domain.Vocabulary;
using Domain.Writing;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Writing.SubmitWriting;

public sealed class SubmitWritingCommandHandler
    : IRequestHandler<SubmitWritingCommand, WritingAssessmentDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IWritingTaskRepository _tasks;
    private readonly ITopicWritingAssessor _assessor;
    private readonly IWritingContentProvider _content;
    private readonly IEntitlementService _entitlements;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ITopicCompletionStore _completions;
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly ITopicAccessPolicy _access;
    private readonly TimeProvider _clock;
    private readonly IAiFeatureScope _aiScope;

    public SubmitWritingCommandHandler(
        IVocabularyTopicRepository topics,
        IWritingTaskRepository tasks,
        ITopicWritingAssessor assessor,
        IWritingContentProvider content,
        IEntitlementService entitlements,
        ILearnerProfileRepository profiles,
        ITopicCompletionStore completions,
        IDailyProgressRecorder dailyProgress,
        ITopicAccessPolicy access,
        TimeProvider clock,
        IAiFeatureScope? aiScope = null)
    {
        _topics = topics;
        _tasks = tasks;
        _assessor = assessor;
        _content = content;
        _entitlements = entitlements;
        _profiles = profiles;
        _completions = completions;
        _dailyProgress = dailyProgress;
        _access = access;
        _clock = clock;
        _aiScope = aiScope ?? NoOpAiFeatureScope.Instance;
    }

    public async Task<WritingAssessmentDto> Handle(
        SubmitWritingCommand request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);
        await _access.EnsureLearningAccessAsync(
            request.LearnerId, topic.Id, SkillType.Writing, cancellationToken);

        var task = await _tasks.GetByTopicIdAsync(topic.Id, cancellationToken);
        if (task is null || !task.IsFilled)
            throw new NotFoundException("Writing task", request.TopicId);

        // Cap the submission length (docs/development-guide.md rule 10): the AI assessment is a paid LLM call whose
        // cost scales with input size, so a text far longer than this level's range is refused before
        // the call - and before consuming any quota - rather than billed. The hard cap sits a little
        // above the expected max so honest overflow is not punished.
        var hardCap = WritingWordRange.HardCap(task.MaxWords);
        if (WritingTask.CountWords(request.Text) > hardCap)
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.Text),
                    $"Submission exceeds the {hardCap}-word limit for this level."),
            });

        // Freemium gating (PROJECT-SPEC H.1): AI writing assessment is limited for Free
        // learners (3/month); Premium is unlimited. Checked before the (paid) LLM call.
        await _entitlements.EnsureAllowedAsync(
            request.LearnerId, PremiumFeature.WritingAssessment, cancellationToken);

        // Grade against the learner's actual CEFR level. A learner may browse a harder topic, but
        // clear production that meets their current level must still be capable of earning 5/5.
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        var assessmentLevel = profile?.OverallLevel ?? task.Level;
        using var aiScope = await _aiScope.EnterAsync(AiFeature.WritingAssessment, request.LearnerId, cancellationToken);
        var assessment = await _assessor.AssessAsync(
            task, request.Text, assessmentLevel, cancellationToken);

        // Charged here, before the result is persisted, because the quota pays for the AI CALL - and
        // that call has already happened and already cost money. Moving it after the commit would
        // make an assessment free whenever persistence fails, which is the more expensive mistake.
        await _entitlements.RecordUsageAsync(
            request.LearnerId, PremiumFeature.WritingAssessment, cancellationToken);

        // Resolve each issue's vetted Uzbek explanation (rule 11).
        var issues = assessment.Issues
            .Select(i => new WritingIssueDto(
                i.Dimension,
                i.StartOffset,
                i.EndOffset,
                i.Category,
                _content.GetIssueExplanation(i.IssueCode) ?? string.Empty))
            .ToList();

        var now = _clock.GetUtcNow();

        // Feed the Writing skill score (G.4) and the error heatmap (C.7). If the learner has
        // no profile yet we skip silently - assessment still returns to the caller.
        if (profile is not null)
        {
            profile.RecordActivity(SkillType.Writing, assessment.OverallPercent, now);

            // Grammar issues map to error-heatmap buckets (G.3 ↔ C.7).
            foreach (var issue in assessment.Issues.Where(i => i.Category is not null))
                profile.RecordError(issue.Category!.Value, SkillType.Writing, now);

            // Staged, not committed: the profile and the topic record must land together or not at
            // all, so a failure between them cannot leave half of this submission durable.
            await _profiles.TrackAsync(profile, cancellationToken);
        }

        // Credit the score toward this topic's Writing module (K.5). The store keeps the best
        // score per module, so re-attempting can only raise it. A profile is not required (a
        // topic's mastery checklist is tracked independently, like reading/grammar/vocabulary).
        var record = await _completions.GetAsync(request.LearnerId, topic.Id, cancellationToken)
                     ?? TopicCompletionRecord.Start(request.LearnerId, topic.Id, topic.Level, now);
        record.RecordModule(SkillType.Writing, assessment.OverallPercent, now);
        await _completions.TrackAsync(record, cancellationToken);

        // One commit for everything this submission changed.
        await _completions.CommitAsync(cancellationToken);

        // The learner's result is durable from here on, so a reward failure must not fail the request.
        if (assessment.OverallPercent >= TopicCompletionRecord.MasteryThreshold)
        {
            await LearningRewards.AwardBestEffortAsync(
                () => _dailyProgress.RecordSkillAsync(
                    request.LearnerId, SkillType.Writing, assessment.OverallPercent, now, cancellationToken),
                cancellationToken);
        }

        return new WritingAssessmentDto(
            topic.Id,
            assessment.TaskId,
            assessment.DimensionScores.Select(DimensionScoreDto.FromDomain).ToList(),
            issues,
            assessment.OverallBand,
            assessment.OverallPercent,
            assessmentLevel,
            assessment.Source,
            TopicCompletionDto.FromDomain(record));
    }
}
