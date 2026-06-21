using Application.Common;
using Application.Video.Dtos;
using Application.Video.Models;
using Application.Video.Ports;
using MediatR;

namespace Application.Video.GenerateVideoQuiz;

public sealed record GetGeneratedVideoQuizQuery(Guid QuizId, Guid LearnerId) : IRequest<GeneratedVideoQuizDto>;

public sealed class GetGeneratedVideoQuizQueryHandler(IVideoQuizStore store, TimeProvider clock)
    : IRequestHandler<GetGeneratedVideoQuizQuery, GeneratedVideoQuizDto>
{
    public async Task<GeneratedVideoQuizDto> Handle(GetGeneratedVideoQuizQuery request, CancellationToken cancellationToken)
    {
        var quiz = await store.GetAsync(request.QuizId, cancellationToken)
            ?? throw new VideoQuizUnavailableException("quiz_expired");
        if (quiz.LearnerId != request.LearnerId) throw new ForbiddenException("Quiz belongs to another learner.");
        if (quiz.ExpiresAt <= clock.GetUtcNow()) throw new VideoQuizUnavailableException("quiz_expired");
        return GeneratedVideoQuizDto.FromSession(quiz);
    }
}
