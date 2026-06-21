using Application.Translation.Ports;
using Domain.Assessment;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Translation;

public sealed class ResilientTextTranslator : ITextTranslator
{
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromMilliseconds(400),
        TimeSpan.FromSeconds(1),
    };

    private readonly ITextTranslator _inner;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ResilientTextTranslator> _logger;

    public ResilientTextTranslator(
        ITextTranslator inner,
        TimeProvider timeProvider,
        ILogger<ResilientTextTranslator> logger)
    {
        _inner = inner;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<string?> TranslateAsync(
        string englishText,
        CefrLevel? level = null,
        TranslationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var translation = await _inner.TranslateAsync(englishText, level, context, cancellationToken);
            if (!string.IsNullOrWhiteSpace(translation))
                return translation.Trim();

            if (attempt == RetryDelays.Length)
                break;

            var delay = RetryDelays[attempt];
            _logger.LogInformation(
                "AI translation returned no usable response on attempt {Attempt}/{MaxAttempts}; retrying in {DelayMs}ms.",
                attempt + 1,
                RetryDelays.Length + 1,
                delay.TotalMilliseconds);
            await Task.Delay(delay, _timeProvider, cancellationToken);
        }

        _logger.LogWarning(
            "AI translation returned no usable response after {Attempts} attempts.",
            RetryDelays.Length + 1);
        return null;
    }
}
