using Application.Grammar.Admin;
using Domain.Learning;
using MediatR;

namespace Application.Grammar.Admin.CreateGrammarLesson;

/// <summary>
/// Curates a new grammar lesson (metadata only; the five steps are filled later).
/// <paramref name="RequestingUserId"/> is the acting admin (from the session); the handler
/// authorizes it as an admin and rejects everyone else with a 403.
/// <paramref name="Title"/> maps to the lesson topic; <paramref name="Category"/> is the
/// <see cref="ErrorCategory"/> name and <paramref name="Level"/> the CEFR band name (e.g. "B1").
/// </summary>
public sealed record CreateGrammarLessonCommand(
    Guid RequestingUserId,
    string Title,
    string Category,
    string Level)
    : IRequest<GrammarLessonAdminDto>;
