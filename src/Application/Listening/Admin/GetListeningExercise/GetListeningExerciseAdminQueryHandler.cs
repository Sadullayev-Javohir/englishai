using Application.Common;
using Application.Identity.Dtos;
using Application.Listening.Ports;
using Application.Vocabulary;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Listening;
using MediatR;

namespace Application.Listening.Admin.GetListeningExercise;

public sealed class GetListeningExerciseAdminQueryHandler
    : IRequestHandler<GetListeningExerciseAdminQuery, ListeningExerciseAdminDetailDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IListeningRepository _exercises;
    private readonly IListeningAudioCache _audio;
    private readonly IVocabularyTopicRepository _topics;

    public GetListeningExerciseAdminQueryHandler(
        IAdminAuthorization admin,
        IListeningRepository exercises,
        IListeningAudioCache audio,
        IVocabularyTopicRepository topics)
    {
        _admin = admin;
        _exercises = exercises;
        _audio = audio;
        _topics = topics;
    }

    public async Task<ListeningExerciseAdminDetailDto> Handle(
        GetListeningExerciseAdminQuery request,
        CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var exercise = await _exercises.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ListeningExercise), request.Id);
        var metadata = await _audio.GetMetadataAsync(exercise.Id, cancellationToken);
        IReadOnlyList<TargetWordDto> words = Array.Empty<TargetWordDto>();
        if (exercise.VocabularyTopicId is Guid topicId)
        {
            var topic = await _topics.GetByIdAsync(topicId, cancellationToken);
            if (topic is not null) words = TopicTargetWords.DetailedOf(topic);
        }

        return new ListeningExerciseAdminDetailDto(
            exercise.Id,
            exercise.Title,
            exercise.Topic,
            exercise.Level.ToString(),
            exercise.Status.ToString(),
            exercise.VocabularyTopicId,
            exercise.Transcript,
            new ListeningAudioAdminDto(
                $"/api/listening/exercise/{exercise.Id}/audio",
                metadata is not null,
                metadata?.SizeBytes,
                metadata?.CreatedAt,
                metadata?.ContentType ?? "audio/mpeg"),
            exercise.Segments
                .OrderBy(segment => segment.Order)
                .Select(segment => new ListeningSegmentAdminDto(
                    segment.Id,
                    segment.Order,
                    segment.StartMs,
                    segment.EndMs,
                    segment.Speaker,
                    segment.Text))
                .ToArray(),
            exercise.Questions.Select(question => new ListeningQuestionAdminDto(
                question.Id,
                question.Prompt,
                question.Options,
                question.CorrectOptionIndex,
                question.HintCode,
                question.Explanation)).ToArray(),
            words,
            exercise.CreatedAt);
    }
}
