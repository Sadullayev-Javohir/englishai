using Application.Common;
using Application.Grammar.Ports;
using Application.Identity.Dtos;
using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;
using MediatR;

namespace Application.Grammar.Admin.UpdateGrammarLessonContent;

public sealed class UpdateGrammarLessonContentCommandHandler : IRequestHandler<UpdateGrammarLessonContentCommand, GrammarLessonAdminDetailDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IGrammarRepository _lessons;

    public UpdateGrammarLessonContentCommandHandler(IAdminAuthorization admin, IGrammarRepository lessons)
    {
        _admin = admin;
        _lessons = lessons;
    }

    public async Task<GrammarLessonAdminDetailDto> Handle(UpdateGrammarLessonContentCommand request, CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");
        var lesson = await _lessons.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(GrammarLesson), request.Id);
        var payload = request.Payload;
        lesson.AdminReplace(
            payload.Title,
            Enum.Parse<ErrorCategory>(payload.Category, true),
            Enum.Parse<CefrLevel>(payload.Level, true),
            Enum.Parse<GrammarLessonStatus>(payload.Status, true),
            payload.VocabularyTopicId,
            payload.GrammarFocusCode,
            payload.ContextIntro,
            payload.Explanation,
            payload.Examples.Select(item => GrammarExample.Create(item.English, item.Uzbek)),
            payload.CommonMistakes.Select(item => GrammarCommonMistake.Create(item.Text)),
            payload.Exercises.Select(item => GrammarExercise.Create(
                Enum.Parse<GrammarExerciseType>(item.Type, true), item.Prompt, item.Options,
                item.CorrectOptionIndex, item.HintCode, item.Explanation)),
            payload.ApplicationTasks.Select(item => GrammarApplicationTask.Create(
                Enum.Parse<SkillType>(item.TargetSkill, true), item.Prompt)));
        lesson.ReplaceCuratedRuleContent(
            payload.CuratedTitleUz,
            payload.CuratedSummaryUz,
            payload.CuratedFormulas,
            payload.CuratedRules.Select(item => GrammarCuratedRule.Create(item.HeadingUz, item.BodyUz)));
        await _lessons.SaveAsync(lesson, cancellationToken);
        return GrammarLessonAdminDetailDto.FromDomain(lesson);
    }
}
