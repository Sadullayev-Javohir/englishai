using Application.Common;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.GetTopicCompletion;

public sealed class GetTopicCompletionQueryHandler
    : IRequestHandler<GetTopicCompletionQuery, TopicCompletionDto>
{
    private readonly ITopicCompletionStore _completions;
    private readonly IVocabularyTopicRepository _topics;
    private readonly IComplimentaryAccess _complimentary;

    public GetTopicCompletionQueryHandler(
        ITopicCompletionStore completions,
        IVocabularyTopicRepository topics,
        IComplimentaryAccess complimentary)
    {
        _completions = completions;
        _topics = topics;
        _complimentary = complimentary;
    }

    public async Task<TopicCompletionDto> Handle(
        GetTopicCompletionQuery request, CancellationToken cancellationToken)
    {
        var fullAccess = await _complimentary.HasFullAccessAsync(request.LearnerId, cancellationToken);

        var record = await _completions.GetAsync(request.LearnerId, request.TopicId, cancellationToken);
        if (record is not null)
        {
            var dto = TopicCompletionDto.FromDomain(record);
            return fullAccess ? dto.AllModulesUnlocked() : dto;
        }

        // Not started yet: report a zeroed checklist so the UI renders all six modules.
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
            ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        var empty = TopicCompletionRecord.Start(
            request.LearnerId, request.TopicId, topic.Level, DateTimeOffset.UnixEpoch);
        var emptyDto = TopicCompletionDto.FromDomain(empty);
        return fullAccess ? emptyDto.AllModulesUnlocked() : emptyDto;
    }
}
