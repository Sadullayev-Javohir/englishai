using Application.Assistant.Ports;
using Application.Assistant.Sessions;
using Application.Common;
using Domain.Assistant;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Assistant;

public sealed class AssistantSessionServiceTests
{
    private static IAssistantKnowledgeRetriever Knowledge()
    {
        var knowledge = Substitute.For<IAssistantKnowledgeRetriever>();
        knowledge.RetrieveAsync(Arg.Any<AssistantKnowledgeRequest>(), Arg.Any<CancellationToken>())
            .Returns(AssistantKnowledgeResult.Empty);
        return knowledge;
    }

    [Fact]
    public async Task List_enforces_max_page_size_and_returns_cursor()
    {
        var learnerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var repository = Substitute.For<IAssistantSessionRepository>();
        repository.ListActiveAsync(learnerId, Arg.Any<DateTimeOffset>(), null, null, 51, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, 51)
                .Select(index => AssistantSession.Create(learnerId, "general", "page", null, $"Chat {index}", now.AddMinutes(-index)))
                .ToArray());
        var user = Substitute.For<ICurrentUserAccessor>();
        user.LearnerId.Returns(learnerId);
        var service = new AssistantSessionService(repository, Substitute.For<IContextualAssistantCoordinator>(), Knowledge(), user, TimeProvider.System);

        var page = await service.ListAsync(null, 500, CancellationToken.None);

        page.Items.Should().HaveCount(50);
        page.NextCursor.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Create_and_list_are_learner_scoped()
    {
        var learnerId = Guid.NewGuid();
        var repository = Substitute.For<IAssistantSessionRepository>();
        var coordinator = Substitute.For<IContextualAssistantCoordinator>();
        var user = Substitute.For<ICurrentUserAccessor>();
        user.LearnerId.Returns(learnerId);
        AssistantSession? added = null;
        repository.AddAsync(Arg.Do<AssistantSession>(x => added = x), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repository.ListActiveAsync(learnerId, Arg.Any<DateTimeOffset>(), null, null, 21, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<AssistantSession>>(added is null ? Array.Empty<AssistantSession>() : new[] { added }));
        var service = new AssistantSessionService(repository, coordinator, Knowledge(), user, TimeProvider.System);

        var created = await service.CreateAsync(new("grammar", "topic", "t1", "Lesson"), CancellationToken.None);
        var listed = await service.ListAsync(null, 20, CancellationToken.None);

        created.Skill.Should().Be("grammar");
        listed.Items.Should().ContainSingle(x => x.Id == created.Id);
    }

    [Fact]
    public async Task Send_reuses_completed_client_request()
    {
        var learnerId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var session = AssistantSession.Create(learnerId, "grammar", "topic", "t1", "Lesson", now);
        session.AddMessage("user", "Explain", "completed", "local", requestId, null, now);
        session.AddMessage("assistant", "Answer", "completed", "cache", requestId, 10, now);
        var repository = Substitute.For<IAssistantSessionRepository>();
        repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var coordinator = Substitute.For<IContextualAssistantCoordinator>();
        var user = Substitute.For<ICurrentUserAccessor>();
        user.LearnerId.Returns(learnerId);
        var service = new AssistantSessionService(repository, coordinator, Knowledge(), user, TimeProvider.System);

        var result = await service.SendAsync(session.Id, new("Explain", "Lesson context", "", requestId), CancellationToken.None);

        result.AssistantMessage.Text.Should().Be("Answer");
        await coordinator.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default);
    }

