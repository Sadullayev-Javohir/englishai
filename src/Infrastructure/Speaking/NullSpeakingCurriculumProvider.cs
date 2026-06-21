using Application.Speaking.Ports;
using Domain.Assessment;
using Domain.Speaking;

namespace Infrastructure.Speaking;

public sealed class NullSpeakingCurriculumProvider : ISpeakingCurriculumProvider
{
    public Task<SpeakingCurriculumContext?> ForTopicAsync(Guid topicId, CancellationToken cancellationToken) =>
        Task.FromResult<SpeakingCurriculumContext?>(null);

    public Task<SpeakingCurriculumContext?> MatchAsync(CefrLevel level, string text, CancellationToken cancellationToken) =>
        Task.FromResult<SpeakingCurriculumContext?>(null);
}
