using Application.Assistant.Contextual;
using Application.Assistant.Dtos;
using Application.Assistant.Ports;
using Application.Common;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Assistant;

public sealed class ContextualAssistantHandlerTests
{
    [Fact]
    public async Task Sends_bounded_context_through_shared_coordinator()
    {
        var coordinator = Substitute.For<IContextualAssistantCoordinator>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.LearnerId.Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        ContextualAssistantWork? captured = null;
        coordinator.ExecuteAsync(
                Arg.Do<ContextualAssistantWork>(x => captured = x),
                "11111111-1111-1111-1111-111111111111",
                Arg.Any<CancellationToken>())
            .Returns("Tuzatilgan javob");

        var result = await new AskContextualAssistantQueryHandler(coordinator, currentUser).Handle(
            new AskContextualAssistantQuery(
                "writing", "My holiday", "Prompt and guidance", "I goed home", "Tekshiring",
                new[] { new ContextualAssistantTurnDto("user", "Oldingi savol") }),
            CancellationToken.None);

        result.ReplyUz.Should().Be("Tuzatilgan javob");
        captured.Should().NotBeNull();
        captured!.Area.Should().Be("writing");
        captured.FocusText.Should().Be("I goed home");
        captured.History.Should().ContainSingle();
    }
}
