using Application.Common;
using Application.Grammar.Ports;
using Application.Identity.Dtos;
using Domain.Grammar;
using MediatR;

namespace Application.Grammar.Admin.GetGrammarLesson;

public sealed class GetGrammarLessonAdminQueryHandler : IRequestHandler<GetGrammarLessonAdminQuery, GrammarLessonAdminDetailDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IGrammarRepository _lessons;

    public GetGrammarLessonAdminQueryHandler(
        IAdminAuthorization admin,
        IGrammarRepository lessons)
    {
        _admin = admin;
        _lessons = lessons;
    }

    public async Task<GrammarLessonAdminDetailDto> Handle(GetGrammarLessonAdminQuery request, CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");
        var lesson = await _lessons.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(GrammarLesson), request.Id);
        return GrammarLessonAdminDetailDto.FromDomain(lesson);
    }
}
