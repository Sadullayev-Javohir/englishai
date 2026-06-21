using Application.Grammar.Admin;
using MediatR;

namespace Application.Grammar.Admin.GetAllGrammarLessons;

/// <summary>
/// Returns every grammar lesson for the admin catalog, ordered by CEFR level
/// (A1→C2) then by creation date (newest first). <paramref name="RequestingUserId"/> is the viewer
/// (from the session); the handler authorizes it as an admin and rejects everyone else with a 403.
/// </summary>
public sealed record GetAllGrammarLessonsQuery(Guid RequestingUserId)
    : IRequest<IReadOnlyList<GrammarLessonAdminDto>>;
