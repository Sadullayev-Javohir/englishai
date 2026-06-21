using Application.Translation.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Translation.TranslateText;

/// <summary>
/// Translates one piece of displayed English teaching text into Uzbek on demand - used when a learner
/// clicks or hovers a sentence to see its meaning. The result is cached so a given sentence is only
/// ever paid for once (rules 10, 11). <see cref="Level"/> is optional and tunes the wording to the
/// learner's CEFR level when known.
/// </summary>
public sealed record TranslateTextQuery(
    string Text,
    CefrLevel? Level = null,
    string? Speaker = null,
    string? Topic = null,
    IReadOnlyList<string>? PreviousTurns = null) : IRequest<TranslationDto>;
