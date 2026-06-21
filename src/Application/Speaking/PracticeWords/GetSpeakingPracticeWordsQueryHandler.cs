using Application.Speaking.Common;
using Application.Speaking.Ports;
using MediatR;

namespace Application.Speaking.PracticeWords;

public sealed class GetSpeakingPracticeWordsQueryHandler
    : IRequestHandler<GetSpeakingPracticeWordsQuery, IReadOnlyList<SpeakingPracticeWordDto>>
{
    private readonly ISpeakingPracticeWordRepository _repository;
    private readonly IPhonemeVisualLibrary _phonemeLibrary;

    public GetSpeakingPracticeWordsQueryHandler(
        ISpeakingPracticeWordRepository repository,
        IPhonemeVisualLibrary phonemeLibrary)
    {
        _repository = repository;
        _phonemeLibrary = phonemeLibrary;
    }

    public async Task<IReadOnlyList<SpeakingPracticeWordDto>> Handle(
        GetSpeakingPracticeWordsQuery request,
        CancellationToken cancellationToken) =>
        (await _repository.GetActiveAsync(request.LearnerId, cancellationToken))
            .Where(word => PronunciationDetailResolver.IsSupported(_phonemeLibrary, word.Word))
            .Select(SpeakingPracticeWordDto.FromDomain)
            .ToList();
}
