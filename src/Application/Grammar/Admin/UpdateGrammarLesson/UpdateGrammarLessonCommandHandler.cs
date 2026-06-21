using Application.Common;
using Application.Grammar.Admin;
using Application.Grammar.Ports;
using Application.Identity.Dtos;
using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;
using MediatR;

namespace Application.Grammar.Admin.UpdateGrammarLesson;

public sealed class UpdateGrammarLessonCommandHandler
    : IRequestHandler<UpdateGrammarLessonCommand, GrammarLessonAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IGrammarRepository _lessons;

    public UpdateGrammarLessonCommandHandler(IAdminAuthorization admin, IGrammarRepository lessons)
    {
        _admin = admin;
        _lessons = lessons;
    }

    public async Task<GrammarLessonAdminDto> Handle(
        UpdateGrammarLessonCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var lesson = await _lessons.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(GrammarLesson), request.Id);

        var category = Enum.Parse<ErrorCategory>(request.Category, ignoreCase: true);
        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);

        lesson.AdminUpdate(request.Title, category, level);

        await _lessons.SaveAsync(lesson, cancellationToken);

        return GrammarLessonAdminDto.FromDomain(lesson);
    }
}
