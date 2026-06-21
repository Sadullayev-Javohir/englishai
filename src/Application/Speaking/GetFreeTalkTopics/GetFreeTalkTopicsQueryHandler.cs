using Application.Speaking.Dtos;
using Domain.Speaking;
using MediatR;

namespace Application.Speaking.GetFreeTalkTopics;

public sealed class GetFreeTalkTopicsQueryHandler
    : IRequestHandler<GetFreeTalkTopicsQuery, IReadOnlyList<FreeTalkTopicDto>>
{
    public Task<IReadOnlyList<FreeTalkTopicDto>> Handle(
        GetFreeTalkTopicsQuery request, CancellationToken cancellationToken)
    {
        // A specific level narrows to its 20 topics; otherwise the whole A1→C2 catalog is returned
        // (the "all levels" browse view, or when no level filter was supplied).
        var topics = request.Level is { } level
            ? FreeTalkTopicCatalog.ForLevel(level)
            : FreeTalkTopicCatalog.All;

        IReadOnlyList<FreeTalkTopicDto> result = topics
            .Select(FreeTalkTopicDto.FromDomain)
            .ToList();

        return Task.FromResult(result);
    }
}
