using Application.Common;
using Application.Competition.Ports;
using Domain.Competition;
using FluentAssertions;
using Infrastructure.Competition;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Web.Hubs;

namespace Integration.Tests;

public sealed class CompetitionHubTests
{
    [Fact]
    public async Task Resume_restores_group_for_authenticated_participant()
    {
        var learnerId = Guid.NewGuid();
        var competition = Competition.Create(
            learnerId,
            "Host",
            "Scale test",
            new CompetitionSettings(),
            [Guid.NewGuid()]);
        var repository = new InMemoryCompetitionRepository();
        await repository.AddAsync(competition, CancellationToken.None);

        var groups = Substitute.For<IGroupManager>();
        var hub = Hub(learnerId, repository, groups, "connection-a");

        var snapshot = await hub.ResumeCompetition(competition.Id);

        snapshot.Id.Should().Be(competition.Id);
        await groups.Received(1).AddToGroupAsync(
            "connection-a",
            $"{CompetitionHub.GroupPrefix}:{competition.Id}",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resume_rejects_authenticated_non_participant()
    {
        var competition = Competition.Create(
            Guid.NewGuid(),
            "Host",
            "Private competition",
            new CompetitionSettings(),
            [Guid.NewGuid()]);
        var repository = new InMemoryCompetitionRepository();
        await repository.AddAsync(competition, CancellationToken.None);

        var groups = Substitute.For<IGroupManager>();
        var hub = Hub(Guid.NewGuid(), repository, groups, "connection-b");

        var act = () => hub.ResumeCompetition(competition.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
        await groups.DidNotReceive().AddToGroupAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static CompetitionHub Hub(
        Guid learnerId,
        ICompetitionRepository repository,
        IGroupManager groups,
        string connectionId)
    {
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.LearnerId.Returns(learnerId);
        return new CompetitionHub(
            Substitute.For<ISender>(),
            Substitute.For<ILogger<CompetitionHub>>(),
            currentUser,
            repository)
        {
            Context = Context(connectionId),
            Groups = groups,
            Clients = Substitute.For<IHubCallerClients>(),
        };
    }

    private static HubCallerContext Context(string connectionId)
    {
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns(connectionId);
        context.ConnectionAborted.Returns(CancellationToken.None);
        return context;
    }
}
