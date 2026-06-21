using Domain.Assessment;
using Domain.Speaking;

namespace Application.Speaking.Ports;

public interface ISpeakingCurriculumProvider
{
    Task<SpeakingCurriculumContext?> ForTopicAsync(Guid topicId, CancellationToken cancellationToken);
    Task<SpeakingCurriculumContext?> MatchAsync(CefrLevel level, string text, CancellationToken cancellationToken);
}
