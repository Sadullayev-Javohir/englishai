using Application.Common;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.ResetTopicModuleScore;

public sealed class ResetTopicModuleScoreCommandHandler
    : IRequestHandler<ResetTopicModuleScoreCommand, TopicCompletionDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly ITopicCompletionStore _completions;
    private readonly TimeProvider _clock;

    public ResetTopicModuleScoreCommandHandler(
        IVocabularyTopicRepository topics,
        ITopicCompletionStore completions,
        TimeProvider clock)
    {
        _topics = topics;
        _completions = completions;
        _clock = clock;
    }

    public async Task<TopicCompletionDto> Handle(
        ResetTopicModuleScoreCommand request,
        CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
            ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);
        var now = _clock.GetUtcNow();
        var record = await _completions.GetAsync(request.LearnerId, request.TopicId, cancellationToken)
            ?? TopicCompletionRecord.Start(request.LearnerId, request.TopicId, topic.Level, now);

        record.ResetModule(request.Module, now);
        await _completions.SaveAsync(record, cancellationToken);
        return TopicCompletionDto.FromDomain(record);
    }
}
