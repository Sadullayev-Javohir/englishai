using Application.Common;
using Application.Grammar.Admin;
using Application.Grammar.Ports;
using Application.Identity.Dtos;
using Domain.Grammar;
using MediatR;

namespace Application.Grammar.Admin.DeleteGrammarLesson;

public sealed class DeleteGrammarLessonCommandHandler
    : IRequestHandler<DeleteGrammarLessonCommand, Unit>
{
    private readonly IAdminAuthorization _admin;
    private readonly IGrammarRepository _lessons;

    public DeleteGrammarLessonCommandHandler(IAdminAuthorization admin, IGrammarRepository lessons)
    {
        _admin = admin;
        _lessons = lessons;
    }

    public async Task<Unit> Handle(
        DeleteGrammarLessonCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var lesson = await _lessons.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(GrammarLesson), request.Id);

        await _lessons.DeleteAsync(request.Id, cancellationToken);

        return Unit.Value;
    }
}
