using MediatR;

namespace Application.Grammar.Admin.UpdateGrammarLessonContent;

public sealed record UpdateGrammarLessonContentCommand(
    Guid RequestingUserId, Guid Id, GrammarLessonAdminFullUpdateDto Payload)
    : IRequest<GrammarLessonAdminDetailDto>;
