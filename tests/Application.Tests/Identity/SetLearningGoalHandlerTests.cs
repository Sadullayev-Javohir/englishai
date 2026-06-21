using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.Ports;
using Application.Identity.SetLearningGoal;
using Application.Learning.Ports;
using Application.Tests.Learning;
using Domain.Analytics;
using Domain.Assessment;
using Domain.Common;
using Domain.Identity;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Identity;

public class SetLearningGoalHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IProductEventStore _events = Substitute.For<IProductEventStore>();

    private SetLearningGoalCommandHandler CreateHandler() =>
        new(_accounts, _profiles, _events, new FixedTimeProvider(Now));

    private UserAccount GivenAccount()
    {
        var account = UserAccount.Register("sub-1", "me@example.com", "Aziz", null, Now);
        account.SetUsername("aziz_k");
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        return account;
    }

    private LearnerProfile GivenProfile(Guid learnerId)
    {
        var profile = LearnerProfile.CreateAtLevel(learnerId, CefrLevel.A2, Now);
        _profiles.GetByLearnerIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns(profile);
        return profile;
    }

    [Fact]
    public async Task Sets_the_goal_on_the_profile_persists_and_records_the_funnel_event()
    {
        var account = GivenAccount();
        var profile = GivenProfile(account.Id);

        var result = await CreateHandler().Handle(
            new SetLearningGoalCommand(account.Id, LearningGoal.Work), CancellationToken.None);

        result.LearningGoal.Should().Be(LearningGoal.Work);
        result.HasOnboarded.Should().BeTrue();
        profile.LearningGoal.Should().Be(LearningGoal.Work);
        await _profiles.Received(1).SaveAsync(profile, Arg.Any<CancellationToken>());
        await _events.Received(1).AppendOnceAsync(
            account.Id,
            ProductEventType.OnboardingGoalSelected,
            Now,
            nameof(LearningGoal.Work),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_profile_throws_not_found_and_does_not_persist()
    {
        var account = GivenAccount();
        _profiles.GetByLearnerIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns((LearnerProfile?)null);

        var act = () => CreateHandler().Handle(
            new SetLearningGoalCommand(account.Id, LearningGoal.IeltsCefr), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _profiles.DidNotReceive().SaveAsync(Arg.Any<LearnerProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_account_throws_not_found_and_does_not_persist()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var act = () => CreateHandler().Handle(
            new SetLearningGoalCommand(Guid.NewGuid(), LearningGoal.Travel), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _profiles.DidNotReceive().SaveAsync(Arg.Any<LearnerProfile>(), Arg.Any<CancellationToken>());
    }
}
