using Application.Common;
using Application.Video.Dtos;
using Application.Video.Models;
using Application.Video.Ports;
using FluentValidation;
using MediatR;

namespace Application.Video.GenerateVideoQuiz;

public sealed record GenerateVideoQuizCommand(Guid VideoLessonId, Guid LearnerId) : IRequest<GeneratedVideoQuizDto>;

public sealed class GenerateVideoQuizCommandValidator : AbstractValidator<GenerateVideoQuizCommand>
{
    public GenerateVideoQuizCommandValidator()
    {
        RuleFor(x => x.VideoLessonId).NotEmpty();
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}

public sealed class GenerateVideoQuizCommandHandler(
    IVideoRepository videos, IVideoQuizGenerator generator, IVideoQuizStore store, TimeProvider clock)
    : IRequestHandler<GenerateVideoQuizCommand, GeneratedVideoQuizDto>
{
    public async Task<GeneratedVideoQuizDto> Handle(GenerateVideoQuizCommand request, CancellationToken cancellationToken)
    {
        var lesson = await videos.GetByIdAsync(request.VideoLessonId, cancellationToken)
            ?? throw new NotFoundException("Video lesson", request.VideoLessonId);
        if (lesson.Transcript.Count == 0)
            throw new VideoQuizUnavailableException("transcript_unavailable");
        var questions = await generator.GenerateAsync(lesson.Title, lesson.Level, lesson.Transcript, cancellationToken);
        if (questions.Count == 0)
            throw new VideoQuizUnavailableException("quiz_generation_unavailable");
        var quiz = new GeneratedVideoQuiz(Guid.NewGuid(), lesson.Id, request.LearnerId, lesson.Title,
            lesson.YouTubeVideoId, clock.GetUtcNow().AddHours(2), questions);
        await store.SaveAsync(quiz, cancellationToken);
        return GeneratedVideoQuizDto.FromSession(quiz);
    }
}
