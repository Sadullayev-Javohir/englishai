using MediatR;

namespace Application.Speaking.PracticeWords;

public sealed record GetSpeakingPracticeWordsQuery(Guid LearnerId)
    : IRequest<IReadOnlyList<SpeakingPracticeWordDto>>;
