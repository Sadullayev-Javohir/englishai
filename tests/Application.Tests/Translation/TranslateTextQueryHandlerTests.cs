using Application.Translation.Ports;
using Application.Translation.TranslateText;
using Application.Ai;
using Application.Common;
using Domain.Assessment;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Translation;

/// <summary>
/// Verifies the on-demand sentence translator: it serves cached translations without calling the LLM,
/// calls and caches on a miss, and returns a null translation (honest "unavailable") rather than
/// fabricating text when the translator cannot translate (rules 8, 10, 11).
/// </summary>
public class TranslateTextQueryHandlerTests
{
    private readonly ITextTranslator _translator = Substitute.For<ITextTranslator>();
    private readonly ITranslationCache _cache = Substitute.For<ITranslationCache>();

    private TranslateTextQueryHandler Handler() => new(_translator, _cache);

    [Fact]
    public async Task Returns_cached_translation_without_calling_the_translator()
    {
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<string>())
            .Returns(call => { call[1] = "Muzey haqida gap."; return true; });

        var result = await Handler().Handle(
            new TranslateTextQuery("A sentence about the museum.", CefrLevel.A2), CancellationToken.None);

        result.Translation.Should().Be("Muzey haqida gap.");
        await _translator.DidNotReceiveWithAnyArgs().TranslateAsync(default!, default, default, default);
    }

    [Fact]
    public async Task On_a_miss_translates_and_caches_the_result()
    {
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<string>()).Returns(false);
        _translator.TranslateAsync("Hello world.", CefrLevel.B1, Arg.Any<TranslationContext>(), Arg.Any<CancellationToken>())
            .Returns("Salom dunyo.");

        var result = await Handler().Handle(
            new TranslateTextQuery("Hello world.", CefrLevel.B1), CancellationToken.None);

        result.Translation.Should().Be("Salom dunyo.");
        _cache.Received(1).Set(Arg.Any<string>(), "Salom dunyo.");
    }

    [Fact]
    public async Task Returns_null_translation_and_does_not_cache_when_unavailable()
    {
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<string>()).Returns(false);
        _translator.TranslateAsync(Arg.Any<string>(), Arg.Any<CefrLevel?>(), Arg.Any<TranslationContext>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await Handler().Handle(
            new TranslateTextQuery("Untranslatable."), CancellationToken.None);

        result.Translation.Should().BeNull();
        _cache.DidNotReceiveWithAnyArgs().Set(default!, default!);
    }

    [Fact]
    public async Task Passes_speaking_context_to_the_translator()
    {
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<string>()).Returns(false);
        _translator.TranslateAsync(
                "That is a good point.", CefrLevel.B1,
                Arg.Is<TranslationContext>(context => context.Speaker == "tutor" && context.Topic == "Travel"),
                Arg.Any<CancellationToken>())
            .Returns("Bu yaxshi fikr.");

        var result = await Handler().Handle(new TranslateTextQuery(
            "That is a good point.", CefrLevel.B1, "tutor", "Travel", new[] { "learner: I like trains." }),
            CancellationToken.None);

        result.Translation.Should().Be("Bu yaxshi fikr.");
    }

    [Fact]
    public async Task Enters_the_translation_ai_feature_scope_on_a_cache_miss()
    {
        var scope = Substitute.For<IAiFeatureScope>();
        var learnerId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.LearnerId.Returns(learnerId);
        scope.EnterAsync(AiFeature.Translation, learnerId, Arg.Any<CancellationToken>())
            .Returns(Substitute.For<IDisposable>());
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<string>()).Returns(false);
        _translator.TranslateAsync(Arg.Any<string>(), Arg.Any<CefrLevel?>(), Arg.Any<TranslationContext>(), Arg.Any<CancellationToken>())
            .Returns("Tarjima");

        await new TranslateTextQueryHandler(_translator, _cache, scope, currentUser).Handle(
            new TranslateTextQuery("Translation"), CancellationToken.None);

        await scope.Received(1).EnterAsync(AiFeature.Translation, learnerId, Arg.Any<CancellationToken>());
    }
}
