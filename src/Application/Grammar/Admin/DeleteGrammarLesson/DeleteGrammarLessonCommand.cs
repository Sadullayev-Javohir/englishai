using MediatR;

namespace Application.Grammar.Admin.DeleteGrammarLesson;

/// <summary>
/// Deletes a grammar lesson from the catalog by id. <paramref name="RequestingUserId"/> is the acting
/// admin (from the session); the handler authorizes it as an admin and rejects everyone else with a 403.
/// </summary>
public sealed record DeleteGrammarLessonCommand(Guid RequestingUserId, Guid Id) : IRequest<Unit>;
