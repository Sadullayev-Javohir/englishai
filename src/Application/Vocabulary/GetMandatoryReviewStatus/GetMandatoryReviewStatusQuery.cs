using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.GetMandatoryReviewStatus;

public sealed record GetMandatoryReviewStatusQuery(Guid LearnerId)
    : IRequest<MandatoryReviewStatusDto>;
