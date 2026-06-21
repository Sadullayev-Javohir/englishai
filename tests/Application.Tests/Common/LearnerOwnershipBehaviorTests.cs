using Application.Common;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.Common;

public class LearnerOwnershipBehaviorTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    private sealed record LearnerScoped(Guid LearnerId) : IRequest<string>;
    private sealed record NotScoped(string Topic) : IRequest<string>;
    private sealed record ExemptScoped(Guid LearnerId) : IRequest<string>, IOwnershipExempt;

    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();

    private async Task<string> Run<TRequest>(TRequest request) where TRequest : notnull
    {
        var behavior = new LearnerOwnershipBehavior<TRequest, string>(_currentUser);
        return await behavior.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);
    }

    [Fact]
    public async Task Passes_when_learner_matches_authenticated_user()
    {
        _currentUser.LearnerId.Returns(Owner);

        (await Run(new LearnerScoped(Owner))).Should().Be("ok");
    }

    [Fact]
    public async Task Forbids_when_learner_differs_from_authenticated_user()
    {
        _currentUser.LearnerId.Returns(Owner);

        var act = () => Run(new LearnerScoped(Guid.NewGuid()));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Skips_enforcement_when_no_authenticated_user()
    {
        _currentUser.LearnerId.Returns((Guid?)null);

        // A different learner id is allowed through because nobody is authenticated
        // (background jobs / anonymous test host).
        (await Run(new LearnerScoped(Guid.NewGuid()))).Should().Be("ok");
    }

    [Fact]
    public async Task Ignores_requests_without_a_learner_id()
    {
        _currentUser.LearnerId.Returns(Owner);

        (await Run(new NotScoped("greetings"))).Should().Be("ok");
    }

    [Fact]
    public async Task Allows_empty_learner_id_as_server_filled()
    {
        _currentUser.LearnerId.Returns(Owner);

        (await Run(new LearnerScoped(Guid.Empty))).Should().Be("ok");
    }

    [Fact]
    public async Task Allows_a_different_learner_for_ownership_exempt_requests()
    {
        _currentUser.LearnerId.Returns(Owner);

        // Intentionally cross-learner reads (e.g. public leaderboard progress) opt out via
        // IOwnershipExempt, so a populated, mismatched id is allowed through.
        (await Run(new ExemptScoped(Guid.NewGuid()))).Should().Be("ok");
    }
}
