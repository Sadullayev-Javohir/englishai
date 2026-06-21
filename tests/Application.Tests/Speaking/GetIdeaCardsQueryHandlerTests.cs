using Application.Speaking;
using Application.Speaking.GetIdeaCards;
using Application.Speaking.Ports;
using Domain.Assessment;
using Domain.Speaking;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public class GetIdeaCardsQueryHandlerTests
{
    private readonly IConversationStore _conversations = Substitute.For<IConversationStore>();
    private readonly IIdeaCardGenerator _generator = Substitute.For<IIdeaCardGenerator>();

    private GetIdeaCardsQueryHandler CreateHandler() => new(_conversations, _generator);

    private static ConversationSession SessionWithTurns()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "daily_routine");
        session.AddTutorTurn("What time do you wake up?");
        return session;
    }

    [Fact]
    public async Task Handle_returns_the_generator_cards_for_an_existing_session()
    {
        var session = SessionWithTurns();
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _generator.GenerateAsync(session, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new IdeaCard("Do you wake up early or late?", "I wake up at", "⏰"),
        });

        var result = await CreateHandler().Handle(new GetIdeaCardsQuery(session.Id), CancellationToken.None);

        result.Cards.Should().ContainSingle();
        result.Cards[0].Prompt.Should().Be("Do you wake up early or late?");
        result.Cards[0].Starter.Should().Be("I wake up at");
        result.Cards[0].Emoji.Should().Be("⏰");
    }

    [Fact]
    public async Task Handle_falls_back_to_defaults_when_the_session_is_missing()
    {
        _conversations.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ConversationSession?)null);

        var result = await CreateHandler().Handle(new GetIdeaCardsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Cards.Should().HaveCount(IdeaCardDefaults.Cards.Count);
        await _generator.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_falls_back_to_defaults_when_the_generator_returns_nothing()
    {
        var session = SessionWithTurns();
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _generator.GenerateAsync(session, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IdeaCard>());

        var result = await CreateHandler().Handle(new GetIdeaCardsQuery(session.Id), CancellationToken.None);

        result.Cards.Should().HaveCount(IdeaCardDefaults.Cards.Count);
    }

    [Fact]
    public async Task Handle_falls_back_to_defaults_when_the_generator_throws()
    {
        var session = SessionWithTurns();
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _generator.GenerateAsync(session, Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<IdeaCard>>(_ => throw new InvalidOperationException("LLM down"));

        var result = await CreateHandler().Handle(new GetIdeaCardsQuery(session.Id), CancellationToken.None);

        result.Cards.Should().HaveCount(IdeaCardDefaults.Cards.Count);
    }
}
