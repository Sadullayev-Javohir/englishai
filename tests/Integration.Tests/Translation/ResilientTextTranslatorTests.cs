using Application.Translation.Ports;
using Domain.Assessment;
using FluentAssertions;
using Infrastructure.Translation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Translation;

public class ResilientTextTranslatorTests
{
    private readonly ITextTranslator _inner = Substitute.For<ITextTranslator>();

    [Fact]
    public async Task Retries_empty_ai_responses_until_a_translation_is_returned()
    {
        _inner.TranslateAsync(
                "Tell me about your family.",
                CefrLevel.A2,
                Arg.Any<TranslationContext>(),
                Arg.Any<CancellationToken>())
            .Returns((string?)null, "", "Oilangiz haqida gapirib bering.");
        var translator = Create();

        var result = await translator.TranslateAsync(
            "Tell me about your family.",
            CefrLevel.A2,
            new TranslationContext("tutor", "Family"));

        result.Should().Be("Oilangiz haqida gapirib bering.");
        await _inner.Received(3).TranslateAsync(
            "Tell me about your family.",
            CefrLevel.A2,
            Arg.Any<TranslationContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_null_after_the_retry_budget_is_exhausted()
    {
        _inner.TranslateAsync(
                Arg.Any<string>(),
                Arg.Any<CefrLevel?>(),
                Arg.Any<TranslationContext>(),
                Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var translator = Create();

        var result = await translator.TranslateAsync("Unavailable.");

        result.Should().BeNull();
        await _inner.Received(3).TranslateAsync(
            "Unavailable.",
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stops_retrying_when_the_request_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        _inner.TranslateAsync(
                Arg.Any<string>(),
                Arg.Any<CefrLevel?>(),
                Arg.Any<TranslationContext>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return (string?)null;
            });
        var translator = Create();

        var act = () => translator.TranslateAsync(
            "Cancel this translation.",
            cancellationToken: cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await _inner.Received(1).TranslateAsync(
            "Cancel this translation.",
            null,
            null,
            cancellation.Token);
    }

    private ResilientTextTranslator Create() => new(
        _inner,
        new ImmediateTimeProvider(),
        NullLogger<ResilientTextTranslator>.Instance);

    private sealed class ImmediateTimeProvider : TimeProvider
    {
        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            callback(state);
            return new NoOpTimer();
        }
    }

    private sealed class NoOpTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
