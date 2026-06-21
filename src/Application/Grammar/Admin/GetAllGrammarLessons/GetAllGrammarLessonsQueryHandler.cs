using Application.Common;
using Application.Grammar.Admin;
using Application.Grammar.Ports;
using Application.Identity.Dtos;
using Domain.Grammar;
using MediatR;

namespace Application.Grammar.Admin.GetAllGrammarLessons;

public sealed class GetAllGrammarLessonsQueryHandler
    : IRequestHandler<GetAllGrammarLessonsQuery, IReadOnlyList<GrammarLessonAdminDto>>
{
    private readonly IAdminAuthorization _admin;
    private readonly IGrammarRepository _lessons;

    public GetAllGrammarLessonsQueryHandler(IAdminAuthorization admin, IGrammarRepository lessons)
    {
        _admin = admin;
        _lessons = lessons;
    }

    public async Task<IReadOnlyList<GrammarLessonAdminDto>> Handle(
        GetAllGrammarLessonsQuery request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var all = await _lessons.GetAllAsync(cancellationToken);

        return all
            .OrderBy(l => l.Level)
            .ThenByDescending(l => l.CreatedAt)
            .Select(GrammarLessonAdminDto.FromDomain)
            .ToList();
    }
}
