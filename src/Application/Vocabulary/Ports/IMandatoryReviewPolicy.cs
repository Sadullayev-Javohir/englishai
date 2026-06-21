using Application.Vocabulary.Dtos;

namespace Application.Vocabulary.Ports;

public interface IMandatoryReviewPolicy
{
    Task<MandatoryReviewStatusDto> GetStatusAsync(
        Guid learnerId,
        CancellationToken cancellationToken);

    Task EnsureAccessAsync(Guid learnerId, CancellationToken cancellationToken);
}