    [Fact]
    public async Task Send_passes_page_context_to_coordinator()
    {
        var learnerId = Guid.NewGuid();
        var session = AssistantSession.Create(learnerId, "vocabulary", "topic", "banking", "Banking", DateTimeOffset.UtcNow);
        var repository = Substitute.For<IAssistantSessionRepository>();
        repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var coordinator = Substitute.For<IContextualAssistantCoordinator>();
        ContextualAssistantWork? captured = null;
        coordinator.ExecuteAsync(Arg.Do<ContextualAssistantWork>(work => captured = work), learnerId.ToString(), Arg.Any<CancellationToken>())
            .Returns("Bank means bank in this lesson.");
        var user = Substitute.For<ICurrentUserAccessor>();
        user.LearnerId.Returns(learnerId);
        var service = new AssistantSessionService(repository, coordinator, Knowledge(), user, TimeProvider.System);

        await service.SendAsync(session.Id, new("What does bank mean here?", "Passage about opening a bank account.", "bank", Guid.NewGuid()), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Context.Should().Contain("opening a bank account");
        captured.FocusText.Should().Be("bank");
    }

    [Fact]
    public async Task Send_appends_retrieved_knowledge_and_persists_sources()
    {
        var learnerId = Guid.NewGuid();
        var session = AssistantSession.Create(learnerId, "vocabulary", "page", null, "EnglishAI", DateTimeOffset.UtcNow);
        var repository = Substitute.For<IAssistantSessionRepository>();
        repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var coordinator = Substitute.For<IContextualAssistantCoordinator>();
        ContextualAssistantWork? captured = null;
        coordinator.ExecuteAsync(Arg.Do<ContextualAssistantWork>(work => captured = work), learnerId.ToString(), Arg.Any<CancellationToken>())
            .Returns("Mother is a noun.");
        var knowledge = Substitute.For<IAssistantKnowledgeRetriever>();
        knowledge.RetrieveAsync(Arg.Any<AssistantKnowledgeRequest>(), Arg.Any<CancellationToken>()).Returns(
            new AssistantKnowledgeResult("Words:\n- mother | ona | part of speech: Noun", [
                new AssistantKnowledgeSource("vocabulary", "topic", "family", "Family", "/app/vocabulary/topic/family", "mother", 100),
            ]));
        var user = Substitute.For<ICurrentUserAccessor>();
        user.LearnerId.Returns(learnerId);
        var service = new AssistantSessionService(repository, coordinator, knowledge, user, TimeProvider.System);

        var result = await service.SendAsync(session.Id, new("mother", "", "", Guid.NewGuid()), CancellationToken.None);

        captured!.Context.Should().Contain("mother | ona");
        result.AssistantMessage.Sources.Should().ContainSingle(source => source.Title == "Family");
    }

    [Fact]
    public async Task Send_bounds_large_page_context_before_calling_ai()
    {
        var learnerId = Guid.NewGuid();
        var session = AssistantSession.Create(learnerId, "speaking", "topic", "travel", "Travel", DateTimeOffset.UtcNow);
        var repository = Substitute.For<IAssistantSessionRepository>();
        repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var coordinator = Substitute.For<IContextualAssistantCoordinator>();
        ContextualAssistantWork? captured = null;
        coordinator.ExecuteAsync(Arg.Do<ContextualAssistantWork>(work => captured = work), learnerId.ToString(), Arg.Any<CancellationToken>())
            .Returns("Where would you like to travel?");
        var user = Substitute.For<ICurrentUserAccessor>();
        user.LearnerId.Returns(learnerId);
        var service = new AssistantSessionService(repository, coordinator, Knowledge(), user, TimeProvider.System);

        await service.SendAsync(session.Id, new("Help me answer", new string('c', 10000), new string('f', 3000), Guid.NewGuid()), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Context.Should().HaveLength(4000);
        captured.FocusText.Should().HaveLength(2000);
    }

    [Fact]
    public async Task Send_does_not_persist_generic_fallback_when_provider_is_unavailable()
    {
        var learnerId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var session = AssistantSession.Create(learnerId, "general", "page", null, "EnglishAI", DateTimeOffset.UtcNow);
        var repository = Substitute.For<IAssistantSessionRepository>();
        repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var coordinator = Substitute.For<IContextualAssistantCoordinator>();
        coordinator.ExecuteAsync(Arg.Any<ContextualAssistantWork>(), learnerId.ToString(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var user = Substitute.For<ICurrentUserAccessor>();
        user.LearnerId.Returns(learnerId);
        var service = new AssistantSessionService(repository, coordinator, Knowledge(), user, TimeProvider.System);

        await FluentActions.Awaiting(() => service.SendAsync(session.Id, new("Who is this?", "", "", requestId), CancellationToken.None))
            .Should().ThrowAsync<AssistantUnavailableException>();

        session.Messages.Should().ContainSingle(message => message.Role == "user");
        session.Messages.Should().NotContain(message => message.Role == "assistant");
    }
}
