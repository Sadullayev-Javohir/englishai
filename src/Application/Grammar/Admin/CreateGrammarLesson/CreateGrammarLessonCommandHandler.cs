using Application.Common;
using Application.Grammar.Admin;
using Application.Grammar.Ports;
using Application.Identity.Dtos;
using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;
using MediatR;

namespace Application.Grammar.Admin.CreateGrammarLesson;

public sealed class CreateGrammarLessonCommandHandler
    : IRequestHandler<CreateGrammarLessonCommand, GrammarLessonAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IGrammarRepository _lessons;
    private readonly TimeProvider _clock;

    public CreateGrammarLessonCommandHandler(
        IAdminAuthorization admin, IGrammarRepository lessons, TimeProvider clock)
    {
        _admin = admin;
        _lessons = lessons;
        _clock = clock;
    }

    public async Task<GrammarLessonAdminDto> Handle(
        CreateGrammarLessonCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var category = Enum.Parse<ErrorCategory>(request.Category, ignoreCase: true);
        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);
        var now = _clock.GetUtcNow();

        var lesson = GrammarLesson.CreateManual(request.Title, category, level, now);

        await _lessons.SaveAsync(lesson, cancellationToken);

        return GrammarLessonAdminDto.FromDomain(lesson);
    }
}
