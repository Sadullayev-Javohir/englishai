using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.GetDueReviews;

/// <summary>Words a learner is due to review now, each with its mini-test type.</summary>
public sealed record GetDueReviewsQuery(Guid LearnerId) : IRequest<IReadOnlyList<DueReviewDto>>;
