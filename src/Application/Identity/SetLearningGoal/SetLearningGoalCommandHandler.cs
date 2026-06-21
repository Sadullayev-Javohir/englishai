using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Domain.Analytics;
using Domain.Identity;
using Domain.Learning;
using MediatR;

namespace Application.Identity.SetLearningGoal;

public sealed class SetLearningGoalCommandHandler
    : IRequestHandler<SetLearningGoalCommand, AuthenticatedUserDto>
{
    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IProductEventStore _events;
    private readonly TimeProvider _clock;

    public SetLearningGoalCommandHandler(
        IUserAccountStore accounts,
        ILearnerProfileRepository profiles,
        IProductEventStore events,
        TimeProvider clock)
    {
        _accounts = accounts;
        _profiles = profiles;
        _events = events;
        _clock = clock;
    }

    public async Task<AuthenticatedUserDto> Handle(
        SetLearningGoalCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);

        // The goal now lives on the learner profile, and is asked only after onboarding (the level-
        // choice flow creates the profile), so a profile must already exist. A caller who reaches
        // this before onboarding gets a clean 404 rather than a silently dropped goal.
        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LearnerProfile), account.Id);

        profile.SetLearningGoal(request.Goal);
        await _profiles.SaveAsync(profile, cancellationToken);

        // Activation funnel signal (goal-based onboarding). Idempotent per (learner, type) so a
        // learner who revisits the goal screen does not inflate the funnel; the source carries the
        // chosen goal for segment debugging (never free-form text - rule-safe).
        await _events.AppendOnceAsync(
            account.Id,
            ProductEventType.OnboardingGoalSelected,
            _clock.GetUtcNow(),
            source: request.Goal.ToString(),
            cancellationToken);

        return AuthenticatedUserDto.From(account, profile);
    }
}
