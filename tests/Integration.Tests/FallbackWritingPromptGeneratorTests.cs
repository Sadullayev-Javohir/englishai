using Application.Writing.Models;
using Application.Writing.Ports;
using Domain.Assessment;
using FluentAssertions;
using Infrastructure.Writing;
using NSubstitute;
using Xunit;

namespace Integration.Tests;

public class FallbackWritingPromptGeneratorTests
{
    [Fact]
    public async Task Uses_local_generator_when_primary_returns_no_content()
    {
        var primary = Substitute.For<IWritingPromptGenerator>();
        primary.GenerateAsync(
                "Travel", CefrLevel.B1, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedWritingPrompt.Empty);
        var generator = new FallbackWritingPromptGenerator(primary, new LocalWritingPromptGenerator());

        var result = await generator.GenerateAsync("Travel", CefrLevel.B1, new[] { "journey" });

        result.HasContent.Should().BeTrue();
        result.Prompt.Should().Contain("Travel");
        result.Guidance.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Keeps_primary_content_when_generation_succeeds()
    {
        var expected = new GeneratedWritingPrompt("Primary prompt", new[] { "Primary guidance" });
        var primary = Substitute.For<IWritingPromptGenerator>();
        primary.GenerateAsync(
                "Travel", CefrLevel.B1, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        var fallback = Substitute.For<IWritingPromptGenerator>();
        var generator = new FallbackWritingPromptGenerator(primary, fallback);

        var result = await generator.GenerateAsync("Travel", CefrLevel.B1);

        result.Should().Be(expected);
        await fallback.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Uses_local_generator_when_primary_throws()
    {
        var primary = Substitute.For<IWritingPromptGenerator>();
        primary.GenerateAsync(
                "Travel", CefrLevel.B1, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns<Task<GeneratedWritingPrompt>>(_ => throw new HttpRequestException("gateway unavailable"));
        var generator = new FallbackWritingPromptGenerator(primary, new LocalWritingPromptGenerator());

        var result = await generator.GenerateAsync("Travel", CefrLevel.B1, new[] { "journey" });

        result.HasContent.Should().BeTrue();
        result.Prompt.Should().Contain("Travel");
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var primary = Substitute.For<IWritingPromptGenerator>();
        primary.GenerateAsync(
                "Travel", CefrLevel.B1, Arg.Any<IReadOnlyList<string>?>(), cancellation.Token)
            .Returns<Task<GeneratedWritingPrompt>>(_ => throw new OperationCanceledException(cancellation.Token));
        var fallback = Substitute.For<IWritingPromptGenerator>();
        var generator = new FallbackWritingPromptGenerator(primary, fallback);

        var act = () => generator.GenerateAsync("Travel", CefrLevel.B1, cancellationToken: cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await fallback.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>());
    }
}
