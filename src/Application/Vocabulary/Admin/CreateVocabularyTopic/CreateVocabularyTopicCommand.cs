using Application.Vocabulary.Admin;
using Domain.Assessment;
using MediatR;

namespace Application.Vocabulary.Admin.CreateVocabularyTopic;

/// <summary>
/// Curates a new vocabulary topic (metadata only; the passage/words are lazily filled later).
/// <paramref name="RequestingUserId"/> is the acting admin (from the session); the handler
/// authorizes it as an admin and rejects everyone else with a 403.
/// <paramref name="Slug"/> is a stable key (e.g. "a1-my-family") matching <c>^[a-z0-9-]+$</c>.
/// <paramref name="Level"/> is the CEFR band name (e.g. "B1") parsed to <see cref="CefrLevel"/>.
/// </summary>
public sealed record CreateVocabularyTopicCommand(
    Guid RequestingUserId,
    string Slug,
    string Title,
    string TitleUz,
    string Category,
    string GrammarFocusCode,
    string Level,
    int Sequence = 0)
    : IRequest<VocabularyTopicAdminDto>;
