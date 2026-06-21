using Application.Vocabulary.Admin;
using Domain.Assessment;
using MediatR;

namespace Application.Vocabulary.Admin.UpdateVocabularyTopic;

/// <summary>
/// Updates a curated vocabulary topic's metadata (and CEFR level). <paramref name="RequestingUserId"/>
/// is the acting admin (from the session); the handler authorizes it as an admin and rejects everyone
/// else with a 403. <paramref name="Level"/> is the CEFR band name (e.g. "B1") parsed to
/// <see cref="CefrLevel"/> by the validator.
/// </summary>
public sealed record UpdateVocabularyTopicCommand(
    Guid RequestingUserId,
    Guid Id,
    string Title,
    string TitleUz,
    string Category,
    string GrammarFocusCode,
    int Sequence,
    string Level,
    string Passage,
    IReadOnlyList<UpdateVocabularyTopicWord> Words)
    : IRequest<VocabularyTopicAdminDto>;

public sealed record UpdateVocabularyTopicWord(
    string Word,
    string Translation,
    string? ExampleSentence,
    string PartOfSpeech,
    string? LexicalCategory,
    string? Register,
    string? UsageNote,
    string? ImageUrl,
    string? ImageSource,
    string? ImageAttribution);
