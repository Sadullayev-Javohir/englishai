using Application.Learning.Ports;
using Domain.Learning;
using MediatR;

namespace Application.Learning.StartLearning;

public sealed class StartLearningCommandHandler : IRequestHandler<StartLearningCommand, Unit>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly TimeProvider _clock;

    public StartLearningCommandHandler(ILearnerProfileRepository profiles, TimeProvider clock)
    {
        _profiles = profiles;
        _clock = clock;
    }

    public async Task<Unit> Handle(StartLearningCommand request, CancellationToken cancellationToken)
    {
        // Never clobber an existing profile - the learner may already have a placement-seeded
        // one. Onboarding only needs to create a profile when none exists yet.
        var existing = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        if (existing is null)
        {
            var profile = LearnerProfile.CreateAtLevel(
                request.LearnerId, request.Level, _clock.GetUtcNow());
            await _profiles.SaveAsync(profile, cancellationToken);
        }

        return Unit.Value;
    }
}
