using Application.Vocabulary.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Vocabulary.GetVocabularyTopics;

/// <summary>
/// Lists the curated vocabulary topics for a CEFR level (PROJECT-SPEC module 4 - 100 topics per
/// level). <paramref name="AllLevels"/> returns the whole A1→C2 ladder easiest-first (each level's
/// topics gated independently); an explicit <paramref name="Level"/> browses that band; otherwise
/// the learner's current level is used.
/// </summary>
public sealed record GetVocabularyTopicsQuery(
    Guid LearnerId,
    CefrLevel? Level = null,
    bool AllLevels = false) : IRequest<IReadOnlyList<VocabularyTopicSummaryDto>>;
