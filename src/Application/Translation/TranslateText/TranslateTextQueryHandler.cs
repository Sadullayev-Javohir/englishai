using Application.Translation.Dtos;
using Application.Translation.Ports;
using Application.Ai;
using Application.Common;
using MediatR;

namespace Application.Translation.TranslateText;

/// <summary>
/// Resolves a sentence's Uzbek translation, cache-first: a cache hit returns immediately, otherwise
/// the translator is called once and the result is cached (rules 10, 11). A failed translation
/// returns a null translation (honest "unavailable") rather than fabricated text (rules 8, 11).
/// </summary>
public sealed class TranslateTextQueryHandler : IRequestHandler<TranslateTextQuery, TranslationDto>
{
    private readonly ITextTranslator _translator;
    private readonly ITranslationCache _cache;
    private readonly IAiFeatureScope _aiScope;
    private readonly ICurrentUserAccessor? _currentUser;

    public TranslateTextQueryHandler(
        ITextTranslator translator,
        ITranslationCache cache,
        IAiFeatureScope? aiScope = null,
        ICurrentUserAccessor? currentUser = null)
    {
        _translator = translator;
        _cache = cache;
        _aiScope = aiScope ?? NoOpAiFeatureScope.Instance;
        _currentUser = currentUser;
    }

    public async Task<TranslationDto> Handle(TranslateTextQuery request, CancellationToken cancellationToken)
    {
        var text = request.Text.Trim();
        var context = new TranslationContext(request.Speaker, request.Topic, request.PreviousTurns);
        var key = CacheKey(text, request.Level, context);

        if (_cache.TryGet(key, out var cached))
            return new TranslationDto(text, cached);

        using var aiScope = await _aiScope.EnterAsync(
            AiFeature.Translation, _currentUser?.LearnerId, cancellationToken);
        var translation = await _translator.TranslateAsync(text, request.Level, context, cancellationToken);

        if (!string.IsNullOrWhiteSpace(translation))
            _cache.Set(key, translation.Trim());

        return new TranslationDto(text, translation?.Trim());
    }

    // Level is part of the key so the same sentence can be translated differently per CEFR level.
    private static string CacheKey(
        string text, Domain.Assessment.CefrLevel? level, TranslationContext context) =>
        $"{(level.HasValue ? (int)level.Value : 0)}|{context.Speaker}|{context.Topic}|{string.Join("~", context.PreviousTurns ?? Array.Empty<string>())}|{text}";
}
