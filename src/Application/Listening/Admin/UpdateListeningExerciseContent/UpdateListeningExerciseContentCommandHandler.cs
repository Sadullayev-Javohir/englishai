using Application.Common;
using Application.Identity.Dtos;
using Application.Listening.Admin.GetListeningExercise;
using Application.Listening.Ports;
using Domain.Assessment;
using Domain.Listening;
using MediatR;

namespace Application.Listening.Admin.UpdateListeningExerciseContent;

public sealed class UpdateListeningExerciseContentCommandHandler
    : IRequestHandler<UpdateListeningExerciseContentCommand, ListeningExerciseAdminDetailDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IListeningRepository _exercises;
    private readonly IMediator _mediator;

    public UpdateListeningExerciseContentCommandHandler(
        IAdminAuthorization admin,
        IListeningRepository exercises,
        IMediator mediator)
    {
        _admin = admin;
        _exercises = exercises;
        _mediator = mediator;
    }

    public async Task<ListeningExerciseAdminDetailDto> Handle(
        UpdateListeningExerciseContentCommand request,
        CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var exercise = await _exercises.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ListeningExercise), request.Id);
        var payload = request.Payload;
        var segments = payload.Segments.Select(item => ListeningSegment.Create(
            item.Order,
            item.StartMs,
            item.EndMs,
            item.Speaker,
            item.Text)).ToArray();
        var questions = payload.Questions.Select(item => ListeningQuestion.Create(
            item.Prompt,
            item.Options,
            item.CorrectOptionIndex,
            item.HintCode,
            item.Explanation)).ToArray();

        exercise.AdminReplace(
            payload.Title,
            payload.Topic,
            Enum.Parse<CefrLevel>(payload.Level, true),
            Enum.Parse<ListeningExerciseStatus>(payload.Status, true),
            payload.VocabularyTopicId,
            payload.Transcript,
            segments,
            questions);
        await _exercises.SaveAsync(exercise, cancellationToken);

        return await _mediator.Send(
            new GetListeningExerciseAdminQuery(request.RequestingUserId, request.Id),
            cancellationToken);
    }
}
