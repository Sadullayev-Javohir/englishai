using Application.Books.Models;
using Application.Books.Ports;
using Domain.Assessment;
using FluentAssertions;
using Infrastructure.Books;
using NSubstitute;
using Xunit;

namespace Infrastructure.Tests.Books;

public class FallbackBookContentGeneratorTests
{
    private readonly IBookContentGenerator _primary = Substitute.For<IBookContentGenerator>();
    private readonly IBookContentGenerator _fallback = Substitute.For<IBookContentGenerator>();

    [Fact]
    public async Task Uses_fallback_when_primary_throws()
    {
        var expected = GeneratedSection();
        _primary.GenerateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns<Task<GeneratedBookSection>>(_ => throw new TimeoutException());
        _fallback.GenerateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await CreateGenerator().GenerateAsync("Book", "Synopsis", "Section", 1, 3, CefrLevel.A2);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Uses_fallback_when_primary_returns_empty_content()
    {
        var expected = GeneratedSection();
        _primary.GenerateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedBookSection.Empty);
        _fallback.GenerateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await CreateGenerator().GenerateAsync("Book", "Synopsis", "Section", 1, 3, CefrLevel.A2);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Does_not_hide_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _primary.GenerateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CefrLevel>(), cancellation.Token)
            .Returns<Task<GeneratedBookSection>>(_ => throw new OperationCanceledException(cancellation.Token));

        var action = () => CreateGenerator().GenerateAsync(
            "Book", "Synopsis", "Section", 1, 3, CefrLevel.A2, cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        await _fallback.DidNotReceiveWithAnyArgs().GenerateAsync(
            default!, default!, default!, default, default, default, default);
    }

    private FallbackBookContentGenerator CreateGenerator() => new(_primary, _fallback);

    private static GeneratedBookSection GeneratedSection() =>
        new("Body", new List<GeneratedBookQuestion>
        {
            new("Question", new List<string> { "One", "Two" }, 0, "Because"),
        });
}
