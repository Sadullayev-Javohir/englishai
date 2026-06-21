using Application.Ai;
using Application.Assistant.Ports;
using Application.Video.Ports;
using FluentAssertions;
using Infrastructure.Assistant;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Assistant;

public sealed class ContextualAssistantCoordinatorTests
{
    private static readonly ContextualAssistantWork Work = new(
        "cache-key", "grammar", "Present Perfect", "Subject + have/has + V3", "I have finished.",
        "Bu qoidani tushuntiring", Array.Empty<ContextualAssistantTurn>());

    [Fact]
    public async Task Returns_local_fallback_when_Hermes_times_out()
    {
        var assistant = Substitute.For<IContextualAssistant>();
        assistant.AnswerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<IReadOnlyList<ContextualAssistantTurn>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, call.ArgAt<CancellationToken>(6));
                return null;
            });
        var admission = Substitute.For<IAiAdmissionControl>();
        var coordinator = Create(assistant, admission, new ContextualAssistantOptions { RequestTimeoutSeconds = 10 });

        var result = await coordinator.ExecuteAsync(Work, "learner", CancellationToken.None);

        result.Should().Contain("Darsdagi qoida");
        admission.Received(1).RecordFallback(AiFeature.Assistant, AiSubscriptionTier.Free);
    }

    [Fact]
    public async Task Returns_local_fallback_without_caching_it_when_Hermes_reply_is_empty()
    {
        var assistant = Substitute.For<IContextualAssistant>();
        assistant.AnswerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<IReadOnlyList<ContextualAssistantTurn>>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var cache = Substitute.For<IVideoExplainCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var coordinator = Create(assistant, Substitute.For<IAiAdmissionControl>(), new ContextualAssistantOptions(), cache);

        var result = await coordinator.ExecuteAsync(Work, "learner", CancellationToken.None);

        result.Should().NotBeNullOrWhiteSpace();
        await cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Preserves_caller_cancellation_instead_of_returning_fallback()
    {
        var assistant = Substitute.For<IContextualAssistant>();
        assistant.AnswerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<IReadOnlyList<ContextualAssistantTurn>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, call.ArgAt<CancellationToken>(6));
                return null;
            });
        var coordinator = Create(assistant, Substitute.For<IAiAdmissionControl>(), new ContextualAssistantOptions());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => coordinator.ExecuteAsync(Work, "learner", cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private static ContextualAssistantCoordinator Create(
        IContextualAssistant assistant,
        IAiAdmissionControl admission,
        ContextualAssistantOptions options,
        IVideoExplainCache? cache = null)
    {
        var contexts = Substitute.For<IAiRequestContextResolver>();
        contexts.ResolveAsync(AiFeature.Assistant, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiRequestContext("learner", AiSubscriptionTier.Free, AiFeature.Assistant));
        cache ??= Substitute.For<IVideoExplainCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        return new ContextualAssistantCoordinator(
            assistant,
            new LocalContextualAssistant(),
            admission,
            contexts,
            cache,
            options,
            NullLogger<ContextualAssistantCoordinator>.Instance);
    }
}
