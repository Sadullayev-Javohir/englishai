using MediatR;

namespace Application.Vocabulary.Admin.DeleteVocabularyTopic;

/// <summary>
/// Deletes a curated vocabulary topic from the catalog by id. <paramref name="RequestingUserId"/> is
/// the acting admin (from the session); the handler authorizes it as an admin and rejects everyone
/// else with a 403.
/// </summary>
public sealed record DeleteVocabularyTopicCommand(Guid RequestingUserId, Guid Id) : IRequest<Unit>;
