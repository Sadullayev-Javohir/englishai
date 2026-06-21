using Application.Speaking.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Speaking.GetFreeTalkTopics;

/// <summary>
/// Lists the curated free-talk conversation topics for the "Erkin suhbat" picker. The catalog is
/// static curated content (20 topics per CEFR level, 120 total).
/// <para>
/// When <paramref name="Level"/> is set only that level's 20 topics are returned; otherwise
/// <paramref name="AllLevels"/> decides between the whole A1→C2 catalog (true) and - when both are
/// unset - the whole catalog as well. The picker passes the learner's chosen level filter.
/// </para>
/// </summary>
public sealed record GetFreeTalkTopicsQuery(CefrLevel? Level = null, bool AllLevels = false)
    : IRequest<IReadOnlyList<FreeTalkTopicDto>>;
