using Application.Vocabulary.Admin;
using MediatR;

namespace Application.Vocabulary.Admin.GetAllVocabularyTopics;

/// <summary>
/// Returns every curated vocabulary topic for the admin catalog, ordered by CEFR level
/// (A1→C2) then within-level <see cref="Domain.Vocabulary.VocabularyTopic.Sequence"/>.
/// <paramref name="RequestingUserId"/> is the viewer (from the session); the handler authorizes it
/// as an admin and rejects everyone else with a 403.
/// </summary>
public sealed record GetAllVocabularyTopicsQuery(Guid RequestingUserId)
    : IRequest<IReadOnlyList<VocabularyTopicAdminDto>>;
