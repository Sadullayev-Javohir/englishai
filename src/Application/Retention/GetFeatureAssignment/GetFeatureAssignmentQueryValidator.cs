using FluentValidation;

namespace Application.Retention.GetFeatureAssignment;

public sealed class GetFeatureAssignmentQueryValidator : AbstractValidator<GetFeatureAssignmentQuery>
{
    public GetFeatureAssignmentQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty();
    }
}
