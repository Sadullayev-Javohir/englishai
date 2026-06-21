using Application.Learning.Dtos;
using Domain.Learning;
using MediatR;

namespace Application.Learning.RecordSkillActivity;

/// <summary>
/// Records one scored practice/assessment outcome for a learner's skill, plus any
/// errors observed during it (which feed the error heatmap). Returns the refreshed
/// learner overview. Other modules (Speaking, Reading, ...) call this as work happens.
/// </summary>
public sealed record RecordSkillActivityCommand(
    Guid LearnerId,
    SkillType Skill,
    int Score,
    IReadOnlyList<ErrorCategory>? Errors = null) : IRequest<LearnerOverviewDto>;
