using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using MediatR;

namespace Application.Vocabulary.GetMandatoryReviewStatus;

public sealed class GetMandatoryReviewStatusQueryHandler
    : IRequestHandler<GetMandatoryReviewStatusQuery, MandatoryReviewStatusDto>
{
    private readonly IMandatoryReviewPolicy _policy;

    public GetMandatoryReviewStatusQueryHandler(IMandatoryReviewPolicy policy)
    {
        _policy = policy;
    }

    public Task<MandatoryReviewStatusDto> Handle(
        GetMandatoryReviewStatusQuery request,
        CancellationToken cancellationToken) =>
        _policy.GetStatusAsync(request.LearnerId, cancellationToken);
}
