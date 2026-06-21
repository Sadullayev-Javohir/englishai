using Application.Grammar.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Grammar.GetGrammarCatalog;

/// <summary>
/// Returns the grammar catalog - the 50 shared learning-spine topics for a CEFR level (every skill
/// teaches the same set). The view depends on the filters: <see cref="AllLevels"/> returns the whole
/// A1→C2 ladder easiest-first; an explicit <see cref="Level"/> browses just that band (so a learner
/// can browse the grammar of any level); otherwise it falls back to the learner's own level, or a
/// default level if the learner has no profile yet. Each topic's grammar lesson (its grammar focus
/// taught in the topic's own context) is generated lazily when the topic is opened.
/// </summary>
public sealed record GetGrammarCatalogQuery(
    Guid LearnerId,
    CefrLevel? Level = null,
    bool AllLevels = false) : IRequest<IReadOnlyList<GrammarTopicSummaryDto>>;
