using Application.Vocabulary.Dtos;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.LearnWord;

/// <summary>
/// Records a newly learned word for a learner and starts its 3/7/21 review schedule
/// (PROJECT-SPEC B.1). Other modules (Speaking, Video, Reading) call this when the
/// learner saves a word.
/// </summary>
public sealed record LearnWordCommand(
    Guid LearnerId,
    string Word,
    string Translation,
    string? ExampleSentence = null,
    VocabularySource Source = VocabularySource.Manual) : IRequest<VocabularyItemDto>;
