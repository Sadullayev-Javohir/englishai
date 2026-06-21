using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using MediatR;

namespace Application.Vocabulary.GetVocabulary;

public sealed class GetVocabularyQueryHandler
    : IRequestHandler<GetVocabularyQuery, IReadOnlyList<VocabularyItemDto>>
{
    private readonly IVocabularyRepository _vocabulary;

    public GetVocabularyQueryHandler(IVocabularyRepository vocabulary)
    {
        _vocabulary = vocabulary;
    }

    public async Task<IReadOnlyList<VocabularyItemDto>> Handle(
        GetVocabularyQuery request, CancellationToken cancellationToken)
    {
        var items = await _vocabulary.GetByLearnerIdAsync(request.LearnerId, cancellationToken);

        return items.Select(VocabularyItemDto.FromDomain).ToList();
    }
}
