using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.GetVocabulary;

/// <summary>All vocabulary a learner is studying, with each word's current SRS state.</summary>
public sealed record GetVocabularyQuery(Guid LearnerId) : IRequest<IReadOnlyList<VocabularyItemDto>>;
