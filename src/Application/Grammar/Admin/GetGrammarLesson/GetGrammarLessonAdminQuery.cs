using MediatR;

namespace Application.Grammar.Admin.GetGrammarLesson;

public sealed record GetGrammarLessonAdminQuery(Guid RequestingUserId, Guid Id)
    : IRequest<GrammarLessonAdminDetailDto>;
