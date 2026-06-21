using Application.Common;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using Domain.Learning;
using MediatR;

namespace Application.Learning.RecordSkillActivity;

public sealed class RecordSkillActivityCommandHandler
    : IRequestHandler<RecordSkillActivityCommand, LearnerOverviewDto>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly TimeProvider _clock;

    public RecordSkillActivityCommandHandler(ILearnerProfileRepository profiles, TimeProvider clock)
    {
        _profiles = profiles;
        _clock = clock;
    }

    public async Task<LearnerOverviewDto> Handle(
        RecordSkillActivityCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LearnerProfile), request.LearnerId);

        var now = _clock.GetUtcNow();
        profile.RecordActivity(request.Skill, request.Score, now);

        if (request.Errors is not null)
        {
            foreach (var category in request.Errors)
                profile.RecordError(category, request.Skill, now);
        }

        await _profiles.SaveAsync(profile, cancellationToken);

        return LearnerOverviewDto.FromDomain(profile, now);
    }
}
