using Application.Grammar.Admin;
using Domain.Learning;
using MediatR;

namespace Application.Grammar.Admin.UpdateGrammarLesson;

/// <summary>
/// Updates a grammar lesson's catalog metadata (topic, error category and CEFR level).
/// <paramref name="RequestingUserId"/> is the acting admin (from the session); the handler
/// authorizes it as an admin and rejects everyone else with a 403.
/// <paramref name="Title"/> maps to the lesson topic; <paramref name="Category"/> is the
/// <see cref="ErrorCategory"/> name and <paramref name="Level"/> the CEFR band name.
/// </summary>
public sealed record UpdateGrammarLessonCommand(
    Guid RequestingUserId,
    Guid Id,
    string Title,
    string Category,
    string Level)
    : IRequest<GrammarLessonAdminDto>;
